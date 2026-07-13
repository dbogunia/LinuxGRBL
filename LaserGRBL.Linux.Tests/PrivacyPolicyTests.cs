using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Privacy;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class PrivacyPolicyTests
{
    [Fact]
    public void Telemetry_and_all_optional_network_features_are_disabled_by_default()
    {
        var settings = PrivacySettings.Default;
        var policy = new PrivacyPolicyService(settings);

        Assert.False(settings.TelemetryEnabled);
        Assert.False(policy.IsAllowed(OutboundNetworkPurpose.UpdateCheck));
        Assert.False(policy.IsAllowed(OutboundNetworkPurpose.MaterialDatabaseUpdate));
        Assert.False(policy.IsAllowed(OutboundNetworkPurpose.UsageStatistics));
        Assert.False(policy.IsAllowed(OutboundNetworkPurpose.LaserStatistics));
        Assert.False(policy.IsAllowed(OutboundNetworkPurpose.TelegramNotification));
    }

    [Fact]
    public async Task Disabled_policy_flags_prevent_outbound_calls()
    {
        var policy = new PrivacyPolicyService(PrivacySettings.Default);
        var calls = 0;

        foreach (var purpose in new[] { OutboundNetworkPurpose.UpdateCheck, OutboundNetworkPurpose.MaterialDatabaseUpdate, OutboundNetworkPurpose.UsageStatistics, OutboundNetworkPurpose.LaserStatistics, OutboundNetworkPurpose.TelegramNotification })
        {
            var result = await policy.ExecuteAsync(purpose, _ =>
            {
                calls++;
                return Task.FromResult(OperationResult<string>.Success("called"));
            });
            Assert.False(result.Succeeded);
        }

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Separate_opt_in_controls_each_policy_area()
    {
        var policy = new PrivacyPolicyService(new PrivacySettings(
            TelemetryEnabled: true,
            UpdateChecksEnabled: true,
            MaterialUpdatesEnabled: false,
            UsageStatisticsEnabled: true,
            LaserStatisticsEnabled: false,
            TelegramNotificationsEnabled: true));

        Assert.True(policy.IsAllowed(OutboundNetworkPurpose.UpdateCheck));
        Assert.False(policy.IsAllowed(OutboundNetworkPurpose.MaterialDatabaseUpdate));
        Assert.True(policy.IsAllowed(OutboundNetworkPurpose.UsageStatistics));
        Assert.False(policy.IsAllowed(OutboundNetworkPurpose.LaserStatistics));
        Assert.True(policy.IsAllowed(OutboundNetworkPurpose.TelegramNotification));
    }

    [Fact]
    public async Task Update_manifest_reports_malformed_timeout_cancellation_offline_and_integrity_failures()
    {
        var current = new Version(0, 1, 0);

        Assert.False((await new ReleaseManifestUpdateService(new ManifestClient("{bad"), new Uri("https://example.com/manifest.json"), current).CheckAsync()).Succeeded);
        Assert.False((await new ReleaseManifestUpdateService(new ManifestClient(null, OperationResult<string>.Failure("timeout")), new Uri("https://example.com/manifest.json"), current).CheckAsync()).Succeeded);
        Assert.False((await new ReleaseManifestUpdateService(new ManifestClient(null, OperationResult<string>.Failure("cancelled")), new Uri("https://example.com/manifest.json"), current).CheckAsync()).Succeeded);
        Assert.False((await new ReleaseManifestUpdateService(new ManifestClient(null, OperationResult<string>.Failure("offline")), new Uri("https://example.com/manifest.json"), current).CheckAsync()).Succeeded);
        Assert.False((await new ReleaseManifestUpdateService(new ManifestClient("""{"version":"0.2.0","artifactVersion":"0.3.0","releaseUrl":"https://example.com/release","sha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}"""), new Uri("https://example.com/manifest.json"), current).CheckAsync()).Succeeded);
        Assert.False((await new ReleaseManifestUpdateService(new ManifestClient("""{"version":"0.2.0","artifactVersion":"0.2.0","releaseUrl":"https://example.com/release"}"""), new Uri("https://example.com/manifest.json"), current).CheckAsync()).Succeeded);
        Assert.False((await new ReleaseManifestUpdateService(new ManifestClient("""{"version":"0.2.0","artifactVersion":"0.2.0","releaseUrl":"http://example.com/release","sha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}"""), new Uri("https://example.com/manifest.json"), current).CheckAsync()).Succeeded);
        Assert.False((await new ReleaseManifestUpdateService(new ManifestClient(Manifest("0.2.0")), new Uri("http://example.com/manifest.json"), current).CheckAsync()).Succeeded);
    }

    [Fact]
    public async Task Update_policy_blocks_manifest_client_when_disabled_and_allows_when_enabled()
    {
        var client = new ManifestClient(Manifest("0.2.0"));
        var disabled = new PrivacyPolicyService(PrivacySettings.Default);
        var enabled = new PrivacyPolicyService(PrivacySettings.Default with { UpdateChecksEnabled = true });

        var blocked = await disabled.ExecuteAsync(OutboundNetworkPurpose.UpdateCheck, token => new ReleaseManifestUpdateService(client, new Uri("https://example.com/manifest.json"), new Version(0, 1, 0)).CheckAsync(token));
        var allowed = await enabled.ExecuteAsync(OutboundNetworkPurpose.UpdateCheck, token => new ReleaseManifestUpdateService(client, new Uri("https://example.com/manifest.json"), new Version(0, 1, 0)).CheckAsync(token));

        Assert.False(blocked.Succeeded);
        Assert.True(allowed.Succeeded);
        Assert.Equal(1, client.RequestCount);
    }

    [Fact]
    public async Task External_url_open_is_routed_through_platform_service()
    {
        var inner = new FakeUrlService();
        var service = new PrivacyAwareExternalUrlService(inner, new PrivacyPolicyService(PrivacySettings.Default));

        var success = await service.OpenAsync(new Uri("https://example.com/release"));
        var failure = await new PrivacyAwareExternalUrlService(new FakeUrlService(fail: true), new PrivacyPolicyService(PrivacySettings.Default)).OpenAsync(new Uri("https://example.com/release"));
        var rejected = await service.OpenAsync(new Uri("file:///tmp/test"));

        Assert.True(success.Succeeded);
        Assert.Equal("https://example.com/release", inner.Opened?.ToString());
        Assert.False(failure.Succeeded);
        Assert.False(rejected.Succeeded);
    }

    [Fact]
    public async Task Telegram_token_reentry_requires_enabled_policy_and_available_secret_store()
    {
        var disabled = new TelegramNotificationService(new FakeSecretStore(), new PrivacyPolicyService(PrivacySettings.Default));
        var unavailable = new TelegramNotificationService(new UnavailableSecretStore(), new PrivacyPolicyService(PrivacySettings.Default with { TelegramNotificationsEnabled = true }));
        var availableStore = new FakeSecretStore();
        var enabled = new TelegramNotificationService(availableStore, new PrivacyPolicyService(PrivacySettings.Default with { TelegramNotificationsEnabled = true }));

        Assert.False((await disabled.SaveTokenAsync("abc")).Succeeded);
        Assert.False((await unavailable.SaveTokenAsync("abc")).Succeeded);
        Assert.True((await enabled.SaveTokenAsync("abc")).Succeeded);
        var sent = await enabled.SendAsync((token, _) => Task.FromResult(OperationResult.Success()));

        Assert.True(sent.Succeeded);
        Assert.Equal("abc", availableStore.Value);
    }

    [Fact]
    public void Privacy_settings_are_persisted_in_port_settings_without_secret_material()
    {
        var settings = PortSettings.Default with { Privacy = PrivacySettings.Default with { UpdateChecksEnabled = true, TelegramNotificationsEnabled = true } };

        Assert.True(settings.Normalize().Privacy?.UpdateChecksEnabled);
        Assert.True(settings.Normalize().Privacy?.TelegramNotificationsEnabled);
    }

    private sealed class ManifestClient(string? manifest, OperationResult<string>? result = null) : IUpdateManifestClient
    {
        public int RequestCount { get; private set; }
        public Task<OperationResult<string>> GetManifestAsync(Uri manifestUri, CancellationToken cancellationToken = default)
        {
            RequestCount++;
            return Task.FromResult(result ?? OperationResult<string>.Success(manifest ?? ""));
        }
    }

    private sealed class FakeUrlService(bool fail = false) : IExternalUrlService
    {
        public Uri? Opened { get; private set; }
        public Task<OperationResult> OpenAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            Opened = uri;
            return Task.FromResult(fail ? OperationResult.Failure("open failed") : OperationResult.Success());
        }
    }

    private sealed class FakeSecretStore : ISecretStore
    {
        public string? Value { get; private set; }
        public Task<SecretReadResult> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(Value is null ? new SecretReadResult(SecretReadStatus.Missing) : new SecretReadResult(SecretReadStatus.Found, Value));

        public Task<OperationResult> SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Value = value;
            return Task.FromResult(OperationResult.Success());
        }

        public Task<OperationResult> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            Value = null;
            return Task.FromResult(OperationResult.Success());
        }
    }

    private static string Manifest(string version) => $$"""{"version":"{{version}}","artifactVersion":"{{version}}","releaseUrl":"https://example.com/release","sha256":"{{new string('a', 64)}}","notes":"new"}""";
}
