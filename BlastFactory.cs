using BlastDesign.tool;
using Newtonsoft.Json;
using SkiaSharp;
using MathNet.Spatial.Euclidean;

// 多边形工厂类
public class BlastFactory
{
    private readonly Config _config;
    // 底部多边形
    private PreSplitPolygon bottomPolygon = null!;
    // 确定爆破线的多边形
    private List<BasePolygon> blastPolygons = new List<BasePolygon>();
    // 爆破孔，包括预裂孔、缓冲孔和主爆孔，平面位置
    private List<HashSet<Point3D>> blastHolePositions = new List<HashSet<Point3D>>();
    // 炮孔位置，包括顶部和底部
    private List<List<HolePosition>> holePositionLists = new List<List<HolePosition>>();
    // 爆破时间
    private Dictionary<Point3D, double> blastTiming = new Dictionary<Point3D, double>();
    // 爆破线
    private List<List<Point3D>> blastLinePoints = new List<List<Point3D>>();
    // 绘画类
    private BlastDrawer blastDrawer;
    // 最大最小坐标
    private double maxX = 0, maxY = 0, minX = 1e9, minY = 1e9;

    /// <summary>
    /// 构造函数，初始化 BlastFactory 并处理多边形和孔位。
    /// </summary>
    /// <param name="config">配置信息</param>
    public BlastFactory(Config config)
    {
        _config = config;
        ProcessPolygons(CreatePreSplitPolygon());
        AssignHolePosition();
        var holeTiming = new HoleTiming(holePositionLists, _config);
        (blastTiming, blastLinePoints) = holeTiming.TimingHoles();
        blastDrawer = new BlastDrawer(maxX, maxY, minX, minY, _config.PreSplitHoleSpacing, _config.PreSplitHoleOffset);
    }

    /// <summary>
    /// 打印所有孔的位置和信息。
    /// </summary>
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

    /// <summary>
    /// 获取孔位信息。
    /// </summary>
    /// <returns>孔位信息</returns>
    public List<List<HolePosition>> GetHoles()
    {
        return holePositionLists;
    }

    /// <summary>
    /// 绘制孔设计的平面图并保存为 SVG 文件。
    /// </summary>
    /// <param name="filePath">文件路径，默认为 "./images/hole_design.svg"</param>
    public void DrawHoleDesign(string filePath = "./images/hole_design.svg")
    {
        blastDrawer.DrawHoleDesign(blastPolygons, blastHolePositions, filePath);
    }

    /// <summary>
    /// 绘制爆破网络图并保存为 SVG 文件。
    /// </summary>
    /// <param name="filePath">文件路径，默认为 "./images/timing_network.svg"</param>
    public void DrawTiming(string filePath = "./images/timing_network.svg")
    {
        blastDrawer.DrawTimingNetwork(blastTiming, blastLinePoints, filePath);
    }

    /// <summary>
    /// 绘制爆破过程的 GIF 动画。
    /// </summary>
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

    /// <summary>
    /// 绘制剖面图并保存到指定文件夹。
    /// </summary>
    /// <param name="folderPath">文件夹路径，默认为 "./images"</param>
    public void DrawCrossSection(string folderPath = "./images")
    {
        // 确保文件夹路径以 '/' 结尾
        if (!folderPath.EndsWith("/"))
        {
            folderPath += "/";
        }
        // 创建文件夹（如果不存在）
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        var crossSection = new CrossSection(_config);
        var count = 0;
        foreach (var x in _config.CrossSectionXCoordinates)
        {
            // 通过顶部多边形和底部多边形计算出剖面图的边界
            List<Point3D> newEdges = crossSection.CalculateCrossSectionEdges(blastPolygons[0], bottomPolygon, x);
            // 找到距离 X 最近的炮孔对应的 Y 坐标
            List<Point3D> newHoles = crossSection.FindNearestBlastHoles(blastHolePositions, x);
            // 计算轮廓线的角度
            var angle = Math.Atan2(newEdges[1].Z - newEdges[2].Z, -newEdges[1].Y + newEdges[2].Y);
            angle = angle * 180 / Math.PI;
            Console.WriteLine($"剖面图的角度：{angle:F2}");
            var hole1 = new Point3D(newHoles[0].X, newHoles[0].Y, newHoles[0].Z);
            var hole2 = new Point3D(newHoles[0].X, newHoles[0].Y - _config.BlastHoleDiameters[0], newHoles[0].Z);
            var newLine1 = crossSection.CalculateBlastHoleLine(hole1, hole1.Z - bottomPolygon.Edges.First().Start.Z, angle);
            var newLine2 = crossSection.CalculateBlastHoleLine(hole2, hole1.Z - bottomPolygon.Edges.First().Start.Z, angle);
            List<List<Point3D>> newLines = new List<List<Point3D>> { newLine1, newLine2 };
            hole1 = new Point3D(newHoles[1].X, newHoles[1].Y, newHoles[1].Z);
            hole2 = new Point3D(newHoles[1].X, newHoles[1].Y - _config.BlastHoleDiameters[1], newHoles[1].Z);
            newLine1 = crossSection.CalculateBlastHoleLine(hole1, hole1.Z - bottomPolygon.Edges.First().Start.Z, angle);
            newLine2 = crossSection.CalculateBlastHoleLine(hole2, hole1.Z - bottomPolygon.Edges.First().Start.Z, angle);
            newLines.AddRange(new List<List<Point3D>> { newLine1, newLine2 });
            var bottomHoles = crossSection.DivideLine(newLine1[1], newEdges[3], newHoles.Count - 2);
            for (int i = 2; i < newHoles.Count; i++)
            {
                hole1 = new Point3D(newHoles[i].X, newHoles[i].Y, newHoles[i].Z);
                hole2 = new Point3D(newHoles[i].X, newHoles[i].Y - _config.BlastHoleDiameters[2], newHoles[i].Z);
                angle = Math.Atan2(hole1.Z - bottomHoles[i - 2].Z, -hole1.Y + bottomHoles[i - 2].Y);
                var diameter = hole1.Z - bottomPolygon.Edges.First().Start.Z + (i > 1 ? _config.Depth * Math.Sin(angle) : 0);
                angle = angle * 180 / Math.PI;
                var newLine = crossSection.CalculateBlastHoleLine(hole1, diameter, angle);
                newLines.Add(newLine);
                newLine = crossSection.CalculateBlastHoleLine(hole2, diameter, angle);
                newLines.Add(newLine);
            }
            blastDrawer.DrawCrossSection(newEdges, newLines, folderPath + "cross_section_" + count++ + ".svg");
        }
    }

