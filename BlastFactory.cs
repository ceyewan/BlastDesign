using BlastDesign.tool;
using Newtonsoft.Json;
using SkiaSharp;
using MathNet.Spatial.Euclidean;

namespace BlastDesign.tool.BlackBoxTest
{
    // 多边形工厂类
    public class BlastFactory
    {
        private readonly Config _config = null!;
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
        private BlastDrawer blastDrawer = null!;
        // 最大最小坐标
        private double maxX = 0, maxY = 0, minX = 1e9, minY = 1e9;
        // 是否没有永久轮廓线
        private bool hasNoPermanentEdge = false;

        /// <summary>
        /// 构造函数，初始化 BlastFactory 并处理多边形和孔位。
        /// </summary>
        /// <param name="config">配置信息</param>
        public BlastFactory(Config config)
        {
            try
            {
                _config = config;
                ProcessPolygons(CreatePreSplitPolygon());
                blastDrawer = new BlastDrawer(maxX, maxY, minX, minY, _config.PreSplitHoleSpacing, _config.PreSplitHoleOffset);
                AssignHolePosition();
                var holeTiming = new HoleTiming(holePositionLists, _config);
                (blastTiming, blastLinePoints) = holeTiming.TimingHoles(hasNoPermanentEdge);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred in {nameof(BlastFactory)} constructor: {ex.Message}");
            }
        }

