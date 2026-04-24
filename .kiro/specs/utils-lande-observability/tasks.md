# Tasks

## Phase 1: Solution Scaffolding and Build Infrastructure

- [x] 1.1 Create `Directory.Build.props` with `net8.0` target, nullable enable, warnings-as-errors, `LangVersion=latest`, central package versioning, and `[PublicAPI]` marker support
- [x] 1.2 Create `Directory.Packages.props` with pinned versions for Serilog 3.x, YamlDotNet, FluentAssertions, xUnit, FsCheck, BenchmarkDotNet, and all other dependencies
- [x] 1.3 Create `NuGet.Config` with GitHub Packages source using environment variable PAT (no secrets committed)
- [x] 1.4 Create `Intellect.Erp.Observability.sln` solution file with all project references and solution folders
- [x] 1.5 Create empty project files (`.csproj`) for all 10 `src/` packages, 3 `tests/` projects, and `samples/SampleHost` with correct project references and dependency graph
- [x] 1.6 Update `.gitignore` with .NET-specific patterns (bin, obj, packages, user files)
- [x] 1.7 Verify `dotnet build` succeeds on the empty solution with central versioning enforced

## Phase 2: Abstractions Layer

- [x] 2.1 Implement `IAppLogger<T>` interface with Debug, Information, Warning, Error, Critical, BeginScope, BeginOperation, and Checkpoint method signatures
- [x] 2.2 Implement context accessor interfaces: `ICorrelationContextAccessor`, `IUserContextAccessor`, `ITenantContextAccessor`, `IModuleContextAccessor`
- [x] 2.3 Implement `IErrorFactory` interface with factory methods for all 12 exception types plus `FromCatalog`
- [x] 2.4 Implement `IErrorCatalog` interface with `TryGet`, `GetOrDefault`, and `All` members
- [x] 2.5 Implement `IRedactionEngine` interface with `Redact`, `RedactJson`, `RedactProperties`, and `RedactObject` methods
- [x] 2.6 Implement `IAuditHook` interface with `EmitAsync(AuditEvent, CancellationToken)` method
- [x] 2.7 Implement records: `ErrorCatalogEntry`, `ErrorResponse`, `FieldError`, `AuditEvent`
- [x] 2.8 Implement enums: `ErrorCategory`, `ErrorSeverity`, `AuditOutcome`, `SensitivityMode`
- [x] 2.9 Implement attributes: `[BusinessOperation]`, `[ErrorCode]`, `[Sensitive]`, `[DoNotLog]`, `[Mask]`, `[PublicAPI]`
- [x] 2.10 Write unit tests for all abstractions (attribute construction, record equality, enum values, serialization)

## Phase 3: Error Handling Package

- [x] 3.1 Implement `AppException` abstract base class with ErrorCode, Category, Severity, Retryable, and CorrelationId properties
- [x] 3.2 Implement all 12 concrete exception subclasses: `ValidationException` (with FieldError[]), `BusinessRuleException`, `NotFoundException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`, `IntegrationException`, `DependencyException`, `DataIntegrityException`, `ConcurrencyException`, `ExternalSystemException`, `SystemException`
- [x] 3.3 Implement `BusinessRuleException` with `IDomainPolicyRejectionException` interface and `ConcurrencyException` with optional `ISagaCompensationException` interface
- [x] 3.4 Implement `DefaultErrorFactory` with `ICorrelationContextAccessor` injection for correlation ID stamping
- [x] 3.5 Implement `YamlErrorCatalogLoader` with schema validation (code format regex, required fields) using YamlDotNet
- [x] 3.6 Implement `InMemoryErrorCatalog` with `TryGet`, `GetOrDefault`, `All`, and fallback to `ERP-CORE-SYS-0001` for unknown codes
- [x] 3.7 Implement `ErrorHandlingOptions` binding to `ErrorHandling` config section with DataAnnotations validation
- [x] 3.8 Write unit tests for exception hierarchy (type checks, property defaults, interface implementations)
- [x] 3.9 Write unit tests for `DefaultErrorFactory` (all factory methods, `FromCatalog` with known and unknown codes)
  - [x] 3.9.1 [PBT] Property test: Error catalog round-trip — for any valid ErrorCatalogEntry serialized to YAML and loaded, FromCatalog produces an AppException with matching ErrorCode, Category, Severity, and Retryable (Property 8)
  - [x] 3.9.2 [PBT] Property test: Error code format validation — for any string, the validator accepts it iff it matches the ERP-MODULE-CATEGORY-SEQ4 pattern with valid segments (Property 9)
- [x] 3.10 Write unit tests for `YamlErrorCatalogLoader` (valid YAML, invalid schema, missing fields, duplicate codes)
- [x] 3.11 Create `config/error-catalog/core.yaml` with default error entries (ERP-CORE-SYS-0001 through ERP-CORE-SYS-0005, ERP-CORE-VAL-0001)

## Phase 4: Observability Core Package

