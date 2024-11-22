using BlastDesign.tool;
using Newtonsoft.Json;
using SkiaSharp;
using MathNet.Spatial.Euclidean;

// 多边形工厂类
public class BlastFactory
{
    private readonly Config _config;
    // 底部多边形
    BasePolygon bottomPolygon = null!;
    // 确定爆破线的多边形
    List<BasePolygon> blastPolygons = new List<BasePolygon>();
    // 爆破孔，包括预裂孔、缓冲孔和主爆孔，平面位置
    List<HashSet<Point3D>> blastHolePositions = new List<HashSet<Point3D>>();
    // 炮孔位置，包括顶部和底部
    List<List<HolePosition>> holePositionLists = new List<List<HolePosition>>();
    // 爆破时间
    Dictionary<Point3D, double> blastTiming = new Dictionary<Point3D, double>();
    // 爆破线
    List<List<Point3D>> blastLinePoints = new List<List<Point3D>>();
    // 绘画类
    private BlastDrawer blastDrawer;
    // 最大最小坐标
    double maxX = 0, maxY = 0, minX = 1e9, minY = 1e9;

    public class Point3DEqualityComparer : IEqualityComparer<Point3D>
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

    public BlastFactory(Config config)
    {
        _config = config;
        ProcessPolygons(CreatePreSplitPolygon());
        AssignHolePosition();
        var holeTiming = new HoleTiming(holePositionLists, _config);
        (blastTiming, blastLinePoints) = holeTiming.TimingHoles();
        blastDrawer = new BlastDrawer(maxX, maxY, minX, minY, _config.PreSplitHoleSpacing, _config.PreSplitHoleOffset);
    }

