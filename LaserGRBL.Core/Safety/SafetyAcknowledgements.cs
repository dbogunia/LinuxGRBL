using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Core.Safety;

public enum RiskyOperation { StartJob, FirmwareFlash, Reset, Abort, LaserTest }

public enum SafetyAcknowledgementScope { OneTime, PerVersion, PerSession, PerOperation }

public sealed record SafetyAcknowledgementState(
    bool FirstRunSafetyAccepted,
    string? LegalNoticeVersion,
    string? FirmwareWarningVersion)
{
    public static SafetyAcknowledgementState Empty { get; } = new(false, null, null);
}

public sealed record SafetyRequirement(
    RiskyOperation Operation,
    SafetyAcknowledgementScope Scope,
    string MessageKey,
    string FallbackMessage);

public interface ISafetyGate
{
    OperationResult EnsureAllowed(RiskyOperation operation);
}

public sealed class PermissiveSafetyGate : ISafetyGate
{
    public static PermissiveSafetyGate Instance { get; } = new();

    public OperationResult EnsureAllowed(RiskyOperation operation) => OperationResult.Success();
}

public sealed class SafetyAcknowledgementService : ISafetyGate
{
    public const string CurrentLegalNoticeVersion = "linux-port-safety-v1";
    public const string CurrentFirmwareWarningVersion = "linux-port-firmware-v1";

    private readonly SafetyAcknowledgementState state;

    public SafetyAcknowledgementService(SafetyAcknowledgementState? state) => this.state = state ?? SafetyAcknowledgementState.Empty;

    public IReadOnlyList<SafetyRequirement> Requirements { get; } =
    [
        new(RiskyOperation.StartJob, SafetyAcknowledgementScope.OneTime, "Safety.StartJob", "Review laser safety and legal warnings before starting a job."),
        new(RiskyOperation.FirmwareFlash, SafetyAcknowledgementScope.PerVersion, "Safety.FirmwareFlash", "Firmware flashing can leave a controller unusable; confirm the warning before continuing."),
        new(RiskyOperation.Reset, SafetyAcknowledgementScope.PerSession, "Safety.Reset", "Soft reset can interrupt motion; confirm current machine state before reset."),
        new(RiskyOperation.Abort, SafetyAcknowledgementScope.PerOperation, "Safety.Abort", "Abort sends a best-effort laser-off command; verify the machine is safe."),
        new(RiskyOperation.LaserTest, SafetyAcknowledgementScope.PerOperation, "Safety.LaserTest", "Laser test commands require explicit per-operation acknowledgement.")
    ];

    public OperationResult EnsureAllowed(RiskyOperation operation)
    {
        if (!state.FirstRunSafetyAccepted) return OperationResult.Failure("Safety acknowledgement is required before risky machine operations.", operation.ToString());
        if (operation is RiskyOperation.FirmwareFlash && state.FirmwareWarningVersion != CurrentFirmwareWarningVersion)
            return OperationResult.Failure("Firmware warning acknowledgement is required before flashing.", operation.ToString());
        if (state.LegalNoticeVersion != CurrentLegalNoticeVersion)
            return OperationResult.Failure("Legal and safety notice acknowledgement is required.", operation.ToString());
        return OperationResult.Success();
    }

    public SafetyAcknowledgementState AcceptFirstRun() => state with
    {
        FirstRunSafetyAccepted = true,
        LegalNoticeVersion = CurrentLegalNoticeVersion
    };

    public SafetyAcknowledgementState AcceptFirmwareWarning() => state with
    {
        FirstRunSafetyAccepted = true,
        LegalNoticeVersion = CurrentLegalNoticeVersion,
        FirmwareWarningVersion = CurrentFirmwareWarningVersion
    };
}

public sealed class SafetyCountdown
{
    private readonly int requiredTicks;
    private int ticks;

    public SafetyCountdown(int requiredTicks)
    {
        this.requiredTicks = Math.Max(1, requiredTicks);
    }

    public int RemainingTicks => Math.Max(0, requiredTicks - ticks);
    public bool IsComplete => ticks >= requiredTicks;
    public bool IsCancelled { get; private set; }

    public OperationResult Tick()
    {
        if (IsCancelled) return OperationResult.Failure("Safety countdown was cancelled.");
        if (!IsComplete) ticks++;
        return OperationResult.Success();
    }

    public void Cancel() => IsCancelled = true;
}
