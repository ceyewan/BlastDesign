using Clipper2Lib;
using MathNet.Spatial.Euclidean;

namespace BlastDesign.tool.BlackBoxTest
{
    public class Constant
    {
        public static double ErrorThreshold = 0.01;
    }

    public class Edge
    {
        public Point3D Start { get; set; }
        public Point3D End { get; set; }
        public int style { get; set; }
        public Edge(Point3D start, Point3D end, int Style = 1)
        {
            Start = start;
            End = end;
            style = Style;
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
    public abstract class BasePolygon : IDisposable
    {
        public List<Edge> Edges { get; protected set; }
        public List<Point3D> HolePoints { get; protected set; }
        public double MinDistanceToFreeLine { get; protected set; }
        public Point3D StartPoint { get; protected set; }
        public Point3D EndPoint { get; protected set; }
        public double TotalLength { get; protected set; }
        public bool HasNoPermanentEdge { get; protected set; } // 是否没有永久轮廓线

        protected BasePolygon(List<Edge> edges)
        {
            Edges = edges;
            HolePoints = new List<Point3D>();
            // 如果有类型为 3 的边，设置起点和终点；
            if (Edges.Any(e => e.style == 3))
            {
                StartPoint = Edges.First(e => e.style == 3).Start;
                EndPoint = Edges.Last(e => e.style == 3).End;
                // 类型为 3 的边的数量
                // Console.WriteLine("Number of Contour Lines: " + Edges.Count(e => e.style == 3));
            }
            else if (Edges.Any(e => e.style == 4))
            {
                HasNoPermanentEdge = true;
                StartPoint = Edges.First(e => e.style == 4).Start;
                EndPoint = Edges.Last(e => e.style == 4).End;
            }
            else
            {
                throw new Exception("Error!!! The polygon does not have contour lines.");
            }
            // 计算从 Start 到 End 的长度，注意是折线的长度
            TotalLength = 0;
            var currentEdge = Edges.First(e => e.Start.Equals(StartPoint, Constant.ErrorThreshold));
            while (true)
            {
                TotalLength += currentEdge.Length();
                if (currentEdge.End.Equals(EndPoint, Constant.ErrorThreshold))
                {
                    break;
                }
                currentEdge = Edges.First(e => e.Start.Equals(currentEdge.End, Constant.ErrorThreshold));
            }
        }

        // 偏移方法 - 所有多边形都具有相同的偏移逻辑
        public abstract BasePolygon? Offset(double edgeDistance, bool isContourLineEndHoleEnabled = false);

        // 布孔方法 - 不同类型多边形有不同的布孔逻辑
        public abstract void ArrangeHoles(double spacing, bool isContourLineEndHoleEnabled = true);

        protected void AddPoint(Point3D point, bool isContourLineEndHoleEnabled)
        {
            if (isContourLineEndHoleEnabled || !IsPointOnFreeEdge(point))
            {
                HolePoints.Add(point);
            }
        }

        protected void AddPoint(Point3D point, List<Edge> freeEdges)
        {
            if (!IsPointNearFreeEdge(point, freeEdges))
            {
                HolePoints.Add(point);
            }
        }

        protected void AddPoints(IEnumerable<Point3D> points, List<Edge> edges, PreSplitPolygon preSplitPolygon)
        {
            var pointsToAdd = points.Where(point => !edges.Any(edge => IsPointOnEdge(point, edge)));
            foreach (var point in pointsToAdd)
            {
                AddPoint(point, preSplitPolygon.GetFreeEdges());
            }
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
                    // 如果这条边平行于任何一条自由边，设置为自由边；否则设置为普通轮廓线
                    if (IsEdgeOverlapping(new Edge(start, end, 1), Edges))
                    {
                        newEdges.Add(new Edge(start, end, 1));
                    }
                    else
                    {
                        newEdges.Add(new Edge(start, end, 3));
                    }
                }
            }
            // 如果 newEdges 中有 1，那么循环左移将 1 移动到列表最前面
            if (newEdges.Any(e => e.style == 1))
            {
                int index = newEdges.FindIndex(e => e.style == 1);
                newEdges = newEdges.Skip(index).Concat(newEdges.Take(index)).ToList();
            }
            return newEdges;
        }

