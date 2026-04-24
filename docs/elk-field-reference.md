# ELK Field Reference — Canonical Field Set (Schema v1)

Reference for all canonical fields emitted by the Observability Platform in structured log entries. These fields are indexed by the ELK stack and available for Kibana dashboards and queries.

## Schema Version

All log entries include `log.schema: "v1"`. This version identifier ensures forward compatibility — field names in v1 will not change without a major version bump.

## Canonical Fields

### Core Fields

| Field            | Type     | Source              | Description                                              |
|------------------|----------|---------------------|----------------------------------------------------------|
| `@timestamp`     | datetime | Serilog             | UTC timestamp of the log event (ISO-8601)                |
| `level`          | string   | Serilog             | Log level: `Debug`, `Information`, `Warning`, `Error`, `Critical` |
| `messageTemplate`| string   | Serilog             | The structured message template                          |
| `message`        | string   | Serilog             | The rendered message                                     |
| `exception`      | string   | Serilog             | Exception details (when present)                         |
| `log.schema`     | string   | SchemaVersionEnricher | Schema version identifier, always `"v1"`               |

### Application Context

| Field            | Type     | Source                       | Description                                    |
|------------------|----------|------------------------------|------------------------------------------------|
| `app`            | string   | ModuleContextEnricher        | Application name from `Observability:ApplicationName` |
| `env`            | string   | ModuleContextEnricher        | Deployment environment (e.g., `Development`, `Production`) |
| `machine`        | string   | MachineEnricher              | Machine/host name from `Environment.MachineName` |
| `module`         | string   | ModuleContextEnricher        | Module name from `Observability:ModuleName`    |

### Correlation and Tracing

| Field            | Type     | Source                          | Description                                  |
|------------------|----------|---------------------------------|----------------------------------------------|
| `correlationId`  | string   | CorrelationEnricher             | ULID-26 correlation ID for request tracing   |
| `causationId`    | string   | CorrelationEnricher             | ID of the event that caused this operation   |
| `traceParent`    | string   | CorrelationEnricher             | W3C traceparent header value                 |

### User Context (post-authentication)

| Field            | Type     | Source                          | Description                                  |
|------------------|----------|---------------------------------|----------------------------------------------|
| `userId`         | string   | UserContextEnricher             | Authenticated user identifier                |
| `userName`       | string   | UserContextEnricher             | Authenticated user display name (redacted)   |
| `role`           | string   | UserContextEnricher             | User role                                    |

### Tenant Context

| Field            | Type     | Source                          | Description                                  |
|------------------|----------|---------------------------------|----------------------------------------------|
| `tenantId`       | string   | TenantContextEnricher           | Tenant identifier                            |
| `stateCode`      | string   | TenantContextEnricher           | State code for the PACS                      |
| `pacsId`         | string   | TenantContextEnricher           | PACS (Primary Agricultural Credit Society) ID|
| `branchCode`     | string   | TenantContextEnricher           | Branch code                                  |

### Business Operation (from `[BusinessOperation]` attribute)

| Field            | Type     | Source                          | Description                                  |
|------------------|----------|---------------------------------|----------------------------------------------|
| `feature`        | string   | BusinessOperationFilter         | Feature name (e.g., `Disbursement`)          |
| `operation`      | string   | BusinessOperationFilter         | Operation name (e.g., `Create`)              |

### HTTP Request (from RequestLoggingMiddleware)

| Field            | Type     | Source                          | Description                                  |
|------------------|----------|---------------------------------|----------------------------------------------|
| `httpMethod`     | string   | RequestLoggingMiddleware        | HTTP method (GET, POST, etc.)                |
| `path`           | string   | RequestLoggingMiddleware        | Request path                                 |
| `route`          | string   | RequestLoggingMiddleware        | Matched route template                       |
| `status`         | integer  | RequestLoggingMiddleware        | HTTP response status code                    |
| `durationMs`     | double   | RequestLoggingMiddleware        | Request duration in milliseconds             |

### Checkpoint (from `IAppLogger.Checkpoint`)

| Field            | Type     | Source                          | Description                                  |
|------------------|----------|---------------------------------|----------------------------------------------|
| `checkpoint`     | string   | AppLogger.Checkpoint            | Named checkpoint (e.g., `PaymentInitiated`)  |

### Audit (from `IAuditHook`)

| Field            | Type     | Source                          | Description                                  |
|------------------|----------|---------------------------------|----------------------------------------------|
| `audit.v1`       | boolean  | LogOnlyAuditHook               | Audit event marker, always `true`            |
| `audit.eventId`  | string   | LogOnlyAuditHook               | Unique audit event identifier                |
| `audit.entityType`| string  | LogOnlyAuditHook               | Entity type (e.g., `Loan`, `Member`)         |
| `audit.entityId` | string   | LogOnlyAuditHook               | Entity identifier                            |
| `audit.outcome`  | string   | LogOnlyAuditHook               | Outcome: `Success`, `Failure`, `Rejected`    |

## Example JSON Log Entry

A typical structured log entry as it appears in Elasticsearch:

```json
{
  "@timestamp": "2024-06-15T10:23:45.123Z",
  "level": "Information",
  "messageTemplate": "Loan {LoanId} disbursement accepted for member {MemberId}",
  "message": "Loan LN-00042 disbursement accepted for member MBR-1234",
  "log.schema": "v1",
  "app": "epacs-loans",
  "env": "Production",
  "machine": "worker-node-03",
  "module": "Loans",
  "correlationId": "01HZX5PQ8WZ4A5C2M8YT4F3M6V",
  "userId": "usr-9876",
  "userName": "J*** D***",
  "role": "LoanOfficer",
  "tenantId": "tenant-001",
  "stateCode": "MH",
  "pacsId": "PACS-0042",
  "branchCode": "BR-001",
  "feature": "Disbursement",
  "operation": "Create",
  "checkpoint": "LoanDisbursementAccepted",
  "LoanId": "LN-00042",
  "MemberId": "MBR-1234"
}
```

## Kibana Query Examples

```
# Find all errors for a specific correlation ID
correlationId: "01HZX5PQ8WZ4A5C2M8YT4F3M6V" AND level: "Error"

# Find slow requests in the Loans module
module: "Loans" AND durationMs: >3000

# Find all audit events for a specific entity
audit.v1: true AND audit.entityType: "Loan" AND audit.entityId: "LN-00042"

# Find all checkpoints in a business flow
correlationId: "01HZX5PQ8WZ4A5C2M8YT4F3M6V" AND checkpoint: *

# Find all validation failures
level: "Warning" AND message: "Validation*"
```
