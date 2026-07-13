using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record UpdateInfo(Version Version, Uri ReleaseUri, string? Notes = null, Version? ArtifactVersion = null, string? Sha256 = null);

public interface IUpdateService
{
    Task<OperationResult<UpdateInfo?>> CheckAsync(CancellationToken cancellationToken = default);
}
