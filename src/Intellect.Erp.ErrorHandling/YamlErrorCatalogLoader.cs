using System.Text.RegularExpressions;
using Intellect.Erp.Observability.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Intellect.Erp.ErrorHandling;

/// <summary>
/// Loads error catalog entries from YAML files with schema validation.
/// </summary>
public static class YamlErrorCatalogLoader
{
    private static readonly Regex ErrorCodeRegex = new(
        @"^ERP-[A-Z]+-[A-Z]{3}-\d{4}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Loads error catalog entries from a YAML file at the specified path.
    /// </summary>
    /// <param name="filePath">The path to the YAML file.</param>
    /// <returns>A read-only list of validated error catalog entries.</returns>
    /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the YAML schema is invalid.</exception>
    public static IReadOnlyList<ErrorCatalogEntry> Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must not be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Error catalog file not found: {filePath}", filePath);

        using var reader = new StreamReader(filePath);
        return Load(reader);
    }

    /// <summary>
    /// Loads error catalog entries from a <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">The text reader containing YAML content.</param>
    /// <returns>A read-only list of validated error catalog entries.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the reader is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the YAML schema is invalid.</exception>
    public static IReadOnlyList<ErrorCatalogEntry> Load(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var document = deserializer.Deserialize<YamlDocument>(reader);

        if (document?.Errors is null || document.Errors.Count == 0)
            return [];

        var entries = new List<ErrorCatalogEntry>(document.Errors.Count);
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in document.Errors)
        {
            Validate(raw);

            if (!seenCodes.Add(raw.Code!))
                throw new InvalidOperationException($"Duplicate error code: {raw.Code}");

            var severity = Enum.Parse<ErrorSeverity>(raw.Severity!, ignoreCase: true);
            var category = Enum.Parse<ErrorCategory>(raw.Category!, ignoreCase: true);

            entries.Add(new ErrorCatalogEntry(
                Code: raw.Code!,
                Title: raw.Title!,
                UserMessage: raw.UserMessage!,
                SupportMessage: raw.SupportMessage ?? string.Empty,
                HttpStatus: raw.HttpStatus!.Value,
                Severity: severity,
                Retryable: raw.Retryable!.Value,
                Category: category));
        }

        return entries.AsReadOnly();
    }

    private static void Validate(YamlErrorEntry raw)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(raw.Code)) missing.Add("code");
        if (string.IsNullOrWhiteSpace(raw.Title)) missing.Add("title");
        if (string.IsNullOrWhiteSpace(raw.UserMessage)) missing.Add("userMessage");
        if (raw.HttpStatus is null) missing.Add("httpStatus");
        if (string.IsNullOrWhiteSpace(raw.Severity)) missing.Add("severity");
        if (raw.Retryable is null) missing.Add("retryable");
        if (string.IsNullOrWhiteSpace(raw.Category)) missing.Add("category");

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Error catalog entry is missing required fields: {string.Join(", ", missing)}");

        if (!ErrorCodeRegex.IsMatch(raw.Code!))
            throw new InvalidOperationException(
                $"Error code '{raw.Code}' does not match the required format: ERP-MODULE-CAT-NNNN");

        if (!Enum.TryParse<ErrorSeverity>(raw.Severity, ignoreCase: true, out _))
            throw new InvalidOperationException(
                $"Invalid severity value '{raw.Severity}' for error code '{raw.Code}'.");

        if (!Enum.TryParse<ErrorCategory>(raw.Category, ignoreCase: true, out _))
            throw new InvalidOperationException(
                $"Invalid category value '{raw.Category}' for error code '{raw.Code}'.");
    }

    /// <summary>YAML document root.</summary>
    private sealed class YamlDocument
    {
        public List<YamlErrorEntry> Errors { get; set; } = [];
    }

    /// <summary>Raw YAML error entry before validation.</summary>
    private sealed class YamlErrorEntry
    {
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? UserMessage { get; set; }
        public string? SupportMessage { get; set; }
        public int? HttpStatus { get; set; }
        public string? Severity { get; set; }
        public bool? Retryable { get; set; }
        public string? Category { get; set; }
    }
}
