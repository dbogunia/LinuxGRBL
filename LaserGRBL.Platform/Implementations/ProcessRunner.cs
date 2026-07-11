using System.Diagnostics;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<OperationResult<ProcessResult>> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        var start = new ProcessStartInfo(request.FileName) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory };
        foreach (var argument in request.Arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) return OperationResult<ProcessResult>.Failure($"Unable to start '{request.FileName}'.");
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeout = request.Timeout is { } value ? new CancellationTokenSource(value) : null;
            using var linked = timeout is null ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            try { await process.WaitForExitAsync(linked.Token); }
            catch (OperationCanceledException) when (timeout?.IsCancellationRequested == true)
            {
                TryKill(process);
                return OperationResult<ProcessResult>.Success(new ProcessResult(-1, await stdout, await stderr, true));
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return OperationResult<ProcessResult>.Failure($"Process '{request.FileName}' was cancelled.");
            }
            return OperationResult<ProcessResult>.Success(new ProcessResult(process.ExitCode, await stdout, await stderr, false));
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            return OperationResult<ProcessResult>.Failure($"Unable to start '{request.FileName}'. Ensure the tool is installed and executable.", request.FileName, exception);
        }
    }

    private static void TryKill(Process process) { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } }
}
