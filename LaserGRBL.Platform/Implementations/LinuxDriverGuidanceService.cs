using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Implementations;

public sealed class LinuxDriverGuidanceService
{
    public OperationResult GetCh341Guidance() => OperationResult.Failure("CH341 drivers are not installed by LaserGRBL on Linux. Connect the device, then use your distribution kernel support or install the vendor driver if required.");
}
