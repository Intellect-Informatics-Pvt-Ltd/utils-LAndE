using Intellect.Erp.Observability.Abstractions;

namespace SampleHost.Controllers;

/// <summary>
/// Sample DTO demonstrating PII-annotated properties.
/// </summary>
public sealed class LoanDisbursementRequest
{
    /// <summary>The member identifier.</summary>
    public string MemberId { get; init; } = default!;

    /// <summary>The loan amount to disburse.</summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Aadhaar number — masked with last 4 digits visible.
    /// </summary>
    [Sensitive(keepLast: 4)]
    public string? AadhaarNumber { get; init; }

    /// <summary>
    /// PAN card number — masked with last 4 characters visible.
    /// </summary>
    [Sensitive(SensitivityMode.Mask, keepLast: 4)]
    public string? PanNumber { get; init; }

    /// <summary>
    /// Account number — custom regex masking for digit sequences.
    /// </summary>
    [Mask(@"\d{6,}", "***")]
    public string? AccountNumber { get; init; }

    /// <summary>
    /// Base64-encoded attachment — completely excluded from logs.
    /// </summary>
    [DoNotLog]
    public string? AttachmentBase64 { get; init; }
}
