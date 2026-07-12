using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public interface IMachineResourceLock : IAsyncDisposable
{
    string ResourceId { get; }
}

public interface IMachineResourceLockProvider
{
    OperationResult<IMachineResourceLock> TryAcquire(string resourceId);
}
