using Intellect.Erp.Observability.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Intellect.Erp.Observability.IntegrationTests.TestWebApp;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("business-operation")]
    [BusinessOperation("Loans", "Disbursement", "Create")]
    public IActionResult BusinessOperationEndpoint()
    {
        return Ok(new { module = "Loans", feature = "Disbursement", operation = "Create" });
    }

    [HttpPost("validate")]
    public IActionResult ValidateEndpoint([FromBody] TestModel model)
    {
        return Ok(model);
    }
}

public class TestModel
{
    [Required(ErrorMessage = "Name is required")]
    public string? Name { get; set; }

    [Range(1, 100, ErrorMessage = "Age must be between 1 and 100")]
    public int Age { get; set; }
}
