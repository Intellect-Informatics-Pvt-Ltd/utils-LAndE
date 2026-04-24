# Requirements Document

## Introduction

This document defines the requirements for **utils-LAndE** — a comprehensive .NET 8 observability and error handling platform for the ePACS ERP system (NABARD programme). The platform is delivered as a family of NuGet packages (`Intellect.Erp.Observability.*` and `Intellect.Erp.ErrorHandling`) that standardize structured logging, correlation propagation, global exception handling, error cataloging, sensitive-field masking, and audit-hook extensibility across all ePACS modules.

The platform composes with existing sibling utilities (`Intellect.Erp.Traceability` and `Intellect.Erp.Messaging`) through optional integration shims, and targets one-line adoption for consuming modules such as l3_Loans, l3_savingsDeposit, l3_FAS, l3_voucherProcessing, l3_membership, l3_merchandise, l3_uniteCommonAPI, and l3_auditProcessing.

## Glossary

- **Observability_Platform**: The complete set of NuGet packages (`Intellect.Erp.Observability.*` and `Intellect.Erp.ErrorHandling`) that provide structured logging, error handling, masking, correlation, and audit capabilities.
- **AppLogger**: The application-level structured logger (`IAppLogger<T>`) that wraps `ILogger<T>` and provides business checkpoint logging, scoped operations, and canonical context enrichment.
- **Correlation_ID**: A ULID-26 identifier that uniquely traces a request across HTTP, Kafka, and background job boundaries. Propagated via the `X-Correlation-Id` header.
- **Error_Catalog**: A centralized registry of stable error codes loaded from per-module YAML files, keyed by the format `ERP-<MODULE>-<CATEGORY>-<SEQ4>`.
- **Error_Response**: The standardized JSON error envelope returned to API consumers, compatible with RFC 7807 ProblemDetails plus ePACS extensions.
- **Redaction_Engine**: The component (`IRedactionEngine`) responsible for masking and redacting sensitive data through three layers: structural path policies, attribute-driven reflection, and regex fallback patterns.
- **Error_Factory**: The DI-resolvable service (`IErrorFactory`) that creates typed `AppException` instances from error codes and catalog entries.
- **Context_Accessor**: A family of DI-resolvable interfaces (`ICorrelationContextAccessor`, `IUserContextAccessor`, `ITenantContextAccessor`, `IModuleContextAccessor`) that provide canonical request context for enrichment.
- **Middleware_Pipeline**: The ordered set of ASP.NET Core middlewares registered by `UseObservability()`: Correlation, GlobalException, ContextEnrichment, and RequestLogging.
- **Audit_Hook**: The extensibility interface (`IAuditHook`) for emitting structured audit events in LogOnly, TraceabilityBridge, or Kafka modes.
- **Log4Net_Bridge**: The `SerilogForwardingAppender` that routes legacy log4net output into the Serilog pipeline with full enrichment.
- **Traceability_Shim**: The optional integration package (`Intellect.Erp.Observability.Integrations.Traceability`) that bridges `ITraceContextAccessor` and `IMaskingPolicy` from the sibling Traceability utility.
- **Messaging_Shim**: The optional integration package (`Intellect.Erp.Observability.Integrations.Messaging`) that enriches Kafka event envelopes with correlation and context fields.
- **Consumer_Module**: Any ePACS ERP module (e.g., l3_Loans, l3_FAS) that adopts the Observability_Platform.
- **ELK_Stack**: The Elasticsearch-Logstash-Kibana infrastructure that ingests and indexes structured log output.
- **Canonical_Field_Set**: The stable set of JSON field names (schema v1) emitted in every structured log entry for ELK indexing.
- **Business_Operation**: A controller action or service method annotated with `[BusinessOperation]` to declare module, feature, and operation context.
- **Sensitive_Attribute**: The `[Sensitive]` annotation applied to DTO properties to trigger automatic masking during log enrichment.
- **AppException**: The abstract base exception class carrying ErrorCode, Category, Severity, Retryable flag, and CorrelationId snapshot.

