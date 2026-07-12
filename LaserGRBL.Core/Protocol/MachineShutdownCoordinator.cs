using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Core.Protocol;

public enum MachineShutdownReason { ApplicationClose, Cancellation, TransportFailure, SessionLogout }

public sealed record MachineShutdownRequest(MachineShutdownReason Reason, TimeSpan Timeout);

public sealed record MachineShutdownResult(
    MachineShutdownReason Reason,
    bool SafetyAttempted,
    bool SafetySucceeded,
    bool TransportDisposed,
    MachineIssue Issue,
    string Message);

public sealed class MachineShutdownCoordinator(MachineSession session)
{
    public async Task<MachineShutdownResult> ShutdownAsync(IMachineTransport? transport, MachineShutdownRequest request, CancellationToken cancellationToken = default)
    {
        var safetyAttempted = false;
        var safetySucceeded = true;
        var transportDisposed = false;
        var message = "Session closed cleanly.";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(request.Timeout);

        if (transport is not null && transport.IsOpen && session.IsProgramActive)
        {
            safetyAttempted = true;
            var abort = await session.AbortProgramAsync(transport, timeout.Token);
            safetySucceeded = abort.Succeeded;
            if (!abort.Succeeded) message = abort.Error?.Message ?? "Safety command failed.";
        }

        if (request.Reason == MachineShutdownReason.TransportFailure)
            await session.HandleTransportClosedAsync(CancellationToken.None);
        else
            await session.DisconnectAsync(CancellationToken.None);

        if (transport is not null)
        {
            await transport.DisposeAsync();
            transportDisposed = true;
        }

        return new MachineShutdownResult(request.Reason, safetyAttempted, safetySucceeded, transportDisposed, session.LastIssue, message);
    }
}

public enum RecoveryResumeDecision { Allowed, Refused }

public sealed record MachineRecoverySnapshot(
    string? JobIdentity,
    MachinePosition? LastKnownPosition,
    bool WasHomed,
    bool UserAcknowledgedUnsafeResume);

public sealed record MachineRecoveryDecision(RecoveryResumeDecision Decision, string Message);

public static class MachineRecoveryPolicy
{
    public static MachineRecoveryDecision EvaluateResume(MachineRecoverySnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.JobIdentity))
            return new MachineRecoveryDecision(RecoveryResumeDecision.Refused, "Resume refused: previous job identity is not verified.");
        if (snapshot.LastKnownPosition is null)
            return new MachineRecoveryDecision(RecoveryResumeDecision.Refused, "Resume refused: last machine position is not verified.");
        if (!snapshot.WasHomed)
            return new MachineRecoveryDecision(RecoveryResumeDecision.Refused, "Resume refused: homing state is not verified.");
        if (!snapshot.UserAcknowledgedUnsafeResume)
            return new MachineRecoveryDecision(RecoveryResumeDecision.Refused, "Resume refused: user acknowledgement is required after restart.");

        return new MachineRecoveryDecision(RecoveryResumeDecision.Allowed, "Resume can continue after verified user acknowledgement.");
    }
}