- [x] 4.1 Implement `AppLogger<T>` wrapping `ILogger<T>` with BeginScope (dictionary → LogContext), BeginOperation (module/feature/operation scope), and Checkpoint (Information-level structured log)
- [x] 4.2 Implement Serilog enrichers: `CorrelationEnricher`, `UserContextEnricher`, `TenantContextEnricher`, `ModuleContextEnricher`, `MachineEnricher`, `SchemaVersionEnricher` — all with try/catch and telemetry counter on failure
- [x] 4.3 Implement `DefaultRedactionEngine` with three-layer pipeline: structural path masking (JsonElement deep-walk), attribute-driven masking (cached per type in ConcurrentDictionary), and regex fallback (compiled patterns for Aadhaar, PAN, mobile, account, IFSC, email, JWT, connection strings)
- [x] 4.4 Implement `ObservabilityOptions` binding to `Observability` config section with DataAnnotations + custom `IValidateOptions<T>` (ApplicationName non-empty, ModuleName non-empty, ES URL parseable when enabled, masking enabled in Production)
- [x] 4.5 Implement `UseObservability(IHostBuilder)` Serilog bootstrap with Console/File/Elasticsearch sinks configured from `ObservabilityOptions`
- [x] 4.6 Implement `AddObservability(IServiceCollection, IConfiguration)` DI registration for IAppLogger<T>, context accessors, IRedactionEngine, enrichers, ObservabilityOptions with ValidateOnStart
- [x] 4.7 Write unit tests for `AppLogger<T>` (delegation to ILogger, scope creation, checkpoint emission)
  - [x] 4.7.1 [PBT] Property test: Log scope dictionary completeness — for any dictionary passed to BeginScope, all keys appear in the LogContext within the scope (Property 15)
- [x] 4.8 Write unit tests for all enrichers (correct field names, exception swallowing, telemetry counter increment)
  - [x] 4.8.1 [PBT] Property test: Canonical field set stability — for any log entry emitted through the platform, the JSON output contains @timestamp, level, app, env, machine, module, correlationId, log.schema (Property 14)
- [x] 4.9 Write unit tests for `DefaultRedactionEngine` (structural masking, attribute masking, regex masking, layer ordering, shallow copy guarantee)
  - [x] 4.9.1 [PBT] Property test: Sensitive attribute masking — for any string with [Sensitive(keepLast=N)], last N chars match original and preceding chars are masked (Property 10)
  - [x] 4.9.2 [PBT] Property test: DoNotLog exclusion — for any value on a [DoNotLog] property, the field is absent from redacted output (Property 11)
  - [x] 4.9.3 [PBT] Property test: Redaction non-mutation — for any input object, original property values remain unchanged after RedactObject (Property 12)
- [x] 4.10 Write unit tests for `ObservabilityOptions` validation (valid config, missing ApplicationName, missing ModuleName, invalid ES URL, masking disabled in Production)

## Phase 5: ASP.NET Core Integration Package

- [x] 5.1 Implement `CorrelationMiddleware` with header reading (both spellings), ULID-26 generation, HttpContext.Items storage, LogContext push, response header echo, and Traceability passthrough detection
- [x] 5.2 Implement `GlobalExceptionMiddleware` with full exception-to-HTTP mapping matrix, ErrorResponse serialization, Production safety guard, FluentValidation conversion, and single structured error log
- [x] 5.3 Implement `ContextEnrichmentMiddleware` with user claims reading, tenant/PACS/state extraction, and redacted LogContext push
- [x] 5.4 Implement `RequestLoggingMiddleware` with start/end logging, duration tracking, slow request warning, path exclusion, and optional body capture with redaction
- [x] 5.5 Implement `BusinessOperationFilter` and `ValidationResultFilter` action filters
- [x] 5.6 Implement HttpContext-backed accessors: `HttpContextCorrelationContextAccessor`, `HttpContextUserContextAccessor`, `HttpContextTenantContextAccessor`, `ConfigurationModuleContextAccessor`
- [x] 5.7 Implement `UseObservability(IApplicationBuilder)` with middleware ordering, Traceability detection, and marker property
- [x] 5.8 Implement `AddErrorHandling(IServiceCollection, IConfiguration)` extension for ErrorFactory, ErrorCatalog, and ErrorResponse serializer registration
- [-] 5.9 Write integration tests using `WebApplicationFactory` for CorrelationMiddleware (header propagation, ULID generation, response echo)
  - [x] 5.9.1 [PBT] Property test: Correlation ID round-trip — for any valid correlation ID as inbound header, the response header echoes the exact same value (Property 1)
  - [x] 5.9.2 [PBT] Property test: ULID format invariant — for any request without correlation header, the generated ID matches ULID-26 format (Property 2)
- [x] 5.10 Write integration tests for GlobalExceptionMiddleware (all 12 exception types + TaskCanceled + FluentValidation + unknown, Production safety guard)
  - [x] 5.10.1 [PBT] Property test: Error response required fields — for any AppException, the serialized ErrorResponse contains all required fields (Property 6)
  - [x] 5.10.2 [PBT] Property test: Error response type URI — for any error code and ClientErrorUriBase, the type field equals base + errorCode (Property 13)
