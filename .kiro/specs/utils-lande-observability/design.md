# Design Document: utils-LAndE Observability Platform

## Overview

This document describes the technical design for the utils-LAndE observability and error handling platform — a family of .NET 8 NuGet packages that standardize structured logging, correlation propagation, global exception handling, error cataloging, sensitive-field masking, and audit-hook extensibility for the ePACS ERP system.

The platform is organized as a layered set of packages with strict dependency direction: Abstractions → ErrorHandling → Core → AspNetCore → Propagation, with optional integration shims for Traceability and Messaging sibling utilities.

## Architecture

### Package Dependency Graph

```
Intellect.Erp.Observability.Abstractions  (zero deps, only MEL.Abstractions)
    ↑
Intellect.Erp.ErrorHandling               (depends on Abstractions)
    ↑
Intellect.Erp.Observability.Core          (depends on Abstractions, ErrorHandling, Serilog, System.Text.Json)
    ↑
Intellect.Erp.Observability.AspNetCore    (depends on Core, ErrorHandling, Microsoft.AspNetCore.App)
    ↑
Intellect.Erp.Observability.Propagation   (depends on Core)
    
Intellect.Erp.Observability.AuditHooks           (depends on Abstractions, Core)
Intellect.Erp.Observability.Log4NetBridge         (depends on Abstractions, log4net)
Intellect.Erp.Observability.Integrations.Traceability  (depends on Abstractions, Traceability)
Intellect.Erp.Observability.Integrations.Messaging     (depends on Propagation, Messaging.Contracts)
Intellect.Erp.Observability.Testing               (depends on Abstractions, Core)
```

### Solution Layout

```
utils-LAndE/
├── Directory.Build.props
├── Directory.Packages.props
├── Intellect.Erp.Observability.sln
├── NuGet.Config
├── src/
│   ├── Intellect.Erp.Observability.Abstractions/
│   ├── Intellect.Erp.ErrorHandling/
│   ├── Intellect.Erp.Observability.Core/
│   ├── Intellect.Erp.Observability.AspNetCore/
│   ├── Intellect.Erp.Observability.Propagation/
│   ├── Intellect.Erp.Observability.AuditHooks/
│   ├── Intellect.Erp.Observability.Log4NetBridge/
│   ├── Intellect.Erp.Observability.Integrations.Traceability/
│   ├── Intellect.Erp.Observability.Integrations.Messaging/
│   └── Intellect.Erp.Observability.Testing/
├── tests/
│   ├── Intellect.Erp.Observability.UnitTests/
│   ├── Intellect.Erp.Observability.IntegrationTests/
│   └── Intellect.Erp.ErrorHandling.UnitTests/
├── samples/
│   └── SampleHost/
└── docs/
```

### Middleware Pipeline Order

The `UseObservability()` extension on `IApplicationBuilder` registers middlewares in this exact order:

```
1. CorrelationMiddleware        → generates/reads correlation ID, pushes to LogContext
2. GlobalExceptionMiddleware    → catches exceptions, maps to Error_Response
3. [Framework: UseRouting, UseAuthentication, UseAuthorization]
4. ContextEnrichmentMiddleware  → pushes user/tenant/pacs/state into LogContext (post-auth)
5. RequestLoggingMiddleware     → logs request start/end with duration and status
6. [Framework: UseEndpoints]
```

## Components

### Component 1: Abstractions Layer

**Package:** `Intellect.Erp.Observability.Abstractions`
**Dependencies:** `Microsoft.Extensions.Logging.Abstractions` only

This package defines all public contracts with zero implementation dependencies. It contains:

