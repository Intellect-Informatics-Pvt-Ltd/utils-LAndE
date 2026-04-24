using Intellect.Erp.Observability.AspNetCore;
using Intellect.Erp.Observability.Core;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog bootstrap from Observability config
builder.Host.UseObservability();

// 2. Register observability services (IAppLogger<T>, IRedactionEngine, enrichers)
builder.Services.AddObservability(builder.Configuration);

// 3. Register HttpContext-backed context accessors
builder.Services.AddObservabilityAccessors();

// 4. Register error handling (IErrorFactory, IErrorCatalog from YAML)
builder.Services.AddErrorHandling(builder.Configuration);

// 5. Register controllers
builder.Services.AddControllers();

var app = builder.Build();

// 6. Register observability middleware pipeline
//    (Correlation → GlobalException → ContextEnrichment → RequestLogging)
app.UseObservability();

app.UseRouting();
app.MapControllers();

app.Run();
