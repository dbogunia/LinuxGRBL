using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Privacy;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class PrivacyPolicyService(PrivacySettings settings)
{
    public PrivacySettings Settings { get; } = settings ?? PrivacySettings.Default;

    public OperationResult EnsureAllowed(OutboundNetworkPurpose purpose) =>
        IsAllowed(purpose)
            ? OperationResult.Success()
            : OperationResult.Failure("Outbound network action is disabled by privacy settings.", purpose.ToString());

    public bool IsAllowed(OutboundNetworkPurpose purpose) => purpose switch
    {
        OutboundNetworkPurpose.UpdateCheck => Settings.UpdateChecksEnabled,
        OutboundNetworkPurpose.MaterialDatabaseUpdate => Settings.MaterialUpdatesEnabled,
        OutboundNetworkPurpose.UsageStatistics => Settings.TelemetryEnabled && Settings.UsageStatisticsEnabled,
        OutboundNetworkPurpose.LaserStatistics => Settings.TelemetryEnabled && Settings.LaserStatisticsEnabled,
        OutboundNetworkPurpose.TelegramNotification => Settings.TelegramNotificationsEnabled,
        OutboundNetworkPurpose.ExternalUrlOpen => true,
        _ => false
    };

    public async Task<OperationResult<T>> ExecuteAsync<T>(OutboundNetworkPurpose purpose, Func<CancellationToken, Task<OperationResult<T>>> action, CancellationToken cancellationToken = default)
    {
        var allowed = EnsureAllowed(purpose);
        return !allowed.Succeeded
            ? OperationResult<T>.Failure(allowed.Error!.Message, allowed.Error.Detail, allowed.Error.Exception)
            : await action(cancellationToken);
    }
}

public sealed class PrivacyAwareExternalUrlService(IExternalUrlService inner, PrivacyPolicyService policy) : IExternalUrlService
{
    public Task<OperationResult> OpenAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (uri.Scheme is not ("https" or "http")) return Task.FromResult(OperationResult.Failure("Only http and https URLs can be opened externally.", uri.ToString()));
        var allowed = policy.EnsureAllowed(OutboundNetworkPurpose.ExternalUrlOpen);
        return !allowed.Succeeded ? Task.FromResult(allowed) : inner.OpenAsync(uri, cancellationToken);
    }
}

public sealed class UnavailableExternalUrlService : IExternalUrlService
{
    public Task<OperationResult> OpenAsync(Uri uri, CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure("External URL opening is unavailable on this system.", uri.ToString()));
}

public sealed class TelegramNotificationService(ISecretStore secrets, PrivacyPolicyService policy)
{
    public const string TokenKey = "telegram-token";

    public async Task<OperationResult> SaveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!policy.Settings.TelegramNotificationsEnabled)
            return OperationResult.Failure("Telegram notifications are disabled by privacy settings.");
        if (string.IsNullOrWhiteSpace(token)) return OperationResult.Failure("Telegram token cannot be empty.");

        var result = await secrets.SetAsync(TokenKey, token, cancellationToken);
        return result.Succeeded ? OperationResult.Success() : OperationResult.Failure("Telegram token cannot be persisted securely.", result.Error?.Message, result.Error?.Exception);
    }

    public async Task<OperationResult> SendAsync(Func<string, CancellationToken, Task<OperationResult>> send, CancellationToken cancellationToken = default)
    {
        var allowed = policy.EnsureAllowed(OutboundNetworkPurpose.TelegramNotification);
        if (!allowed.Succeeded) return allowed;

        var token = await secrets.GetAsync(TokenKey, cancellationToken);
        return token.Status switch
        {
            SecretReadStatus.Found when !string.IsNullOrWhiteSpace(token.Value) => await send(token.Value, cancellationToken),
            SecretReadStatus.Unavailable => OperationResult.Failure("Telegram notifications require secure secret storage.", token.Error?.Message),
            _ => OperationResult.Failure("Telegram token must be re-entered before notifications can be sent.")
        };
    }
}
