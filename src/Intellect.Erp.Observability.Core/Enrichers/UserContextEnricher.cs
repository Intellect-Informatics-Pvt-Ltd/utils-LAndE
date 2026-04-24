using Intellect.Erp.Observability.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace Intellect.Erp.Observability.Core.Enrichers;

/// <summary>
/// Enriches log events with user context (userId, userName, role) from
/// <see cref="IUserContextAccessor"/>. Values are masked through <see cref="IRedactionEngine"/>.
/// </summary>
public sealed class UserContextEnricher : ILogEventEnricher
{
    private static long _errorCount;

    private readonly IUserContextAccessor _accessor;
    private readonly IRedactionEngine _redactionEngine;

    public UserContextEnricher(IUserContextAccessor accessor, IRedactionEngine redactionEngine)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _redactionEngine = redactionEngine ?? throw new ArgumentNullException(nameof(redactionEngine));
    }

    /// <summary>Gets the total number of enrichment errors encountered.</summary>
    public static long ErrorCount => Interlocked.Read(ref _errorCount);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            var userId = _accessor.UserId;
            if (userId is not null)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("userId", _redactionEngine.Redact(userId)));
            }

            var userName = _accessor.UserName;
            if (userName is not null)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("userName", _redactionEngine.Redact(userName)));
            }

            var role = _accessor.Role;
            if (role is not null)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("role", _redactionEngine.Redact(role)));
            }
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
        }
    }
}
