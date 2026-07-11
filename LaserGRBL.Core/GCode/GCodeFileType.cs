namespace LaserGRBL.Core.GCode;

public enum GCodeFileType { GCode, RasterImage, Svg, Project, Unsupported }

public static class GCodeFileRouter
{
    public static GCodeFileType Classify(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".nc" or ".cnc" or ".tap" or ".gcode" or ".ngc" => GCodeFileType.GCode,
        ".bmp" or ".png" or ".jpg" or ".jpeg" or ".gif" => GCodeFileType.RasterImage,
        ".svg" => GCodeFileType.Svg,
        ".lps" => GCodeFileType.Project,
        _ => GCodeFileType.Unsupported
    };
}
