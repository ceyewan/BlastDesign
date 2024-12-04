using MathNet.Spatial.Euclidean;

public static class HoleSort
{
    public static List<Point3D> Sort(BasePolygon basePolygon, HashSet<Point3D> blastHoles)
    {
        Point3D startPoint = GetStartPosition(basePolygon);
        List<Point3D> holes = blastHoles.ToList();
        holes = holes.OrderBy(h => DistanceBetweenPoints(basePolygon, startPoint, h)).ToList();
        return holes;
    }

    private static Point3D GetStartPosition(BasePolygon basePolygon)
    {
        return basePolygon.StartPoint;
    }

    private static double DistanceBetweenPoints(BasePolygon basePolygon, Point3D point1, Point3D point2)
    {
        // 检查 point1 和 point2 是否在 basePolygon 的边上
        Edge? edge1 = basePolygon.Edges.FirstOrDefault(e => basePolygon.IsPointOnEdge(point1, e));
        Edge? edge2 = basePolygon.Edges.FirstOrDefault(e => basePolygon.IsPointOnEdge(point2, e));
        if (edge1 == null || edge2 == null)
        {
            throw new ArgumentException("point1 或 point2 不在 basePolygon 的边上");
        }
        if (edge1.Equals(edge2))
        {
            return (point1 - point2).Length;
        }
        double distance = (point1 - edge1.End).Length;
        Point3D currentPoint = edge1.End;
        while (true)
        {
            Edge? nextEdge = basePolygon.Edges.FirstOrDefault(e => e.Start.Equals(currentPoint));
            if (nextEdge == null)
            {
                throw new InvalidOperationException("basePolygon 中的边不连通");
            }
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