# Error Catalog Authoring Guide

How to create and maintain per-module error catalog YAML files for the Observability Platform.

## Overview

The error catalog is a centralized registry of stable error codes. Each module maintains its own YAML file under `config/error-catalog/`. At startup, the platform loads all files listed in `ErrorHandling:CatalogFiles` and merges them into an in-memory catalog.

## YAML Schema

Each catalog file has a top-level `errors` array. Every entry requires these fields:

| Field            | Type    | Required | Description                                                |
|------------------|---------|----------|------------------------------------------------------------|
| `code`           | string  | Yes      | Unique error code in `ERP-<MODULE>-<CATEGORY>-<SEQ4>` format |
| `title`          | string  | Yes      | Short human-readable title                                 |
| `userMessage`    | string  | Yes      | Consumer-safe message returned in the API response         |
| `supportMessage` | string  | No       | Internal message for support engineers (not returned to clients) |
| `httpStatus`     | integer | Yes      | HTTP status code (e.g., 400, 404, 422, 500)               |
| `severity`       | string  | Yes      | One of: `Info`, `Warning`, `Error`, `Critical`             |
| `retryable`      | boolean | Yes      | Whether the client should retry the request                |
| `category`       | string  | Yes      | Error category (see below)                                 |

## Error Code Format

```
ERP-<MODULE>-<CATEGORY>-<SEQ4>
```

### MODULE Segment

The module identifier. Valid values:

| Code         | Module                |
|--------------|-----------------------|
| `CORE`       | Core / shared errors  |
| `LOANS`      | Loans module          |
| `SAVINGS`    | Savings & Deposits    |
| `MEMBERSHIP` | Membership            |
| `FAS`        | Financial Accounting  |
| `VOUCHER`    | Voucher Processing    |
| `MERCHANDISE`| Merchandise           |
| `AUDIT`      | Audit Processing      |
| `UNITE`      | Unite Common API      |

### CATEGORY Segment

A three-letter code mapping to `ErrorCategory`:

| Code  | ErrorCategory   | Typical HTTP Status |
|-------|-----------------|---------------------|
| `VAL` | Validation      | 400                 |
| `BIZ` | Business        | 422                 |
| `NFD` | NotFound        | 404                 |
| `CFL` | Conflict        | 409                 |
| `SEC` | Security        | 401 / 403           |
| `INT` | Integration     | 502                 |
| `DEP` | Dependency      | 503                 |
| `DAT` | Data            | 500                 |
| `CON` | Concurrency     | 409                 |
| `SYS` | System          | 500                 |

### SEQ4 Segment

A zero-padded four-digit sequence number (e.g., `0001`, `0042`). Sequence numbers are assigned per module per category and never reused.

### Examples

```
ERP-LOANS-VAL-0001    → Loans module, Validation, first entry
ERP-CORE-SYS-0001    → Core module, System, first entry
ERP-SAVINGS-BIZ-0003 → Savings module, Business, third entry
```

## Example Catalog File

```yaml
# config/error-catalog/loans.yaml
errors:
  - code: "ERP-LOANS-VAL-0001"
    title: "Invalid loan amount"
    userMessage: "Loan amount must be greater than zero."
    supportMessage: "Client submitted a loan disbursement request with amount <= 0."
    httpStatus: 400
    severity: "Warning"
    retryable: false
    category: "Validation"

  - code: "ERP-LOANS-NFD-0001"
    title: "Loan not found"
    userMessage: "The requested loan could not be found."
    supportMessage: "Loan lookup by ID returned no results."
    httpStatus: 404
    severity: "Warning"
    retryable: false
    category: "NotFound"

  - code: "ERP-LOANS-BIZ-0001"
    title: "Loan limit exceeded"
    userMessage: "The loan amount exceeds the maximum disbursement limit."
    supportMessage: "Loan amount exceeded the configured maximum for the member's tier."
    httpStatus: 422
    severity: "Warning"
    retryable: false
    category: "Business"

  - code: "ERP-LOANS-CFL-0001"
    title: "Loan already approved"
    userMessage: "This loan has already been approved and cannot be approved again."
    supportMessage: "Duplicate approval attempt — loan is in Approved state."
    httpStatus: 409
    severity: "Warning"
    retryable: false
    category: "Conflict"
```

## Validation Rules

The `YamlErrorCatalogLoader` validates each entry at startup:

1. `code` must match the regex `^ERP-[A-Z]+-[A-Z]{3}-\d{4}$`
2. The MODULE segment must be a recognized module name
3. The CATEGORY segment must be a valid three-letter category code
4. `title`, `userMessage`, `httpStatus`, `severity`, `retryable`, and `category` are required
5. `severity` must be one of `Info`, `Warning`, `Error`, `Critical`
6. `category` must be a valid `ErrorCategory` enum value
7. Duplicate error codes across files cause a warning log at startup

## Registration

Add your catalog file to `appsettings.json`:

```json
{
  "ErrorHandling": {
    "CatalogFiles": [
      "config/error-catalog/core.yaml",
      "config/error-catalog/loans.yaml"
    ]
  }
}
```

## Using Catalog Errors in Code

Inject `IErrorFactory` and call `FromCatalog`:

```csharp
// Throws an exception populated from the catalog entry
throw _errorFactory.FromCatalog("ERP-LOANS-VAL-0001");

// Override the user message
throw _errorFactory.FromCatalog("ERP-LOANS-VAL-0001", "Custom message for this context.");
```

If the error code is not found in the catalog, the factory falls back to `ERP-CORE-SYS-0001` and logs a warning.

## Best Practices

- Keep error codes stable. Once assigned, a code should not change meaning.
- Use `supportMessage` for internal troubleshooting details that should not be exposed to API consumers.
- Group related errors by category within the YAML file for readability.
- Start sequence numbers at `0001` for each module/category combination.
- Review the [core.yaml](../config/error-catalog/core.yaml) file for shared error definitions before creating module-specific duplicates.