    public PreSplitPolygon CreatePreSplitPolygon()
    {
        // 创建多边形边缘的通用方法
        List<Edge> CreateEdges(string[] points, string[] styles)
        {
            var edges = new List<Edge>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                var start = ParsePoint(points[i]);
                var end = ParsePoint(points[i + 1]);
                var isContour = styles[i] == "1";
                edges.Add(new Edge(start, end, isContour));
            }
            return edges;
        }
        // 解析点的方法
        Point3D ParsePoint(string point)
        {
            var coordinates = point.Split(',');
            var x = double.Parse(coordinates[0].TrimStart('('));
            var y = double.Parse(coordinates[1]);
            var z = double.Parse(coordinates[2].TrimEnd(')'));
            maxX = Math.Max(maxX, x);
            minX = Math.Min(minX, x);
            maxY = Math.Max(maxY, y);
            minY = Math.Min(minY, y);
            return new Point3D(x, y, z);
        }
        // 创建顶部和底部多边形
        var topEdges = CreateEdges(_config.TopPoints, _config.TopStyle);
        var bottomEdges = CreateEdges(_config.BottomPoints, _config.BottomStyle);
        bottomPolygon = new PreSplitPolygon(bottomEdges);
        return new PreSplitPolygon(topEdges, _config.MinDistanceToFreeLine);
    }

    // 处理多边形，偏移并布点
    public void ProcessPolygons(PreSplitPolygon preSplitPolygon)
    {
        // 处理预裂孔多边形
        Console.WriteLine("自由边数量：" + preSplitPolygon.GetFreeEdges().Count);
        foreach (var edge in preSplitPolygon.GetFreeEdges())
        {
            Console.WriteLine($"自由边：{edge.Start} -> {edge.End}");
        }
        preSplitPolygon.ArrangeHoles(_config.PreSplitHoleSpacing, _config.IsContourLineEndHoleEnabled);
        blastPolygons.Add(preSplitPolygon);
        blastHolePositions.Add(new HashSet<Point3D>(preSplitPolygon.HolePoints, new Point3DEqualityComparer()));
        // 处理缓冲孔多边形
        var bufferPolygon = preSplitPolygon.Offset(_config.PreSplitHoleOffset, _config.PreSplitHoleOffset, _config.BufferHoleSpacing) as BufferPolygon;
        if (bufferPolygon == null)
        {
            return;
        }
        bufferPolygon.ArrangeHoles(_config.BufferHoleSpacing);
        blastPolygons.Add(bufferPolygon);
        blastHolePositions.Add(new HashSet<Point3D>(bufferPolygon.HolePoints, new Point3DEqualityComparer()));
        var mainBlastPolygon = bufferPolygon.Offset(_config.BufferHoleOffset, _config.BufferHoleOffset, _config.MainBlastHoleSpacing) as MainBlastPolygon;
        if (mainBlastPolygon == null)
        {
            return;
        }
        blastPolygons.Add(mainBlastPolygon);
        mainBlastPolygon.ArrangeHoles(_config.MainBlastHoleSpacing);
        blastHolePositions.Add(new HashSet<Point3D>(mainBlastPolygon.HolePoints, new Point3DEqualityComparer()));
        // 处理主爆孔多边形
        BasePolygon prevPolygon = mainBlastPolygon;
        while (true)
        {
            mainBlastPolygon = prevPolygon.Offset(_config.MainBlastHoleOffset, _config.MainBlastHoleOffset, _config.MainBlastHoleSpacing) as MainBlastPolygon;
            if (mainBlastPolygon == null)
            {
                break;
            }
            blastPolygons.Add(mainBlastPolygon);
            if (!mainBlastPolygon.HolePoints.Any())
            {
                continue;
            }
            mainBlastPolygon.ArrangeHoles(_config.MainBlastHoleSpacing);
            blastHolePositions.Add(new HashSet<Point3D>(mainBlastPolygon.HolePoints, new Point3DEqualityComparer()));
            prevPolygon = mainBlastPolygon;
        }
    }

    private void AssignHolePosition()
    {
        int count = 1;
        for (int i = 0; i < blastHolePositions.Count; i++)
        {
            List<Point3D> sortedHoles = HoleSort.Sort(blastPolygons[i], blastHolePositions[i], (PreSplitPolygon)blastPolygons[0]);
            holePositionLists.Add(new List<HolePosition>());
            for (int j = 0; j < sortedHoles.Count; j++)
            {
                var hole = sortedHoles[j];
                var holePosition = new HolePosition();
                holePosition.Top = hole;
                holePosition.Bottom = new Point3D(hole.X, hole.Y, 0);
                holePosition.HoleStyle = i switch
                {
                    0 => HolePosition.HoleType.PreSplitHole,
                    1 => HolePosition.HoleType.BufferHole,
                    _ => HolePosition.HoleType.MainBlastHole,
                };
                holePosition.RowId = i + 1;
                holePosition.ColumnId = j + 1;
                holePosition.HoleId = count++;
                holePositionLists[i].Add(holePosition);
            }
        }
    }
    public void PrintHoles()
    {
        Console.WriteLine("预裂孔：");
        foreach (var hole in holePositionLists[0])
        {
            Console.WriteLine($"顶部坐标：({hole.Top.X:F2}, {hole.Top.Y:F2}, {hole.Top.Z:F2})，底部坐标：({hole.Bottom.X:F2}, {hole.Bottom.Y:F2}, {hole.Bottom.Z:F2})，孔类型：{hole.HoleStyle}，行号：{hole.RowId}，列号：{hole.ColumnId}，孔号：{hole.HoleId}");
        }
        Console.WriteLine("缓冲孔：");
        foreach (var hole in holePositionLists[1])
        {
            Console.WriteLine($"顶部坐标：({hole.Top.X:F2}, {hole.Top.Y:F2}, {hole.Top.Z:F2})，底部坐标：({hole.Bottom.X:F2}, {hole.Bottom.Y:F2}, {hole.Bottom.Z:F2})，孔类型：{hole.HoleStyle}，行号：{hole.RowId}，列号：{hole.ColumnId}，孔号：{hole.HoleId}");
        }
        Console.WriteLine("主爆孔：");
        for (int i = 2; i < holePositionLists.Count; i++)
        {
            Console.WriteLine($"第{i - 1}个多边形的主爆孔：");
            foreach (var hole in holePositionLists[i])
            {
                Console.WriteLine($"顶部坐标：({hole.Top.X:F2}, {hole.Top.Y:F2}, {hole.Top.Z:F2})，底部坐标：({hole.Bottom.X:F2}, {hole.Bottom.Y:F2}, {hole.Bottom.Z:F2})，孔类型：{hole.HoleStyle}，行号：{hole.RowId}，列号：{hole.ColumnId}，孔号：{hole.HoleId}");
            }
        }
    }

    // 绘制平面图
    public void DrawHoleDesign()
    {
        blastDrawer.DrawHoleDesign(blastPolygons, blastHolePositions);
    }

    // 绘制爆破网络图
    public void DrawTiming()
    {
        blastDrawer.DrawTimingNetwork(blastTiming, blastLinePoints);
    }

    // 绘制 Gif 备选图片
    public void DrawGif()
    {
        var uniqueTimes = blastTiming.Values.Distinct().OrderBy(t => t).ToList();
        Directory.CreateDirectory("frames");
        for (int i = 0; i < uniqueTimes.Count; i++)
        {
            var currentTime = uniqueTimes[i];
            var previousTime = i > 0 ? uniqueTimes[i - 1] : 0;
            blastDrawer.DrawAnimationFrame(
                blastPolygons,
                blastTiming,
                currentTime,
                previousTime,
                $"frames/{i + 1:000}.png");
        }
    }

    // 绘制剖面图
    public void DrawCrossSection()
    {
        foreach (var x in _config.CrossSectionXCoordinates)
        {
            Console.WriteLine($"剖面图的x坐标：{x}");
            // 通过顶部多边形和底部多边形计算出剖面图的边界
            List<Point3D> newEdges = CalculateCrossSectionEdges(blastPolygons[0], bottomPolygon, x);
            // 找到距离 X 最近的炮孔对应的 Y 坐标
            List<Point3D> newHoles = FindNearestBlastHoles(blastHolePositions, x);
            // 输出剖面图的炮孔
            foreach (var hole in newHoles)
            {
                Console.WriteLine($"剖面图的炮孔：({hole.X:F2}, {hole.Y:F2}, {hole.Z:F2})");
            }
            List<List<Point3D>> newLines = new List<List<Point3D>>();
            for (int i = 0; i < newHoles.Count; i++)
            {
                int index = (i < 2) ? i : 2;
                var hole1 = new Point3D(newHoles[i].X, newHoles[i].Y, newHoles[i].Z);
                var hole2 = new Point3D(newHoles[i].X, newHoles[i].Y + _config.BlastHoleDiameters[index], newHoles[i].Z);
                var diameter = hole1.Z + (i > 1 ? _config.Depth : 0);
                var inclinationAngle = _config.InclinationAngle;
                var newLine = CalculateBlastHoleLine(hole1, diameter, inclinationAngle);
                newLines.Add(newLine);
                newLine = CalculateBlastHoleLine(hole2, diameter, inclinationAngle);
                newLines.Add(newLine);
            }
            // 输出剖面图的炮孔线条
            foreach (var line in newLines)
            {
                Console.WriteLine($"剖面图的炮孔线条：({line[0].X:F2}, {line[0].Y:F2}, {line[0].Z:F2}) -> ({line[1].X:F2}, {line[1].Y:F2}, {line[1].Z:F2})");
            }
            blastDrawer.DrawCrossSection(newEdges, newLines, "cross_section_" + x + ".svg");
        }
    }

    // 计算剖面图的边界
    private List<Point3D> CalculateCrossSectionEdges(BasePolygon topPolygon, BasePolygon bottomPolygon, double x)
    {
        List<Point3D> newPoints = new List<Point3D>();
        foreach (var edge in topPolygon.Edges)
        {
            var start = edge.Start;
            var end = edge.End;
            if (start.X <= x && end.X >= x || start.X >= x && end.X <= x)
            {
                var y = start.Y + (x - start.X) * (end.Y - start.Y) / (end.X - start.X);
                newPoints.Add(new Point3D(x, y, start.Z));
            }
        }
        foreach (var edge in bottomPolygon.Edges)
        {
            var start = edge.Start;
            var end = edge.End;
            if (start.X <= x && end.X >= x || start.X >= x && end.X <= x)
            {
                var y = start.Y + (x - start.X) * (end.Y - start.Y) / (end.X - start.X);
                newPoints.Add(new Point3D(x, y, start.Z));
            }
        }
        // 调整顺序，使得点按照顺序连接，后两个点交换位置
        if (newPoints.Count == 4)
        {
            var temp = newPoints[2];
            newPoints[2] = newPoints[3];
            newPoints[3] = temp;
        }
        // 输出剖面图的边界
        foreach (var point in newPoints)
        {
            Console.WriteLine($"剖面图的边界：({point.X:F2}, {point.Y:F2}, {point.Z:F2})");
        }
        return newPoints;
    }

    // 找到距离 X 最近的炮孔对应的 Y 坐标
    private List<Point3D> FindNearestBlastHoles(List<HashSet<Point3D>> blastHolePositions, double x)
    {
        List<Point3D> newHoles = new List<Point3D>();
        foreach (var blastHoles in blastHolePositions)
        {
            var nearestHole = blastHoles.OrderBy(hole => Math.Abs(hole.X - x)).First();
            // 将 nearestHole 平移到平面 X = x 上
            nearestHole = new Point3D(x, nearestHole.Y, nearestHole.Z);
            newHoles.Add(nearestHole);
        }
        return newHoles;
    }

    // 计算炮孔线条的坐标，从起点开始，倾斜角度为 BlastHoleDiameters 度，终点在下底面上
    public List<Point3D> CalculateBlastHoleLine(Point3D hole, double diameter, double inclinationAngle)
    {
        var angle = inclinationAngle * Math.PI / 180;
        var end = new Point3D(hole.X, hole.Y + diameter / Math.Tan(angle), hole.Z - diameter);
        return new List<Point3D> { hole, end };
    }
}
