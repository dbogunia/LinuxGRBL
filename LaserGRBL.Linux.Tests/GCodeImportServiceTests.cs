using LaserGRBL.Core.GCode;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class GCodeImportServiceTests
{
    [Fact]
    public async Task Unsupported_input_returns_user_facing_failure_without_mutating_job()
    {
        var job = new GCodeJob();
        var result = await GCodeImportService.ImportAsync(job, "drawing.txt", append: false);

        Assert.False(result.Succeeded);
        Assert.Empty(job.Lines);
        Assert.Equal("drawing.txt", result.Error?.Detail);
    }

    [Fact]
    public async Task Svg_routing_requests_a_converter_instead_of_a_file_dialog()
    {
        var result = await GCodeImportService.ImportAsync(new GCodeJob(), "drawing.svg", append: false);

        Assert.False(result.Succeeded);
        Assert.Equal("Svg", result.Error?.Detail);
    }
}