- [x] 5.11 Write integration tests for ContextEnrichmentMiddleware (authenticated vs unauthenticated, field presence)
- [x] 5.12 Write integration tests for RequestLoggingMiddleware (duration logging, slow request warning, path exclusion, body capture with redaction)
- [x] 5.13 Write integration tests for action filters (BusinessOperation scope push, ModelState validation conversion)

## Phase 6: Propagation Package

- [x] 6.1 Implement `CorrelationDelegatingHandler` with X-Correlation-Id, traceparent (W3C format), and optional causation/tenant/state headers
- [x] 6.2 Implement `KafkaHeaders` static helpers with `WriteCorrelation` and `ReadCorrelation` methods
- [x] 6.3 Implement `TraceableBackgroundService` abstract base with scoped correlation ID, enrichment scope, and error logging with continuation
- [x] 6.4 Write unit tests for `CorrelationDelegatingHandler` (header propagation, W3C format)
  - [x] 6.4.1 [PBT] Property test: Outbound HTTP correlation propagation — for any correlation ID in scope, outbound request header matches (Property 3)
  - [x] 6.4.2 [PBT] Property test: W3C traceparent format — for any Activity with trace/span IDs, traceparent matches W3C regex (Property 4)
- [x] 6.5 Write unit tests for `KafkaHeaders` (write/read round-trip, null handling)
  - [x] 6.5.1 [PBT] Property test: Kafka header completeness — for any context values, ReadCorrelation(WriteCorrelation(context)) preserves all non-null values (Property 5)
- [x] 6.6 Write unit tests for `TraceableBackgroundService` (correlation scope, error logging, continuation after failure)

## Phase 7: Audit Hooks Package

- [x] 7.1 Implement `LogOnlyAuditHook` with structured Serilog log at Information level and `audit.v1=true` tag
- [x] 7.2 Implement `TraceabilityBridgeAuditHook` with AuditEvent → AuditActivityRecord adaptation (requires Traceability shim)
- [x] 7.3 Implement `KafkaAuditHook` with JSON serialization and topic publishing (requires Messaging shim)
- [x] 7.4 Implement audit hook mode selection and DI registration based on `Observability:AuditHook:Mode` config
- [x] 7.5 Write unit tests for all three audit hook implementations (log output, adaptation mapping, serialization)

## Phase 8: Log4Net Bridge Package

- [x] 8.1 Implement `SerilogForwardingAppender` extending `AppenderSkeleton` with level mapping, lock-free ConcurrentQueue, bounded capacity, drop-oldest on backpressure, and telemetry counter
- [x] 8.2 Write unit tests for `SerilogForwardingAppender` (level mapping, message/exception preservation, enricher application, backpressure behavior)

## Phase 9: Integration Shims

- [x] 9.1 Implement Traceability shim adapters: `TraceabilityCorrelationAdapter`, `TraceabilityUserAdapter`, `TraceabilityTenantAdapter`, `TraceabilityMaskingAdapter`, `TraceabilityAuditAdapter`
- [x] 9.2 Implement `AddTraceabilityIntegration(IServiceCollection)` registration that replaces default accessors with Traceability adapters when types are resolvable
- [x] 9.3 Implement Messaging shim: `ObservabilityProducerContextEnricher` with W3C traceparent format fix
- [x] 9.4 Implement `AddMessagingIntegration(IServiceCollection)` registration
- [x] 9.5 Write integration tests for Traceability shim (context delegation, masking delegation, correlation passthrough, audit adaptation)
- [x] 9.6 Write integration tests for Messaging shim (envelope enrichment, traceparent format)

## Phase 10: Testing Harness Package

- [x] 10.1 Implement `FakeAppLogger<T>` with in-memory log capture and assertion methods
- [x] 10.2 Implement `FakeCorrelationContextAccessor`, `FakeErrorFactory`, and other test doubles
- [x] 10.3 Implement `InMemoryLogSink` Serilog sink for capturing LogEvent instances
- [x] 10.4 Implement `LogAssertions` FluentAssertions extensions for log verification
- [x] 10.5 Write golden-file snapshot tests for ELK JSON canonical field set (schema v1 field names must not change)

## Phase 11: Sample Host and Documentation

- [x] 11.1 Create `samples/SampleHost` minimal ASP.NET Core application demonstrating one-line adoption (`UseObservability`, `AddObservability`, `AddErrorHandling`)
- [x] 11.2 Add sample controllers with `[BusinessOperation]` annotations, typed exception throwing, and PII-annotated DTOs
- [x] 11.3 Add sample `appsettings.json` with complete `Observability` and `ErrorHandling` configuration
- [x] 11.4 Add sample `config/error-catalog/sample.yaml` with example error entries
- [x] 11.5 Write `docs/adoption-guide.md` — step-by-step adoption guide for consuming modules
- [x] 11.6 Write `docs/error-catalog-authoring.md` — guide for creating per-module error catalog YAML files
- [x] 11.7 Write `docs/elk-field-reference.md` — canonical ELK field set reference (schema v1)
- [x] 11.8 Write `docs/migration-from-log4net.md` — log4net to Serilog migration guide
- [x] 11.9 Update `README.md` with project overview, package descriptions, quick-start guide, and links to documentation
