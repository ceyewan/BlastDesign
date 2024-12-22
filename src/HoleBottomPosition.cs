using BlastDesign.tool;
using MathNet.Spatial.Euclidean;

public class HoleBottomPosition
{
    private readonly Config config = null!;
    private PreSplitPolygon bottomPolygon = null!;
    // 确定爆破线的多边形
    private List<BasePolygon> blastPolygons = new List<BasePolygon>();
    // 爆破孔，包括预裂孔、缓冲孔和主爆孔，平面位置
    private List<HashSet<Point3D>> blastHolePositions = new List<HashSet<Point3D>>();
    // 是否有永久轮廓面
    private bool hasNoPermanentEdge = false;
    // 炮孔编号
    private int count = 1;
    // 结果炮孔坐标
    private List<List<HolePosition>> holePositionLists = new List<List<HolePosition>>();
    // 每一排炮孔的倾斜角度
    private List<double> inclinationAngle = new List<double>();
    // x 的范围，用于画剖面图，从而确定每一排炮孔的倾斜角度
    private int minX, maxX;
    public HoleBottomPosition(Config config, PreSplitPolygon bottomPolygon, List<BasePolygon> blastPolygons, List<HashSet<Point3D>> blastHolePositions)
    {
        this.config = config;
        this.bottomPolygon = bottomPolygon;
        this.blastPolygons = blastPolygons;
        this.hasNoPermanentEdge = blastPolygons[0].HasNoPermanentEdge;
        this.blastHolePositions = blastHolePositions;
        FindXRange();
        var crossSection = new CrossSection(config, bottomPolygon, blastPolygons, blastHolePositions);
        inclinationAngle = crossSection.CalculateInclinationAngle((minX + maxX) / 2);
    }

    // 获取炮孔底部坐标
    public List<List<HolePosition>> GetHoleBottomPosition()
    {
        if (!hasNoPermanentEdge)
        {
            // 处理第一排炮孔的底部坐标
            ProcessFirstRowBottomPosition();
            // 处理第二排炮孔的底部坐标
            ProcessSecondRowBottomPosition();
        }
        for (int i = hasNoPermanentEdge ? 0 : 2; i < blastHolePositions.Count; i++)
        {
            List<Point3D> sortedHoles = HoleSort.Sort(blastPolygons[i], blastHolePositions[i]);
            holePositionLists.Add(new List<HolePosition>());
            for (int j = 0; j < sortedHoles.Count; j++)
            {
                var hole = sortedHoles[j];
                var holePosition = new HolePosition();
                holePosition.Top = hole;
                holePosition.Bottom = new Point3D(hole.X, hole.Y, 0);
                holePosition.HoleStyle = HolePosition.HoleType.MainBlastHole;
                holePosition.RowId = i + 1;
                holePosition.ColumnId = j + 1;
                holePosition.HoleId = count++;
                var diameter = holePosition.Top.Z - bottomPolygon.Edges.First().Start.Z + config.Depth * Math.Sin(inclinationAngle[i]);
                var end = new Point3D(holePosition.Top.X, holePosition.Top.Y + diameter / Math.Tan(inclinationAngle[i]), holePosition.Top.Z - diameter);
                holePosition.Bottom = end;
                holePositionLists[i].Add(holePosition);
            }
        }
        return holePositionLists;
    }

