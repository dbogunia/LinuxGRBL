using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public interface IExternalUrlService
{
    Task<OperationResult> OpenAsync(Uri uri, CancellationToken cancellationToken = default);
}
