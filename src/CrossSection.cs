using BlastDesign.tool;
using SkiaSharp;
using MathNet.Spatial.Euclidean;

public class CrossSection : IDisposable
{
    #region Fields
    private readonly Config _config;
    private PreSplitPolygon _bottomPolygon;
    private List<BasePolygon> _blastPolygons;
    private List<HashSet<Point3D>> _blastHolePositions;
    private bool _hasNoPermanentEdge;
    private bool _disposed = false;
    #endregion

    #region Constructor
    public CrossSection(Config config, PreSplitPolygon bottomPolygon, List<BasePolygon> blastPolygons, List<HashSet<Point3D>> blastHolePositions)
    {
        _config = config;
        _bottomPolygon = bottomPolygon;
        _blastPolygons = blastPolygons;
        _blastHolePositions = blastHolePositions;
        _hasNoPermanentEdge = blastPolygons[0].HasNoPermanentEdge;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 计算剖面图的边界
    /// </summary>
    public List<Point3D> CalculateCrossSectionEdges(double x)
    {
        BasePolygon topPolygon = _blastPolygons[0];
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

        foreach (var edge in _bottomPolygon.Edges)
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

        newPoints.AddRange(new[] { a, b, c, d });
        return newPoints;
    }

    /// <summary>
    /// 找到距离 X 最近的炮孔对应的 Y 坐标
    /// </summary>
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

            if (Math.Abs(nearestHole.X - x) > _config.MainBlastHoleSpacing / 2)
            {
                continue;
            }

            nearestHole = new Point3D(x, nearestHole.Y, nearestHole.Z);
            newHoles.Add(nearestHole);
        }

        return newHoles;
    }

    /// <summary>
    /// 计算炮孔线条的坐标，从起点开始，倾斜角度为 inclinationAngle 度，终点在下底面上
    /// </summary>
    public List<Point3D> CalculateBlastHoleLine(Point3D hole, double diameter, double inclinationAngle)
    {
        var end = new Point3D(hole.X, hole.Y + diameter / Math.Tan(inclinationAngle), hole.Z - diameter);
        return new List<Point3D> { hole, end };
    }

    /// <summary>
    /// 给定起点和终点，等分线段为 count + 1 份，返回 count 个等分点坐标
    /// </summary>
    public List<Point3D> DivideLine(Point3D start, Point3D end, double startPadding, double endPadding, int count)
    {
        List<Point3D> points = new List<Point3D> { start };
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
        return points;
    }

    /// <summary>
    /// 重构版本的 DivideLine，用于没有永久轮廓的情况
    /// </summary>
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

    /// <summary>
    /// 实现 IDisposable 接口
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 根据中间剖面确定每一排炮孔的倾斜角度
    /// </summary>
    public List<double> CalculateInclinationAngle(double x)
    {
        var angles = new List<double>();
        angles.AddRange(Enumerable.Repeat(Math.PI / 2, _blastHolePositions.Count));
        List<Point3D> newEdges = CalculateCrossSectionEdges(x);
        List<Point3D> newHoles = FindNearestBlastHoles(_blastHolePositions, x);
        List<Point3D> bottomHoles;
        // 计算 newHoles[newHoles.Count - 1] 和 newEdges[0] 之间的距离
        var distance = newHoles[newHoles.Count - 1].DistanceTo(newEdges[0]);
        if (_hasNoPermanentEdge)
        {
            bottomHoles = DivideLine(newEdges[2], newEdges[3], distance, newHoles.Count - 2);
        }
        else
        {
            bottomHoles = DivideLine(newEdges[2], newEdges[3], _config.PreSplitHoleOffset, distance, newHoles.Count - 3);
        }

        for (int i = 0; i < newHoles.Count; i++)
        {
            var angle = Math.Atan2(newHoles[i].Z - bottomHoles[i].Z, -newHoles[i].Y + bottomHoles[i].Y);
            angles[i] = angle;
        }

        return angles;
    }

    /// <summary>
    /// 释放资源的具体实现
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _config.Dispose();
            }
            _disposed = true;
        }
    }
    #endregion

    #region Destructor
    ~CrossSection()
    {
        Dispose(false);
    }
    #endregion
}