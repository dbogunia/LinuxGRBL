using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public interface IUpdateManifestClient
{
    Task<OperationResult<string>> GetManifestAsync(Uri manifestUri, CancellationToken cancellationToken = default);
}
