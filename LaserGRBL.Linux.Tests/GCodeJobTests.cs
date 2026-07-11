using LaserGRBL.Core.GCode;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class GCodeJobTests
{
    [Theory]
    [InlineData("job.nc", GCodeFileType.GCode)]
    [InlineData("image.PNG", GCodeFileType.RasterImage)]
    [InlineData("drawing.svg", GCodeFileType.Svg)]
    [InlineData("project.lps", GCodeFileType.Project)]
    [InlineData("notes.txt", GCodeFileType.Unsupported)]
    public void Routes_supported_import_extensions(string path, GCodeFileType expected) => Assert.Equal(expected, GCodeFileRouter.Classify(path));

    [Fact]
    public void Parses_words_ignoring_comments_and_calculates_bounds()
    {
        var job = new GCodeJob();
        job.Load(["G0 X-1.5 Y2 (move)", "G1X10Y-3 ; burn"], append: false);

        Assert.Equal(-1.5m, job.Lines[0].Words['X']);
        Assert.Equal(new GCodeBounds(-1.5m, -3m, 10m, 2m), job.Bounds);
    }

    [Fact]
    public void Append_preserves_existing_job_content_while_replace_clears_it()
    {
        var job = new GCodeJob();
        job.Load(["G0 X1"], append: false);
        job.Load(["G0 X2"], append: true);
        Assert.Equal(2, job.Lines.Count);
        job.Load(["G0 X3"], append: false);
        Assert.Single(job.Lines);
        Assert.Equal(3m, job.Lines[0].Words['X']);
    }

    [Fact]
    public void Renders_header_passes_and_footer_for_multiple_cycles()
    {
        var job = new GCodeJob();
        job.Load(["G1 X1"], append: false);

        var rendered = job.Render(true, true, true, 2, "HEADER", "PASS-A\nPASS-B", "FOOTER").ToArray();

        Assert.Equal(["HEADER", "G1 X1", "PASS-A", "PASS-B", "G1 X1", "FOOTER"], rendered);
    }
}