## Requirements

### Requirement 1: Correlation ID Generation and Propagation

**User Story:** As an operations engineer, I want every request to carry a unique correlation ID across HTTP, Kafka, and background job boundaries, so that I can trace a complete transaction through distributed logs.

#### Acceptance Criteria

1. WHEN an inbound HTTP request contains an `X-Correlation-Id` or `X-Correlation-ID` header, THE Middleware_Pipeline SHALL use the provided value as the Correlation_ID for that request.
2. WHEN an inbound HTTP request does not contain a correlation header, THE Middleware_Pipeline SHALL generate a new ULID-26 value as the Correlation_ID.
3. THE Middleware_Pipeline SHALL store the Correlation_ID in `HttpContext.Items["CorrelationId"]` and push it into the Serilog `LogContext` scope for the duration of the request.
4. THE Middleware_Pipeline SHALL echo the Correlation_ID in the `X-Correlation-Id` response header on every HTTP response.
5. WHEN an outbound HTTP request is made through an `HttpClient` registered with `AddObservabilityCorrelation()`, THE CorrelationDelegatingHandler SHALL set the `X-Correlation-Id` header to the current Correlation_ID.
6. WHEN an outbound HTTP request is made through an `HttpClient` registered with `AddObservabilityCorrelation()` and an `Activity.Current` exists, THE CorrelationDelegatingHandler SHALL set the `traceparent` header in W3C format (`00-{traceId}-{spanId}-{flags}`).
7. WHEN Kafka message headers are written using `KafkaHeaders.WriteCorrelation()`, THE Propagation package SHALL include the current Correlation_ID, CausationId, TraceParent, TenantId, and UserId in the header dictionary.
8. WHEN a `TraceableBackgroundService` executes, THE Observability_Platform SHALL establish a scoped Correlation_ID derived from the job identifier or caller context.
9. WHILE the `Intellect.Erp.Traceability` middleware is detected in the pipeline, THE CorrelationMiddleware SHALL act as a no-op passthrough to avoid duplicate correlation generation.

### Requirement 2: Structured Logging with Canonical Context

**User Story:** As a developer, I want a structured logger that automatically enriches every log entry with canonical context fields, so that I can search and filter logs in ELK without manual field population.

#### Acceptance Criteria

1. THE AppLogger SHALL implement `IAppLogger<T>` with Debug, Information, Warning, Error, and Critical log methods that delegate to `ILogger<T>`.
2. THE AppLogger SHALL provide a `BeginScope` method that accepts an `IReadOnlyDictionary<string, object?>` and pushes all key-value pairs into the Serilog `LogContext`.
3. THE AppLogger SHALL provide a `BeginOperation` method that pushes module, feature, operation, and optional extra context into the Serilog `LogContext` as a disposable scope.
4. THE AppLogger SHALL provide a `Checkpoint` method that emits a structured log entry at Information level with a named checkpoint and optional data dictionary.
5. WHEN a log entry is emitted, THE Observability_Platform SHALL include the Canonical_Field_Set: `@timestamp`, `level`, `app`, `env`, `machine`, `module`, `correlationId`, and `log.schema` (value `v1`).
6. WHEN a log entry is emitted during an authenticated HTTP request, THE Observability_Platform SHALL include `userId`, `userName`, `role`, `tenantId`, `stateCode`, `pacsId`, and `branchCode` fields from the Context_Accessors.
7. WHEN a log entry is emitted during an HTTP request, THE Observability_Platform SHALL include `httpMethod`, `path`, `route`, `status`, and `durationMs` fields.
8. WHEN a controller action is annotated with `[BusinessOperation]`, THE BusinessOperationFilter SHALL push `module`, `feature`, and `operation` fields into the log scope for that action.

### Requirement 3: Global Exception Handling and Error Response

**User Story:** As an API consumer, I want all errors returned in a consistent, consumer-safe JSON format with a stable error code and correlation ID, so that I can programmatically handle errors without parsing unstructured messages.

#### Acceptance Criteria

