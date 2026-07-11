using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Core.GCode;

public static class GCodeImportService
{
    public static async Task<OperationResult<GCodeFileType>> ImportAsync(GCodeJob job, string path, bool append, CancellationToken cancellationToken = default)
    {
        var type = GCodeFileRouter.Classify(path);
        if (type == GCodeFileType.Unsupported) return OperationResult<GCodeFileType>.Failure("Unsupported input file type.", path);
        if (type != GCodeFileType.GCode) return OperationResult<GCodeFileType>.Failure("This input type requires its dedicated converter.", type.ToString());
        try
        {
            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            job.Load(lines, append);
            return OperationResult<GCodeFileType>.Success(type);
        }
        catch (Exception exception)
        {
            return OperationResult<GCodeFileType>.Failure("Unable to read G-code file.", path, exception);
        }
    }
}
