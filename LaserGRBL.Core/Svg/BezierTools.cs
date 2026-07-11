namespace LaserGRBL.Core.Svg;

public readonly record struct SvgPoint(double X, double Y);

public static class BezierTools
{
    public static IEnumerable<SvgPoint> FlattenTo(IReadOnlyList<SvgPoint> points, double error = 0.01, int maxSubdivisions = 20)
    {
        if (points.Count == 0) return [];
        var result = new List<SvgPoint> { points[0] };
        for (var index = 0; index + 3 < points.Count; index += 3)
            result.AddRange(FlattenSegment(points.Skip(index).Take(4).ToArray(), error, 0, maxSubdivisions).Skip(1));
        return result;
    }

    private static IEnumerable<SvgPoint> FlattenSegment(SvgPoint[] segment, double error, int subdivisions, int maxSubdivisions)
    {
        if (subdivisions >= maxSubdivisions || (Math.Sqrt(TriangleArea(segment[0], segment[1], segment[2])) < error && Math.Sqrt(TriangleArea(segment[1], segment[2], segment[3])) < error)) return segment;
        var (first, second) = Split(segment, 0.5);
        return FlattenSegment(first, error, subdivisions + 1, maxSubdivisions).Concat(FlattenSegment(second, error, subdivisions + 1, maxSubdivisions).Skip(1));
    }

    private static (SvgPoint[] First, SvgPoint[] Second) Split(SvgPoint[] points, double t)
    {
        var degree = points.Length - 1;
        var values = new SvgPoint[degree + 1, degree + 1];
        for (var index = 0; index <= degree; index++) values[0, index] = points[index];
        for (var row = 1; row <= degree; row++)
        for (var column = 0; column <= degree - row; column++)
            values[row, column] = new((1 - t) * values[row - 1, column].X + t * values[row - 1, column + 1].X, (1 - t) * values[row - 1, column].Y + t * values[row - 1, column + 1].Y);
        var first = new SvgPoint[degree + 1];
        var second = new SvgPoint[degree + 1];
        for (var index = 0; index <= degree; index++) { first[index] = values[index, 0]; second[index] = values[degree - index, index]; }
        return (first, second);
    }

    private static double TriangleArea(SvgPoint a, SvgPoint b, SvgPoint c) => Math.Abs(a.X * b.Y + b.X * c.Y + c.X * a.Y - a.Y * b.X - b.Y * c.X - c.Y * a.X) / 2;
}
