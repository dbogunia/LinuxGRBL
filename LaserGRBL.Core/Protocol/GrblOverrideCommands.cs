namespace LaserGRBL.Core.Protocol;

public enum OverrideChannel { Feed, Rapid, Spindle }

public static class GrblOverrideCommands
{
    public static byte? NextCommand(OverrideChannel channel, int current, int target) => channel switch
    {
        OverrideChannel.Feed => NextLinear(current, target, 0x90, 0x91, 0x92, 0x93, 0x94),
        OverrideChannel.Spindle => NextLinear(current, target, 0x99, 0x9A, 0x9B, 0x9C, 0x9D),
        OverrideChannel.Rapid when target == 100 && current != 100 => 0x95,
        OverrideChannel.Rapid when target == 50 && current != 50 => 0x96,
        OverrideChannel.Rapid when target == 25 && current != 25 => 0x97,
        _ => null
    };

    private static byte? NextLinear(int current, int target, byte reset, byte upLarge, byte downLarge, byte upSmall, byte downSmall) =>
        target == 100 && current != 100 ? reset :
        target - current >= 10 ? upLarge :
        current - target >= 10 ? downLarge :
        target - current >= 1 ? upSmall :
        current - target >= 1 ? downSmall : null;
}
