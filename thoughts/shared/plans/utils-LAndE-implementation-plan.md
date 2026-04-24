# utils-LAndE — Comprehensive Implementation Plan

**Package family:** `Intellect.Erp.Observability.*` and `Intellect.Erp.ErrorHandling`
**Target:** ePACS ERP (NABARD programme) — .NET 8 modular codebase
**Repo:** `/Users/narayanaa/DevCode/Intellect/Code/RCA/utils-LAndE`
**Sibling utilities to integrate with:** `Intellect.Erp.Traceability`, `Intellect.Erp.Messaging`
**Primary consumers (pilot → rollout):** `l3_Loans`, `l3_savingsDeposit`, `l3_FAS`, `l3_voucherProcessing`, `l3_membership`, `l3_merchandise`, `l3_uniteCommonAPI`, `l3_auditProcessing`

---

## 0. How to use this document

This plan is organized so that you can execute it top-down. Each phase has:
- A clear deliverable
- A small number of files to create or edit
- Acceptance criteria
- A testing slice

All sections map back to a Functional Requirement (FR-x) or Non-Functional Requirement (NFR-x) from the BRD (`thoughts/shared/prs/ERP_Logging_Error_Handling_Utility_BRD.docx`).

---

## 1. Executive synthesis

### 1.1 What the BRD asks for
A DI-first, ELK-friendly, Serilog-based shared platform utility that standardizes:
- Structured logging with canonical context (module, operation, user, PACS, state, tenant, correlation)
- Correlation id generation / propagation (HTTP, Kafka, jobs)
- Global exception handling and consumer-safe error responses
- A centralized error catalog with stable error codes
- Sensitive-field masking and redaction
- Annotation-driven adoption (`[BusinessOperation]`, `[ErrorCode]`, `[Sensitive]`, `[DoNotLog]`)
- Audit-hook extensibility for future financial traceability

### 1.2 What already exists in sibling utilities

**`Intellect.Erp.Traceability` (already solved — do not duplicate):**
- `TraceContext` record with UserId/TenantId/StateCode/CorrelationId/TraceId/SpanId/Channel/BranchCode/IpAddress/UserAgent/geo
- `ITraceContextAccessor.Resolve()` consolidating claims + HTTP headers
- `TraceabilityMiddleware` — reads `X-Correlation-Id` / ULID fallback, echoes on response, stamps `HttpContext.Items`
- `IMaskingPolicy` / `DefaultMaskingPolicy`
- `KafkaCorrelationPropagator` — read/write string-header dictionary with correlationId/causationId/traceId/spanId/sagaId/sagaStep/tenantId/userId/eventId/eventType
- `AuditActivityRecord`, `AuditOutcome`, `ErrorCategory`, `OutcomeBinder`
- `TraceabilityActivitySource`, `TraceabilityMeter` (counters/histograms)
- `IAuditEnricher` for cross-cutting fields
- Marker exceptions: `ISagaCompensationException`, `IDomainPolicyRejectionException`
- Health check `/health/traceability`

