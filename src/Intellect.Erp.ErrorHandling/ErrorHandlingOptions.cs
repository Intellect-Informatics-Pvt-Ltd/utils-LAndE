using System.ComponentModel.DataAnnotations;

namespace Intellect.Erp.ErrorHandling;

/// <summary>
/// Configuration options for the error handling subsystem.
/// Binds to the <c>ErrorHandling</c> configuration section.
/// </summary>
public sealed class ErrorHandlingOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "ErrorHandling";

    /// <summary>
    /// Gets or sets a value indicating whether exception details (type, stack trace)
    /// should be included in error responses. Refused in Production environments.
    /// </summary>
    public bool IncludeExceptionDetailsInResponse { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether error responses should be formatted
    /// as RFC 7807 ProblemDetails.
    /// </summary>
    public bool ReturnProblemDetails { get; set; }

    /// <summary>
    /// Gets or sets the default error code used when no specific code is available.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string DefaultErrorCode { get; set; } = "ERP-CORE-SYS-0001";

    /// <summary>
    /// Gets or sets the paths to per-module error catalog YAML files.
    /// </summary>
    public string[] CatalogFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the base URI for RFC 7807 <c>type</c> field construction.
    /// The error code is appended to this base to form the full URI.
    /// </summary>
    public string ClientErrorUriBase { get; set; } = "https://errors.epacs.in/";
}
