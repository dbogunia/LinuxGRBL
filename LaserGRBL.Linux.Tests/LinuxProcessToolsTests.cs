using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class LinuxProcessToolsTests
{
    [Fact]
    public void Avrdude_arguments_are_individual_and_include_device_firmware_and_baud()
    {
        var request = new LinuxFirmwareFlashService(new RecordedRunner()).BuildProcessRequest(new FirmwareFlashRequest("/dev/ttyACM0", "/tmp/firmware.hex", 115200, true));

        Assert.Equal("avrdude", request.FileName);
        Assert.Equal(["-p", "atmega328p", "-c", "arduino", "-P", "/dev/ttyACM0", "-b", "115200", "-D", "-U", "flash:w:/tmp/firmware.hex:i"], request.Arguments);
    }

    [Fact]
    public async Task Missing_avrdude_has_actionable_result()
    {
        var service = new LinuxFirmwareFlashService(new RecordedRunner(OperationResult<ProcessResult>.Failure("missing", "avrdude")));
        var result = await service.FlashAsync(new FirmwareFlashRequest("/dev/ttyUSB0", "/tmp/fw.hex", 115200, false));
        Assert.False(result.Succeeded);
        Assert.Contains("Install avrdude", result.Error!.Message);
    }

    [Fact]
    public void Autotrace_arguments_keep_temp_working_directory_and_paths_separate()
    {
        var request = new LinuxAutotraceService(new RecordedRunner()).BuildProcessRequest(new AutotraceRequest("/tmp/in image.png", "/tmp/out image.svg", "/tmp/lasergrbl"));
        Assert.Equal("/tmp/lasergrbl", request.WorkingDirectory);
        Assert.Equal("/tmp/out image.svg", request.Arguments[1]);
        Assert.Equal("/tmp/in image.png", request.Arguments[^1]);
    }

    [Fact]
    public async Task Missing_autotrace_has_actionable_result()
    {
        var service = new LinuxAutotraceService(new RecordedRunner(OperationResult<ProcessResult>.Failure("missing", "autotrace")));
        var result = await service.TraceAsync(new AutotraceRequest("in.png", "out.svg", Path.GetTempPath()));
        Assert.False(result.Succeeded);
        Assert.Contains("Install autotrace", result.Error!.Message);
    }

    [Fact]
    public void Ch341_returns_linux_guidance_instead_of_an_executable_installer()
    {
        var result = new LinuxDriverGuidanceService().GetCh341Guidance();
        Assert.False(result.Succeeded);
        Assert.Contains("not installed", result.Error!.Message);
    }

    [Fact]
    public async Task Process_runner_captures_stdout_stderr_and_exit_code()
    {
        var result = await new ProcessRunner().RunAsync(new ProcessRequest("/bin/sh", ["-c", "printf output; printf error >&2; exit 7"]));
        Assert.True(result.Succeeded);
        Assert.Equal(7, result.Value!.ExitCode);
        Assert.Equal("output", result.Value.StandardOutput);
        Assert.Equal("error", result.Value.StandardError);
        Assert.False(result.Value.TimedOut);
    }

    [Fact]
    public async Task Process_runner_reports_timeout_and_cancellation()
    {
        var runner = new ProcessRunner();
        var timedOut = await runner.RunAsync(new ProcessRequest("/bin/sh", ["-c", "sleep 2"], Timeout: TimeSpan.FromMilliseconds(50)));
        Assert.True(timedOut.Succeeded); Assert.True(timedOut.Value!.TimedOut);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await runner.RunAsync(new ProcessRequest("/bin/sh", ["-c", "sleep 2"]), cancellation.Token);
        Assert.False(cancelled.Succeeded); Assert.Contains("cancelled", cancelled.Error!.Message);
    }

    private sealed class RecordedRunner(OperationResult<ProcessResult>? result = null) : IProcessRunner
    {
        public Task<OperationResult<ProcessResult>> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default) => Task.FromResult(result ?? OperationResult<ProcessResult>.Success(new ProcessResult(0, "", "", false)));
    }
}
