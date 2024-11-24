using Clipper2Lib;
using MathNet.Spatial.Euclidean;

public class Constant
{
    public static double ErrorThreshold = 0.01;
}

public class Edge
{
    public Point3D Start { get; set; }
    public Point3D End { get; set; }
    public bool IsContour { get; set; }
    public Edge(Point3D start, Point3D end, bool isContour = true)
    {
        Start = start;
        End = end;
        IsContour = isContour;
    }

    public double Length()
        => (End - Start).Length;

    // 获取边的方向向量
    public Vector3D Direction()
        => (End - Start).Normalize().ToVector3D();

    // 获取边的法线向量
    public Vector3D Normal()
    {
        return new Vector3D(End.Y - Start.Y, Start.X - End.X, 0).Normalize().ToVector3D();
    }
}

// 基础多边形类
public abstract class BasePolygon
{
    public List<Edge> Edges { get; protected set; }
    public List<Point3D> HolePoints { get; protected set; }
    public double MinDistanceToFreeLine { get; protected set; }

    protected BasePolygon(List<Edge> edges)
    {
        Edges = edges;
        HolePoints = new List<Point3D>();
    }

    // 偏移方法 - 所有多边形都具有相同的偏移逻辑
    public abstract BasePolygon? Offset(double edgeDistance, double holeDistance, double spacing);

    // 布孔方法 - 不同类型多边形有不同的布孔逻辑
    public abstract void ArrangeHoles(double spacing, bool isContourLineEndHoleEnabled = true);

    protected void AddPoint(Point3D point, bool isContourLineEndHoleEnabled)
    {
        if (isContourLineEndHoleEnabled || !IsPointOnFreeEdge(point))
        {
            HolePoints.Add(point);
        }
    }

    protected void AddPoints(IEnumerable<Point3D> points, List<Edge> Edges)
    {
        foreach (var point in points)
        {
            if (IsPointOnCouterEdge(point) && !IsPointNearFreeEdge(point, Edges))
            {
                HolePoints.Add(point);
            }
        }
    }

    private bool IsPointOnCouterEdge(Point3D point)
    {
        foreach (var edge in Edges)
        {
            if (isPointOnEdge(point, edge))
            {
                return true;
            }
        }
        return false;
    }

    // 使用 Clipper 实现多边形的偏移逻辑，返回偏移后的多边形的边
    protected List<Edge> OffsetEdges(double distance, ExtendedPolygon extendedPolygon)
    {
        // 创建原始多边形路径
        PathD originalPolygonPath = new PathD();
        foreach (var edge in Edges)
        {
            originalPolygonPath.Add(new PointD(edge.Start.X, edge.Start.Y));
        }
        // 计算偏移后的多边形路径
        PathsD offsetPaths = extendedPolygon.Offset(distance), solutionPaths;
        if (extendedPolygon.isClosedPolygon())
        {
            // 计算偏移路径和原始多边形路径的交集，得到新的多边形路径
            solutionPaths = Clipper.Intersect(new PathsD { originalPolygonPath }, offsetPaths, FillRule.NonZero);
        }
        else
        {
            // 计算原始路径和偏移路径的差集，得到新的多边形路径
            solutionPaths = Clipper.Difference(new PathsD { originalPolygonPath }, offsetPaths, FillRule.NonZero);
        }
        // 从结果路径中提取新的边
        List<Edge> newEdges = new List<Edge>();
        foreach (var path in solutionPaths)
        {
            for (int i = 0; i < path.Count; i++)
            {
                var start = new Point3D(path[i].x, path[i].y, Edges.First().Start.Z);
                var end = new Point3D(path[(i + 1) % path.Count].x, path[(i + 1) % path.Count].y, Edges.First().Start.Z);
                newEdges.Add(new Edge(start, end, true));
            }
        }
        return newEdges;
    }

    // 偏移一个点，返回偏移后的点
    protected List<Point3D> OffsetPoints(double distance, double spacing)
    {
        List<Point3D> returnPoints = new List<Point3D>();
        foreach (var p in HolePoints)
        {
            foreach (var edge in Edges)
            {
                if (isPointOnEdge(p, edge))
                {
                    Vector3D normal = edge.Normal();
                    Vector3D direction = edge.Direction();
                    returnPoints.Add(p + distance * normal);
                    returnPoints.Add(p - distance * normal);
                }
            }
        }
        return returnPoints;
    }

    // 判断一个点是否在边上
    public bool isPointOnEdge(Point3D point, Edge edge)
    {
        var line3D = new LineSegment3D(edge.Start, edge.End);
        return line3D.ClosestPointTo(point).DistanceTo(point) < Constant.ErrorThreshold;
    }

