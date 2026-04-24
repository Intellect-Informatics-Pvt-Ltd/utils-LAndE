using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Propagation;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Propagation;

/// <summary>
/// Property-based test: Kafka header completeness.
/// For any context values, ReadCorrelation(WriteCorrelation(context)) preserves all non-null values.
/// **Validates: Requirements 1.7**
/// </summary>
public class KafkaHeaderCompletenessPropertyTests
{
    /// <summary>
    /// Property 5: For any set of context values (correlationId, causationId, traceParent, tenantId, userId),
    /// WriteCorrelation followed by ReadCorrelation preserves all non-null values.
    /// **Validates: Requirements 1.7**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ContextValuesArbitrary) }, MaxTest = 200)]
    public void ReadCorrelation_After_WriteCorrelation_Preserves_All_NonNull_Values(ContextValues ctx)
    {
        // Arrange
        var headers = new Dictionary<string, byte[]>();
        var correlationAccessor = new FakeCorrelationAccessor
        {
            CorrelationId = ctx.CorrelationId,
            CausationId = ctx.CausationId,
            TraceParent = ctx.TraceParent
        };
        var tenantAccessor = ctx.TenantId is not null
            ? new FakeTenantAccessor { TenantId = ctx.TenantId }
            : null;

        // Act
        KafkaHeaders.WriteCorrelation(headers, correlationAccessor, tenantAccessor, ctx.UserId);
        var result = KafkaHeaders.ReadCorrelation(headers);

        // Assert — all non-null input values must be preserved
        if (ctx.CorrelationId is not null)
            result.CorrelationId.Should().Be(ctx.CorrelationId);
        else
            result.CorrelationId.Should().BeNull();

        if (ctx.CausationId is not null)
            result.CausationId.Should().Be(ctx.CausationId);
        else
            result.CausationId.Should().BeNull();

        if (ctx.TraceParent is not null)
            result.TraceParent.Should().Be(ctx.TraceParent);
        else
            result.TraceParent.Should().BeNull();

        if (ctx.TenantId is not null)
            result.TenantId.Should().Be(ctx.TenantId);
        else
            result.TenantId.Should().BeNull();

        if (ctx.UserId is not null)
            result.UserId.Should().Be(ctx.UserId);
        else
            result.UserId.Should().BeNull();
    }

    #region Generators

    /// <summary>
    /// Holds generated context values for property testing.
    /// </summary>
    public sealed class ContextValues
    {
        public string? CorrelationId { get; init; }
        public string? CausationId { get; init; }
        public string? TraceParent { get; init; }
        public string? TenantId { get; init; }
        public string? UserId { get; init; }

        public override string ToString() =>
            $"CorrelationId={CorrelationId}, CausationId={CausationId}, TraceParent={TraceParent}, TenantId={TenantId}, UserId={UserId}";
    }

    public static class ContextValuesArbitrary
    {
        public static Arbitrary<ContextValues> ContextValues()
        {
            // Generate nullable non-empty strings
            var nullableString = Gen.OneOf(
                Gen.Constant<string?>(null),
                Arb.Default.NonEmptyString().Generator.Select(s => (string?)s.Get));

            var gen = from correlationId in nullableString
                      from causationId in nullableString
                      from traceParent in nullableString
                      from tenantId in nullableString
                      from userId in nullableString
                      select new ContextValues
                      {
                          CorrelationId = correlationId,
                          CausationId = causationId,
                          TraceParent = traceParent,
                          TenantId = tenantId,
                          UserId = userId
                      };

            return Arb.From(gen);
        }
    }

    #endregion

    #region Helpers

    private sealed class FakeCorrelationAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string? TraceParent { get; set; }
    }

    private sealed class FakeTenantAccessor : ITenantContextAccessor
    {
        public string? TenantId { get; set; }
        public string? StateCode { get; set; }
        public string? PacsId { get; set; }
        public string? BranchCode { get; set; }
    }

    #endregion
}
