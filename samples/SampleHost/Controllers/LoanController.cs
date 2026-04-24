using Intellect.Erp.ErrorHandling.Exceptions;
using Intellect.Erp.Observability.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace SampleHost.Controllers;

/// <summary>
/// Sample controller demonstrating [BusinessOperation] annotations,
/// typed exception throwing, and IAppLogger usage.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LoanController : ControllerBase
{
    private readonly IAppLogger<LoanController> _logger;
    private readonly IErrorFactory _errorFactory;

    public LoanController(IAppLogger<LoanController> logger, IErrorFactory errorFactory)
    {
        _logger = logger;
        _errorFactory = errorFactory;
    }

    /// <summary>
    /// Creates a new loan disbursement.
    /// Demonstrates [BusinessOperation] annotation and typed exception throwing.
    /// </summary>
    [HttpPost("disburse")]
    [BusinessOperation("Loans", "Disbursement", "Create")]
    public IActionResult Disburse([FromBody] LoanDisbursementRequest request)
    {
        // Validation — throws ValidationException (HTTP 400)
        if (request.Amount <= 0)
        {
            throw _errorFactory.Validation(
                "Loan amount must be greater than zero.",
                [new FieldError("Amount", "amount-positive", "Must be greater than 0")]);
        }

        // Not found — throws NotFoundException (HTTP 404)
        if (request.MemberId == "UNKNOWN")
        {
            throw _errorFactory.NotFound("Member not found.");
        }

        // Business rule — throws BusinessRuleException (HTTP 422)
        if (request.Amount > 1_000_000)
        {
            throw _errorFactory.BusinessRule("Loan amount exceeds maximum disbursement limit.");
        }

        // Checkpoint logging for business process tracking
        _logger.Checkpoint("LoanDisbursementAccepted", new Dictionary<string, object?>
        {
            ["MemberId"] = request.MemberId,
            ["Amount"] = request.Amount,
        });

        return Ok(new { success = true, loanId = Guid.NewGuid().ToString("N")[..12] });
    }

    /// <summary>
    /// Retrieves a loan by ID.
    /// Demonstrates NotFoundException usage.
    /// </summary>
    [HttpGet("{loanId}")]
    [BusinessOperation("Loans", "Inquiry", "GetById")]
    public IActionResult GetById(string loanId)
    {
        // Simulate not found
        if (loanId == "000000000000")
        {
            throw _errorFactory.NotFound($"Loan '{loanId}' not found.");
        }

        _logger.Information("Loan {LoanId} retrieved successfully", loanId);

        return Ok(new { loanId, status = "Active", amount = 50000m });
    }

    /// <summary>
    /// Approves a loan.
    /// Demonstrates BusinessRuleException and Conflict scenarios.
    /// </summary>
    [HttpPost("{loanId}/approve")]
    [BusinessOperation("Loans", "Approval", "Approve")]
    public IActionResult Approve(string loanId)
    {
        // Simulate conflict
        if (loanId == "ALREADY_APPROVED")
        {
            throw _errorFactory.Conflict("Loan has already been approved.");
        }

        _logger.Checkpoint("LoanApproved", new Dictionary<string, object?>
        {
            ["LoanId"] = loanId,
        });

        return Ok(new { success = true, loanId, status = "Approved" });
    }
}