    // 计算一个点到线段的距离
    protected double DistanceToEdge(Point3D point, Edge edge)
    {
        var line3D = new LineSegment3D(edge.Start, edge.End);
        return line3D.ClosestPointTo(point).DistanceTo(point);
    }

    // 判断点是否在自由线上
    protected bool IsPointOnFreeEdge(Point3D point)
    {
        foreach (var edge in Edges)
        {
            if (!edge.IsContour)
            {
                if (DistanceToEdge(point, edge) < Constant.ErrorThreshold)
                {
                    return true;
                }
            }
        }
        return false;
    }

    protected bool IsEqualToAnyFreeEdge(Edge edge, List<Edge> Edges)
    {
        foreach (var e in Edges)
        {
            if (edge.Direction().Equals(e.Direction(), Constant.ErrorThreshold))
            {
                return true;
            }
        }
        return false;
    }

    // 判断点是否在自由线周围
    protected bool IsPointNearFreeEdge(Point3D point, List<Edge> Edges)
    {
        foreach (var edge in Edges)
        {
            if (DistanceToEdge(point, edge) < MinDistanceToFreeLine)
            {
                return true;
            }
        }
        return false;
    }

    // 在当前边上按照指定间距布孔（缓冲孔和主爆孔）
    protected (List<Point3D> Points, double RemainingLength) PlaceHolesAlongEdge(double initialOffset, Edge edge, double spacing, bool isForward)
    {
        List<Point3D> points = new List<Point3D>();
        Vector3D direction = edge.Direction();
        Point3D currentPoint = isForward ? edge.Start + initialOffset * direction : edge.End - initialOffset * direction;
        while (true)
        {
            if (!isPointOnEdge(currentPoint, edge))
            {
                double remainingLength = isForward ? (edge.End - currentPoint).Length : (edge.Start - currentPoint).Length;
                return (points, remainingLength);
            }
            points.Add(currentPoint);
            currentPoint = isForward ? currentPoint + spacing * direction : currentPoint - spacing * direction;
        }
    }
}

// 扩展多边形类
public class ExtendedPolygon
{
    bool isClosed;
    // 闭合多边形路径
    PathD polygonPath;
    // 开放多边形路径
    PathD openPath;
    double offsetDistance;
    public ExtendedPolygon(List<Edge> edges)
    {
        polygonPath = new PathD();
        openPath = new PathD();
        offsetDistance = 0;
        isClosed = edges.First().Start.Equals(edges.Last().End);
        if (isClosed)
        {
            foreach (var edge in edges)
            {
                polygonPath.Add(new PointD(edge.Start.X, edge.Start.Y));
            }
        }
        else
        {
            foreach (var edge in edges)
            {
                openPath.Add(new PointD(edge.Start.X, edge.Start.Y));
            }
            openPath.Add(new PointD(edges.Last().End.X, edges.Last().End.Y));
        }
        Console.WriteLine("ExtendedPolygon created.");
    }

    public PathsD Offset(double distance)
    {
        offsetDistance += distance;
        if (isClosed)
        {
            return Clipper.InflatePaths(new PathsD { polygonPath }, offsetDistance, JoinType.Miter, EndType.Polygon);
        }
        return Clipper.InflatePaths(new PathsD { openPath }, offsetDistance, JoinType.Miter, EndType.Square);
    }

    public bool isClosedPolygon()
    {
        return isClosed;
    }
}

// 预裂孔多边形
public class PreSplitPolygon : BasePolygon
{
    public ExtendedPolygon extendedPolygon { get; private set; }
    public PreSplitPolygon(List<Edge> edges, double MinDistanceToFreeLine = 0.5) : base(edges)
    {
        this.MinDistanceToFreeLine = MinDistanceToFreeLine;
        extendedPolygon = CreateExtendedPolygon();
    }

    public override BasePolygon Offset(double edgeDistance, double holeDistance, double spacing)
    {
        List<Edge> newEdges = OffsetEdges(edgeDistance, extendedPolygon);
        List<Point3D> newPoints = OffsetPoints(holeDistance, spacing);
        return new BufferPolygon(newEdges, newPoints, this, MinDistanceToFreeLine);
    }

    public override void ArrangeHoles(double spacing, bool isContourLineEndHoleEnabled = false)
    {
        // 仅在轮廓边上布置炮孔
        foreach (var edge in Edges.Where(e => e.IsContour))
        {
            ArrangeHoleOnEdge(edge, spacing, isContourLineEndHoleEnabled);
        }
    }

    // 获取自由边方法，供其他多边形使用
    public List<Edge> GetFreeEdges()
    {
        return Edges.Where(e => !e.IsContour).ToList();
    }

    // 创建 extendPolygon 对象，供其他多边形使用
    public ExtendedPolygon CreateExtendedPolygon()
    {
        return OffsetContourStartAndEnd();
    }

