using MathNet.Spatial.Euclidean;

public static class HoleSort
{
    public static List<Point3D> Sort(BasePolygon basePolygon, HashSet<Point3D> blastHoles, PreSplitPolygon preSplitPolygon)
    {
        Point3D startPoint = GetStartPosition(basePolygon, preSplitPolygon);
        List<Point3D> holes = blastHoles.ToList();
        holes = holes.OrderBy(h => DistanceBetweenPoints(basePolygon, startPoint, h)).ToList();
        return holes;
    }

    private static Point3D GetStartPosition(BasePolygon basePolygon, PreSplitPolygon preSplitPolygon)
    {
        foreach (var edge in basePolygon.Edges)
        {
            if (isPointOnFreeEdge(edge.Start, preSplitPolygon.GetFreeEdges()) && !isPointOnFreeEdge(edge.Start + edge.Length() / 2 * edge.Direction(), preSplitPolygon.GetFreeEdges()))
            {
                return edge.Start;
            }
        }
        // 如果没有找到符合条件的边，返回默认值
        return basePolygon.Edges.First().Start;
    }

    private static bool isPointOnFreeEdge(Point3D point, List<Edge> freeEdges)
    {
        foreach (var edge in freeEdges)
        {
            if (point.DistanceTo(edge.Start) + point.DistanceTo(edge.End) - edge.Length() < Constant.ErrorThreshold)
            {
                return true;
            }
        }
        return false;
    }

    private static double DistanceBetweenPoints(BasePolygon basePolygon, Point3D point1, Point3D point2)
    {
        Edge edge1 = basePolygon.Edges.First(e => basePolygon.IsPointOnEdge(point1, e));
        Edge edge2 = basePolygon.Edges.First(e => basePolygon.IsPointOnEdge(point2, e));
        if (edge1.Equals(edge2))
        {
            return (point1 - point2).Length;
        }
        double distance = (point1 - edge1.End).Length;
        Point3D currentPoint = edge1.End;
        while (true)
        {
            Edge nextEdge = basePolygon.Edges.First(e => e.Start.Equals(currentPoint));
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