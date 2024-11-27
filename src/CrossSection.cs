using BlastDesign.tool;
using SkiaSharp;
using MathNet.Spatial.Euclidean;

public class CrossSection
{
    private Config _config;
    public CrossSection(Config config)
    {
        _config = config;
    }
    // 计算剖面图的边界
    public List<Point3D> CalculateCrossSectionEdges(BasePolygon topPolygon, BasePolygon bottomPolygon, double x)
    {
        Point3D a = new Point3D(), b = new Point3D(), c = new Point3D(), d = new Point3D();
        List<Point3D> newPoints = new List<Point3D>();
        foreach (var edge in topPolygon.Edges)
        {
            var start = edge.Start;
            var end = edge.End;
            if (start.X <= x && end.X >= x || start.X >= x && end.X <= x)
            {
                var y = start.Y + (x - start.X) * (end.Y - start.Y) / (end.X - start.X);
                if (edge.style == 3)
                {
                    b = new Point3D(x, y, start.Z);
                }
                else
                {
                    a = new Point3D(x, y, start.Z);
                }
            }
        }
        foreach (var edge in bottomPolygon.Edges)
        {
            var start = edge.Start;
            var end = edge.End;
            if (start.X <= x && end.X >= x || start.X >= x && end.X <= x)
            {
                var y = start.Y + (x - start.X) * (end.Y - start.Y) / (end.X - start.X);
                if (edge.style == 3)
                {
                    c = new Point3D(x, y, start.Z);
                }
                else
                {
                    d = new Point3D(x, y, start.Z);
                }
            }
        }
        newPoints.AddRange([a, b, c, d]);
        return newPoints;
    }

    // 找到距离 X 最近的炮孔对应的 Y 坐标
    public List<Point3D> FindNearestBlastHoles(List<HashSet<Point3D>> blastHolePositions, double x)
    {
        List<Point3D> newHoles = new List<Point3D>();
        foreach (var blastHoles in blastHolePositions)
        {
            if (!blastHoles.Any())
            {
                continue;
            }
            var nearestHole = blastHoles.OrderBy(hole => Math.Abs(hole.X - x)).First();
            // 距离太远，不绘制
            if (Math.Abs(nearestHole.X - x) > _config.MainBlastHoleSpacing / 2)
            {
                continue;
            }
            // 将 nearestHole 平移到平面 X = x 上
            nearestHole = new Point3D(x, nearestHole.Y, nearestHole.Z);
            newHoles.Add(nearestHole);
        }
        return newHoles;
    }

    // 计算炮孔线条的坐标，从起点开始，倾斜角度为 inclinationAngle 度，终点在下底面上
    public List<Point3D> CalculateBlastHoleLine(Point3D hole, double diameter, double inclinationAngle)
    {
        var angle = inclinationAngle * Math.PI / 180;
        var end = new Point3D(hole.X, hole.Y + diameter / Math.Tan(angle), hole.Z - diameter);
        return new List<Point3D> { hole, end };
    }

    // 给定起点和终点，等分线段为 count + 1 份，返回 count 个等分点坐标
    public List<Point3D> DivideLine(Point3D start, Point3D end, int count)
    {
        List<Point3D> points = new List<Point3D>();
        for (int i = 1; i <= count; i++)
        {
            var x = start.X + (end.X - start.X) * i / (count + 1);
            var y = start.Y + (end.Y - start.Y) * i / (count + 1);
            var z = start.Z + (end.Z - start.Z) * i / (count + 1);
            points.Add(new Point3D(x, y, z));
        }
        return points;
    }
}