    private ExtendedPolygon OffsetContourStartAndEnd()
    {
        (Point3D newStart, Point3D newEnd) = MoveContourStartandEnd();
        var newEdges = new List<Edge>();
        foreach (var edge in Edges)
        {
            if (edge.IsContour)
            {
                // 如果是第一条轮廓线，修改起点然后添加
                if (edge.Start.Equals(FindFirstAndLastContour().Item1.Start))
                {
                    newEdges.Add(new Edge(newStart, edge.End, true));
                } // 如果是最后一条轮廓线，修改终点然后添加
                else if (edge.End.Equals(FindFirstAndLastContour().Item2.End))
                {
                    newEdges.Add(new Edge(edge.Start, newEnd, true));
                }
                else
                {
                    newEdges.Add(edge);
                }
            }
        }
        return new ExtendedPolygon(newEdges);
    }

    // 偏移轮廓线的起点和终点，返回新的起点和终点坐标
    private (Point3D, Point3D) MoveContourStartandEnd()
    {
        // 找到第一和最后一个轮廓线
        (Edge first, Edge last) = FindFirstAndLastContour();
        var firstLine3D = new Line3D(first.Start, first.End);
        var lastLine3D = new Line3D(last.Start, last.End);
        if (firstLine3D.IsParallelTo(lastLine3D))
        {
            Point3D newStart = first.Start - first.Length() * first.Direction();
            Point3D newEnd = last.End + last.Length() * last.Direction();
            return (newStart, newEnd);
        }
        // 找到第一和最后一个轮廓线的交点
        (Point3D point1, Point3D point2) = firstLine3D.ClosestPointsBetween(lastLine3D);
        if (!point1.Equals(point2, Constant.ErrorThreshold))
        {
            throw new Exception("Error!!! The contour lines are not parallel and do not intersect.");
        }
        // 如果 first.end 和 point1 在 first.start 的同侧
        if (first.Direction() * (point1 - first.Start) > 0)
        {
            Point3D newStart = first.Start - first.Length() * first.Direction();
            Point3D newEnd = last.End + last.Length() * last.Direction();
            return (newStart, newEnd);
        }
        else
        {
            return (point1, point2);
        }
    }

    // 找到多边形的第一和最后一个轮廓线
    private (Edge, Edge) FindFirstAndLastContour()
    {
        Edge first = Edges.First(e => e.IsContour);
        Edge last = Edges.Last(e => e.IsContour);
        return (first, last);
    }

    // 在线段上均匀分布点
    private void ArrangeHoleOnEdge(Edge edge, double holeSpacing, bool isContourLineEndHoleEnabled)
    {
        Vector3D direction = edge.Direction();
        double pointCount = (int)Math.Round(edge.Length() / holeSpacing);
        double holeSpacingActual = edge.Length() / pointCount;
        for (int i = 0; i <= pointCount; i++)
        {
            Point3D point = edge.Start + i * holeSpacingActual * direction;
            AddPoint(point, isContourLineEndHoleEnabled);
        }
    }

    // 查找多边形上到指定点最近的点
    public Point3D FindNearestPoint(Point3D point)
    {
        double minDistance = double.MaxValue;
        Point3D nearestPoint = new Point3D();
        foreach (var edge in Edges)
        {
            var line3D = new LineSegment3D(edge.Start, edge.End);
            var closestPoint = line3D.ClosestPointTo(point);
            double distance = closestPoint.DistanceTo(point);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = closestPoint;
            }
        }
        return nearestPoint;
    }
}

// 缓冲孔多边形
public class BufferPolygon : BasePolygon
{
    private readonly PreSplitPolygon _preSplitPolygon;

    public BufferPolygon(List<Edge> edges, List<Point3D> points, PreSplitPolygon preSplitPolygon, double MinDistanceToFreeLine) : base(edges)
    {
        this.MinDistanceToFreeLine = MinDistanceToFreeLine;
        _preSplitPolygon = preSplitPolygon;
        AddPoints(points, new List<Edge>());
    }

    public override BasePolygon Offset(double edgeDistance, double holeDistance, double spacing)
    {
        List<Edge> newEdges = OffsetEdges(edgeDistance, _preSplitPolygon.extendedPolygon);
        List<Point3D> newPoints = OffsetPoints(holeDistance, spacing);
        return new MainBlastPolygon(newEdges, newPoints, _preSplitPolygon, MinDistanceToFreeLine);
    }

