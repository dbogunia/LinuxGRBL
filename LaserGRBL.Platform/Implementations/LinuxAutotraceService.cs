using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class LinuxAutotraceService(IProcessRunner processes, string autotracePath = "autotrace") : IAutotraceService
{
    public ProcessRequest BuildProcessRequest(AutotraceRequest request) => new(autotracePath, ["--output-file", request.OutputPath, "--output-format", "svg", request.InputPath], request.TemporaryDirectory, TimeSpan.FromMinutes(2));

    public async Task<OperationResult<string>> TraceAsync(AutotraceRequest request, CancellationToken cancellationToken = default)
    {
        var process = await processes.RunAsync(BuildProcessRequest(request), cancellationToken);
        if (!process.Succeeded) return OperationResult<string>.Failure("Unable to run autotrace. Install autotrace or configure its Linux path.", process.Error?.Detail, process.Error?.Exception);
        if (process.Value!.TimedOut) return OperationResult<string>.Failure("autotrace timed out.");
        return process.Value.ExitCode == 0 ? OperationResult<string>.Success(request.OutputPath) : OperationResult<string>.Failure("autotrace failed.", process.Value.StandardError);
    }
}