**Interfaces:**
- `IAppLogger<T>` — structured logger with `Debug`, `Information`, `Warning`, `Error`, `Critical`, `BeginScope`, `BeginOperation`, and `Checkpoint` methods
- `ICorrelationContextAccessor` — provides `CorrelationId`, `CausationId`, `TraceParent`
- `IUserContextAccessor` — provides `UserId`, `UserName`, `Role`, `ImpersonatingUserId`
- `ITenantContextAccessor` — provides `TenantId`, `StateCode`, `PacsId`, `BranchCode`
- `IModuleContextAccessor` — provides `ModuleName`, `ServiceName`, `Environment`, `Feature`, `Operation`
- `IRedactionEngine` — `Redact(string)`, `RedactJson(JsonElement)`, `RedactProperties(dict)`, `RedactObject(object, Type)`
- `IErrorFactory` — factory methods for each exception type plus `FromCatalog`
- `IErrorCatalog` — `TryGet(code)`, `GetOrDefault(code)`, `All`
- `IAuditHook` — `EmitAsync(AuditEvent, CancellationToken)`

**Records and Enums:**
- `ErrorCatalogEntry(Code, Title, UserMessage, SupportMessage, HttpStatus, Severity, Retryable, Category)`
- `ErrorResponse` — the JSON error envelope
- `FieldError(Field, Code, Message)`
- `AuditEvent(EventId, CorrelationId, Module, Feature, Operation, Actor, TenantId, PacsId, EntityType, EntityId, Outcome, ErrorCode, Data, OccurredAt)`
- `ErrorCategory` enum: Validation, Business, NotFound, Conflict, Security, Integration, Dependency, Data, Concurrency, System
- `ErrorSeverity` enum: Info, Warning, Error, Critical
- `AuditOutcome` enum: Success, Failure, Rejected
- `SensitivityMode` enum: Mask, Hash, Redact

**Attributes:**
- `[BusinessOperation(module, feature, operation)]` — targets Method, Class
- `[ErrorCode(code)]` — targets Method
- `[Sensitive(mode, keepLast)]` — targets Property, Field, Parameter; defaults: `SensitivityMode.Mask`, `keepLast=4`
- `[DoNotLog]` — targets Property, Field, Parameter
- `[Mask(regex, replacement)]` — targets Property, Field; default `replacement="***"`
- `[PublicAPI]` — marker for public API surface

### Component 2: Error Handling

**Package:** `Intellect.Erp.ErrorHandling`
**Dependencies:** `Intellect.Erp.Observability.Abstractions`

**Exception Hierarchy:**
```
AppException (abstract)
├── ValidationException (+ FieldError[])
├── BusinessRuleException (implements IDomainPolicyRejectionException)
├── NotFoundException
├── ConflictException
├── UnauthorizedException
├── ForbiddenException
├── IntegrationException (retryable flag)
├── DependencyException
├── DataIntegrityException
├── ConcurrencyException (optionally implements ISagaCompensationException)
├── ExternalSystemException
└── SystemException
```

Each exception carries: `ErrorCode` (string), `Category` (ErrorCategory), `Severity` (ErrorSeverity), `Retryable` (bool), `CorrelationId` (string snapshot at throw time).

**DefaultErrorFactory:** Implements `IErrorFactory`, resolves `ICorrelationContextAccessor` to stamp correlation ID on each exception at creation time.

**Error Catalog Loader:**
- `YamlErrorCatalogLoader` — reads YAML files using YamlDotNet, validates schema (code format regex `^ERP-[A-Z]+-[A-Z]{3}-\d{4}$`, required fields), returns `IReadOnlyList<ErrorCatalogEntry>`
- `InMemoryErrorCatalog` — implements `IErrorCatalog`, loaded at startup from merged YAML files
- `ErrorHandlingOptions` — binds to `ErrorHandling` config section: `IncludeExceptionDetailsInResponse`, `ReturnProblemDetails`, `DefaultErrorCode`, `CatalogFiles[]`, `ClientErrorUriBase`

### Component 3: Observability Core