1. WHEN an unhandled exception occurs during HTTP request processing, THE GlobalExceptionMiddleware SHALL catch the exception and return an Error_Response JSON body with the appropriate HTTP status code.
2. WHEN a `ValidationException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 400 with `ErrorCategory` Validation and `retryable` false, including the `fieldErrors` array.
3. WHEN a `BusinessRuleException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 422 with `ErrorCategory` Business and `retryable` false.
4. WHEN a `NotFoundException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 404 with `ErrorCategory` NotFound and `retryable` false.
5. WHEN a `ConflictException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 409 with `ErrorCategory` Conflict and `retryable` false.
6. WHEN an `UnauthorizedException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 401 with `ErrorCategory` Security and `retryable` false.
7. WHEN a `ForbiddenException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 403 with `ErrorCategory` Security and `retryable` false.
8. WHEN a `ConcurrencyException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 409 with `ErrorCategory` Concurrency and `retryable` true.
9. WHEN a `DataIntegrityException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 500 with `ErrorCategory` Data and `retryable` false.
10. WHEN an `IntegrationException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 502 with `ErrorCategory` Integration and the `retryable` flag from the exception.
11. WHEN a `DependencyException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 503 with `ErrorCategory` Dependency and `retryable` true.
12. WHEN a `TaskCanceledException` or `OperationCanceledException` is caught, THE GlobalExceptionMiddleware SHALL return HTTP 499 with `ErrorCategory` System and `retryable` true.
13. WHEN an unrecognized exception is caught, THE GlobalExceptionMiddleware SHALL return HTTP 500 with `ErrorCategory` System, `retryable` false, and the default error code `ERP-CORE-SYS-0001`.
14. THE Error_Response SHALL always include `success`, `errorCode`, `title`, `message`, `correlationId`, `status`, `severity`, `retryable`, and `timestamp` fields.
15. WHILE the `ErrorHandling:IncludeExceptionDetailsInResponse` setting is true and the environment is not Production, THE Error_Response SHALL include `exceptionType` and `stackTrace` fields.
16. IF the `ErrorHandling:IncludeExceptionDetailsInResponse` setting is true and the environment is Production, THEN THE Observability_Platform SHALL refuse to activate exception detail inclusion and log a warning.
17. THE GlobalExceptionMiddleware SHALL emit a single structured Error-level log entry for each caught exception, including the Correlation_ID and error code.
18. WHEN a `FluentValidation.ValidationException` is caught, THE GlobalExceptionMiddleware SHALL convert it to a `ValidationException` with `FieldError[]` and return HTTP 400.
19. WHEN `ModelState.IsValid` is false on a controller action, THE ValidationResultFilter SHALL convert the model state errors into a `ValidationException` with `FieldError[]`.

### Requirement 4: Typed Exception Hierarchy

**User Story:** As a service developer, I want a typed exception hierarchy with stable error codes and categories, so that I can throw domain-specific exceptions and let the middleware handle HTTP mapping consistently.

#### Acceptance Criteria

1. THE Observability_Platform SHALL provide an abstract `AppException` base class carrying `ErrorCode` (string), `Category` (ErrorCategory), `Severity` (ErrorSeverity), `Retryable` (bool), and `CorrelationId` (string) properties.
2. THE Observability_Platform SHALL provide concrete exception subclasses: `ValidationException` (with `FieldError[]`), `BusinessRuleException`, `NotFoundException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`, `IntegrationException`, `DependencyException`, `DataIntegrityException`, `ConcurrencyException`, `ExternalSystemException`, and `SystemException`.
3. THE `BusinessRuleException` SHALL implement the `IDomainPolicyRejectionException` interface from the Traceability utility to enable audit outcome mapping.
4. WHEN a `ConcurrencyException` wraps a saga-scoped operation, THE `ConcurrencyException` SHALL implement the `ISagaCompensationException` interface from the Traceability utility.

### Requirement 5: Centralized Error Catalog

**User Story:** As a support engineer, I want every error to have a stable, documented error code from a centralized catalog, so that I can look up resolution steps without reading source code.

