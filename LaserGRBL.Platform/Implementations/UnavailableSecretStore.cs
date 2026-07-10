using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Implementations;

/// <summary>Reports unavailable secure storage without exposing secret values through settings.</summary>
public sealed class UnavailableSecretStore : ISecretStore
{
    private const string Message = "Secure secret storage is unavailable on this system.";

    public Task<SecretReadResult> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SecretReadResult(SecretReadStatus.Unavailable, Error: new OperationError(Message)));

    public Task<OperationResult> SetAsync(string key, string value, CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure(Message));

    public Task<OperationResult> DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure(Message));
}
