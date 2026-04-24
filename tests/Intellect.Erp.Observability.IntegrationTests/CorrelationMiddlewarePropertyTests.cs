using System.Text.RegularExpressions;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.IntegrationTests.Fixtures;

namespace Intellect.Erp.Observability.IntegrationTests;

/// <summary>
/// Property-based integration tests for CorrelationMiddleware.
/// </summary>
public class CorrelationMiddlewarePropertyTests : IClassFixture<ObservabilityWebApplicationFactory>
{
    private readonly ObservabilityWebApplicationFactory _factory;

    // ULID-26: 26 characters, Crockford Base32 alphabet [0-9A-HJKMNP-TV-Z]
    private static readonly Regex UlidRegex = new(@"^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.Compiled);

    public CorrelationMiddlewarePropertyTests(ObservabilityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// **Validates: Requirements 1.1, 1.4**
    /// Property 1: Correlation ID round-trip — for any valid correlation ID as inbound header,
    /// the response header echoes the exact same value.
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = new[] { typeof(UlidArbitrary) })]
    public Property CorrelationId_RoundTrip_EchoesExactValue(UlidString ulidString)
    {
        return Prop.ForAll(Arb.From<bool>(), _ =>
        {
            var client = _factory.CreateClient();
            var correlationId = ulidString.Value;
            var request = new HttpRequestMessage(HttpMethod.Get, "/test/ok");
            request.Headers.Add("X-Correlation-Id", correlationId);

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
            values!.First().Should().Be(correlationId, "the response header must echo the exact inbound correlation ID");
        });
    }

    /// <summary>
    /// **Validates: Requirements 1.2**
    /// Property 2: ULID format invariant — for any request without correlation header,
    /// the generated ID matches ULID-26 format (26 chars, Crockford Base32).
    /// </summary>
    [Property(MaxTest = 50)]
    public Property NoCorrelationHeader_GeneratedId_MatchesUlid26Format(PositiveInt _seed)
    {
        return Prop.ForAll(Arb.From<bool>(), _ =>
        {
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/test/ok");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
            var generatedId = values!.First();
            generatedId.Should().HaveLength(26, "ULID-26 must be exactly 26 characters");
            UlidRegex.IsMatch(generatedId).Should().BeTrue(
                $"generated ID '{generatedId}' must match Crockford Base32 ULID-26 format");
        });
    }
}

/// <summary>
/// Wrapper type for FsCheck-generated ULID-like strings.
/// </summary>
public class UlidString
{
    public string Value { get; }
    public UlidString(string value) => Value = value;
    public override string ToString() => Value;
}

/// <summary>
/// FsCheck arbitrary that generates random ULID-like strings (26 chars, Crockford Base32).
/// </summary>
public static class UlidArbitrary
{
    private const string CrockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static Arbitrary<UlidString> UlidStringArbitrary()
    {
        var gen = Gen.ArrayOf(26, Gen.Elements(CrockfordBase32.ToCharArray()))
            .Select(chars => new UlidString(new string(chars)));
        return Arb.From(gen);
    }
}
