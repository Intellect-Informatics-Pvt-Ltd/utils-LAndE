using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Intellect.Erp.Observability.Abstractions;
using Microsoft.Extensions.Options;

namespace Intellect.Erp.Observability.Core;

/// <summary>
/// Default implementation of <see cref="IRedactionEngine"/> applying a three-layer
/// masking pipeline: structural path policies, attribute-driven reflection, and regex fallback.
/// All operations work on shallow copies; original objects are never mutated.
/// </summary>
public sealed class DefaultRedactionEngine : IRedactionEngine
{
    private const string RedactedPlaceholder = "***REDACTED***";

    private readonly ConcurrentDictionary<Type, TypeMaskingPlan> _planCache = new();
    private readonly IReadOnlyList<string> _structuralPaths;
    private readonly IReadOnlyList<CompiledPattern> _regexPatterns;

    public DefaultRedactionEngine(IOptions<ObservabilityOptions> options)
    {
        var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _structuralPaths = opts.Masking?.Paths ?? Array.Empty<string>();
        _regexPatterns = BuildRegexPatterns(opts.Masking?.Regexes);
    }

    /// <inheritdoc />
    public string Redact(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = value;
        foreach (var pattern in _regexPatterns)
        {
            result = pattern.Regex.Replace(result, pattern.Replacement);
        }
        return result;
    }

    /// <inheritdoc />
    public JsonElement RedactJson(JsonElement element)
    {
        if (_structuralPaths.Count == 0)
            return element;

        var json = element.GetRawText();
        using var doc = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteRedactedElement(writer, doc.RootElement, "$");
        }

