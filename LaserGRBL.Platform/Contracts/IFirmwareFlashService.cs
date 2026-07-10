using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record FirmwareFlashRequest(string DevicePath, string FirmwarePath, int BaudRate, bool DryRun);

public interface IFirmwareFlashService
{
    Task<OperationResult> FlashAsync(FirmwareFlashRequest request, CancellationToken cancellationToken = default);
}