    /// <summary>
    /// 绘制炮孔装药结构图并保存到指定文件夹。
    /// </summary>
    /// <param name="folderPath">文件夹路径，默认为 "./images"</param>
    public void DrawChargeStructure(string folderPath = "./images")
    {
        // 确保文件夹路径以 '/' 结尾
        if (!folderPath.EndsWith("/"))
        {
            folderPath += "/";
        }
        // 创建文件夹（如果不存在）
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        // Console.WriteLine("绘制预裂孔装药结构图：");
        var newHoleChargeDrawers = new HoleChargeDrawing(_config.PreSplitHoleChargeConfig[0], _config.PreSplitHoleChargeConfig[1], _config.PreSplitHoleChargeConfig[2], _config.PreSplitHoleChargeConfig[3], _config.PreSplitHoleChargeConfig[4]);
        newHoleChargeDrawers.DrawAndSave(folderPath + "pre_split_hole_charge_structure.svg");
        // Console.WriteLine("绘制缓冲孔装药结构图：");
        newHoleChargeDrawers = new HoleChargeDrawing(_config.BufferHoleChargeConfig[0], _config.BufferHoleChargeConfig[1], _config.BufferHoleChargeConfig[2], _config.BufferHoleChargeConfig[3], _config.BufferHoleChargeConfig[4]);
        newHoleChargeDrawers.DrawAndSave(folderPath + "buffer_hole_charge_structure.svg");
        // Console.WriteLine("绘制主爆孔装药结构图：");
        newHoleChargeDrawers = new HoleChargeDrawing(_config.MainBlastHoleChargeConfig[0], _config.MainBlastHoleChargeConfig[1], _config.MainBlastHoleChargeConfig[2], _config.MainBlastHoleChargeConfig[3], _config.MainBlastHoleChargeConfig[4]);
        newHoleChargeDrawers.DrawAndSave(folderPath + "main_blast_hole_charge_structure.svg");
    }

    /// <summary>
    /// 获取最后一行有炮孔的炮孔排，并计算其上炮孔到自由线的最小距离的最大最小值。
    /// </summary>
    /// <returns>最后一行有炮孔的炮孔排的最小距离的最大最小值</returns>
    public (double, double) GetLastRowHoleDistance()
    {
        // 取从后往前第一个不为空的炮孔排
        var lastRowHoles = holePositionLists.Last(row => row.Any());
        double maxDistance = 0;
        double minDistance = 1e9;
        foreach (var hole in lastRowHoles)
        {
            var distance = ((PreSplitPolygon)blastPolygons[0]).FindNearestDistanceToFreeEdge(hole.Top);
            maxDistance = Math.Max(maxDistance, distance);
            minDistance = Math.Min(minDistance, distance);
        }
        return (maxDistance, minDistance);
    }

    /// <summary>
    /// 获取主爆孔的排数
    /// </summary>
    public int GetMainBlastHoleRows()
    {
        // 排除没有孔的情况
        int nonEmptyRows = holePositionLists.Count(row => row.Any());
        return nonEmptyRows - 2;
    }

    // 私有辅助方法

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

    private PreSplitPolygon CreatePreSplitPolygon()
    {
        // 创建多边形边缘的通用方法
        List<Edge> CreateEdges(string[] points, string[] styles)
        {
            var edges = new List<Edge>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                var start = ParsePoint(points[i]);
                var end = ParsePoint(points[i + 1]);
                var style = int.Parse(styles[i]);
                edges.Add(new Edge(start, end, style));
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
        _config.CrossSectionXCoordinates[0] = minX + (maxX - minX) * 1 / 5;
        _config.CrossSectionXCoordinates[1] = minX + (maxX - minX) * 2 / 5;
        _config.CrossSectionXCoordinates[2] = minX + (maxX - minX) * 3 / 5;
        _config.CrossSectionXCoordinates[3] = minX + (maxX - minX) * 4 / 5;
        return new PreSplitPolygon(topEdges, _config.MinDistanceToFreeLine);
    }

    // 处理多边形，偏移并布点
    private void ProcessPolygons(PreSplitPolygon preSplitPolygon)
    {
        // 处理预裂孔多边形
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

    // 处理多边形，偏移并布点，没有永久轮廓线的情况
    

    // 分配设置孔位
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
                if (i == 0)
                {
                    var bottomHole = bottomPolygon.FindNearestPoint(holePosition.Top);
                    holePosition.Bottom = bottomHole;
                }
                else
                {
                    var angle = _config.InclinationAngle * Math.PI / 180;
                    var diameter = holePosition.Top.Z - bottomPolygon.Edges.First().Start.Z;
                    var end = new Point3D(holePosition.Top.X, holePosition.Top.Y + diameter / Math.Tan(angle), holePosition.Top.Z - diameter);
                    holePosition.Bottom = end;
                }
                holePositionLists[i].Add(holePosition);
            }
        }
    }
}