**Package:** `Intellect.Erp.Observability.Core`
**Dependencies:** Abstractions, ErrorHandling, Serilog 3.x, Serilog.Sinks.Elasticsearch, Serilog.Sinks.Console, Serilog.Sinks.File, Serilog.Enrichers.*, System.Text.Json

**AppLogger<T>:** Wraps `ILogger<T>`. `BeginScope` pushes dictionary into Serilog `LogContext`. `BeginOperation` creates a scope with module/feature/operation keys. `Checkpoint` emits an Information-level log with `checkpoint` field and optional data.

**Serilog Enrichers:**
- `CorrelationEnricher` — reads from `ICorrelationContextAccessor`
- `UserContextEnricher` — reads from `IUserContextAccessor`, masks values through `IRedactionEngine`
- `TenantContextEnricher` — reads from `ITenantContextAccessor`
- `ModuleContextEnricher` — reads from `IModuleContextAccessor`
- `MachineEnricher` — adds `machine` field from `Environment.MachineName`
- `SchemaVersionEnricher` — adds `log.schema=v1` constant

All enrichers wrap their logic in try/catch; exceptions are swallowed and a telemetry counter `observability.enricher.errors` is incremented.

**Redaction Engine (`DefaultRedactionEngine`):**
Three-layer pipeline applied in order:
1. **Structural:** JSON path patterns from `Observability:Masking:Paths` config. Uses `System.Text.Json` `JsonElement` deep-walk.
2. **Attribute-driven:** Scans `[Sensitive]`, `[DoNotLog]`, `[Mask]` attributes. Results cached in `ConcurrentDictionary<Type, TypeMaskingPlan>` for zero-reflection on hot path.
3. **Regex fallback:** Compiled `Regex` instances from `Observability:Masking:Regexes` config plus built-in defaults (Aadhaar, PAN, mobile, account, IFSC, email, JWT, connection strings).

All operations work on shallow copies. Original objects are never mutated.

**Configuration:**
- `ObservabilityOptions` — binds to `Observability` section. Validated with `DataAnnotations` + custom `IValidateOptions<T>` for cross-field rules (e.g., masking must be enabled in Production).
- `UseObservability(IHostBuilder)` — configures Serilog from `ObservabilityOptions`, registers Console/File/ES sinks based on config.
- `AddObservability(IServiceCollection, IConfiguration)` — registers `IAppLogger<T>`, all context accessors, `IRedactionEngine`, enrichers, `ObservabilityOptions` with `ValidateOnStart`.

### Component 4: ASP.NET Core Integration

**Package:** `Intellect.Erp.Observability.AspNetCore`
**Dependencies:** Core, ErrorHandling, `Microsoft.AspNetCore.App` (framework ref)

**CorrelationMiddleware:**
- Reads `X-Correlation-Id` or `X-Correlation-ID` header (case-insensitive check for both spellings)
- Falls back to `traceparent` header extraction
- Generates ULID-26 if no inbound value
- Stores in `HttpContext.Items["CorrelationId"]`
- Pushes `CorrelationId` property into Serilog `LogContext` via `LogContext.PushProperty`
- Echoes `X-Correlation-Id` on response via `OnStarting` callback
- **Traceability detection:** checks `IApplicationBuilder.Properties` for Traceability marker; if present, becomes a no-op passthrough

**GlobalExceptionMiddleware:**
- Wraps `await _next(context)` in try/catch
- Maps `AppException` subclasses to HTTP status codes per the mapping matrix
- Maps `FluentValidation.ValidationException` → converts to `ValidationException` with `FieldError[]`
- Maps `TaskCanceledException`/`OperationCanceledException` → HTTP 499
- Maps unknown exceptions → HTTP 500 with `ERP-CORE-SYS-0001`
- Serializes `ErrorResponse` as JSON with `System.Text.Json`
- Suppresses `exceptionType`/`stackTrace`/`supportReference` in Production
- Refuses to enable `IncludeExceptionDetailsInResponse` when `Environment=Production`
- Emits single Error-level structured log with correlation ID and error code