    // 处理第一排炮孔的底部坐标
    private void ProcessFirstRowBottomPosition()
    {
        // 获取第一排炮孔的顶部坐标
        List<Point3D> sortedTopHoles = HoleSort.Sort(blastPolygons[0], blastHolePositions[0]);
        List<int> holeCount = new List<int>();
        foreach (var edge in blastPolygons[0].Edges)
        {
            if (edge.style == 3 || edge.style == 5)
            {
                int index1 = sortedTopHoles.FindIndex(p => p.Equals(edge.Start, Constant.ErrorThreshold));
                int index2 = sortedTopHoles.FindIndex(p => p.Equals(edge.End, Constant.ErrorThreshold));
                if (index1 == -1) index1 = -1;
                if (index2 == -1) index2 = sortedTopHoles.Count;
                holeCount.Add(index2 - index1 + 1);
            }
        }
        // 布置底部炮孔
        bottomPolygon.ArrangeHoles(holeCount, config.IsContourLineEndHoleEnabled);
        List<Point3D> sortedBottomHoles = HoleSort.Sort(bottomPolygon, new HashSet<Point3D>(bottomPolygon.HolePoints, new Point3DEqualityComparer()));
        // 两个排序后的炮孔列表的长度必须相等
        if (sortedTopHoles.Count != sortedBottomHoles.Count)
        {
            Console.WriteLine("顶部炮孔数量：" + sortedTopHoles.Count);
            Console.WriteLine("底部炮孔数量：" + sortedBottomHoles.Count);
            throw new ArgumentException("第一排顶部炮孔和底部炮孔数量不一致");
        }
        holePositionLists.Add(new List<HolePosition>());
        for (int i = 0; i < sortedBottomHoles.Count; i++)
        {
            HolePosition holePosition = new HolePosition();
            holePosition.Top = sortedTopHoles[i];
            holePosition.Bottom = sortedBottomHoles[i];
            holePosition.HoleStyle = HolePosition.HoleType.PreSplitHole;
            holePosition.RowId = 1;
            holePosition.ColumnId = i + 1;
            holePosition.HoleId = count++;
            holePositionLists[0].Add(holePosition);
        }
    }

    // 处理第二排炮孔的底部坐标
    private void ProcessSecondRowBottomPosition()
    {
        // 获取第二排炮孔的顶部坐标
        List<Point3D> sortedTopHoles = HoleSort.Sort(blastPolygons[1], blastHolePositions[1]);
        int holeCount = sortedTopHoles.Count;
        if (!sortedTopHoles[0].Equals(blastPolygons[1].StartPoint, Constant.ErrorThreshold))
        {
            holeCount++;
        }
        if (!sortedTopHoles[sortedTopHoles.Count - 1].Equals(blastPolygons[1].EndPoint, Constant.ErrorThreshold))
        {
            holeCount++;
        }
        // 底部炮孔偏移得到缓冲孔的底部坐标
        BufferPolygon bufferPolygon = (BufferPolygon)bottomPolygon.Offset(config.PreSplitHoleOffset);
        bufferPolygon.ArrangeHoles(holeCount, config.IsContourLineEndHoleEnabled);
        List<Point3D> sortedBottomHoles = HoleSort.Sort(bufferPolygon, new HashSet<Point3D>(bufferPolygon.HolePoints, new Point3DEqualityComparer()));
        // 两个排序后的炮孔列表的长度必须相等
        if (sortedTopHoles.Count != sortedBottomHoles.Count)
        {
            Console.WriteLine("顶部炮孔数量：" + sortedTopHoles.Count);
            Console.WriteLine("底部炮孔数量：" + sortedBottomHoles.Count);
            throw new ArgumentException("第二排顶部炮孔和底部炮孔数量不一致");
        }
        holePositionLists.Add(new List<HolePosition>());
        for (int i = 0; i < sortedBottomHoles.Count; i++)
        {
            HolePosition holePosition = new HolePosition();
            holePosition.Top = sortedTopHoles[i];
            holePosition.Bottom = sortedBottomHoles[i];
            holePosition.HoleStyle = HolePosition.HoleType.BufferHole;
            holePosition.RowId = 2;
            holePosition.ColumnId = i + 1;
            holePosition.HoleId = count++;
            holePositionLists[1].Add(holePosition);
        }
    }


    // 3D 点相等性比较器，用于比较两个 Point3D 对象是否相等。
    private class Point3DEqualityComparer : IEqualityComparer<Point3D>
    {
        public bool Equals(Point3D v1, Point3D v2)
        {
            return v1.Equals(v2, Constant.ErrorThreshold);
        }

        public int GetHashCode(Point3D v)
        {
            int hashX = ((int)(v.X / Constant.ErrorThreshold)).GetHashCode();
            int hashY = ((int)(v.Y / Constant.ErrorThreshold)).GetHashCode();
            int hashZ = ((int)(v.Z / Constant.ErrorThreshold)).GetHashCode();
            return hashX ^ hashY ^ hashZ;
        }
    }

    // 通过遍历 blastPolygons 中的所有边，找到 x 的范围
    private void FindXRange()
    {
        minX = int.MaxValue;
        maxX = int.MinValue;
        foreach (var edge in blastPolygons[0].Edges)
        {
            minX = Math.Min(minX, (int)Math.Min(edge.Start.X, edge.End.X));
            maxX = Math.Max(maxX, (int)Math.Max(edge.Start.X, edge.End.X));
        }
    }
}

