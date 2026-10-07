using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Options;
using MySqlLikeException = System.Data.Common.DbException;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Abstractions;

/// <summary>
/// What a log line or a response may say about infrastructure (2026-10-07, the log-leak sweep). Each
/// input below is a shape the estate actually wrote: a MySQL access-denied message, a connection string,
/// a server address, a broker URL with credentials.
/// </summary>
public class SensitiveTextTests
{
    private sealed class FakeDbException : MySqlLikeException
    {
        public FakeDbException(string message) : base(message) { }
    }

    [Theory]
    [InlineData("Access denied for user 'clam14530'@'10.20.30.40' (using password: YES)", "'***'@'***'")]
    [InlineData("Server=192.168.1.9;Port=3306;Database=MHCluster6;User Id=erp;Password=s3cret", "Server=***")]
    [InlineData("Cannot connect to 192.168.1.9:3306", "[address]")]
    [InlineData("amqp://svc:hunter2@broker:5672/vhost", "amqp://***@broker")]
    public void Mask_removes_addresses_logins_and_connection_values(string input, string expected)
    {
        var masked = SensitiveText.Mask(input);
        masked.Should().Contain(expected);
        foreach (var secret in new[] { "clam14530", "10.20.30.40", "192.168.1.9", "MHCluster6", "s3cret", "hunter2" })
            masked.Should().NotContain(secret);
    }

    [Fact]
    public void Mask_leaves_the_services_own_words_alone()
        => SensitiveText.Mask("Account 1042 is closed; the voucher was not posted.")
            .Should().Be("Account 1042 is closed; the voucher was not posted.");

    [Fact]
    public void ForClient_replaces_a_drivers_text_even_when_wrapped()
    {
        var wrapped = new InvalidOperationException("Cannot open the database",
            new FakeDbException("Unknown database 'MHCluster6' on 192.168.1.9"));
        SensitiveText.ForClient(wrapped).Should().Be(SensitiveText.InfrastructureFailure);
        SensitiveText.ForClient(new TimeoutException("db1:3306 timed out")).Should().Be(SensitiveText.InfrastructureFailure);
    }

    [Fact]
    public void ForClient_keeps_a_business_message_and_Development_gets_everything()
    {
        SensitiveText.ForClient(new InvalidOperationException("The day is already closed.")).Should().Be("The day is already closed.");
        SensitiveText.ForClient(new FakeDbException("Unknown database 'MHCluster6'"), includeDetail: true)
            .Should().Contain("MHCluster6");
    }

    [Fact]
    public void The_database_fingerprint_matches_the_state_identity_and_the_ops_tool()
    {
        // Pinned in utils-caching StateIdentityTests and build/test_baseline_shape.py too.
        SensitiveText.DatabaseFingerprint("192.168.1.9:3306", "MHCluster6").Should().Be("db#45ccdba4d1");
        SensitiveText.DatabaseFingerprint("192.168.1.9", "MHCluster6").Should().Be("db#45ccdba4d1");
    }

    [Fact]
    public void The_redaction_engine_masks_the_same_topology()
    {
        var engine = new DefaultRedactionEngine(Options.Create(new ObservabilityOptions
        {
            ApplicationName = "t", ModuleName = "t", Masking = new MaskingOptions { Enabled = true },
        }));
        var r = engine.Redact("Access denied for user 'erp'@'10.0.4.7'; Server=db1;Database=ERPDB_MH;Uid=erp");
        r.Should().NotContain("10.0.4.7").And.NotContain("ERPDB_MH").And.NotContain("'erp'@").And.NotContain("db1");
    }
}
