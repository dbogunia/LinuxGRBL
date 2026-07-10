using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Implementations;

/// <summary>Safe fallback until a host-specific sleep inhibitor is implemented.</summary>
public sealed class UnavailableExecutionInhibitor : IExecutionInhibitor
{
    public Task<OperationResult<IAsyncDisposable?>> AcquireAsync(string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult<IAsyncDisposable?>.Failure("System sleep inhibition is unavailable.", reason));
}
