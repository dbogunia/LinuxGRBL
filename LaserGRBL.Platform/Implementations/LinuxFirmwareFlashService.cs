using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class LinuxFirmwareFlashService(IProcessRunner processes, string avrdudePath = "avrdude") : IFirmwareFlashService
{
    public ProcessRequest BuildProcessRequest(FirmwareFlashRequest request) => new(avrdudePath, ["-p", "atmega328p", "-c", "arduino", "-P", request.DevicePath, "-b", request.BaudRate.ToString(System.Globalization.CultureInfo.InvariantCulture), "-D", "-U", $"flash:w:{request.FirmwarePath}:i"], Timeout: TimeSpan.FromMinutes(2));

    public async Task<OperationResult> FlashAsync(FirmwareFlashRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DryRun) return OperationResult.Success();
        var process = await processes.RunAsync(BuildProcessRequest(request), cancellationToken);
        if (!process.Succeeded) return OperationResult.Failure("Unable to run avrdude. Install avrdude and verify device permissions.", process.Error?.Detail, process.Error?.Exception);
        if (process.Value!.TimedOut) return OperationResult.Failure("avrdude timed out.");
        return process.Value.ExitCode == 0 ? OperationResult.Success() : OperationResult.Failure("avrdude failed.", process.Value.StandardError);
    }
}