**ContextEnrichmentMiddleware:**
- Runs after authentication so `HttpContext.User` claims are populated
- Reads user claims → populates `IUserContextAccessor` (HttpContext.Items-backed)
- Reads tenant/PACS/state from claims or headers → populates `ITenantContextAccessor`
- All values pass through `IRedactionEngine` before being pushed to `LogContext`

**RequestLoggingMiddleware:**
- Emits start log at Information level
- Wraps response stream to capture status code and duration
- Emits end log with `httpMethod`, `path`, `route`, `status`, `durationMs`
- Flags requests exceeding `SlowRequestThresholdMs` as Warning
- Skips paths in `ExcludePaths` list
- Optionally captures request/response body (off by default, whitelist-only, redacted)

**Action Filters:**
- `BusinessOperationFilter` — reads `[BusinessOperation]` attribute, pushes module/feature/operation into `LogContext` scope
- `ValidationResultFilter` — checks `ModelState.IsValid`, converts invalid state to `ValidationException` with `FieldError[]`

**HttpContext-backed Accessors:**
- `HttpContextCorrelationContextAccessor` — reads from `HttpContext.Items["CorrelationId"]`
- `HttpContextUserContextAccessor` — reads from `HttpContext.User` claims
- `HttpContextTenantContextAccessor` — reads from claims + custom headers
- `ConfigurationModuleContextAccessor` — reads from `ObservabilityOptions`

**Extension Methods:**
- `UseObservability(IApplicationBuilder)` — registers middlewares in documented order, detects Traceability, sets marker property
- `AddObservabilityCorrelation(IHttpClientBuilder)` — registers `CorrelationDelegatingHandler`

### Component 5: Propagation

**Package:** `Intellect.Erp.Observability.Propagation`
**Dependencies:** Core

**CorrelationDelegatingHandler:**
- Extends `DelegatingHandler`
- Sets `X-Correlation-Id` from `ICorrelationContextAccessor`
- Sets `traceparent` in W3C format from `Activity.Current` when present
- Optionally sets `X-Causation-Id`, `X-Tenant`, `X-State-Code`

**KafkaHeaders:**
- Static helper class with `WriteCorrelation(IDictionary<string, byte[]>, ICorrelationContextAccessor, ...)` and `ReadCorrelation(...)`
- Writes: correlationId, causationId, traceparent (W3C format), tenantId, userId

**TraceableBackgroundService:**
- Abstract base extending `BackgroundService`
- `ExecuteAsync` creates a DI scope, generates/inherits correlation ID, opens enrichment scope
- Catches exceptions, logs at Error level with correlation ID, continues operation

### Component 6: Audit Hooks

**Package:** `Intellect.Erp.Observability.AuditHooks`
**Dependencies:** Abstractions, Core

**Implementations:**
- `LogOnlyAuditHook` — writes `AuditEvent` as structured Serilog log at Information level with `audit.v1=true` tag
- `TraceabilityBridgeAuditHook` — adapts `AuditEvent` → `AuditActivityRecord`, routes via `ITraceSink` (requires Traceability shim)
- `KafkaAuditHook` — serializes `AuditEvent` to JSON, publishes to configured topic via `IKafkaProducer` (requires Messaging shim)

Mode selection via `Observability:AuditHook:Mode` config.

### Component 7: Log4Net Bridge

**Package:** `Intellect.Erp.Observability.Log4NetBridge`
**Dependencies:** Abstractions, `log4net`

**SerilogForwardingAppender:**
- Extends `AppenderSkeleton`
- `Append(LoggingEvent)` maps log4net level → Serilog level, preserves message + exception
- Uses lock-free `ConcurrentQueue<LoggingEvent>` with bounded capacity
- On backpressure: drops oldest entries, increments `observability.log4net.dropped` counter
- Background flush thread drains queue into Serilog pipeline
- All Serilog enrichers (correlation, user, tenant, module, masking) apply to forwarded events