    public override void ArrangeHoles(double interval, bool isContourLineEndHoleEnabled = true)
    {
        List<Point3D> tmp = new List<Point3D>();
        HashSet<Edge> visitedEdges = new HashSet<Edge>();
        // 通过已知孔找到起始点
        Point3D startPoint = HolePoints.First();
        Edge startEdge = Edges.First(e => isPointOnEdge(startPoint, e));
        Edge currentEdge = startEdge;
        double paddingDistance = (startPoint - currentEdge.Start).Length;
        // 正向布孔
        while (true)
        {
            (List<Point3D> edgePoints, double remainLength) = PlaceHolesAlongEdge(paddingDistance, currentEdge, interval, true);
            tmp.AddRange(edgePoints);
            visitedEdges.Add(currentEdge);
            // 从 Edge 中找到起点是 currentEdge 终点的边
            Edge nextEdge = Edges.First(e => e.Start.Equals(currentEdge.End));
            if (IsEqualToAnyFreeEdge(nextEdge, _preSplitPolygon.GetFreeEdges()) || visitedEdges.Contains(nextEdge))
            {
                break;
            }
            currentEdge = nextEdge;
            paddingDistance = remainLength;
        }
        currentEdge = startEdge;
        paddingDistance = (startPoint - currentEdge.End).Length;
        // 反向布孔
        while (true)
        {
            (List<Point3D> edgePoints, double remainLength) = PlaceHolesAlongEdge(paddingDistance, currentEdge, interval, false);
            tmp.AddRange(edgePoints);
            // 从 Edge 中找到终点是 currentEdge 起点的边
            Edge nextEdge = Edges.First(e => e.End.Equals(currentEdge.Start));
            if (IsEqualToAnyFreeEdge(nextEdge, _preSplitPolygon.GetFreeEdges()) || visitedEdges.Contains(nextEdge))
            {
                break;
            }
            currentEdge = nextEdge;
            paddingDistance = remainLength;
        }
        HolePoints = new List<Point3D>();
        AddPoints(tmp, _preSplitPolygon.GetFreeEdges());
    }
}

// 主爆孔多边形
public class MainBlastPolygon : BasePolygon
{
    private readonly PreSplitPolygon _preSplitPolygon;

    public MainBlastPolygon(List<Edge> edges, List<Point3D> points, PreSplitPolygon preSplitPolygon, double MinDistanceToFreeLine) : base(edges)
    {
        this.MinDistanceToFreeLine = MinDistanceToFreeLine;
        _preSplitPolygon = preSplitPolygon;
        AddPoints(points, new List<Edge>());
    }

    public override BasePolygon? Offset(double edgeDistance, double holeDistance, double spacing)
    {
        List<Edge> newEdges = OffsetEdges(edgeDistance, _preSplitPolygon.extendedPolygon);
        List<Point3D> newPoints = OffsetPoints(holeDistance, spacing);
        if (newEdges.Count == 0)
        {
            return null;
        }
        return new MainBlastPolygon(newEdges, newPoints, _preSplitPolygon, MinDistanceToFreeLine);
    }

    public override void ArrangeHoles(double interval, bool isContourLineEndHoleEnabled = true)
    {
        List<Point3D> tmp = new List<Point3D>();
        HashSet<Edge> visitedEdges = new HashSet<Edge>();
        // 通过已知孔找到起始点
        Point3D startPoint = HolePoints.First();
        Edge startEdge = Edges.First(e => isPointOnEdge(startPoint, e));
        Edge currentEdge = startEdge;
        double paddingDistance = (startPoint - currentEdge.Start).Length;
        // 正向布孔
        while (true)
        {
            (List<Point3D> edgePoints, double remainLength) = PlaceHolesAlongEdge(paddingDistance, currentEdge, interval, true);
            tmp.AddRange(edgePoints);
            visitedEdges.Add(currentEdge);
            // 从 Edge 中找到起点是 currentEdge 终点的边
            Edge nextEdge = Edges.First(e => e.Start.Equals(currentEdge.End));
            if (IsEqualToAnyFreeEdge(nextEdge, _preSplitPolygon.GetFreeEdges()) || visitedEdges.Contains(nextEdge))
            {
                break;
            }
            currentEdge = nextEdge;
            paddingDistance = remainLength;
        }
        currentEdge = startEdge;
        paddingDistance = (startPoint - currentEdge.End).Length;
        // 反向布孔
        while (true)
        {
            (List<Point3D> edgePoints, double remainLength) = PlaceHolesAlongEdge(paddingDistance, currentEdge, interval, false);
            tmp.AddRange(edgePoints);
            // 从 Edge 中找到终点是 currentEdge 起点的边
            Edge nextEdge = Edges.First(e => e.End.Equals(currentEdge.Start));
            if (IsEqualToAnyFreeEdge(nextEdge, _preSplitPolygon.GetFreeEdges()) || visitedEdges.Contains(nextEdge))
            {
                break;
            }
            currentEdge = nextEdge;
            paddingDistance = remainLength;
        }
        HolePoints = new List<Point3D>();
        AddPoints(tmp, _preSplitPolygon.GetFreeEdges());
    }
}
