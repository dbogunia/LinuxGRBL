using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class ContinuousJogControllerTests
{
    [Fact]
    public void First_continuous_jog_does_not_abort_then_replacement_does()
    {
        var controller = new ContinuousJogController();
        controller.RequestDirection(JogDirection.East, 1000);
        var first = controller.TakeNext();
        controller.RequestDirection(JogDirection.North, 1000);
        var second = controller.TakeNext();

        Assert.Equal(new ContinuousJogAction(false, "$J=G91X1.0F1000"), first);
        Assert.Equal(new ContinuousJogAction(true, "$J=G91Y1.0F1000"), second);
    }

    [Fact]
    public void Abort_only_requests_realtime_cancellation_when_a_jog_was_active()
    {
        var controller = new ContinuousJogController();
        controller.Abort();
        Assert.Equal(new ContinuousJogAction(false, null), controller.TakeNext());

        controller.RequestPosition(new MachinePosition(1, 2, 0), 500);
        controller.TakeNext();
        controller.Abort();
        Assert.Equal(new ContinuousJogAction(true, null), controller.TakeNext());
    }
}
