using System.Net;
using System.Text.Json;
using FluentAssertions;
using Intellect.Erp.Observability.IntegrationTests.Fixtures;

namespace Intellect.Erp.Observability.IntegrationTests;

/// <summary>
/// Integration tests for GlobalExceptionMiddleware using WebApplicationFactory.
/// Validates: Requirements 3.1–3.17
/// </summary>
public class GlobalExceptionMiddlewareIntegrationTests : IClassFixture<ObservabilityWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddlewareIntegrationTests(ObservabilityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/test/throw/validation", 400)]
    [InlineData("/test/throw/business-rule", 422)]
    [InlineData("/test/throw/not-found", 404)]
    [InlineData("/test/throw/conflict", 409)]
    [InlineData("/test/throw/unauthorized", 401)]
    [InlineData("/test/throw/forbidden", 403)]
    [InlineData("/test/throw/concurrency", 409)]
    [InlineData("/test/throw/data-integrity", 500)]
    [InlineData("/test/throw/integration", 502)]
    [InlineData("/test/throw/dependency", 503)]
    [InlineData("/test/throw/external-system", 502)]
    [InlineData("/test/throw/system", 500)]
    public async Task ExceptionType_MapsToCorrectHttpStatusCode(string path, int expectedStatusCode)
    {
        // Act
        var response = await _client.GetAsync(path);

        // Assert
        ((int)response.StatusCode).Should().Be(expectedStatusCode);
    }

    [Fact]
    public async Task TaskCanceledException_MapsTo499()
    {
        // Act
        var response = await _client.GetAsync("/test/throw/task-canceled");

        // Assert
        ((int)response.StatusCode).Should().Be(499);
    }

    [Fact]
    public async Task UnknownException_MapsTo500()
    {
        // Act
        var response = await _client.GetAsync("/test/throw/unknown");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ErrorResponse_HasAllRequiredFields()
    {
        // Act
        var response = await _client.GetAsync("/test/throw/not-found");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert — all required fields present
        root.TryGetProperty("success", out _).Should().BeTrue("ErrorResponse must have 'success' field");
        root.TryGetProperty("errorCode", out _).Should().BeTrue("ErrorResponse must have 'errorCode' field");
        root.TryGetProperty("title", out _).Should().BeTrue("ErrorResponse must have 'title' field");
        root.TryGetProperty("message", out _).Should().BeTrue("ErrorResponse must have 'message' field");
        root.TryGetProperty("correlationId", out _).Should().BeTrue("ErrorResponse must have 'correlationId' field");
        root.TryGetProperty("status", out _).Should().BeTrue("ErrorResponse must have 'status' field");
        root.TryGetProperty("severity", out _).Should().BeTrue("ErrorResponse must have 'severity' field");
        root.TryGetProperty("retryable", out _).Should().BeTrue("ErrorResponse must have 'retryable' field");
        root.TryGetProperty("timestamp", out _).Should().BeTrue("ErrorResponse must have 'timestamp' field");

        // Assert — field values are correct types
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("status").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task ErrorResponse_ContentTypeIsJson()
    {
        // Act
        var response = await _client.GetAsync("/test/throw/not-found");

        // Assert
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ValidationException_IncludesFieldErrors()
    {
        // Act
        var response = await _client.GetAsync("/test/throw/validation");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        root.TryGetProperty("fieldErrors", out var fieldErrors).Should().BeTrue();
        fieldErrors.GetArrayLength().Should().BeGreaterThan(0);

        var firstError = fieldErrors[0];
        firstError.TryGetProperty("field", out _).Should().BeTrue();
        firstError.TryGetProperty("code", out _).Should().BeTrue();
        firstError.TryGetProperty("message", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_HasTypeUri()
    {
        // Act
        var response = await _client.GetAsync("/test/throw/not-found");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        root.TryGetProperty("type", out var typeField).Should().BeTrue();
        var typeValue = typeField.GetString()!;
        var errorCode = root.GetProperty("errorCode").GetString()!;
        typeValue.Should().Be($"https://errors.epacs.in/{errorCode}");
    }

    [Fact]
    public async Task ErrorResponse_CorrelationIdIsPresent()
    {
        // Act
        var response = await _client.GetAsync("/test/throw/unknown");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        root.TryGetProperty("correlationId", out var corrId).Should().BeTrue();
        corrId.GetString().Should().NotBeNullOrEmpty();
    }
}

/// <summary>
/// Tests for Production safety guard — exception details must not leak in Production.
/// </summary>
public class GlobalExceptionMiddlewareProductionTests : IClassFixture<ProductionWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GlobalExceptionMiddlewareProductionTests(ProductionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Production_DoesNotIncludeExceptionDetails()
    {
        // Act
        var response = await _client.GetAsync("/test/throw/unknown");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert — exceptionType and stackTrace should be absent (null → not serialized)
        root.TryGetProperty("exceptionType", out _).Should().BeFalse(
            "Production responses must not include exceptionType");
        root.TryGetProperty("stackTrace", out _).Should().BeFalse(
            "Production responses must not include stackTrace");
        root.TryGetProperty("supportReference", out _).Should().BeFalse(
            "Production responses must not include supportReference");
    }
}