### Component 8: Integration Shims

**Traceability Shim (`Intellect.Erp.Observability.Integrations.Traceability`):**
- `TraceabilityCorrelationAdapter` — implements `ICorrelationContextAccessor` by delegating to `ITraceContextAccessor`
- `TraceabilityUserAdapter` — implements `IUserContextAccessor` by delegating to `ITraceContextAccessor`
- `TraceabilityTenantAdapter` — implements `ITenantContextAccessor` by delegating to `ITraceContextAccessor`
- `TraceabilityMaskingAdapter` — wraps `IMaskingPolicy` as the structural masking layer in `IRedactionEngine`
- `TraceabilityAuditAdapter` — maps `AuditEvent` → `AuditActivityRecord`
- Registration: `AddTraceabilityIntegration(IServiceCollection)` replaces default accessors with adapters when Traceability types are resolvable

**Messaging Shim (`Intellect.Erp.Observability.Integrations.Messaging`):**
- `ObservabilityProducerContextEnricher` — implements `IProducerContextEnricher`, enriches `EventEnvelope` with correlation/user/tenant fields
- Fixes W3C `traceparent` format (uses `00-{traceId}-{spanId}-{flags}` instead of `Activity.Current.Id`)
- Registration: `AddMessagingIntegration(IServiceCollection)`

### Component 9: Testing Harness

**Package:** `Intellect.Erp.Observability.Testing`
**Dependencies:** Abstractions, Core

- `FakeAppLogger<T>` — captures all log calls in an in-memory list for assertion
- `FakeCorrelationContextAccessor` — settable correlation ID for test scenarios
- `FakeErrorFactory` — creates exceptions without DI
- `InMemoryLogSink` — Serilog sink that captures `LogEvent` instances for assertion
- `LogAssertions` — FluentAssertions extensions for verifying log output (field presence, level, message patterns)

## Correctness Properties

### Property 1: Correlation ID Round-Trip (Req 1, AC 1.1, 1.4)

For any valid correlation ID string provided as an inbound `X-Correlation-Id` header, the middleware pipeline must echo the exact same value in the response `X-Correlation-Id` header. This is a round-trip property: `echo(inject(correlationId)) == correlationId`.

**Test approach:** Property-based test generating random ULID strings, sending HTTP requests with the header, asserting the response header matches exactly.

### Property 2: ULID Format Invariant (Req 1, AC 1.2)

For any HTTP request without a correlation header, the generated correlation ID must be a valid ULID-26 string (26 characters, Crockford Base32 alphabet). This is an invariant property.

**Test approach:** Property-based test sending requests without correlation headers, asserting the response header value matches the ULID regex `^[0-9A-HJKMNP-TV-Z]{26}$`.

### Property 3: Outbound HTTP Correlation Propagation (Req 1, AC 1.5)

For any correlation ID in the current scope, an outbound HTTP request through a client registered with `AddObservabilityCorrelation()` must carry the `X-Correlation-Id` header with the exact same value. Property: `outbound.header == scope.correlationId`.

**Test approach:** Property-based test with in-memory `HttpMessageHandler`, generating random correlation IDs, asserting outbound header matches.

### Property 4: W3C Traceparent Format (Req 1, AC 1.6; Req 13, AC 13.2)

For any `Activity.Current` with a trace ID and span ID, the `traceparent` header must match the W3C format `00-{traceId32hex}-{spanId16hex}-{flags2hex}`. This is a format invariant.

**Test approach:** Property-based test generating random 32-hex trace IDs and 16-hex span IDs, asserting the formatted traceparent matches `^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$`.

### Property 5: Kafka Header Completeness (Req 1, AC 1.7)

For any set of context values (correlationId, causationId, traceParent, tenantId, userId), `KafkaHeaders.WriteCorrelation()` must produce a header dictionary containing all non-null values. Property: `ReadCorrelation(WriteCorrelation(context)) ⊇ context`.