        /// <summary>
        /// 打印所有孔的位置和信息。
        /// </summary>
        public void PrintHoles()
        {
            try
            {
                if (hasNoPermanentEdge)
                {
                    Console.WriteLine("主爆孔：");
                    for (int i = 0; i < holePositionLists.Count; i++)
                    {
                        Console.WriteLine($"第{i + 1}个多边形的主爆孔：");
                        foreach (var hole in holePositionLists[i])
                        {
                            Console.WriteLine($"顶部坐标：({hole.Top.X:F2}, {hole.Top.Y:F2}, {hole.Top.Z:F2})，底部坐标：({hole.Bottom.X:F2}, {hole.Bottom.Y:F2}, {hole.Bottom.Z:F2})，孔类型：{hole.HoleStyle}，行号：{hole.RowId}，列号：{hole.ColumnId}，孔号：{hole.HoleId}");
                        }
                    }
                }
                else
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred in {nameof(PrintHoles)}: {ex.Message}");
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
            try
            {
                blastDrawer.DrawHoleDesign(blastPolygons, blastHolePositions, _config.CrossSectionXCoordinates, filePath, hasNoPermanentEdge);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred in {nameof(DrawHoleDesign)}: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制爆破网络图并保存为 SVG 文件。
        /// </summary>
        /// <param name="filePath">文件路径，默认为 "./images/timing_network.svg"</param>
        public void DrawTiming(string filePath = "./images/timing_network.svg")
        {
            try
            {
                blastDrawer.DrawTimingNetwork(blastTiming, blastLinePoints, hasNoPermanentEdge ? new List<Point3D>() : blastHolePositions[0].ToList(), filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred in {nameof(DrawTiming)}: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制爆破过程的 GIF 动画。
        /// </summary>
        public void DrawGif()
        {
            var uniqueTimes = blastTiming.Values.Distinct().OrderBy(t => t).ToList();
            Directory.CreateDirectory("frames");
            // 清除之前的帧
            foreach (var file in Directory.EnumerateFiles("frames"))
            {
                File.Delete(file);
            }
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
            try
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
                var crossSection = new CrossSection(_config, bottomPolygon, blastPolygons, blastHolePositions);
                int count = 1;
                foreach (var x in _config.CrossSectionXCoordinates)
                {
                    var (newEdges, newLines) = crossSection.GenerateCrossSection(x);
                    blastDrawer.DrawCrossSection(newEdges, newLines, count, folderPath + "cross_section_" + count++ + ".svg", hasNoPermanentEdge);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred in {nameof(DrawCrossSection)}: {ex.Message}");
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
            var newHoleChargeDrawers = new HoleChargeDrawing(_config.PreSplitHoleChargeParameters[0], _config.PreSplitHoleChargeParameters[1], _config.PreSplitHoleChargeParameters[2], _config.PreSplitHoleChargeParameters[3], _config.PreSplitHoleChargeParameters[4]);
            newHoleChargeDrawers.DrawAndSave(folderPath + "pre_split_hole_charge_structure.svg");
            // Console.WriteLine("绘制缓冲孔装药结构图：");
            newHoleChargeDrawers = new HoleChargeDrawing(_config.BufferHoleChargeParameters[0], _config.BufferHoleChargeParameters[1], _config.BufferHoleChargeParameters[2], _config.BufferHoleChargeParameters[3], _config.BufferHoleChargeParameters[4]);
            newHoleChargeDrawers.DrawAndSave(folderPath + "buffer_hole_charge_structure.svg");
            // Console.WriteLine("绘制主爆孔装药结构图：");
            newHoleChargeDrawers = new HoleChargeDrawing(_config.MainBlastHoleChargeParameters[0], _config.MainBlastHoleChargeParameters[1], _config.MainBlastHoleChargeParameters[2], _config.MainBlastHoleChargeParameters[3], _config.MainBlastHoleChargeParameters[4]);
            newHoleChargeDrawers.DrawAndSave(folderPath + "main_blast_hole_charge_structure.svg");
        }

        /// <summary>
        /// 获取最后一行有炮孔的炮孔排，并计算其上炮孔到自由线的最小距离的最大最小值。
        /// </summary>
        /// <returns>最后一行有炮孔的炮孔排的最小距离的最大最小值</returns>
        public (double, double) GetLastRowHoleDistance()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred in {nameof(GetLastRowHoleDistance)}: {ex.Message}");
                return (0, 0);
            }
        }

        /// <summary>
        /// 获取主爆孔的排数
        /// </summary>
        public int GetMainBlastHoleRows()
        {
            try
            {
                // 排除没有孔的情况
                int nonEmptyRows = holePositionLists.Count(row => row.Any());
                return hasNoPermanentEdge ? nonEmptyRows : nonEmptyRows - 2;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred in {nameof(GetMainBlastHoleRows)}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取数码管数量和导爆索长度
        /// </summary>
        /// <returns>数码管数量和导爆索长度</returns>
        public (int, double) GetDetonatingCord()
        {
            // 数码管的数量是 blastHolePositions 中元素的个数
            // 导爆索的长度是所有 预裂孔深度+1 之和
            try
            {
                int detonatingCordCount = 0;
                double detonatingCordLength = 0;
                foreach (var holePositionList in holePositionLists)
                {
                    detonatingCordCount += holePositionList.Count;
                    foreach (var hole in holePositionList)
                    {
                        if (hole.HoleStyle == HolePosition.HoleType.PreSplitHole)
                        {
                            detonatingCordLength += Math.Sqrt(Math.Pow(hole.Top.X - hole.Bottom.X, 2) +
                                Math.Pow(hole.Top.Y - hole.Bottom.Y, 2) + Math.Pow(hole.Top.Z - hole.Bottom.Z, 2)) + 1;
                        }
                    }
                }
                return (detonatingCordCount, detonatingCordLength);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred in {nameof(GetDetonatingCord)}: {ex.Message}");
                return (0, 0);
            }
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
            _config.CrossSectionXCoordinates[0] = minX + (maxX - minX) * 1 / 3;
            _config.CrossSectionXCoordinates[1] = minX + (maxX - minX) * 1 / 2;
            _config.CrossSectionXCoordinates[2] = minX + (maxX - minX) * 2 / 3;
            return new PreSplitPolygon(topEdges, _config.MinDistanceToFreeLine);
        }

        // 处理多边形，偏移并布点
        private void ProcessPolygons(PreSplitPolygon preSplitPolygon)
        {
            hasNoPermanentEdge = preSplitPolygon.HasNoPermanentEdge;
            blastPolygons.Add(preSplitPolygon);
            if (!hasNoPermanentEdge)
            {
                preSplitPolygon.ArrangeHoles(_config.PreSplitHoleSpacing, _config.IsContourLineEndHoleEnabled);
                blastHolePositions.Add(new HashSet<Point3D>(preSplitPolygon.HolePoints, new Point3DEqualityComparer()));
                Console.WriteLine("预裂孔数量：" + preSplitPolygon.HolePoints.Count);
                if (preSplitPolygon.Offset(_config.PreSplitHoleOffset) is BufferPolygon bufferPolygon)
                {
                    bufferPolygon.ArrangeHoles(_config.BufferHoleSpacing);
                    blastPolygons.Add(bufferPolygon);
                    blastHolePositions.Add(new HashSet<Point3D>(bufferPolygon.HolePoints, new Point3DEqualityComparer()));
                    // 处理主爆孔多边形
                    ProcessMainBlastPolygons(bufferPolygon, _config.BufferHoleOffset);
                }
            }
            else
            {
                var mainBlastPolygon = new MainBlastPolygon(preSplitPolygon.Edges, preSplitPolygon, _config.MinDistanceToFreeLine);
                mainBlastPolygon.ArrangeHoles(_config.MainBlastHoleSpacing);
                blastHolePositions.Add(new HashSet<Point3D>(mainBlastPolygon.HolePoints, new Point3DEqualityComparer()));
                ProcessMainBlastPolygons(preSplitPolygon, _config.MainBlastHoleOffset);
            }
        }
        private void ProcessMainBlastPolygons(BasePolygon initialPolygon, double offset)
        {
            var mainBlastPolygon = initialPolygon.Offset(offset, true) as MainBlastPolygon;
            if (mainBlastPolygon == null)
            {
                // Console.WriteLine("主爆孔多边形为空！");
                return;
            }
            blastPolygons.Add(mainBlastPolygon);
            mainBlastPolygon.ArrangeHoles(_config.MainBlastHoleSpacing);
            blastHolePositions.Add(new HashSet<Point3D>(mainBlastPolygon.HolePoints, new Point3DEqualityComparer()));

            BasePolygon prevPolygon = mainBlastPolygon;
            while (true)
            {
                mainBlastPolygon = prevPolygon.Offset(_config.MainBlastHoleOffset) as MainBlastPolygon;
                if (mainBlastPolygon == null)
                {
                    // Console.WriteLine("主爆孔多边形为空！");
                    break;
                }
                blastPolygons.Add(mainBlastPolygon);
                mainBlastPolygon.ArrangeHoles(_config.MainBlastHoleSpacing);
                blastHolePositions.Add(new HashSet<Point3D>(mainBlastPolygon.HolePoints, new Point3DEqualityComparer()));
                prevPolygon = mainBlastPolygon;
            }
        }

        // 分配设置孔位
        private void AssignHolePosition()
        {
            var holeBottomPosition = new HoleBottomPosition(_config, bottomPolygon, blastPolygons, blastHolePositions);
            holePositionLists = holeBottomPosition.GetHoleBottomPosition();
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
                    bottomPolygon.Dispose();
                    foreach (var polygon in blastPolygons)
                    {
                        polygon.Dispose();
                    }
                    blastPolygons.Clear();
                    blastHolePositions.Clear();
                    foreach (var holePositionList in holePositionLists)
                    {
                        holePositionList.Clear();
                    }
                    holePositionLists.Clear();
                    blastTiming.Clear();
                    foreach (var linePoints in blastLinePoints)
                    {
                        linePoints.Clear();
                    }
                    blastLinePoints.Clear();
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

        ~BlastFactory()
        {
            Dispose(false);
        }
    }
}