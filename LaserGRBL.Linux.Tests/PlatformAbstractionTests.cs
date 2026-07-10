using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class PlatformAbstractionTests
{
    [Fact]
    public void Operation_result_preserves_user_and_diagnostic_details()
    {
        var result = OperationResult.Failure("Cannot open device.", "/dev/ttyUSB0", new InvalidOperationException());

        Assert.False(result.Succeeded);
        Assert.Equal("Cannot open device.", result.Error?.Message);
        Assert.Equal("/dev/ttyUSB0", result.Error?.Detail);
        Assert.IsType<InvalidOperationException>(result.Error?.Exception);
    }

    [Fact]
    public void Fake_clock_can_provide_deterministic_elapsed_time()
    {
        IMonotonicClock clock = new FakeClock(TimeSpan.FromSeconds(42));

        Assert.Equal(TimeSpan.FromSeconds(42), clock.Elapsed);
    }

    [Fact]
    public async Task Unavailable_inhibitor_returns_a_nonfatal_failure()
    {
        var result = await new UnavailableExecutionInhibitor().AcquireAsync("Active laser job");

        Assert.False(result.Succeeded);
        Assert.Contains("unavailable", result.Error?.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unavailable_secret_store_is_distinct_from_a_missing_secret()
    {
        var result = await new UnavailableSecretStore().GetAsync("telegram-token");

        Assert.Equal(SecretReadStatus.Unavailable, result.Status);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
    }

    private sealed class FakeClock(TimeSpan elapsed) : IMonotonicClock
    {
        public TimeSpan Elapsed { get; } = elapsed;
    }
}
