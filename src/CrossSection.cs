using System.Runtime.InteropServices;
using MathNet.Spatial.Euclidean;

namespace BlastDesign.tool.BlackBoxTest
{
    /// <summary>
    /// 处理爆破设计的横断面计算
    /// </summary>
    public class CrossSection(
        Config config,
        PreSplitPolygon bottomPolygon,
        List<BasePolygon> blastPolygons,
        List<HashSet<Point3D>> blastHolePositions) : IDisposable
    {
        #region Fields
        private readonly Config _config = config;
        private readonly PreSplitPolygon _bottomPolygon = bottomPolygon;
        private readonly List<BasePolygon> _blastPolygons = blastPolygons;
        private readonly List<HashSet<Point3D>> _blastHolePositions = blastHolePositions;
        private readonly bool _hasNoPermanentEdge = blastPolygons[0].HasNoPermanentEdge;
        private bool _disposed;
        #endregion

        #region Public Methods
        /// <summary>
        /// 生成剖面图，计算边界和炮孔线条
        /// </summary>
        /// <param name="x">横坐标位置</param>
        /// <returns>边界点列表和炮孔线条列表</returns>
        public (List<Point3D>, List<List<Point3D>>) GenerateCrossSection(double x)
        {
            List<Point3D> edges = CalculateCrossSectionEdges(x);
            List<Point3D> holes = FindNearestBlastHoles(_blastHolePositions, x);
            List<double> angles = CalculateInclinationAngle(x);
            List<List<Point3D>> holeLines = [];

            for (int i = 0; i < holes.Count; i++)
            {
                var depth = holes[i].Z - _bottomPolygon.Edges[0].Start.Z;
                var extraDepth = i > 1 || _hasNoPermanentEdge ? _config.Depth * Math.Sin(angles[i]) : 0;
                if (i > 0 || _hasNoPermanentEdge)
                {
                    depth /= _config.IsMainBlastHoleSegmented ? 2 : 1;
                }
                var diameter = depth + extraDepth;

                var hole1 = holes[i];
                var blastHoleDiameter = _config.BlastHoleDiameters[i > 1 || _hasNoPermanentEdge ? 2 : i];
                var hole2 = new Point3D(hole1.X, hole1.Y - blastHoleDiameter, hole1.Z);

                holeLines.Add([.. CalculateBlastHoleLine(hole1, diameter, angles[i])]);
                holeLines.Add([.. CalculateBlastHoleLine(hole2, diameter, angles[i])]);
            }

            return (edges, holeLines);
        }

        /// <summary>
        /// 计算炮孔的倾斜角度
        /// </summary>
        /// <param name="x">横坐标位置</param>
        /// <returns>倾斜角度列表</returns>
        /// <remarks>如果没有永久边坡，则只有两个倾斜角度</remarks>
        /// <remarks>否则，有三个倾斜角度</remarks>
        /// <remarks>第一个倾斜角度是底部边坡的倾斜角度</remarks>
        /// <remarks>第二个倾斜角度是第一排炮孔的倾斜角度</remarks>
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
        #endregion

        #region Private Methods
        /// <summary>
        /// 计算剖面图的边界点
        /// </summary>
        private List<Point3D> CalculateCrossSectionEdges(double x)
        {
            var topPolygon = _blastPolygons[0];
            var (a, b, c, d) = (new Point3D(), new Point3D(), new Point3D(), new Point3D());
            ProcessPolygonEdges(topPolygon, x, ref a, ref b);
            ProcessPolygonEdges(_bottomPolygon, x, ref c, ref d);
            return [a, b, d, c];
        }

        /// <summary>
        /// 处理多边形边缘计算
        /// </summary>
        private static void ProcessPolygonEdges(BasePolygon polygon, double x, ref Point3D point1, ref Point3D point2)
        {
            foreach (var edge in polygon.Edges)
            {
                var (start, end) = (edge.Start, edge.End);
                if ((start.X <= x && end.X >= x) || (start.X >= x && end.X <= x))
                {
                    var y = start.Y + (x - start.X) * (end.Y - start.Y) / (end.X - start.X);
                    if (edge.style is 3 or 4)
                    {
                        point2 = new(x, y, start.Z);
                    }
                    else
                    {
                        point1 = new(x, y, start.Z);
                    }
                }
            }
        }

        /// <summary>
        /// 查找最近的炮孔
        /// </summary>
        private List<Point3D> FindNearestBlastHoles(List<HashSet<Point3D>> positions, double x)
        {
            List<Point3D> newHoles = [];

            foreach (var blastHoles in positions)
            {
                if (blastHoles.Count == 0) continue;

                var nearestHole = blastHoles
                    .MinBy(hole => Math.Abs(hole.X - x));

                if (Math.Abs(nearestHole.X - x) > _config.MainBlastHoleSpacing / 2)
                    continue;

                newHoles.Add(new(x, nearestHole.Y, nearestHole.Z));
            }

            return newHoles;
        }

        /// <summary>
        /// 计算炮孔线条坐标
        /// </summary>
        public static List<Point3D> CalculateBlastHoleLine(Point3D hole, double diameter, double angle)
            => [hole, new(hole.X, hole.Y + diameter / Math.Tan(angle), hole.Z - diameter)];

        /// <summary>
        /// 等分线段计算
        /// </summary>
        private static List<Point3D> DivideLine(Point3D start, Point3D end, double startPadding, double endPadding, int count)
        {
            var direction = (end - start).Normalize();
            var newStart = start - startPadding * direction;
            var newEnd = end - endPadding * direction;
            List<Point3D> points = [start, newStart];

            for (int i = 1; i <= count; i++)
            {
                var t = i / (double)(count + 1);
                points.Add(new(
                    newStart.X + (newEnd.X - newStart.X) * t,
                    newStart.Y + (newEnd.Y - newStart.Y) * t,
                    newStart.Z + (newEnd.Z - newStart.Z) * t));
            }

            points.Add(newEnd);
            return points;
        }

        /// <summary>
        /// 等分线段计算(无永久轮廓版本)
        /// </summary>
        private static List<Point3D> DivideLine(Point3D start, Point3D end, double endPadding, int count)
        {
            var newEnd = end - endPadding * (end - start).Normalize();
            List<Point3D> points = [start];

            for (int i = 1; i <= count; i++)
            {
                var t = i / (double)(count + 1);
                points.Add(new(
                    start.X + (newEnd.X - start.X) * t,
                    start.Y + (newEnd.Y - start.Y) * t,
                    start.Z + (newEnd.Z - start.Z) * t));
            }

            points.Add(newEnd);
            return points;
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _config.Dispose();
                _disposed = true;
            }
        }

        ~CrossSection() => Dispose(false);
        #endregion
    }
}