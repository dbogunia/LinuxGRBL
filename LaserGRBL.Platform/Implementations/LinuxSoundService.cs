using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class LinuxSoundService(IProcessRunner processes, string soundDirectory, bool enabled = true)
    : ISoundService
{
    private static readonly IReadOnlyList<string> Players = ["pw-play", "paplay", "aplay"];

    public Task<OperationResult> PlayAsync(SoundCue cue, CancellationToken cancellationToken = default)
    {
        if (!enabled) return Task.FromResult(OperationResult.Success());
        var path = Path.Combine(soundDirectory, FileNameFor(cue));
        return PlayFileAsync(path, cancellationToken);
    }

    public async Task<OperationResult> PlayFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!enabled) return OperationResult.Success();
        if (!File.Exists(path)) return OperationResult.Failure("Sound file was not found.", path);

        var failures = new List<string>();
        foreach (var player in Players)
        {
            var result = await processes.RunAsync(new ProcessRequest(player, [path], Timeout: TimeSpan.FromSeconds(5)), cancellationToken);
            if (result.Succeeded && result.Value is { ExitCode: 0, TimedOut: false }) return OperationResult.Success();
            failures.Add(result.Error?.Message ?? $"{player} exited with code {result.Value?.ExitCode}");
        }

        return OperationResult.Failure("No supported Linux audio player could play the sound.", string.Join(" | ", failures));
    }

    private static string FileNameFor(SoundCue cue) => cue switch
    {
        SoundCue.Connect => "connect.wav",
        SoundCue.Disconnect => "disconnect.wav",
        SoundCue.Success => "success.wav",
        SoundCue.Warning => "warning.wav",
        SoundCue.Error => "fatal.wav",
        _ => "beep.wav"
    };
}
