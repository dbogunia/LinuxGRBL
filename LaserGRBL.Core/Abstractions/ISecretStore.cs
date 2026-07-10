namespace LaserGRBL.Core.Abstractions;

public enum SecretReadStatus { Found, Missing, Unavailable }

public sealed record SecretReadResult(SecretReadStatus Status, string? Value = null, OperationError? Error = null);

/// <summary>Stores secrets outside ordinary settings serialization.</summary>
public interface ISecretStore
{
    Task<SecretReadResult> GetAsync(string key, CancellationToken cancellationToken = default);

    Task<OperationResult> SetAsync(string key, string value, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(string key, CancellationToken cancellationToken = default);
}
