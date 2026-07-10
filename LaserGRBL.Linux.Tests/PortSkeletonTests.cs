using LaserGRBL.Core;
using LaserGRBL.Platform;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class PortSkeletonTests
{
    [Fact]
    public void Core_targets_net8() => Assert.Equal("net8.0", PortInfo.TargetFramework);

    [Fact]
    public void Test_host_is_linux() => Assert.True(PlatformInfo.IsLinux);
}