#### Acceptance Criteria

1. THE Error_Catalog SHALL load error definitions from per-module YAML files specified in the `ErrorHandling:CatalogFiles` configuration array.
2. THE Error_Catalog SHALL validate each YAML entry against the schema: `code` (matching `ERP-<MODULE>-<CATEGORY>-<SEQ4>` format), `title`, `userMessage`, `httpStatus`, `severity`, `retryable`, and `category` are required fields.
3. WHEN the Error_Factory `FromCatalog` method is called with a known error code, THE Error_Factory SHALL create an `AppException` with properties populated from the catalog entry.
4. IF the Error_Factory `FromCatalog` method is called with an unknown error code, THEN THE Error_Factory SHALL fall back to `ERP-CORE-SYS-0001` and emit a Warning-level log entry and increment a telemetry counter.
5. THE Error_Catalog SHALL expose an `All` property returning all loaded `ErrorCatalogEntry` records and a `TryGet` method for code-based lookup.
6. THE error code format SHALL follow the pattern `ERP-<MODULE>-<CATEGORY>-<SEQ4>` where MODULE is one of CORE, LOANS, SAVINGS, MEMBERSHIP, FAS, VOUCHER, MERCHANDISE, AUDIT, UNITE; CATEGORY is one of VAL, BIZ, NFD, CFL, SEC, INT, DEP, DAT, CON, SYS; and SEQ4 is a zero-padded four-digit sequence number.

### Requirement 6: Sensitive Field Masking and Redaction

**User Story:** As a security officer, I want all PII and sensitive data automatically masked before it reaches any log sink, so that production logs comply with data protection requirements.

#### Acceptance Criteria

1. THE Redaction_Engine SHALL apply three masking layers in order: structural path policies, attribute-driven reflection, and regex fallback patterns.
2. WHEN a field path matches a pattern in the `Observability:Masking:Paths` configuration (e.g., `$.body.password`, `$.headers.authorization`), THE Redaction_Engine SHALL mask the field value before logging.
3. WHEN a DTO property is annotated with `[Sensitive]`, THE Redaction_Engine SHALL mask the property value, retaining only the last N characters as specified by the `keepLast` parameter.
4. WHEN a DTO property is annotated with `[DoNotLog]`, THE Redaction_Engine SHALL completely exclude the property value from log output.
5. WHEN a DTO property is annotated with `[Mask(regex, replacement)]`, THE Redaction_Engine SHALL apply the specified regex pattern and replace matches with the replacement string.
6. THE Redaction_Engine SHALL apply default regex patterns for: Aadhaar numbers (12-digit), PAN (5+4+1 alphanumeric), Indian mobile numbers, account numbers (10-20 digits), IFSC codes (11-char), email addresses, JWT/Bearer tokens, and connection strings containing password or pwd fields.
7. THE Redaction_Engine SHALL cache attribute reflection results per type to achieve zero-reflection overhead on hot paths.
8. THE Redaction_Engine SHALL operate on shallow copies of data and never mutate caller DTOs.
9. WHILE the `Observability:Masking:Enabled` configuration is false and the environment is Production, THE Observability_Platform SHALL refuse to disable masking and log a warning.
10. WHILE `Observability:Masking:UseTraceabilityPolicy` is true and `IMaskingPolicy` from the Traceability utility is resolvable, THE Redaction_Engine SHALL delegate to the Traceability masking policy via adapter rather than running a duplicate pass.

### Requirement 7: Request and Response Body Logging

**User Story:** As a developer debugging integration issues, I want to optionally capture request and response bodies in logs with automatic PII masking, so that I can diagnose payload-level problems without exposing sensitive data.

#### Acceptance Criteria