        // 判断一个点是否在边上
        public bool IsPointOnEdge(Point3D point, Edge edge)
        {
            var line = new LineSegment3D(edge.Start, edge.End);
            return line.ClosestPointTo(point).DistanceTo(point) < Constant.ErrorThreshold;
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
                if (edge.style == 1)
                {
                    if (DistanceToEdge(point, edge) < Constant.ErrorThreshold)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // 判断某条边和给定的一些边是否有重叠
        protected bool IsEdgeOverlapping(Edge edge, List<Edge> Edges)
        {
            foreach (var e in Edges)
            {
                // 首先检查方向是否平行
                if (!edge.Direction().IsParallelTo(e.Direction(), MathNet.Spatial.Units.Angle.FromDegrees(1)))
                {
                    continue;
                }
                // 检查是否有端点在另一边上
                if (IsPointOnEdge(edge.Start, e) ||
                    IsPointOnEdge(edge.End, e) ||
                    IsPointOnEdge(e.Start, edge) ||
                    IsPointOnEdge(e.End, edge))
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
        protected (List<Point3D> Points, double RemainingLength) PlaceHolesAlongEdge(double initialOffset, Edge edge, double spacing)
        {
            List<Point3D> points = new List<Point3D>();
            Vector3D direction = edge.Direction();
            Point3D currentPoint = edge.Start + initialOffset * direction;
            while (true)
            {
                if (!IsPointOnEdge(currentPoint, edge))
                {
                    double remainingLength = (edge.End - currentPoint).Length;
                    return (points, remainingLength);
                }
                points.Add(currentPoint);
                currentPoint += spacing * direction;
            }
        }
        // 实现 IDisposable 接口
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    Edges.Clear();
                    HolePoints.Clear();
                }
                // 释放非托管资源（如果有）
                disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BasePolygon()
        {
            Dispose(false);
        }
    }

    // 扩展多边形类
    public class ExtendedPolygon : IDisposable
    {
        public bool isClosed;
        // 闭合多边形路径
        public PathD polygonPath;
        // 开放多边形路径
        public PathD openPath;
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
            // Console.WriteLine("ExtendedPolygon created.");
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

        // 实现 IDisposable 接口
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    polygonPath.Clear();
                    openPath.Clear();
                }
                // 释放非托管资源（如果有）
                disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~ExtendedPolygon()
        {
            Dispose(false);
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

        public override BasePolygon Offset(double edgeDistance, bool isMainBlastPolygon = false)
        {
            List<Edge> newEdges = OffsetEdges(edgeDistance, extendedPolygon);
            if (isMainBlastPolygon)
            {
                return new MainBlastPolygon(newEdges, this, MinDistanceToFreeLine);
            }
            else
            {
                return new BufferPolygon(newEdges, this, MinDistanceToFreeLine);
            }
        }

        public override void ArrangeHoles(double spacing, bool isContourLineEndHoleEnabled = false)
        {
            foreach (var edge in Edges)
            {
                if (edge.style == 3 || edge.style == 5)
                {
                    ArrangeHoleOnEdge(edge, spacing, isContourLineEndHoleEnabled);
                }
            }
        }

        public void ArrangeHoles(List<int> holeCount, bool isContourLineEndHoleEnabled = false)
        {
            int count = 0;
            foreach (var edge in Edges)
            {
                if (edge.style == 3 || edge.style == 5)
                {
                    ArrangeHoleOnEdge(edge, holeCount[count++], isContourLineEndHoleEnabled);
                }
            }
        }

        // 获取自由边方法，供其他多边形使用
        public List<Edge> GetFreeEdges()
        {
            return Edges.Where(e => e.style == 1).ToList();
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
                if (HasNoPermanentEdge ? edge.style == 4 : edge.style == 3)
                {
                    // 如果只有一条轮廓线，修改起点和终点然后添加
                    if (edge.Start.Equals(FindFirstAndLastContour().Item1.Start) && edge.End.Equals(FindFirstAndLastContour().Item2.End))
                    {
                        newEdges.Add(new Edge(newStart, newEnd, 3));
                    } // 如果是第一条轮廓线，修改起点然后添加
                    else if (edge.Start.Equals(FindFirstAndLastContour().Item1.Start))
                    {
                        newEdges.Add(new Edge(newStart, edge.End, 3));
                    } // 如果是最后一条轮廓线，修改终点然后添加
                    else if (edge.End.Equals(FindFirstAndLastContour().Item2.End))
                    {
                        newEdges.Add(new Edge(edge.Start, newEnd, 3));
                    }
                    else
                    {
                        newEdges.Add(edge);
                    }
                }
            }
            // Console.WriteLine("OffsetContourStartAndEnd.");
            // // 打印 edges 的数量
            // Console.WriteLine("Number of Edges: " + newEdges.Count);
            return new ExtendedPolygon(newEdges);
        }

        // 偏移轮廓线的起点和终点，返回新的起点和终点坐标
        private (Point3D, Point3D) MoveContourStartandEnd()
        {
            // 找到第一和最后一个轮廓线
            (Edge first, Edge last) = FindFirstAndLastContour();
            var firstLine3D = new Line3D(first.Start, first.End);
            var lastLine3D = new Line3D(last.Start, last.End);
            if (firstLine3D.IsParallelTo(lastLine3D, MathNet.Spatial.Units.Angle.FromRadians(Constant.ErrorThreshold)))
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
            Edge? first = Edges.FirstOrDefault(e => e.style == 3);
            Edge? last = Edges.LastOrDefault(e => e.style == 3);
            // 如果没找到类型为 3 的边，就找类型为 4 的边
            if (first == null || last == null)
            {
                first = Edges.FirstOrDefault(e => e.style == 4);
                last = Edges.LastOrDefault(e => e.style == 4);
            }
            // 如果仍然没有找到，抛出异常
            if (first == null || last == null)
            {
                throw new InvalidOperationException("未找到类型为 3 或 4 的边");
            }
            return (first, last);
        }

        // 在线段上均匀分布点
        private int ArrangeHoleOnEdge(Edge edge, double holeSpacing, bool isContourLineEndHoleEnabled)
        {
            Vector3D direction = edge.Direction();
            int pointCount = (int)Math.Round(edge.Length() / holeSpacing);
            double holeSpacingActual = edge.Length() / pointCount;
            for (int i = 0; i <= pointCount; i++)
            {
                Point3D point = edge.Start + i * holeSpacingActual * direction;
                AddPoint(point, isContourLineEndHoleEnabled);
            }
            return pointCount;
        }

        // 在线段上均匀分布点
        private void ArrangeHoleOnEdge(Edge edge, int holeCount, bool isContourLineEndHoleEnabled)
        {
            Vector3D direction = edge.Direction();
            double holeSpacingActual = edge.Length() / (holeCount - 1);
            for (int i = 0; i < holeCount; i++)
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

        // 查找多边形自由边到指定点的最近距离
        public double FindNearestDistanceToFreeEdge(Point3D point)
        {
            double minDistance = double.MaxValue;
            foreach (var edge in Edges)
            {
                if (edge.style.Equals(1))
                {
                    var line3D = new LineSegment3D(edge.Start, edge.End);
                    double distance = line3D.ClosestPointTo(point).DistanceTo(point);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }
            return minDistance;
        }

        // 实现 IDisposable 接口
        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放 BufferPolygon 特有的托管资源
                    extendedPolygon.Dispose();
                }
                // 释放 BasePolygon 的资源
                base.Dispose(disposing);
                disposed = true;
            }
        }
    }

