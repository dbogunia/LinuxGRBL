using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record ProcessRequest(string FileName, IReadOnlyList<string> Arguments, string? WorkingDirectory = null, TimeSpan? Timeout = null);

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);

public interface IProcessRunner
{
    Task<OperationResult<ProcessResult>> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default);
}