1. THE RequestLoggingMiddleware SHALL not capture request or response bodies by default (`CaptureRequestBody` and `CaptureResponseBody` default to false).
2. WHILE `Observability:RequestLogging:CaptureRequestBody` is true, THE RequestLoggingMiddleware SHALL capture and log the request body only for routes listed in the `BodyWhitelist` configuration.
3. WHEN a request or response body is captured, THE RequestLoggingMiddleware SHALL pass the body content through the Redaction_Engine before logging.
4. WHEN a request duration exceeds the `Observability:RequestLogging:SlowRequestThresholdMs` threshold, THE RequestLoggingMiddleware SHALL emit the log entry at Warning level instead of Information level.
5. THE RequestLoggingMiddleware SHALL exclude paths listed in `Observability:RequestLogging:ExcludePaths` (e.g., `/health`, `/metrics`, `/swagger`) from request logging.

### Requirement 8: Configuration and Bootstrapping

**User Story:** As a module developer, I want to adopt the observability platform with a single configuration section and one-line setup calls, so that I can standardize logging and error handling without writing boilerplate middleware code.

#### Acceptance Criteria

1. THE Observability_Platform SHALL provide three extension methods for adoption: `UseObservability()` on `IHostBuilder` for Serilog bootstrap, `AddObservability(IConfiguration)` on `IServiceCollection` for DI registration, and `UseObservability()` on `IApplicationBuilder` for middleware pipeline registration.
2. THE Observability_Platform SHALL read all settings from the `Observability` and `ErrorHandling` configuration sections.
3. WHEN `AddObservability` is called with `ValidateOnStart()`, THE Observability_Platform SHALL validate that `ApplicationName` is non-empty, `ModuleName` is non-empty, Elasticsearch URL is parseable when the ES sink is enabled, catalog files exist on disk, and `Masking:Enabled` is true when the environment is Production.
4. THE Observability_Platform SHALL support per-namespace minimum log level overrides via the `Observability:ModuleOverrides` dictionary.
5. THE Observability_Platform SHALL configure Serilog sinks based on the `Observability:Sinks` section: Console (compact or template format), File (rolling daily JSON lines), and Elasticsearch (with index format `{app}-{env}-{yyyy.MM}` and disk buffer path).
6. WHEN `UseObservability()` is called on `IApplicationBuilder`, THE Middleware_Pipeline SHALL register middlewares in the exact order: CorrelationMiddleware, GlobalExceptionMiddleware, ContextEnrichmentMiddleware, RequestLoggingMiddleware.
7. THE Observability_Platform SHALL provide a separate `AddErrorHandling(IConfiguration)` extension method that registers the Error_Factory, loads the Error_Catalog from YAML files, and configures the Error_Response serializer.

### Requirement 9: Annotation-Driven Adoption

**User Story:** As a developer, I want to annotate controller actions and DTO properties with attributes to declare business operations, error codes, and sensitivity levels, so that the platform automatically enriches logs and masks data without manual coding.

#### Acceptance Criteria

1. THE Observability_Platform SHALL provide a `[BusinessOperation(module, feature, operation)]` attribute applicable to methods and classes.
2. THE Observability_Platform SHALL provide a `[Sensitive(mode, keepLast)]` attribute applicable to properties, fields, and parameters, with `SensitivityMode.Mask` as the default mode and `keepLast` defaulting to 4.
3. THE Observability_Platform SHALL provide a `[DoNotLog]` attribute applicable to properties, fields, and parameters.
4. THE Observability_Platform SHALL provide a `[Mask(regex, replacement)]` attribute applicable to properties and fields, with `replacement` defaulting to `"***"`.
5. THE Observability_Platform SHALL provide an `[ErrorCode(code)]` attribute applicable to methods for declaring the default error code for an action.
6. WHEN a controller action is annotated with `[BusinessOperation]`, THE BusinessOperationFilter SHALL automatically push the declared module, feature, and operation into the log scope without requiring manual `BeginOperation` calls.

### Requirement 10: Audit Hook Extensibility

**User Story:** As a compliance officer, I want the platform to emit structured audit events for business operations, so that financial traceability requirements can be met without each module implementing its own audit logic.

#### Acceptance Criteria

