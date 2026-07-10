using System.Globalization;

namespace LaserGRBL.Core.Protocol;

public enum JogDirection { Home, North, South, West, East, NorthWest, NorthEast, SouthWest, SouthEast, ZUp, ZDown }

public static class JogCommandFactory
{
    public static IReadOnlyList<string> CreateRelative(JogDirection direction, decimal step, double speed, bool trueJogging)
    {
        var movement = RelativeMovement(direction, step);
        if (direction == JogDirection.Home) return trueJogging ? [$"$J=G90X0Y0F{Format(speed)}"] : ["G90", $"G1X0Y0F{Format(speed)}"];
        var command = $"{(trueJogging ? "$J=G91" : "G1")}{movement}F{Format(speed)}";
        return trueJogging ? [command] : ["G91", command, "G90"];
    }

    public static string CreateAbsolute(MachinePosition target, double speed, bool trueJogging) =>
        $"{(trueJogging ? "$J=G90" : "G1")}X{target.X.ToString("0.00", CultureInfo.InvariantCulture)}Y{target.Y.ToString("0.00", CultureInfo.InvariantCulture)}F{Format(speed)}";

    private static string RelativeMovement(JogDirection direction, decimal step)
    {
        var value = step.ToString("0.0", CultureInfo.InvariantCulture);
        return direction switch
        {
            JogDirection.North => $"Y{value}", JogDirection.South => $"Y-{value}", JogDirection.West => $"X-{value}", JogDirection.East => $"X{value}",
            JogDirection.NorthWest => $"X-{value}Y{value}", JogDirection.NorthEast => $"X{value}Y{value}", JogDirection.SouthWest => $"X-{value}Y-{value}", JogDirection.SouthEast => $"X{value}Y-{value}",
            JogDirection.ZUp => $"Z{value}", JogDirection.ZDown => $"Z-{value}",
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
