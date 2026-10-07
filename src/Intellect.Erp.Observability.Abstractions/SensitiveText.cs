using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// What a log line or an HTTP response may say about infrastructure: addresses, logins and
/// connection-string values masked, a database named by its fingerprint, and an exception reduced to
/// what a caller may read.
/// </summary>
/// <remarks>
/// <para><b>WHY (2026-10-07).</b> Services logged their database's name, server address, port and
/// hostname at start-up, and returned <c>ex.Message</c> to callers from catch blocks and global
/// handlers. A MySQL message names the login and the client address (<c>Access denied for user
/// 'erp'@'10.0.4.7'</c>); an unreachable server is named by host and port. The estate's sweep found
/// over a thousand such responses. This is the shared form of the fix; the meta repo's
/// <c>build/check-log-leaks.py</c> keeps the class from coming back.</para>
/// <para>Dependency-free on purpose, so every library and module can call it.</para>
/// </remarks>
public static class SensitiveText
{
    /// <summary>The sentence a caller gets instead of a database driver's or a network client's own text.</summary>
    public const string InfrastructureFailure =
        "A database or service call failed. The service log has the detail (quote the correlation id).";

    private static readonly Regex UserAtHost = new(@"'[^'\s]{1,64}'@'[^'\s]{1,255}'", RegexOptions.Compiled);

    private static readonly Regex ConnectionValue = new(
        @"(?i)\b(server|host|data source|datasource|address|user id|userid|uid|user|password|pwd|database|initial catalog)\s*=\s*[^;'""]+",
        RegexOptions.Compiled);

    private static readonly Regex Ipv4 = new(@"\b(?:\d{1,3}\.){3}\d{1,3}(?::\d{1,5})?\b", RegexOptions.Compiled);

    private static readonly Regex UrlCredentials = new(@"(?i)\b([a-z][a-z0-9+.-]*://)[^/\s:@]+:[^/\s@]+@", RegexOptions.Compiled);

    /// <summary>
    /// <paramref name="text"/> with MySQL <c>'user'@'host'</c> pairs, connection-string values, IPv4
    /// addresses (with their port) and credentials inside URLs masked. Everything else is unchanged.
    /// </summary>
    /// <param name="text">The text to mask; null gives an empty string.</param>
    /// <returns>The masked text.</returns>
    public static string Mask(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;
        var s = UserAtHost.Replace(text, "'***'@'***'");
        s = ConnectionValue.Replace(s, m => m.Groups[1].Value + "=***");
        s = Ipv4.Replace(s, "[address]");
        s = UrlCredentials.Replace(s, m => m.Groups[1].Value + "***@");
        return s;
    }

    /// <summary>
    /// What <paramref name="exception"/> may say in an HTTP response. With <paramref name="includeDetail"/>
    /// (Development) the raw message. Otherwise: an exception from a database driver, a socket, an HTTP
    /// client, a timeout or I/O - anywhere in the InnerException chain - becomes
    /// <see cref="InfrastructureFailure"/>; any other message (the service's own words) is returned
    /// through <see cref="Mask"/>.
    /// </summary>
    /// <param name="exception">The exception; null gives an empty string.</param>
    /// <param name="includeDetail">True only in Development.</param>
    /// <returns>The text for the response.</returns>
    public static string ForClient(Exception? exception, bool includeDetail = false)
    {
        if (exception == null)
            return string.Empty;
        if (includeDetail)
            return exception.Message;
        for (var e = exception; e != null; e = e.InnerException)
        {
            if (IsInfrastructure(e))
                return InfrastructureFailure;
        }
        return Mask(exception.Message);
    }

    /// <summary>
    /// Whether <paramref name="exception"/> was thrown by infrastructure (a database driver, a socket, an
    /// HTTP client, Redis, Kafka, a timeout, I/O) rather than by the service's own code.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>True for infrastructure.</returns>
    public static bool IsInfrastructure(Exception exception)
    {
        if (exception is System.Data.Common.DbException or System.Net.Sockets.SocketException
            or System.Net.Http.HttpRequestException or TimeoutException or IOException)
            return true;
        var name = exception.GetType().FullName ?? string.Empty;
        return name.StartsWith("MySql", StringComparison.Ordinal)
               || name.StartsWith("StackExchange.Redis", StringComparison.Ordinal)
               || name.StartsWith("Confluent.Kafka", StringComparison.Ordinal)
               || name.StartsWith("Npgsql", StringComparison.Ordinal);
    }

    /// <summary>
    /// A stable, non-reversible name for a database: <c>db#</c> + the first 10 hex characters of SHA-256
    /// over <c>host:port/name</c> (host lower-cased, port defaulting to 3306). The same scheme as
    /// Intellect.Utils.Caching's STATE-IDENTITY report and <c>ops/l2r2 db fingerprint</c>.
    /// </summary>
    /// <param name="server"><c>host</c> or <c>host:port</c>.</param>
    /// <param name="database">The database (schema) name.</param>
    /// <returns>The fingerprint, e.g. <c>db#45ccdba4d1</c>.</returns>
    public static string DatabaseFingerprint(string? server, string? database)
    {
        var s = (server ?? string.Empty).Trim().ToLowerInvariant();
        var host = s;
        var port = "3306";
        var colon = s.LastIndexOf(':');
        if (colon > 0 && s.IndexOf(':') == colon && int.TryParse(s[(colon + 1)..], out _))
        {
            host = s[..colon];
            port = s[(colon + 1)..];
        }
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{host}:{port}/{(database ?? string.Empty).Trim()}"));
        return "db#" + Convert.ToHexString(hash, 0, 5).ToLowerInvariant();
    }
}