1. THE Observability_Platform SHALL provide an `IAuditHook` interface with an `EmitAsync(AuditEvent, CancellationToken)` method.
2. THE `AuditEvent` record SHALL include: EventId, CorrelationId, Module, Feature, Operation, Actor, TenantId, PacsId, EntityType, EntityId, Outcome, ErrorCode, Data dictionary, and OccurredAt timestamp.
3. WHILE the `Observability:AuditHook:Mode` is `LogOnly`, THE Audit_Hook SHALL write the audit event as a structured Serilog log entry at Information level with an `audit.v1=true` tag.
4. WHILE the `Observability:AuditHook:Mode` is `TraceabilityBridge` and the Traceability_Shim is referenced, THE Audit_Hook SHALL route audit events to `AuditActivityRecord` via the Traceability `ITraceSink`.
5. WHILE the `Observability:AuditHook:Mode` is `Kafka` and the Messaging_Shim is referenced, THE Audit_Hook SHALL publish audit events to the topic specified in `Observability:AuditHook:Topic` via `IKafkaProducer`.

### Requirement 11: Log4Net Bridge for Legacy Modules

**User Story:** As a developer maintaining a legacy module that uses log4net, I want log4net output to flow into the Serilog pipeline with full enrichment, so that I can get unified ELK visibility without rewriting all logging calls.

#### Acceptance Criteria

1. THE Log4Net_Bridge SHALL provide a `SerilogForwardingAppender` that extends log4net `AppenderSkeleton`.
2. WHEN a log4net `ILog` emits a log event, THE SerilogForwardingAppender SHALL forward the event to the Serilog pipeline with the original log level, message, and exception preserved.
3. WHEN a log event is forwarded through the SerilogForwardingAppender, THE Serilog pipeline SHALL apply all configured enrichers (correlation, user, tenant, module) and masking to the forwarded event.
4. THE SerilogForwardingAppender SHALL use a lock-free queue internally and drop oldest entries on backpressure, incrementing a telemetry counter for dropped events.

### Requirement 12: Traceability Integration Shim

**User Story:** As a module developer using both Traceability and Observability utilities, I want the two utilities to share correlation context, masking policies, and audit pipelines without duplication, so that I get a unified experience.

#### Acceptance Criteria

1. WHEN the Traceability_Shim is referenced and `ITraceContextAccessor` is resolvable from DI, THE Context_Accessors SHALL delegate to `ITraceContextAccessor` for UserId, TenantId, StateCode, CorrelationId, and BranchCode.
2. WHEN the Traceability_Shim is referenced and `IMaskingPolicy` is resolvable from DI, THE Redaction_Engine SHALL delegate structural masking to the Traceability `IMaskingPolicy` via adapter.
3. WHEN the Traceability middleware is detected in the pipeline (via a marker on `IApplicationBuilder.Properties`), THE CorrelationMiddleware SHALL skip its own correlation generation and act as a passthrough.
4. THE Traceability_Shim SHALL adapt `AuditEvent` to `AuditActivityRecord` when the audit mode is `TraceabilityBridge`.

### Requirement 13: Messaging Integration Shim

**User Story:** As a module developer using both Messaging and Observability utilities, I want Kafka event envelopes automatically enriched with correlation and context fields in the correct W3C format, so that distributed traces are consistent.

#### Acceptance Criteria

1. WHEN the Messaging_Shim is referenced, THE Messaging_Shim SHALL register an `IProducerContextEnricher` that enriches the Messaging utility's `EventEnvelope` with Correlation_ID, CausationId, UserId, and TenantId.
2. WHEN the Messaging_Shim enriches an `EventEnvelope`, THE Messaging_Shim SHALL format the `traceparent` header in W3C format (`00-{traceId}-{spanId}-{flags}`) rather than using `Activity.Current.Id` directly.

### Requirement 14: Error Response Contract

**User Story:** As a frontend developer, I want error responses to follow a documented JSON schema with stable field names, so that I can build reliable error handling in the UI.

#### Acceptance Criteria

