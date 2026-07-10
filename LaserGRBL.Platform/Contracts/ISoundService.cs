using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public enum SoundCue { Connect, Disconnect, Success, Warning, Error }

public interface ISoundService
{
    Task<OperationResult> PlayAsync(SoundCue cue, CancellationToken cancellationToken = default);
}
