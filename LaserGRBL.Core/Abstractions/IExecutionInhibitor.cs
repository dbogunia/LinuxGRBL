namespace LaserGRBL.Core.Abstractions;

public interface IExecutionInhibitor
{
    Task<OperationResult<IAsyncDisposable?>> AcquireAsync(string reason, CancellationToken cancellationToken = default);
}