**Test approach:** Property-based test generating random context values, writing headers, reading them back, asserting all non-null values are preserved.

### Property 6: Error Response Required Fields (Req 3, AC 3.14; Req 14, AC 14.1)

For any `AppException` caught by the `GlobalExceptionMiddleware`, the serialized `ErrorResponse` must contain all required fields: `success`, `errorCode`, `title`, `message`, `correlationId`, `status`, `severity`, `retryable`, `timestamp`. This is an invariant.

**Test approach:** Property-based test generating random `AppException` instances (varying code, category, severity, retryable), serializing through the middleware, asserting all required JSON fields are present and non-null.

### Property 7: Exception-to-HTTP Mapping Determinism (Req 3, AC 3.2–3.13)

For any `AppException` subclass, the `GlobalExceptionMiddleware` must map it to a deterministic HTTP status code based solely on the exception type. The mapping is a pure function: `httpStatus(exceptionType) == constant`. This is an idempotence/determinism property.

**Test approach:** Example-based tests covering all 12 exception types plus `TaskCanceledException`, `FluentValidation.ValidationException`, and unknown exceptions, asserting the expected HTTP status code for each.

### Property 8: Error Catalog Round-Trip (Req 5, AC 5.1, 5.3)

For any valid `ErrorCatalogEntry` serialized to YAML and loaded by the catalog loader, `FromCatalog(entry.Code)` must produce an `AppException` whose `ErrorCode`, `Category`, `Severity`, and `Retryable` properties match the original entry. This is a round-trip property: `properties(FromCatalog(load(serialize(entry)))) == properties(entry)`.

**Test approach:** Property-based test generating random valid `ErrorCatalogEntry` instances, serializing to YAML, loading, calling `FromCatalog`, asserting property equality.

### Property 9: Error Code Format Validation (Req 5, AC 5.6)

For any string, the error code validator must accept it if and only if it matches the pattern `^ERP-[A-Z]+-[A-Z]{3}-\d{4}$` with valid MODULE and CATEGORY segments. This is a metamorphic property: `isValid(code) == regex.IsMatch(code) && validModule(code) && validCategory(code)`.

**Test approach:** Property-based test generating random strings (both valid and invalid codes), asserting the validator agrees with the regex + segment validation.

### Property 10: Sensitive Attribute Masking (Req 6, AC 6.3)

For any string value on a property annotated with `[Sensitive(keepLast=N)]`, the redacted output must have length ≥ N, the last N characters must match the original, and all preceding characters must be masked. Property: `redacted.EndsWith(original[^N:]) && redacted[0:^N].All(c => c == '*')`.

**Test approach:** Property-based test generating random strings and keepLast values, applying redaction, asserting the tail matches and the prefix is masked.

### Property 11: DoNotLog Complete Exclusion (Req 6, AC 6.4)

For any value on a property annotated with `[DoNotLog]`, the redacted output must be null or excluded from the property dictionary. Property: `redactedProperties.ContainsKey(doNotLogField) == false`.

**Test approach:** Property-based test generating random objects with `[DoNotLog]` properties, redacting, asserting the field is absent from output.

### Property 12: Redaction Non-Mutation (Req 6, AC 6.8)

For any input object passed to `IRedactionEngine.RedactObject()`, the original object's property values must remain unchanged after redaction. Property: `original == snapshot(original)` after `RedactObject(original)`.

**Test approach:** Property-based test generating random DTOs, taking a deep snapshot before redaction, asserting equality after redaction.

### Property 13: Error Response Type URI (Req 14, AC 14.3)

For any error code and configured `ClientErrorUriBase`, the `type` field in the `ErrorResponse` must equal `{base}{errorCode}`. Property: `response.type == config.base + response.errorCode`.

