using FluentAssertions;
using Intellect.Erp.RequestResponseLogging.Helpers;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.RequestResponseLogging;

/// <summary>
/// The payload logger masks identity and secret fields whatever a service configures (2026-10-07). Every module
/// configured the same five names, matched exactly, so <c>AadharNo</c>, <c>MobileNo</c>, <c>otp</c> and the tenant
/// connection string <c>connStr1</c> went into the request/response log.
/// </summary>
public class PayloadMaskingTests
{
    private static readonly string[] TheFiveEveryModuleConfigured = { "password", "token", "authorization", "aadhaar", "pan" };

    [Fact]
    public void Identity_and_secret_fields_are_masked_beyond_the_configured_five()
    {
        var masked = PayloadMaskingHelper.MaskJson(
            """{"AadharNo":"123412341234","member":{"MobileNo":"9876543210","otp":"4321"},"connStr1":"U2VydmVyPWRiMQ==","name":"x"}""",
            TheFiveEveryModuleConfigured);
        masked.Should().NotContain("123412341234").And.NotContain("9876543210").And.NotContain("4321")
            .And.NotContain("U2VydmVyPWRiMQ==").And.Contain("\"name\":\"x\"");
    }

    [Fact]
    public void A_configured_field_is_still_masked()
        => PayloadMaskingHelper.MaskJson("""{"customField":"v"}""", new[] { "customField" }).Should().NotContain("\"v\"");
}
