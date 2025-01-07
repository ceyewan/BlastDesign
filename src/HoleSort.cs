using MathNet.Spatial.Euclidean;

namespace BlastDesign.tool.BlackBoxTest
{
    /// <summary>
    /// 炮孔排序工具类
    /// </summary>
    public static class HoleSort
    {
        /// <summary>
        /// 对炮孔进行排序
        /// </summary>
        /// <param name="basePolygon">基础多边形</param>
        /// <param name="blastHoles">炮孔集合</param>
        /// <returns>排序后的炮孔列表</returns>
        public static List<Point3D> Sort(BasePolygon basePolygon, HashSet<Point3D> blastHoles)
        {
            var startPoint = GetStartPosition(basePolygon);
            return [.. blastHoles.OrderBy(h => DistanceBetweenPoints(basePolygon, startPoint, h))];
        }

        /// <summary>
        /// 获取起始位置
        /// </summary>
        private static Point3D GetStartPosition(BasePolygon basePolygon) =>
            basePolygon.StartPoint;

        /// <summary>
        /// 计算多边形边上两点间的距离
        /// </summary>
        /// <param name="basePolygon">基础多边形</param>
        /// <param name="point1">起点</param>
        /// <param name="point2">终点</param>
        /// <returns>边上的距离</returns>
        /// <exception cref="ArgumentException">当点不在多边形边上时抛出</exception>
        /// <exception cref="InvalidOperationException">当多边形边不连通时抛出</exception>
        private static double DistanceBetweenPoints(BasePolygon basePolygon, Point3D point1, Point3D point2)
        {
            // 检查点是否在多边形边上
            var edge1 = basePolygon.Edges.First(e => basePolygon.IsPointOnEdge(point1, e))
                ?? throw new ArgumentException("point1 不在 basePolygon 的边上");

            var edge2 = basePolygon.Edges.First(e => basePolygon.IsPointOnEdge(point2, e))
                ?? throw new ArgumentException("point2 不在 basePolygon 的边上");

            // 如果两点在同一条边上，直接计算距离
            if (edge1.Equals(edge2))
            {
                return (point1 - point2).Length;
            }

            // 计算从 point1 到第一条边终点的距离
            double distance = (point1 - edge1.End).Length;
            var currentPoint = edge1.End;

            // 遍历多边形边，直到找到包含 point2 的边
            while (true)
            {
                var nextEdge = basePolygon.Edges.First(e =>
                    e.Start.Equals(currentPoint, Constant.ErrorThreshold))
                    ?? throw new InvalidOperationException("basePolygon 中的边不连通");

                if (nextEdge.Equals(edge2))
                {
                    distance += (nextEdge.Start - point2).Length;
                    break;
                }

                distance += nextEdge.Length();
                currentPoint = nextEdge.End;
            }

            return distance;
        }
    }
}