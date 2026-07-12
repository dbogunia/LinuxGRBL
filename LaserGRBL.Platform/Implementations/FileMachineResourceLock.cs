using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class FileMachineResourceLockProvider(string lockDirectory) : IMachineResourceLockProvider
{
    public OperationResult<IMachineResourceLock> TryAcquire(string resourceId)
    {
        Directory.CreateDirectory(lockDirectory);
        var lockPath = Path.Combine(lockDirectory, $"{Sanitize(resourceId)}.lock");
        try
        {
            var stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            stream.SetLength(0);
            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                writer.WriteLine(Environment.ProcessId);
                writer.WriteLine(resourceId);
                writer.Flush();
            }
            stream.Position = 0;
            return OperationResult<IMachineResourceLock>.Success(new FileMachineResourceLock(resourceId, stream));
        }
        catch (IOException exception)
        {
            return OperationResult<IMachineResourceLock>.Failure("Machine resource is already owned by another process.", resourceId, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<IMachineResourceLock>.Failure("Unable to acquire machine resource lock.", resourceId, exception);
        }
    }

    private static string Sanitize(string resourceId)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(resourceId.Select(character => invalid.Contains(character) || character is '/' or '\\' or ':' ? '_' : character));
    }

    private sealed class FileMachineResourceLock(string resourceId, FileStream stream) : IMachineResourceLock
    {
        public string ResourceId => resourceId;

        public async ValueTask DisposeAsync() => await stream.DisposeAsync();
    }
}
