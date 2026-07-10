using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record UpdateInfo(Version Version, Uri ReleaseUri, string? Notes = null);

public interface IUpdateService
{
    Task<OperationResult<UpdateInfo?>> CheckAsync(CancellationToken cancellationToken = default);
}