    // 缓冲孔多边形
    public class BufferPolygon : BasePolygon
    {
        private readonly PreSplitPolygon _preSplitPolygon;
        private bool disposed = false;

        public BufferPolygon(List<Edge> edges, PreSplitPolygon preSplitPolygon, double MinDistanceToFreeLine) : base(edges)
        {
            this.MinDistanceToFreeLine = MinDistanceToFreeLine;
            _preSplitPolygon = preSplitPolygon;
        }

        public override BasePolygon Offset(double edgeDistance, bool isMainBlastPolygon = false)
        {
            List<Edge> newEdges = OffsetEdges(edgeDistance, _preSplitPolygon.extendedPolygon);
            return new MainBlastPolygon(newEdges, _preSplitPolygon, MinDistanceToFreeLine);
        }

        public override void ArrangeHoles(double interval, bool isContourLineEndHoleEnabled = true)
        {
            double intervalActual = TotalLength / (int)Math.Round(TotalLength / interval);
            // 从 Start 开始，每隔 interval 布置一个孔，直到 End 为止
            List<Point3D> tmp = new List<Point3D>();
            Edge currentEdge = Edges.First(e => e.Start.Equals(StartPoint));
            double paddingDistance = (StartPoint - currentEdge.Start).Length;
            while (true)
            {
                (List<Point3D> edgePoints, double remainLength) = PlaceHolesAlongEdge(paddingDistance, currentEdge, intervalActual);
                tmp.AddRange(edgePoints);
                if (currentEdge.End.Equals(EndPoint))
                {
                    break;
                }
                currentEdge = Edges.First(e => e.Start.Equals(currentEdge.End));
                paddingDistance = remainLength;
            }
            // 缓冲孔，将自由边上的炮孔排除
            AddPoints(tmp, _preSplitPolygon.GetFreeEdges(), _preSplitPolygon);
        }