        stream.Position = 0;
        using var resultDoc = JsonDocument.Parse(stream);
        return resultDoc.RootElement.Clone();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> RedactProperties(IReadOnlyDictionary<string, object?> properties)
    {
        var result = new Dictionary<string, object?>(properties.Count);
        foreach (var kvp in properties)
        {
            if (kvp.Value is string strVal)
            {
                result[kvp.Key] = Redact(strVal);
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        return result;
    }

    /// <inheritdoc />
    public object RedactObject(object obj, Type? type = null)
    {
        var targetType = type ?? obj.GetType();
        var plan = _planCache.GetOrAdd(targetType, BuildMaskingPlan);

        // Create a shallow copy via MemberwiseClone or property copy
        var copy = CreateShallowCopy(obj, targetType, plan);
        ApplyMaskingPlan(copy, plan);
        return copy;
    }

    #region Structural Path Masking

    private void WriteRedactedElement(Utf8JsonWriter writer, JsonElement element, string currentPath)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    var propPath = $"{currentPath}.{prop.Name}";
                    writer.WritePropertyName(prop.Name);
                    if (ShouldMaskPath(propPath))
                    {
                        writer.WriteStringValue(RedactedPlaceholder);
                    }
                    else
                    {
                        WriteRedactedElement(writer, prop.Value, propPath);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var itemPath = $"{currentPath}[{index}]";
                    WriteRedactedElement(writer, item, itemPath);
                    index++;
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private bool ShouldMaskPath(string path)
    {
        foreach (var configuredPath in _structuralPaths)
        {
            if (PathMatches(configuredPath, path))
                return true;
        }
        return false;
    }

    private static bool PathMatches(string pattern, string path)
    {
        // Simple path matching: exact match or wildcard suffix
        if (string.Equals(pattern, path, StringComparison.OrdinalIgnoreCase))
            return true;

        // Support wildcard patterns like $.body.*
        if (pattern.EndsWith(".*", StringComparison.Ordinal))
        {
            var prefix = pattern[..^2];
            return path.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, path, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    #endregion

    #region Attribute-Driven Masking

    private static TypeMaskingPlan BuildMaskingPlan(Type type)
    {
        var entries = new List<PropertyMaskEntry>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            var doNotLog = prop.GetCustomAttribute<DoNotLogAttribute>();
            if (doNotLog is not null)
            {
                entries.Add(new PropertyMaskEntry(prop, MaskAction.Exclude, null, null, null));
                continue;
            }

            var sensitive = prop.GetCustomAttribute<SensitiveAttribute>();
            if (sensitive is not null)
            {
                entries.Add(new PropertyMaskEntry(prop, MaskAction.Sensitive, sensitive.Mode, sensitive.KeepLast, null));
                continue;
            }

            var mask = prop.GetCustomAttribute<MaskAttribute>();
            if (mask is not null)
            {
                var regex = new Regex(mask.Regex, RegexOptions.Compiled);
                entries.Add(new PropertyMaskEntry(prop, MaskAction.Mask, null, null, new CompiledPattern(regex, mask.Replacement)));
                continue;
            }
        }

        return new TypeMaskingPlan(type, entries);
    }

    private object CreateShallowCopy(object original, Type type, TypeMaskingPlan plan)
    {
        // Use Activator to create a new instance and copy all readable/writable properties
        var copy = Activator.CreateInstance(type)!;
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(original);
                prop.SetValue(copy, value);
            }
        }

        return copy;
    }

    private void ApplyMaskingPlan(object copy, TypeMaskingPlan plan)
    {
        foreach (var entry in plan.Entries)
        {
            switch (entry.Action)
            {
                case MaskAction.Exclude:
                    // Set to default value (null for reference types)
                    entry.Property.SetValue(copy, GetDefaultValue(entry.Property.PropertyType));
                    break;

                case MaskAction.Sensitive:
                    var sensitiveValue = entry.Property.GetValue(copy);
                    if (sensitiveValue is string strVal && !string.IsNullOrEmpty(strVal))
                    {
                        var masked = entry.Mode switch
                        {
                            SensitivityMode.Redact => RedactedPlaceholder,
                            SensitivityMode.Hash => Convert.ToBase64String(
                                System.Security.Cryptography.SHA256.HashData(
                                    System.Text.Encoding.UTF8.GetBytes(strVal))),
                            _ => MaskWithKeepLast(strVal, entry.KeepLast ?? 4)
                        };
                        entry.Property.SetValue(copy, masked);
                    }
                    break;

                case MaskAction.Mask:
                    var maskValue = entry.Property.GetValue(copy);
                    if (maskValue is string maskStr && entry.Pattern is not null)
                    {
                        entry.Property.SetValue(copy, entry.Pattern.Regex.Replace(maskStr, entry.Pattern.Replacement));
                    }
                    break;
            }
        }

        // Apply regex fallback to all string properties
        var properties = plan.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (!prop.CanRead || !prop.CanWrite || prop.PropertyType != typeof(string))
                continue;

            // Skip properties already handled by attribute masking
            if (plan.Entries.Any(e => e.Property == prop))
                continue;

            var value = prop.GetValue(copy) as string;
            if (!string.IsNullOrEmpty(value))
            {
                prop.SetValue(copy, Redact(value));
            }
        }
    }

    private static string MaskWithKeepLast(string value, int keepLast)
    {
        if (keepLast >= value.Length)
            return value;

        var maskLength = value.Length - keepLast;
        return new string('*', maskLength) + value[^keepLast..];
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    #endregion

    #region Regex Patterns

    private static IReadOnlyList<CompiledPattern> BuildRegexPatterns(string[]? configRegexes)
    {
        var patterns = new List<CompiledPattern>();

        // Built-in default patterns
        patterns.AddRange(GetBuiltInPatterns());

        // Config-driven patterns (format: "pattern|replacement")
        if (configRegexes is not null)
        {
            foreach (var entry in configRegexes)
            {
                var parts = entry.Split('|', 2);
                if (parts.Length == 2)
                {
                    patterns.Add(new CompiledPattern(
                        new Regex(parts[0], RegexOptions.Compiled),
                        parts[1]));
                }
            }
        }

        return patterns;
    }

    private static IEnumerable<CompiledPattern> GetBuiltInPatterns()
    {
        // Aadhaar: 12 digits (with optional spaces/hyphens)
        yield return new CompiledPattern(
            new Regex(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled),
            "****-****-****");

        // PAN: 5 alpha + 4 digits + 1 alpha
        yield return new CompiledPattern(
            new Regex(@"\b[A-Z]{5}\d{4}[A-Z]\b", RegexOptions.Compiled),
            "***PAN***");

        // Indian mobile: 10 digits starting with 6-9
        yield return new CompiledPattern(
            new Regex(@"\b[6-9]\d{9}\b", RegexOptions.Compiled),
            "***MOBILE***");

        // Account numbers: 10-20 digits
        yield return new CompiledPattern(
            new Regex(@"\b\d{10,20}\b", RegexOptions.Compiled),
            "***ACCOUNT***");

        // IFSC: 11 characters (4 alpha + 0 + 6 alphanumeric)
        yield return new CompiledPattern(
            new Regex(@"\b[A-Z]{4}0[A-Z0-9]{6}\b", RegexOptions.Compiled),
            "***IFSC***");

        // Email
        yield return new CompiledPattern(
            new Regex(@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled),
            "***EMAIL***");

        // JWT / Bearer tokens
        yield return new CompiledPattern(
            new Regex(@"(?i)(bearer\s+)[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+", RegexOptions.Compiled),
            "$1***TOKEN***");

        // Connection strings with password/pwd
        yield return new CompiledPattern(
            new Regex(@"(?i)(password|pwd)\s*=\s*[^;]+", RegexOptions.Compiled),
            "$1=***");
    }

    #endregion

    internal sealed record CompiledPattern(Regex Regex, string Replacement);

    private enum MaskAction { Exclude, Sensitive, Mask }

    private sealed record PropertyMaskEntry(
        PropertyInfo Property,
        MaskAction Action,
        SensitivityMode? Mode,
        int? KeepLast,
        CompiledPattern? Pattern);

    private sealed record TypeMaskingPlan(Type Type, IReadOnlyList<PropertyMaskEntry> Entries);
}
