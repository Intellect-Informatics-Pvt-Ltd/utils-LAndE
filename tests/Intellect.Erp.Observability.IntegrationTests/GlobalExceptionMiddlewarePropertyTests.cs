using System.Text.Json;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.IntegrationTests.Fixtures;

namespace Intellect.Erp.Observability.IntegrationTests;

/// <summary>
/// Property-based integration tests for GlobalExceptionMiddleware.
/// </summary>
public class GlobalExceptionMiddlewarePropertyTests : IClassFixture<ObservabilityWebApplicationFactory>
{
    private readonly ObservabilityWebApplicationFactory _factory;

    private static readonly string[] RequiredFields =
    {
        "success", "errorCode", "title", "message",
        "correlationId", "status", "severity", "retryable", "timestamp"
    };

    public GlobalExceptionMiddlewarePropertyTests(ObservabilityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// **Validates: Requirements 3.14, 14.1**
    /// Property 6: Error response required fields — for any AppException, the serialized
    /// ErrorResponse contains all required fields.
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = new[] { typeof(ExceptionEndpointArbitrary) })]
    public Property ErrorResponse_ContainsAllRequiredFields(ExceptionEndpointChoice endpoint)
    {
        return Prop.ForAll(Arb.From<bool>(), _ =>
        {
            var client = _factory.CreateClient();
            var response = client.GetAsync(endpoint.Path).GetAwaiter().GetResult();
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var field in RequiredFields)
            {
                root.TryGetProperty(field, out var prop).Should().BeTrue(
                    $"ErrorResponse for {endpoint.Path} must contain '{field}' field");

                prop.ValueKind.Should().NotBe(JsonValueKind.Null,
                    $"'{field}' field should not be null for {endpoint.Path}");
            }

            root.GetProperty("success").GetBoolean().Should().BeFalse();
        });
    }

    /// <summary>
    /// **Validates: Requirements 14.3**
    /// Property 13: Error response type URI — for any error code and ClientErrorUriBase,
    /// the type field equals base + errorCode.
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = new[] { typeof(ExceptionEndpointArbitrary) })]
    public Property ErrorResponse_TypeUri_EqualsBaseAndErrorCode(ExceptionEndpointChoice endpoint)
    {
        return Prop.ForAll(Arb.From<bool>(), _ =>
        {
            var client = _factory.CreateClient();
            var response = client.GetAsync(endpoint.Path).GetAwaiter().GetResult();
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.TryGetProperty("type", out var typeField).Should().BeTrue(
                $"ErrorResponse for {endpoint.Path} must contain 'type' field");
            root.TryGetProperty("errorCode", out var errorCodeField).Should().BeTrue();

            var typeValue = typeField.GetString()!;
            var errorCode = errorCodeField.GetString()!;
            var expectedType = $"https://errors.epacs.in/{errorCode}";

            typeValue.Should().Be(expectedType,
                $"type field must equal ClientErrorUriBase + errorCode for {endpoint.Path}");
        });
    }
}

/// <summary>
/// Wrapper type for FsCheck-generated exception endpoint choices.
/// </summary>
public class ExceptionEndpointChoice
{
    public string Path { get; }
    public ExceptionEndpointChoice(string path) => Path = path;
    public override string ToString() => Path;
}

/// <summary>
/// FsCheck arbitrary that generates random exception endpoint paths.
/// </summary>
public static class ExceptionEndpointArbitrary
{
    private static readonly string[] Endpoints =
    {
        "/test/throw/validation",
        "/test/throw/business-rule",
        "/test/throw/not-found",
        "/test/throw/conflict",
        "/test/throw/unauthorized",
        "/test/throw/forbidden",
        "/test/throw/concurrency",
        "/test/throw/data-integrity",
        "/test/throw/integration",
        "/test/throw/dependency",
        "/test/throw/external-system",
        "/test/throw/system",
    };

    public static Arbitrary<ExceptionEndpointChoice> ExceptionEndpointChoiceArbitrary()
    {
        var gen = Gen.Elements(Endpoints).Select(p => new ExceptionEndpointChoice(p));
        return Arb.From(gen);
    }
}