1. THE Error_Response SHALL include the fields: `success` (bool, always false), `errorCode` (string), `title` (string), `message` (string), `correlationId` (string), `traceId` (string, when OpenTelemetry is active), `status` (int), `severity` (string), `retryable` (bool), `timestamp` (ISO-8601 UTC string).
2. WHEN the caught exception is a `ValidationException`, THE Error_Response SHALL include a `fieldErrors` array where each entry contains `field`, `code`, and `message` strings.
3. THE Error_Response SHALL include a `type` field formatted as `{ErrorHandling:ClientErrorUriBase}{errorCode}` for RFC 7807 compatibility.
4. WHEN `ErrorHandling:ReturnProblemDetails` is true, THE Error_Response SHALL be compatible with the RFC 7807 ProblemDetails schema (including `type`, `title`, `status` fields).
5. THE Error_Response serializer SHALL suppress `exceptionType`, `stackTrace`, and `supportReference` fields when the environment is Production and `IncludeExceptionDetailsInResponse` is false.

### Requirement 15: Resilient Logging Pipeline

**User Story:** As an operations engineer, I want the logging pipeline to never break business request processing, even when log sinks are unavailable, so that observability failures do not cause production outages.

#### Acceptance Criteria

1. IF an enricher throws an exception during log processing, THEN THE Observability_Platform SHALL swallow the exception and increment a telemetry counter.
2. IF the Elasticsearch sink is unavailable, THEN THE Observability_Platform SHALL buffer log entries to the disk path specified in `Observability:Sinks:Elasticsearch:BufferPath` and retry delivery.
3. THE Observability_Platform SHALL expose a health check endpoint at the path specified in `Observability:Telemetry:HealthCheckPath` (default `/health/observability`) reporting sink connectivity status.
4. THE Observability_Platform SHALL register zero static singletons that hold state; all abstractions SHALL be resolvable through `IServiceProvider`.

### Requirement 16: Background Job Observability

**User Story:** As a developer writing background services, I want correlation context and structured logging available in background jobs, so that job execution is traceable in the same ELK pipeline as HTTP requests.

#### Acceptance Criteria

1. THE Observability_Platform SHALL provide a `TraceableBackgroundService` abstract base class extending `BackgroundService`.
2. WHEN a `TraceableBackgroundService` executes, THE base class SHALL establish a scoped Correlation_ID and open an enrichment scope with module and operation context.
3. WHEN a `TraceableBackgroundService` execution fails, THE base class SHALL log the exception at Error level with the scoped Correlation_ID and continue operation.

### Requirement 17: Solution Scaffolding and Build Standards

**User Story:** As a platform developer, I want the solution to follow .NET 8 best practices with central package versioning, nullable enabled, and warnings-as-errors, so that the codebase maintains consistent quality standards.

#### Acceptance Criteria

1. THE solution SHALL target `net8.0` with nullable reference types enabled, warnings treated as errors, and latest language version.
2. THE solution SHALL use `Directory.Build.props` for shared build properties and `Directory.Packages.props` for centralized NuGet version pinning.
3. THE solution SHALL not commit NuGet authentication secrets to the repository; GitHub Packages PAT SHALL be provided via environment variable only.
4. THE Observability_Platform SHALL mark all public API members with `[PublicAPI]` markers; breaking changes SHALL require a major version bump.

### Requirement 18: Testing Infrastructure

**User Story:** As a platform developer, I want a testing harness library with fakes, sinks, and assertion helpers, so that consuming modules can write integration tests against the observability platform without requiring real ELK infrastructure.

#### Acceptance Criteria

1. THE Observability_Platform SHALL provide an `Intellect.Erp.Observability.Testing` package containing fake implementations of `IAppLogger<T>`, `ICorrelationContextAccessor`, `IErrorFactory`, and in-memory log sinks.
2. THE Observability_Platform SHALL achieve complete test coverage on middleware, masking, and error mapping components using xUnit and FluentAssertions.
3. THE Observability_Platform SHALL include property-based tests (FsCheck) for `ErrorCatalogEntry` invariants and correlation propagation round-trips.
4. THE Observability_Platform SHALL include golden-file snapshot tests for the ELK JSON Canonical_Field_Set to prevent unintentional field name changes across versions.