**Test approach:** Property-based test generating random error codes and URI bases, asserting the `type` field is the concatenation.

### Property 14: Canonical Field Set Stability (Req 2, AC 2.5)

For any log entry emitted through the Observability_Platform, the JSON output must contain the canonical fields: `@timestamp`, `level`, `app`, `env`, `machine`, `module`, `correlationId`, `log.schema`. This is an invariant preserved across all log operations.

**Test approach:** Golden-file snapshot test plus property-based test emitting logs with random content, asserting all canonical fields are present in the captured `LogEvent` properties.

### Property 15: Log Scope Dictionary Completeness (Req 2, AC 2.2)

For any `IReadOnlyDictionary<string, object?>` passed to `BeginScope`, all keys from the dictionary must appear as properties in the Serilog `LogContext` within the scope. Property: `∀ key ∈ dict: logContext.Contains(key)`.

**Test approach:** Property-based test generating random dictionaries, opening a scope, emitting a log, asserting all dictionary keys appear in the captured log event properties.

## File Changes

### New Files

- `Directory.Build.props` — shared build properties: `net8.0`, nullable enable, warnings-as-errors, `LangVersion=latest`, central package versioning
- `Directory.Packages.props` — pinned NuGet versions: Serilog 3.x, YamlDotNet, FluentAssertions, xUnit, FsCheck, BenchmarkDotNet
- `NuGet.Config` — GitHub Packages source with env-var PAT (no secrets)
- `Intellect.Erp.Observability.sln` — solution file referencing all projects
- `src/Intellect.Erp.Observability.Abstractions/` — all interfaces, records, enums, attributes
- `src/Intellect.Erp.ErrorHandling/` — exception hierarchy, DefaultErrorFactory, YamlErrorCatalogLoader, InMemoryErrorCatalog, ErrorHandlingOptions
- `src/Intellect.Erp.Observability.Core/` — AppLogger, enrichers, DefaultRedactionEngine, ObservabilityOptions, DI extensions, Serilog bootstrap
- `src/Intellect.Erp.Observability.AspNetCore/` — middlewares, filters, HttpContext-backed accessors, UseObservability extension
- `src/Intellect.Erp.Observability.Propagation/` — CorrelationDelegatingHandler, KafkaHeaders, TraceableBackgroundService
- `src/Intellect.Erp.Observability.AuditHooks/` — IAuditHook implementations (LogOnly, TraceabilityBridge, Kafka)
- `src/Intellect.Erp.Observability.Log4NetBridge/` — SerilogForwardingAppender
- `src/Intellect.Erp.Observability.Integrations.Traceability/` — adapter classes for Traceability utility
- `src/Intellect.Erp.Observability.Integrations.Messaging/` — ObservabilityProducerContextEnricher
- `src/Intellect.Erp.Observability.Testing/` — fakes, in-memory sink, assertion helpers
- `tests/Intellect.Erp.Observability.UnitTests/` — unit tests for Core, AspNetCore, Propagation
- `tests/Intellect.Erp.Observability.IntegrationTests/` — WebApplicationFactory integration tests
- `tests/Intellect.Erp.ErrorHandling.UnitTests/` — unit tests for exception hierarchy, catalog, factory
- `samples/SampleHost/` — minimal ASP.NET Core host demonstrating one-line adoption
- `docs/adoption-guide.md` — step-by-step adoption guide for consuming modules
- `docs/error-catalog-authoring.md` — guide for creating per-module error catalog YAML files
- `docs/elk-field-reference.md` — canonical ELK field set reference (schema v1)
- `docs/migration-from-log4net.md` — log4net to Serilog migration guide
- `config/error-catalog/core.yaml` — core error catalog with default entries (ERP-CORE-SYS-0001, etc.)

### Modified Files

- `README.md` — updated with project overview, package descriptions, quick-start guide
- `.gitignore` — updated with .NET-specific patterns, bin/obj, NuGet packages
