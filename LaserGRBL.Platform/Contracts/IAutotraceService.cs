using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record AutotraceRequest(string InputPath, string OutputPath, string? TemporaryDirectory = null);

public interface IAutotraceService
{
    Task<OperationResult<string>> TraceAsync(AutotraceRequest request, CancellationToken cancellationToken = default);
}