        public void ArrangeHoles(int holeCount, bool isContourLineEndHoleEnabled = true)
        {
            double intervalActual = TotalLength / (holeCount - 1);
            // 从 Start 开始，每隔 interval 布置一个孔，直到 End 为止
            List<Point3D> tmp = new List<Point3D>();
            Edge currentEdge = Edges.First(e => e.Start.Equals(StartPoint));
            double paddingDistance = (StartPoint - currentEdge.Start).Length;
            while (true)
            {
                (List<Point3D> edgePoints, double remainLength) = PlaceHolesAlongEdge(paddingDistance, currentEdge, intervalActual);
                tmp.AddRange(edgePoints);
                if (currentEdge.End.Equals(EndPoint))
                {
                    break;
                }
                currentEdge = Edges.First(e => e.Start.Equals(currentEdge.End));
                paddingDistance = remainLength;
            }
            // 缓冲孔，将自由边上的炮孔排除
            AddPoints(tmp, _preSplitPolygon.GetFreeEdges(), _preSplitPolygon);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放 BufferPolygon 特有的托管资源
                    _preSplitPolygon.Dispose();
                }
                // 释放 BasePolygon 的资源
                base.Dispose(disposing);
                disposed = true;
            }
        }
    }

    // 主爆孔多边形
    public class MainBlastPolygon : BasePolygon
    {
        private readonly PreSplitPolygon _preSplitPolygon;
        private bool disposed = false;

        public MainBlastPolygon(List<Edge> edges, PreSplitPolygon preSplitPolygon, double MinDistanceToFreeLine) : base(edges)
        {
            this.MinDistanceToFreeLine = MinDistanceToFreeLine;
            _preSplitPolygon = preSplitPolygon;
        }

        public override BasePolygon? Offset(double edgeDistance, bool isMainBlastPolygon = false)
        {
            List<Edge> newEdges = OffsetEdges(edgeDistance, _preSplitPolygon.extendedPolygon);
            if (newEdges.Count == 0)
            {
                return null;
            }
            return new MainBlastPolygon(newEdges, _preSplitPolygon, MinDistanceToFreeLine);
        }

        public override void ArrangeHoles(double interval, bool isContourLineEndHoleEnabled = true)
        {
            // 打印 startPoints 和 endPoints 和 TotalLength
            // Console.WriteLine("Start: " + StartPoint + ", End: " + EndPoint + ", Length: " + TotalLength);
            double intervalActual = TotalLength / (int)Math.Round(TotalLength / interval);
            // 从 Start 开始，每隔 interval 布置一个孔，直到 End 为止
            List<Point3D> tmp = new List<Point3D>();
            Edge currentEdge = Edges.First(e => e.Start.Equals(StartPoint));
            double paddingDistance = (StartPoint - currentEdge.Start).Length;
            while (true)
            {
                (List<Point3D> edgePoints, double remainLength) = PlaceHolesAlongEdge(paddingDistance, currentEdge, intervalActual);
                tmp.AddRange(edgePoints);
                if (currentEdge.End.Equals(EndPoint))
                {
                    break;
                }
                currentEdge = Edges.First(e => e.Start.Equals(currentEdge.End));
                paddingDistance = remainLength;
            }
            // 主爆孔，将在自由边上的炮孔排除
            AddPoints(tmp, _preSplitPolygon.GetFreeEdges(), _preSplitPolygon);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放 BufferPolygon 特有的托管资源
                    _preSplitPolygon.Dispose();
                }
                // 释放 BasePolygon 的资源
                base.Dispose(disposing);
                disposed = true;
            }
        }
    }
}