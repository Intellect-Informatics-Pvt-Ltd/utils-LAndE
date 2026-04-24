# Migration from log4net to Serilog

Guide for bridging legacy log4net output into the Serilog pipeline using the `SerilogForwardingAppender`.

## Overview

The `Intellect.Erp.Observability.Log4NetBridge` package provides a `SerilogForwardingAppender` that routes log4net output into the Serilog pipeline. This means legacy modules using `ILog` get full enrichment (correlation ID, user context, tenant context, masking) and unified ELK visibility without rewriting logging calls.

## Step 1: Add the Bridge Package

Add a reference to the Log4Net Bridge package:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Intellect.Erp.Observability.Log4NetBridge\Intellect.Erp.Observability.Log4NetBridge.csproj" />
</ItemGroup>
```

Or when published as a NuGet package:

```xml
<ItemGroup>
  <PackageReference Include="Intellect.Erp.Observability.Log4NetBridge" />
</ItemGroup>
```

## Step 2: Configure the Appender

Add the `SerilogForwardingAppender` to your log4net configuration (typically `log4net.config` or within `app.config`):

```xml
<log4net>
  <appender name="SerilogForwarder" type="Intellect.Erp.Observability.Log4NetBridge.SerilogForwardingAppender, Intellect.Erp.Observability.Log4NetBridge">
    <!-- Optional: maximum queue capacity before dropping oldest entries -->
    <queueCapacity value="10000" />
  </appender>

  <root>
    <level value="DEBUG" />
    <appender-ref ref="SerilogForwarder" />
  </root>
</log4net>
```

You can keep existing appenders alongside the forwarder during the transition period:

```xml
<root>
  <level value="DEBUG" />
  <appender-ref ref="SerilogForwarder" />
  <appender-ref ref="ExistingFileAppender" />  <!-- Keep during transition -->
</root>
```

## Step 3: Ensure Observability Platform Is Bootstrapped

The Serilog pipeline must be configured before log4net events are forwarded. Make sure your `Program.cs` includes the standard observability setup:

```csharp
builder.Host.UseObservability();
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddObservabilityAccessors();
builder.Services.AddErrorHandling(builder.Configuration);
```

## What Happens Under the Hood

1. Legacy code calls `ILog.Info("Processing loan {0}", loanId)`
2. log4net routes the event to `SerilogForwardingAppender`
3. The appender maps the log4net level to a Serilog level:
   - `DEBUG` → `Debug`
   - `INFO` → `Information`
   - `WARN` → `Warning`
   - `ERROR` → `Error`
   - `FATAL` → `Fatal`
4. The event is queued in a lock-free `ConcurrentQueue`
5. A background thread drains the queue into the Serilog pipeline
6. All configured Serilog enrichers apply (correlation, user, tenant, module, masking)
7. The enriched event flows to all configured sinks (Console, File, Elasticsearch)

## Backpressure Handling

The appender uses a bounded queue to prevent memory issues:

- Default capacity: 10,000 events
- When the queue is full, the oldest entries are dropped
- A telemetry counter `observability.log4net.dropped` tracks dropped events
- Monitor this counter in production to detect logging bottlenecks

## What to Expect

After adding the bridge:

- All log4net output appears in your Serilog sinks (Console, File, Elasticsearch)
- Log entries include correlation IDs, user context, and tenant context
- Sensitive data in log messages is masked by the redaction engine
- You can search legacy log output in Kibana alongside new structured logs

## Gradual Migration Path

1. **Phase 1 — Bridge:** Add the `SerilogForwardingAppender`. All existing `ILog` calls flow through Serilog. No code changes needed.

2. **Phase 2 — New code uses IAppLogger:** Write new code using `IAppLogger<T>` with structured message templates and checkpoints.

3. **Phase 3 — Migrate hot paths:** Replace high-value `ILog` calls with `IAppLogger<T>` for better structured logging (named properties instead of positional `{0}`).

4. **Phase 4 — Remove log4net:** Once all `ILog` usage is migrated, remove the log4net dependency and the bridge package.

## Differences Between log4net and IAppLogger

| Feature                  | log4net `ILog`                    | `IAppLogger<T>`                        |
|--------------------------|-----------------------------------|----------------------------------------|
| Message format           | Positional `{0}`, `{1}`          | Named `{LoanId}`, `{MemberId}`        |
| Structured properties    | Not supported                     | Automatic from template                |
| Business checkpoints     | Not supported                     | `Checkpoint("name", data)`             |
| Scoped operations        | Not supported                     | `BeginOperation(module, feature, op)`  |
| Context enrichment       | Manual via `ThreadContext`        | Automatic via enrichers                |
| PII masking              | Not supported                     | Automatic via `[Sensitive]`, `[DoNotLog]` |
