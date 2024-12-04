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
                if (edge.style == 3 || edge.style == 4)
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
                if (edge.style == 3 || edge.style == 4)
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
        var end = new Point3D(hole.X, hole.Y + diameter / Math.Tan(inclinationAngle), hole.Z - diameter);
        return new List<Point3D> { hole, end };
    }

    // 给定起点和终点，等分线段为 count + 1 份，返回 count 个等分点坐标
    public List<Point3D> DivideLine(Point3D start, Point3D end, double startPadding, double endPadding, int count)
    {
        List<Point3D> points = new List<Point3D> { start };
        // 添加从 start 到 end 距离为 startPadding 的点
        var newStart = start - startPadding * (end - start).Normalize();
        points.Add(newStart);
        var newEnd = end - endPadding * (end - start).Normalize();
        for (int i = 1; i <= count; i++)
        {
            var x = newStart.X + (newEnd.X - newStart.X) * i / (count + 1);
            var y = newStart.Y + (newEnd.Y - newStart.Y) * i / (count + 1);
            var z = newStart.Z + (newEnd.Z - newStart.Z) * i / (count + 1);
            points.Add(new Point3D(x, y, z));
        }
        points.Add(newEnd);
        // Console.WriteLine("Start: " + start + ", End: " + end);
        // // 打印等分点坐标
        // foreach (var point in points)
        // {
        //     Console.WriteLine(point);
        // }
        return points;
    }

    // 重构版本的 DivideLine，用于没有永久轮廓的情况
    public List<Point3D> DivideLine(Point3D start, Point3D end, double endPadding, int count)
    {
        List<Point3D> points = new List<Point3D> { start };
        var newEnd = end - endPadding * (end - start).Normalize();
        for (int i = 1; i <= count; i++)
        {
            var x = start.X + (newEnd.X - start.X) * i / (count + 1);
            var y = start.Y + (newEnd.Y - start.Y) * i / (count + 1);
            var z = start.Z + (newEnd.Z - start.Z) * i / (count + 1);
            points.Add(new Point3D(x, y, z));
        }
        points.Add(newEnd);
        return points;
    }

    // 实现 IDisposable 接口
    private bool disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _config.Dispose();
            }
            // 释放非托管资源
        }
        disposed = true;
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    ~CrossSection()
    {
        Dispose(false);
    }
}