**`Intellect.Erp.Messaging` (integration points — do not replace):**
- Kafka header contract: `correlationId`, `eventId`, `eventType`, `schemaVersion`, `tenant`, `traceparent`, `tracestate`, `idempotencyKey`, `causationId`, `sagaId`
- `MessagingOptions` options pattern, section `Messaging`
- `IKafkaAuditLogger` — pipe-delimited structured lines
- `IErrorClassifier` / `RetryDecision` / `ErrorCategory` enum (separate from traceability's — note the duplication risk)
- `MessagingTelemetry` ActivitySource `Intellect.Erp.Messaging.Kafka`

### 1.3 What today's ERP modules look like (observed anti-patterns)
1. Mis-labeled Serilog app names (FAS logs as `"Merchandise"`).
2. Templates reference `CorrelationId` but no middleware enriches it → empty field in logs.
3. Voucher uses `UseExceptionHandler` that `Console.WriteLine`s and returns plain text.
4. `ex.Message` (and sometimes stack) leaked to clients across Loans/Savings/Membership/Unite/Merchandise/Voucher.
5. Ubiquitous `return new JsonResult(false)` / `StatusCode(500, false)` anti-pattern — HTTP 200 on failure in many cases.
6. String-concat logging: `_log.Error("..." + ex.ToString())`.
7. Dual log4net + Serilog pipelines with inconsistent configuration and different severity thresholds.
8. Correlation header chaos — `X-Correlation-ID` vs `X-Correlation-Id`; some hosts always generate new GUID and discard inbound value.
9. `UseSerilogRequestLogging` placed **after** `MapControllers()` in Loans / Unite (ineffective for controller traffic).
10. Static `ConcurrentDictionary` tenant state in middleware (FAS/Unite/Loans) — hidden globals.
11. `new ExternalService()` inside middleware (bypasses DI).
12. PII logged unmasked (Mobile number, Aadhaar-adjacent endpoints).
13. Dead `ApiResponse<T>` defined in Loans but never used by controllers.
14. `app.Run()` registered twice in Membership (copy-paste residue).
15. Per-controller `try/catch` everywhere as the dominant error pattern.

### 1.4 Strategy
- **Compose, don't duplicate.** utils-LAndE depends on utils-Traceability *only through soft contracts* (interface lookup via DI). A thin optional shim (`Intellect.Erp.Observability.Integrations.Traceability`) bridges the two so modules that adopt both see a unified correlation / masking / audit pipeline. Modules that adopt only utils-LAndE get a standalone, in-process default.
- **Serilog is the canonical pipeline** (every module already has it). Provide a log4net→Serilog bridge for legacy BL.
- **One configuration surface.** A single `Observability` and `ErrorHandling` section; environment overrides; `ValidateOnStart`.
- **One-line adoption.** `builder.Host.UseObservability(); builder.Services.AddObservability(); app.UseObservability();`.
- **Annotation-driven where it helps, DI-first everywhere.** Never force attribute adoption as the only path.
- **Do not break what works.** Provide adapters so existing `JsonResult(false)` surfaces can be migrated incrementally.

---

## 2. Design principles (non-negotiable)

1. **DI-first.** Zero static singletons that hold state. All abstractions resolvable through `IServiceProvider`.
2. **Zero-cost when disabled.** Every feature (body logging, enrichment, audit hooks) toggleable via configuration and fails closed in production.
3. **Never throw from the logging/error pipeline.** Sink outages must not break business flow; errors in enrichers must be swallowed with a single telemetry counter increment.
4. **Consumer-safe by default.** No stack trace, no exception type names, no SQL fragments leave process boundary in Prod. `IncludeExceptionDetailsInResponse=true` is a development-only toggle and refuses to activate when `Environment=Production`.
5. **Correlation id is always present.** Middleware guarantees a value even for anonymous failures before routing.
6. **Mask first, log later.** Enrichers mutate a shallow copy; no mutation of caller DTOs.
7. **ELK-compatible JSON format with stable field names.** Field schema versioned (`log.schema=v1`).
8. **Backwards compatibility.** Every public API is marked as such (`[PublicAPI]` marker + documentation). Breaking changes require a major version bump.
9. **Test every invariant.** 100% coverage on middleware, masking, and error mapping; property tests for correlation propagation.
10. **Warnings as errors, nullable enabled, lang latest** (matches sibling utilities).

---

## 3. Target package & solution layout

```
utils-LAndE/
├── Directory.Build.props                       # net8.0, nullable, warnings-as-errors, central versioning
├── Directory.Packages.props                    # pinned versions (Serilog 3.x, OTel 1.11.x, MediatR 12.x, Polly 8.x)
├── Intellect.Erp.Observability.sln
├── NuGet.Config                                # no in-repo secrets; GitHub Packages via env var PAT
├── build_push_script.sh                        # mirror utils-messaging CI helper (semver bump + pack)
├── README.md
├── docs/
│   ├── adoption-guide.md
│   ├── error-catalog-authoring.md
│   ├── elk-field-reference.md
│   └── migration-from-log4net.md
├── src/
│   ├── Intellect.Erp.Observability.Abstractions/       # ZERO dependencies (only MEL.Abstractions)
│   ├── Intellect.Erp.Observability.Core/               # Serilog + masking + enrichers + error catalog
│   ├── Intellect.Erp.Observability.AspNetCore/         # middleware, filters, ProblemDetails, DI wiring
│   ├── Intellect.Erp.Observability.Propagation/        # HttpClient handler + Kafka header helper + job base
│   ├── Intellect.Erp.ErrorHandling/                    # typed exceptions + error response contract
│   ├── Intellect.Erp.Observability.AuditHooks/         # IAuditHook + default structured-log implementation
│   ├── Intellect.Erp.Observability.Log4NetBridge/      # log4net appender that forwards to Serilog
│   ├── Intellect.Erp.Observability.Integrations.Traceability/  # optional shim: adapt Traceability ↔ Observability
│   ├── Intellect.Erp.Observability.Integrations.Messaging/     # optional shim: adapt Kafka headers + error classifier
│   └── Intellect.Erp.Observability.Testing/            # fakes, sinks, assertion helpers
├── tests/
│   ├── Intellect.Erp.Observability.UnitTests/
│   ├── Intellect.Erp.Observability.IntegrationTests/
│   └── Intellect.Erp.ErrorHandling.UnitTests/
├── samples/
│   └── SampleHost/                                     # minimal host demonstrating adoption + ELK output
└── thoughts/
    └── shared/plans/utils-LAndE-implementation-plan.md (this file)
```

### 3.1 Package dependency matrix
| Package | Hard refs | Optional soft refs (DI lookup only) |
|---|---|---|
| Abstractions | `Microsoft.Extensions.Logging.Abstractions` | — |
| ErrorHandling | Abstractions | — |
| Core | Abstractions, ErrorHandling, `Serilog`, `Serilog.Sinks.Elasticsearch`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Serilog.Enrichers.*`, `System.Text.Json` | `Intellect.Erp.Traceability` (via DI lookup of `ITraceContextAccessor` / `IMaskingPolicy` if the assembly is loaded) |
| AspNetCore | Core, ErrorHandling, `Microsoft.AspNetCore.App` (framework ref) | — |
| Propagation | Core, `Confluent.Kafka.Headers` types via abstractions only | `Intellect.Erp.Messaging.Contracts` (optional, via adapter project) |
| AuditHooks | Abstractions, Core | `Intellect.Erp.Traceability.AuditActivityRecord` via adapter project |
| Log4NetBridge | Abstractions, `log4net` | — |
| Integrations.Traceability | Abstractions, `Intellect.Erp.Traceability` | — |
| Integrations.Messaging | Propagation, `Intellect.Erp.Messaging.Contracts` | — |

The "soft refs" pattern (`services.GetService<T>()` with null-check) means a host using only utils-LAndE gets a standalone experience; a host using both utilities sees unified behavior without either package hard-referencing the other.

---

## 4. Public API contracts (abstractions layer)

### 4.1 `IAppLogger<T>` (FR-11, FR-12)
```csharp
public interface IAppLogger<out T>
{
    void Debug(string messageTemplate, params object?[] args);
    void Information(string messageTemplate, params object?[] args);
    void Warning(string messageTemplate, params object?[] args);
    void Warning(Exception? exception, string messageTemplate, params object?[] args);
    void Error(Exception? exception, string messageTemplate, params object?[] args);
    void Critical(Exception? exception, string messageTemplate, params object?[] args);
    IDisposable BeginScope(IReadOnlyDictionary<string, object?> context);
    IDisposable BeginOperation(string module, string feature, string operation, IReadOnlyDictionary<string, object?>? extra = null);
    void Checkpoint(string name, IReadOnlyDictionary<string, object?>? data = null); // business checkpoint log
}
```
Implemented on top of `ILogger<T>` so Microsoft.Extensions.Logging users transition without behavior change.

### 4.2 Context accessors
```csharp
public interface ICorrelationContextAccessor { string CorrelationId { get; } string? CausationId { get; } string? TraceParent { get; } }
public interface IUserContextAccessor { string? UserId { get; } string? UserName { get; } string? Role { get; } string? ImpersonatingUserId { get; } }
public interface ITenantContextAccessor { string TenantId { get; } string? StateCode { get; } string? PacsId { get; } string? BranchCode { get; } }
public interface IModuleContextAccessor { string ModuleName { get; } string ServiceName { get; } string Environment { get; } string? Feature { get; } string? Operation { get; } }
```
If `Intellect.Erp.Traceability` is present, shims delegate to `ITraceContextAccessor`; otherwise, a `HttpContext.Items`-backed default is used.

### 4.3 Error factory & catalog
```csharp
public interface IErrorFactory
{
    AppException Validation(string code, string userMessage, params FieldError[] fieldErrors);
    AppException Business(string code, string userMessage, string? supportMessage = null);
    AppException NotFound(string code, string userMessage);
    AppException Conflict(string code, string userMessage);
    AppException Unauthorized(string code, string userMessage);
    AppException Forbidden(string code, string userMessage);
    AppException Integration(string code, string userMessage, Exception? inner = null, bool retryable = true);
    AppException Dependency(string code, string userMessage, Exception? inner = null);
    AppException DataIntegrity(string code, string userMessage);
    AppException Concurrency(string code, string userMessage);
    AppException System(string code, string userMessage, Exception? inner = null);
    AppException FromCatalog(string code, Exception? inner = null, IReadOnlyDictionary<string, object?>? data = null);
}

public interface IErrorCatalog
{
    bool TryGet(string code, out ErrorCatalogEntry entry);
    ErrorCatalogEntry GetOrDefault(string code);
    IReadOnlyCollection<ErrorCatalogEntry> All { get; }
}

public sealed record ErrorCatalogEntry(
    string Code,                 // ERP-LOANS-BIZ-0042
    string Title,                // "Loan amount exceeds sanctioned limit"
    string UserMessage,          // consumer-safe, localizable
    string? SupportMessage,      // for L2/L3 with reference to correlation id
    int HttpStatus,              // 400/404/409/500/503
    ErrorSeverity Severity,      // Info/Warning/Error/Critical
    bool Retryable,
    ErrorCategory Category);     // Validation/Business/NotFound/Conflict/Security/Integration/Dependency/Data/Concurrency/System
```

### 4.4 Attributes (FR-11)
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class BusinessOperationAttribute(string module, string feature, string operation) : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ErrorCodeAttribute(string code) : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SensitiveAttribute(SensitivityMode mode = SensitivityMode.Mask, int keepLast = 4) : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DoNotLogAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MaskAttribute(string regex, string replacement = "***") : Attribute;
```

### 4.5 Exception hierarchy (FR-5)
```
AppException (abstract, ErrorCode, Category, Severity, Retryable, CorrelationId snapshot)
├── ValidationException (+ FieldError[])
├── BusinessRuleException
├── NotFoundException
├── ConflictException
├── UnauthorizedException
├── ForbiddenException
├── IntegrationException       // downstream HTTP, SOAP, gRPC
├── DependencyException        // DB, cache, queue, filesystem
├── DataIntegrityException
├── ConcurrencyException
├── ExternalSystemException    // 3rd-party outside our control, different from IntegrationException which is sibling services
└── SystemException            // unknown / catch-all
```
`BusinessRuleException` implements `IDomainPolicyRejectionException` from Traceability → maps to `AuditOutcome.Rejected`.
`ConcurrencyException` implements `ISagaCompensationException` when wrapping a saga-scoped operation.

### 4.6 Error response contract (FR-8)
```json
{
  "success": false,
  "errorCode": "ERP-LOANS-VAL-0001",
  "title": "Validation failed",
  "message": "Some inputs are invalid. Please review and try again.",
  "correlationId": "01HZX5PQ8WZ4A5C2M8YT4F3M6V",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "status": 400,
  "severity": "warning",
  "retryable": false,
  "timestamp": "2026-04-23T10:55:12.442Z",
  "fieldErrors": [
    { "field": "LoanAmount", "code": "ERP-LOANS-VAL-0001.amount-positive", "message": "Loan amount must be greater than zero." }
  ],
  "supportReference": "LNS-UAT-230426-01HZX5PQ",
  "type": "https://errors.epacs.nabard/ERP-LOANS-VAL-0001"
}
```
Compatible with RFC7807 (`type`, `title`, `status`) plus ePACS extensions. Serializer suppresses fields in `Production` based on `IncludeExceptionDetailsInResponse`.

---

## 5. Error code taxonomy (FR-6, FR-7)

Format: `ERP-<MODULE>-<CATEGORY>-<SEQ4>`

| Segment | Values |
|---|---|
| MODULE | `CORE`, `LOANS`, `SAVINGS`, `MEMBERSHIP`, `FAS`, `VOUCHER`, `MERCHANDISE`, `AUDIT`, `UNITE` |
| CATEGORY | `VAL` Validation · `BIZ` Business rule · `NFD` Not found · `CFL` Conflict · `SEC` Security/Authorization · `INT` Integration · `DEP` Dependency · `DAT` Data integrity · `CON` Concurrency · `SYS` Internal failure |
| SEQ4 | Zero-padded per module+category counter |

Each module owns a YAML file under `docs/error-catalog/<module>.yaml`:
```yaml
- code: ERP-LOANS-BIZ-0042
  title: "Loan amount exceeds sanctioned limit"
  userMessage: "The requested loan amount exceeds the approved limit."
  supportMessage: "Limit check failed. See sanction table snapshot in log."
  httpStatus: 422
  severity: warning
  retryable: false
  category: Business
```
Build step: the catalog is merged at startup into `IErrorCatalog`. Unknown codes fall back to `ERP-CORE-SYS-0001` but emit a `Warning` log + metric.

---

## 6. Logging & context model (FR-3)

### 6.1 Canonical ELK field set (schema v1)
| Field | Source | Always | Notes |
|---|---|---|---|
| `@timestamp` | Serilog | yes | UTC ISO-8601 |
| `level` | Serilog | yes | |
| `app` | config `Observability:ApplicationName` | yes | e.g. `epacs-loans` |
| `env` | config `Observability:Environment` | yes | |
| `machine` | enricher | yes | |
| `module` | `IModuleContextAccessor` / `[BusinessOperation]` | yes | |
| `feature` | attribute | on controllers | |
| `operation` | attribute | on controllers | |
| `correlationId` | middleware | yes | ULID-26 |
| `causationId` | inbound header | opt | |
| `traceId`, `spanId` | `Activity.Current` | when OTel on | |
| `httpMethod`, `path`, `route`, `status`, `durationMs` | request logging mw | http requests | |
| `userId`, `userName`, `role` | `IUserContextAccessor` | when auth'd | masked |
| `tenantId`, `stateCode`, `pacsId`, `branchCode` | `ITenantContextAccessor` | when available | |
| `clientIp`, `userAgent` | enricher | http | ip optionally masked |
| `entityType`, `entityId` | `[BusinessEntityId]` attribute + return value | on checkpoints | |
| `errorCode`, `errorCategory`, `severity`, `retryable` | AppException | on errors | |
| `exceptionType`, `stackTrace` | exception | dev/stage only | |
| `checkpoint` | `Checkpoint(name, data)` | optional | |
| `log.schema` | constant `v1` | yes | |

### 6.2 Output formats
- **Console (dev):** compact template with colors, stack traces enabled.
- **File (rolling daily):** JSON lines (Serilog `CompactJsonFormatter`) for ELK file-beat.
- **Elasticsearch (direct):** `Serilog.Sinks.Elasticsearch` with index `{app}-{env}-{yyyy.MM}`. Retry policy + buffer file at `/data/L3-logs/{app}/buffer`.
- **OTel bridge (optional, phase 4):** `Serilog.Sinks.OpenTelemetry` for unified traces+logs.

### 6.3 Minimum-level policy
Single `Observability:DefaultMinimumLevel` + per-namespace `ModuleOverrides` dictionary. Production defaults: `Information`; `Microsoft`/`System` → `Warning`; audit checkpoints → always `Information`.

---

## 7. Masking & redaction (FR-10, NFR-Security)

### 7.1 Three-layer policy (applied in order)
1. **Structural policy** — field path patterns (`$.body.password`, `$.headers.authorization`) configured in `Observability:Masking:Paths`.
2. **Attribute-driven** — runtime reflection scan: `[Sensitive]`, `[Mask]`, `[DoNotLog]`. Cached per type for zero-reflection-on-hot-path.
3. **Regex fallback** — default rules: Aadhaar (12-digit), PAN (5+4+1), mobile (IN format), account-no (10-20 digits), IFSC (11-char), email, JWT/`Bearer` tokens, connection strings (`password=…`, `pwd=…`, `server=…;uid=…;pwd=…`).

### 7.2 Implementation
```csharp
public interface IRedactionEngine
{
    string Redact(string? text);                              // raw text — regex fallback
    JsonElement RedactJson(JsonElement element);              // deep-walk
    IReadOnlyDictionary<string, object?> RedactProperties(IReadOnlyDictionary<string, object?> props);
    object? RedactObject(object? value, Type declaredType);   // attribute-aware
}
```
All enrichers route through `IRedactionEngine`. Calling code never emits raw PII values. Body logging (request/response) is **off by default**; turning it on requires `AllowedBodyLogging[].Route` whitelist and enforces masking.

### 7.3 Integration with Traceability
When `Intellect.Erp.Traceability.IMaskingPolicy` is resolvable, the utils-LAndE engine wraps it via adapter rather than running its own pass twice. Config key `Observability:Masking:UseTraceabilityPolicy=true` enables delegation.

---

## 8. Configuration contract (FR-15)

```json
{
  "Observability": {
    "ApplicationName": "epacs-loans",
    "ServiceName": "Loans.Api",
    "ModuleName": "Loans",
    "Environment": "Prod",
    "DefaultMinimumLevel": "Information",
    "ModuleOverrides": { "Microsoft": "Warning", "System": "Warning", "Loans": "Information" },
    "RequestLogging": {
      "Enabled": true,
      "CaptureRequestBody": false,
      "CaptureResponseBody": false,
      "BodyWhitelist": [],
      "SlowRequestThresholdMs": 500,
      "ExcludePaths": ["/health", "/metrics", "/swagger"]
    },
    "ContextCapture": {
      "User": true, "Tenant": true, "Pacs": true, "State": true, "GeoFromHeaders": false
    },
    "Masking": {
      "Enabled": true,
      "UseTraceabilityPolicy": true,
      "Paths": ["$.body.password","$.body.otp","$.headers.authorization","$.body.aadhaarNumber"],
      "Regexes": { "Aadhaar": "\\b[2-9]{1}[0-9]{11}\\b", "PAN": "\\b[A-Z]{5}[0-9]{4}[A-Z]{1}\\b" }
    },
    "Sinks": {
      "Console": { "Enabled": true, "Format": "Compact" },
      "File": { "Enabled": true, "Path": "/data/L3-logs/epacs/loans-.json", "RollingInterval": "Day", "Retained": 14 },
      "Elasticsearch": { "Enabled": true, "Url": "http://elk:9200", "IndexFormat": "epacs-{app}-{env}-{0:yyyy.MM}", "BufferPath": "/data/L3-logs/epacs/loans-buffer" }
    },
    "Telemetry": {
      "MeterName": "Intellect.Erp.Observability",
      "HealthCheckPath": "/health/observability"
    },
    "AuditHook": {
      "Mode": "LogOnly",         // LogOnly | TraceabilityBridge | Kafka
      "Topic": "erp.audit.v1"    // when Mode=Kafka
    }
  },
  "ErrorHandling": {
    "IncludeExceptionDetailsInResponse": false,
    "ReturnProblemDetails": true,
    "DefaultErrorCode": "ERP-CORE-SYS-0001",
    "CatalogFiles": ["config/error-catalog/core.yaml","config/error-catalog/loans.yaml"],
    "ClientErrorUriBase": "https://errors.epacs.nabard/"
  }
}
```

`AddObservability(configuration).ValidateOnStart()` enforces: ApplicationName non-empty, ModuleName non-empty, ES URL parseable when enabled, catalog files exist, Masking.Enabled==true in Production.

---

## 9. Middleware pipeline (FR-1, FR-2, FR-4, FR-13)

Exact registration order enforced by `UseObservability()`:
```
1. CorrelationMiddleware
     – reads X-Correlation-Id / X-Correlation-ID / traceparent (both spellings)
     – falls through to ULID (matches utils-Traceability scheme)
     – stores in HttpContext.Items["CorrelationId"] (same key as Traceability)
     – sets LogContext.PushProperty("CorrelationId", …) scope for the request
     – echoes header on response
2. GlobalExceptionMiddleware
     – try/await _next
     – catch → ErrorMapper → 4xx/5xx JSON response
     – ensures correlation id present in response body
     – emits single structured Error log
3. UseRouting()                             [framework]
4. UseAuthentication() / UseAuthorization() [framework]
5. ContextEnrichmentMiddleware
     – runs after auth so user claims are populated
     – pushes user/tenant/pacs/state into LogContext (masked)
6. RequestLoggingMiddleware
     – start log at Info, end log with status+duration
     – flags SlowRequestThresholdMs as Warning
7. UseEndpoints()                           [framework]
```

If `Intellect.Erp.Traceability.UseTraceability()` is detected (via a marker feature flag written on `IApplicationBuilder.Properties`), **CorrelationMiddleware is a no-op passthrough** (Traceability already did it). This makes the two utilities safely composable.

### 9.1 GlobalExceptionMiddleware mapping
| Exception | HTTP | ErrorCategory | Retryable |
|---|---|---|---|
| `ValidationException` | 400 | Validation | false |
| `BusinessRuleException` | 422 | Business | false |
| `NotFoundException` | 404 | NotFound | false |
| `ConflictException` | 409 | Conflict | false |
| `UnauthorizedException` | 401 | Security | false |
| `ForbiddenException` | 403 | Security | false |
| `ConcurrencyException` | 409 | Concurrency | true |
| `DataIntegrityException` | 500 | Data | false |
| `IntegrationException` | 502 | Integration | configurable |
| `DependencyException` | 503 | Dependency | true |
| `TaskCanceledException` / `OperationCanceledException` | 499 | System | true |
| `FluentValidation.ValidationException` | 400 (converted) | Validation | false |
| *any other* | 500 | System | false |

### 9.2 Action filters
- `BusinessOperationFilter` reads `[BusinessOperation]` and pushes `module/feature/operation` scope.
- `ValidationResultFilter` converts `ModelState.IsValid == false` into `ValidationException` with `FieldError[]`.

---

## 10. Outbound propagation (FR-13)

### 10.1 HTTP
`CorrelationDelegatingHandler : DelegatingHandler`
- Always sets `X-Correlation-Id`.
- Also sets `traceparent` from `Activity.Current` in proper W3C format (`00-{traceId}-{spanId}-{flags}`) — fixes the format bug observed in utils-messaging.
- Adds optional `X-Causation-Id`, `X-Tenant`, `X-State-Code`.
Registered via `services.AddHttpClient(name).AddObservabilityCorrelation()`.

### 10.2 Kafka (delegating to utils-messaging)
- We **do not** build a Kafka client. Instead `Intellect.Erp.Observability.Propagation.KafkaHeaders` exposes static helpers: `WriteCorrelation(IDictionary<string, byte[]>, ICorrelationContextAccessor, …)` and `ReadCorrelation(…)`.
- The companion shim project `Intellect.Erp.Observability.Integrations.Messaging` registers an `IProducerContextEnricher` that enriches utils-messaging's `EventEnvelope` with correlation/user/tenant fields and fixes the W3C `traceparent` format (currently using `Activity.Current.Id`).

### 10.3 Background jobs
Abstract base `TraceableBackgroundService : BackgroundService` — establishes a scoped correlation id (parent = job id or caller context) and opens an enrichment scope. Adapter for Quartz if modules use it (none of the sampled modules do today, but utils-Traceability has `TraceableQuartzJob`).

---

## 11. AuditHooks (FR-17 future-readiness)

Minimal abstraction in phase 1 so modules can emit audit events now; storage is pluggable.
```csharp
public interface IAuditHook
{
    ValueTask EmitAsync(AuditEvent @event, CancellationToken ct = default);
}

public sealed record AuditEvent(
    string EventId,
    string CorrelationId,
    string Module,
    string Feature,
    string Operation,
    string? Actor,
    string TenantId,
    string? PacsId,
    string? EntityType,
    string? EntityId,
    AuditOutcome Outcome,
    string? ErrorCode,
    IReadOnlyDictionary<string, object?> Data,
    DateTimeOffset OccurredAt);
```
Modes:
- `LogOnly` — writes structured Serilog event at Information level with `audit.v1=true` tag.
- `TraceabilityBridge` — if `Integrations.Traceability` is referenced, routes to `AuditActivityRecord` via `ITraceSink`.
- `Kafka` — via `Integrations.Messaging` + `IKafkaProducer` to `erp.audit.v1` topic.

---

## 12. Log4Net bridge (legacy modules)

`Intellect.Erp.Observability.Log4NetBridge` ships a `SerilogForwardingAppender : AppenderSkeleton`. Module migration:
```xml
<log4net>
  <appender name="SerilogForwarder" type="Intellect.Erp.Observability.Log4NetBridge.SerilogForwardingAppender, Intellect.Erp.Observability.Log4NetBridge" />
  <root><level value="INFO"/><appender-ref ref="SerilogForwarder"/></root>
</log4net>
```
Existing `ILog log = LogManager.GetLogger(typeof(VoucherBl))` keeps working; logs flow into Serilog → ELK with all enrichers applied.

---

## 13. Adoption patterns (before/after)

### 13.1 Program.cs — one-line adoption
**Before (Loans, lines 224–303):** inline middlewares, manual correlation, `ex.Message` leak, wrong `UseSerilogRequestLogging` position.
**After:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseObservability();                           // configures Serilog from Observability section
builder.Services.AddObservability(builder.Configuration);  // DI: IAppLogger<T>, accessors, error catalog, masking
builder.Services.AddErrorHandling(builder.Configuration);  // DI: IErrorFactory + default catalog merge
builder.Services.AddHttpClient("core").AddObservabilityCorrelation();

var app = builder.Build();
app.UseObservability();     // CorrelationMw → GlobalExceptionMw → (routing/auth) → ContextEnrichmentMw → RequestLoggingMw
app.MapControllers();
app.Run();
```

### 13.2 Controller — remove try/catch
**Before (Loans/UnitCostController):**
```csharp
try { return new JsonResult(await _svc.GetConvertedUnitCostFromDTOAsync(dto)); }
catch (Exception ex) { _logger.LogError("..." + ex.ToString()); return new JsonResult(false); }
```
**After:**
```csharp
[HttpPost]
[BusinessOperation("Loans","UnitCost","ConvertFromDto")]
public async Task<IActionResult> GetConverted(UnitCostDto dto, CancellationToken ct)
    => Ok(await _svc.GetConvertedUnitCostFromDTOAsync(dto, ct));
```

### 13.3 Service — throw typed exceptions
**Before:**
```csharp
catch (Exception ex) { _logger.LogError(ex, "Error retrieving product type details"); throw; }
```
**After (only when service adds value):**
```csharp
if (!products.Any())
    throw _errorFactory.NotFound("ERP-FAS-NFD-0002", "No product types configured for this PACS.");
```
Let the middleware handle unexpected exceptions. Do not log+rethrow.

### 13.4 DTO — declare sensitivity
```csharp
public sealed class LoanApplicationRequest
{
    public string MemberId { get; init; } = default!;
    [Sensitive(SensitivityMode.Mask, keepLast: 4)] public string AadhaarNumber { get; init; } = default!;
    [DoNotLog]                                      public string AttachmentBase64 { get; init; } = default!;
    [Mask("\\d{6,}", replacement: "****")]          public string AccountNumber { get; init; } = default!;
}
```

---

## 14. Phased delivery plan

### Phase 0 — Scaffolding (0.5 week)
- Repo layout, `Directory.Build.props`, `Directory.Packages.props`, `NuGet.Config` (no in-repo secrets), `.editorconfig`, `build_push_script.sh` mirroring utils-messaging.
- Solution + empty projects with placeholder namespaces and target framework.
- CI skeleton: `dotnet build && dotnet test`.
- Acceptance: `dotnet build` green on empty solution with central versioning enforced.

### Phase 1 — Abstractions + ErrorHandling (1 week)
- Implement every contract in §4: interfaces, records, attributes, exception hierarchy.
- `ErrorResponse` + `FieldError` + `ErrorCatalogEntry`.
- Full unit tests (public surface, attribute scanning, equality, serialization).
- Error catalog YAML loader with schema validation.
- Acceptance: 100% unit coverage on abstractions + error handling; no hard dependency on Serilog/ASP.NET.

### Phase 2 — Core (1.5 weeks)
- `AppLogger<T>` over `ILogger<T>` + `BeginScope` via Serilog `LogContext`.
- Enrichers: Correlation, User, Tenant, Pacs, Module, Machine, Environment.
- `IRedactionEngine` with all three layers; benchmark 1M ops/s for string redaction.
- `UseObservability(this IHostBuilder)` (Serilog bootstrap), `AddObservability` (DI).
- Error catalog merge from YAML files.
- `ObservabilityOptions` + ValidateOnStart.
- Acceptance: unit + integration tests over masking, enrichment, sink wiring; `dotnet run` in `samples/SampleHost` emits valid compact JSON.

### Phase 3 — AspNetCore (1 week)
- Middlewares in §9. Filters. ProblemDetails adapter.
- Model-validation → `ValidationException` conversion.
- `UseObservability(this IApplicationBuilder)` registers in the documented order; idempotent (detects utils-Traceability and skips `CorrelationMiddleware`).
- Integration tests using `WebApplicationFactory`: correlation propagation, exception mapping per exception type, masked body logging.
- Acceptance: all FR acceptance criteria 1–9 demonstrated in integration tests.

### Phase 4 — Propagation (0.5 week)
- `CorrelationDelegatingHandler` + W3C traceparent format fix.
- `KafkaHeaders` static helpers.
- `TraceableBackgroundService` base class.
- Acceptance: HttpClient correctly propagates; unit tests confirm W3C format.

### Phase 5 — AuditHooks (0.5 week)
- `IAuditHook` + `LogOnly` impl + `AuditEvent` record.
- Acceptance: integration test emits well-formed audit events on a sample controller action tagged `[BusinessOperation]`.

### Phase 6 — Log4Net bridge (0.5 week)
- `SerilogForwardingAppender`; migration doc.
- Acceptance: integration test with a fake log4net ILog emits through Serilog with correlation id attached.

### Phase 7 — Integration shims (0.5 week)
- `Integrations.Traceability`: adapters for `ITraceContextAccessor`→utils-LAndE accessors, `IMaskingPolicy` delegation, `AuditHook→AuditActivityRecord`.
- `Integrations.Messaging`: `IProducerContextEnricher` that enriches envelope with correlation/user/tenant and fixes traceparent format.
- Acceptance: integration tests in `tests/Intellect.Erp.Observability.IntegrationTests/CrossUtility/*` with both utilities referenced.

### Phase 8 — Pilot retrofit: Loans (1 week)
- Replace Program.cs inline middlewares with `UseObservability`.
- Remove `try/catch`→`JsonResult(false)` from sampled controllers.
- Introduce typed exceptions via `IErrorFactory` in services.
- Add `config/error-catalog/loans.yaml` with real codes for top 30 flows.
- Confirm ELK search by `correlationId`, `errorCode`, `pacsId`, `stateCode`.
- Acceptance: BRD §22 criteria satisfied on Loans; no regression in 2-week UAT window.

### Phase 9 — Module rollout (4 weeks, parallelizable)
Per-module checklist:
1. Replace `Program.cs` boot → `UseObservability` + `AddObservability`.
2. Remove stale `TenantConnectionMiddleware` static caches; expose `ITenantContextAccessor` binding to their DB-resolution service.
3. Delete per-controller try/catch where they only log+rethrow or log+return false.
4. Normalize correlation header to `X-Correlation-Id`.
5. Publish module `error-catalog/<module>.yaml`.
6. Enable log4net bridge where legacy BL remains.
7. Mark PII DTOs with attributes.

Priority order based on observed pain and criticality: Loans ✅ → Savings → FAS → VoucherProcessing → Membership → Merchandise → UniteCommonAPI → AuditProcessing.

### Phase 10 — Hardening (ongoing)
- Roslyn analyzer `Intellect.Erp.Observability.Analyzers`: warn on `catch(Exception) { _logger.LogError(...); return ... }`, warn on `string.Concat` inside log calls, warn on `try/catch/rethrow`.
- ELK dashboards (saved searches + visualizations).
- OpenTelemetry bridge (`Serilog.Sinks.OpenTelemetry`) as an opt-in sink.

---

## 15. Testing strategy (NFR-Maintainability)

| Layer | Approach |
|---|---|
| Abstractions | xUnit + FluentAssertions; property tests (FsCheck) for `ErrorCatalogEntry` invariants. |
| Masking | Golden files + regex corpus (Aadhaar, PAN, mobile, accounts, tokens); 1M-ops/s micro-benchmark with BenchmarkDotNet; fuzz with random PII strings. |
| Middleware | `WebApplicationFactory<T>` integration tests covering: header normalization (spellings/casing), trace id from `Activity.Current`, ProblemDetails schema, exception mapping matrix. |
| Propagation | In-memory `HttpClient` factory + assertion of outbound headers; Kafka helpers with `IDictionary<string, byte[]>`. |
| Cross-utility | A test host that registers `AddTraceability` + `AddObservability`; verify no duplicated correlation, single audit envelope, correct masking delegation. |
| Contract | Snapshot tests over ELK JSON field set (`log.schema=v1` fields must not change names without major version bump). |
| Performance | Sink outage simulation (ES down): verify logs buffer to disk, business path unaffected, process does not OOM. |

---

## 16. Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Duplicated correlation id with utils-Traceability | High | Medium | Feature-detect Traceability middleware; `CorrelationMiddleware` becomes passthrough. |
| Two `ErrorCategory` enums (Traceability + Messaging + LAndE) diverging | High | Medium | Define the canonical one in `Intellect.Erp.Observability.Abstractions`; adapters map to/from the others. |
| Modules keep manual logging patterns | High | Medium | Analyzer warnings + pilot reference + code-review checklist. |
| ES outages break business flow | Low | High | Async sinks, disk buffer, circuit breaker, health endpoint `/health/observability`. |
| Body logging leaks PII | Medium | Critical | Disabled by default; whitelist-only; attribute-driven redaction; production guard refuses to enable body logging when `Environment=Production`. |
| Log4net → Serilog performance regression | Low | Medium | Benchmark appender; lock-free queue; drop-oldest on backpressure with metric. |
| Breaking change between versions | Medium | High | `[PublicAPI]` markers; semver strict; analyzer enforcing non-removal of public API. |
| NuGet.Config secrets in repo (as in utils-messaging) | High | Critical | Env-var-only PAT; documented in README; pre-commit hook blocks token patterns. |
| W3C traceparent format mismatch propagated from utils-messaging | High | Medium | `Integrations.Messaging` shim re-formats on publish. |
| Controllers that DO need try/catch for graceful degradation | Medium | Low | Keep the pattern, but extend `IErrorFactory` + typed exceptions so surfaces remain consistent. |

---

## 17. Acceptance criteria traceability

| BRD FR | Addressed by |
|---|---|
| FR-1 Correlation | §9.1 CorrelationMiddleware |
| FR-2 Request lifecycle logging | §9 RequestLoggingMiddleware |
| FR-3 Context enrichment | §6 + §9 ContextEnrichmentMiddleware |
| FR-4 Global exception handling | §9.1 GlobalExceptionMiddleware + §9.1 mapping table |
| FR-5 Typed exceptions | §4.5 hierarchy |
| FR-6 Error codes | §5 taxonomy |
| FR-7 Centralized catalog | §4.3 + §5 YAML catalog |
| FR-8 Response contract | §4.6 |
| FR-9 Validation integration | §9.2 ValidationResultFilter |
| FR-10 Masking | §7 three-layer redaction |
| FR-11 Annotations | §4.4 |
| FR-12 Business checkpoints | §4.1 `Checkpoint(…)` |
| FR-13 Propagation | §10 |
| FR-14 DI adoption | §13 one-line adoption |
| FR-15 Configuration | §8 |
| FR-16 Migration | §13, §14 Phase 8–9 |
| FR-17 Audit hooks | §11 |

Non-functional:
- Performance: §15 benchmarks + §16 sink outage.
- Availability: §16 ES outage mitigation.
- Security: §7.3 production guard + §4.6 Prod-safe response.
- Maintainability: §15 + Phase 10 analyzer.
- Extensibility: §3.1 dependency matrix + integration shims.
- Compliance: §7 mandatory masking + retention config.

---

## 18. Deliverables checklist

- [ ] Repo scaffolding + CI
- [ ] Abstractions library + public API locked
- [ ] ErrorHandling library + 10 canonical catalog entries
- [ ] Core library (Serilog + masking + enrichers + config)
- [ ] AspNetCore middleware + filters + ProblemDetails
- [ ] Propagation (HttpClient + Kafka helpers + job base)
- [ ] AuditHooks (LogOnly + Kafka + Traceability modes)
- [ ] Log4Net bridge
- [ ] Traceability integration shim
- [ ] Messaging integration shim
- [ ] Testing harness library
- [ ] Sample host
- [ ] Documentation set (`docs/adoption-guide.md`, `docs/error-catalog-authoring.md`, `docs/elk-field-reference.md`, `docs/migration-from-log4net.md`)
- [ ] Loans pilot retrofit
- [ ] Rollout playbook + per-module tickets
- [ ] Roslyn analyzer package

---

## 19. Execution backlog (sequenced tasks, atomic enough to work through function-by-function)

Each task below is sized so it does not time out during a single coding pass; this is deliberately granular per the engineering rule of thumb.

### Sprint 1 — Foundations (2 weeks)
1. Create solution, Directory props, NuGet config, gitignore, README skeleton.
2. Add empty `Intellect.Erp.Observability.Abstractions` project.
3. Implement `IAppLogger<T>` + `AppLoggerAdapter<T>` (wraps `ILogger<T>`).
4. Implement `ICorrelationContextAccessor`, `IUserContextAccessor`, `ITenantContextAccessor`, `IModuleContextAccessor` + default `HttpContext.Items`-backed implementations live in `AspNetCore` later.
5. Implement `IErrorFactory`, `IErrorCatalog`, `ErrorCatalogEntry`, `FieldError`, `ErrorResponse`, `ErrorSeverity`, `ErrorCategory`.
6. Implement attribute set in §4.4.
7. Implement `AppException` base + all 12 subclasses in §4.5.
8. Implement `DefaultErrorFactory` + tests.
9. Implement YAML catalog loader (`YamlDotNet`) + schema validation + `InMemoryErrorCatalog` + tests.

### Sprint 2 — Core
10. Implement `ObservabilityOptions` + `ErrorHandlingOptions` + DataAnnotations validators.
11. Implement enrichers: Correlation, User, Tenant, Pacs, Module.
12. Implement `IRedactionEngine` with three layers + unit tests.
13. Implement `AddObservability(IServiceCollection, IConfiguration)` + `UseObservability(IHostBuilder)` Serilog bootstrap with Console/File/ES sinks.
14. Implement catalog merge and `IErrorFactory.FromCatalog`.
15. Benchmarks for redaction hot path.

### Sprint 3 — AspNetCore
16. Implement `CorrelationMiddleware`.
17. Implement `GlobalExceptionMiddleware` with full mapping matrix.
18. Implement `ContextEnrichmentMiddleware`.
19. Implement `RequestLoggingMiddleware`.
20. Implement `BusinessOperationFilter` + `ValidationResultFilter`.
21. Implement HTTP-bound default accessors (`HttpContext.Items`-backed).
22. Implement `UseObservability(IApplicationBuilder)` with ordering guards + Traceability detection.
23. `WebApplicationFactory` integration tests.

### Sprint 4 — Propagation, Audit, Bridges
24. `CorrelationDelegatingHandler` + tests.
25. `KafkaHeaders` helpers + tests.
26. `TraceableBackgroundService` base.
27. `IAuditHook` + `LogOnly` impl + tests.
28. `SerilogForwardingAppender` (log4net bridge) + tests.
29. `Integrations.Traceability` shim.
30. `Integrations.Messaging` shim.
31. Documentation set.
32. Sample host.

### Sprint 5 — Loans pilot
33. Prepare Loans error catalog YAML (30 entries).
34. Refactor Program.cs (remove inline mw → `UseObservability`).
35. Refactor 3 reference controllers (remove try/catch).
36. Refactor 3 reference services (throw typed exceptions via factory).
37. Mark PII DTOs with attributes.
38. Smoke ELK queries for correlationId / errorCode.
39. UAT bake for 2 weeks.

### Sprint 6–8 — Rollout
40. Savings / FAS / Voucher / Membership / Merchandise / Unite / Audit (parallelized by module owner).
41. Roslyn analyzer package.
42. ELK dashboards + runbooks.

---

## 20. Open decisions requiring sign-off

1. **Package namespace:** `Intellect.Erp.Observability.*` (aligned with sibling utilities) vs BRD-suggested `Epcs.Utils.Observability.*`. **Recommendation:** Intellect.Erp.Observability.* for consistency; keep BRD suggestion in docs as rationale.
2. **Correlation header canonical casing:** `X-Correlation-Id` (utils-Traceability already uses this). Normalize inbound variants.
3. **ProblemDetails vs. custom envelope:** Both; custom envelope wraps ProblemDetails so legacy consumers and new tools both work. Config toggle decides which is returned.
4. **Audit mode default:** `LogOnly` in phase 1 so no module has a hard dependency on Traceability until they opt in.
5. **Version baseline:** `1.0.0`; aligns with `Directory.Build.props` pattern.
6. **Framework:** `net8.0` (matches every module sampled).
7. **NuGet source:** GitHub Packages using env-var PAT only; no secret committed (fixes utils-messaging issue).
8. **Target consumers first:** Loans pilot (richest existing patterns + already on Serilog) before FAS/Voucher (legacy log4net).

---

*End of plan. Proceed to Phase 0 scaffolding on sign-off.*
