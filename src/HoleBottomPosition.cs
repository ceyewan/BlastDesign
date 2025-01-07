using BlastDesign.tool;
using MathNet.Spatial.Euclidean;

namespace BlastDesign.tool.BlackBoxTest
{
    /// <summary>
    /// 计算炮孔底部位置的类
    /// </summary>
    public class HoleBottomPosition
    {
        #region Fields 
        private readonly Config _config;
        private readonly PreSplitPolygon _bottomPolygon;
        private readonly List<BasePolygon> _blastPolygons = [];  // 确定爆破线的多边形
        private readonly List<HashSet<Point3D>> _blastHolePositions = [];  // 爆破孔位置(预裂孔、缓冲孔和主爆孔)
        private readonly bool _hasNoPermanentEdge;  // 是否有永久轮廓面
        private int _count = 1;  // 炮孔编号
        private readonly List<List<HolePosition>> _holePositionLists = [];  // 结果炮孔坐标
        private readonly List<double> _inclinationAngle = [];  // 每排炮孔的倾斜角度
        private int _minX, _maxX;  // x的范围,用于绘制剖面图
        #endregion

        #region Constructor
        public HoleBottomPosition(
            Config config,
            PreSplitPolygon bottomPolygon,
            List<BasePolygon> blastPolygons,
            List<HashSet<Point3D>> blastHolePositions)
        {
            _config = config;
            _bottomPolygon = bottomPolygon;
            _blastPolygons = blastPolygons;
            _hasNoPermanentEdge = blastPolygons[0].HasNoPermanentEdge;
            _blastHolePositions = blastHolePositions;

            FindXRange();
            var crossSection = new CrossSection(config, bottomPolygon, blastPolygons, blastHolePositions);
            _inclinationAngle = crossSection.CalculateInclinationAngle((_minX + _maxX) / 2);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 获取炮孔底部坐标
        /// </summary>
        public List<List<HolePosition>> GetHoleBottomPosition()
        {
            if (!_hasNoPermanentEdge)
            {
                ProcessFirstRowBottomPosition();
                ProcessSecondRowBottomPosition();
            }

            for (int i = _hasNoPermanentEdge ? 0 : 2; i < _blastHolePositions.Count; i++)
            {
                List<Point3D> sortedHoles = HoleSort.Sort(_blastPolygons[i], _blastHolePositions[i]);
                _holePositionLists.Add([]);

                foreach (var hole in sortedHoles)
                {
                    var holePosition = new HolePosition
                    {
                        Top = hole,
                        Bottom = new(hole.X, hole.Y, 0),
                        HoleStyle = HolePosition.HoleType.MainBlastHole,
                        RowId = i + 1,
                        ColumnId = sortedHoles.IndexOf(hole) + 1,
                        HoleId = _count++
                    };

                    var diameter = (holePosition.Top.Z - _bottomPolygon.Edges[0].Start.Z) / (_config.IsMainBlastHoleSegmented ? 2 : 1)
                        + _config.Depth * Math.Sin(_inclinationAngle[i]);

                    holePosition.Bottom = new(
                        holePosition.Top.X,
                        holePosition.Top.Y + diameter / Math.Tan(_inclinationAngle[i]),
                        holePosition.Top.Z - diameter);

                    _holePositionLists[i].Add(holePosition);
                }
            }
            if (!_hasNoPermanentEdge && _config.IsMainBlastHoleSegmented)
            {
                foreach (var holePosition in _holePositionLists[1])
                {
                    holePosition.Bottom = holePosition.Middle;
                }
            }
            return _holePositionLists;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 处理第一排炮孔的底部坐标
        /// </summary>
        private void ProcessFirstRowBottomPosition()
        {
            var sortedTopHoles = HoleSort.Sort(_blastPolygons[0], _blastHolePositions[0]);
            List<int> holeCount = [];

            foreach (var edge in _blastPolygons[0].Edges.Where(e => e.style is 3 or 5))
            {
                var index1 = sortedTopHoles.FindIndex(p => p.Equals(edge.Start, Constant.ErrorThreshold));
                var index2 = sortedTopHoles.FindIndex(p => p.Equals(edge.End, Constant.ErrorThreshold));

                if (index1 == -1) index1 = -1;
                if (index2 == -1) index2 = sortedTopHoles.Count;

                holeCount.Add(index2 - index1 + 1);
            }

            _bottomPolygon.ArrangeHoles(holeCount, _config.IsContourLineEndHoleEnabled);
            var sortedBottomHoles = HoleSort.Sort(_bottomPolygon,
                new HashSet<Point3D>(_bottomPolygon.HolePoints, new Point3DEqualityComparer()));

            ValidateHoleCount(sortedTopHoles, sortedBottomHoles, "第一排");
            _holePositionLists.Add([]);

            for (int i = 0; i < sortedBottomHoles.Count; i++)
            {
                _holePositionLists[0].Add(new()
                {
                    Top = sortedTopHoles[i],
                    Bottom = sortedBottomHoles[i],
                    HoleStyle = HolePosition.HoleType.PreSplitHole,
                    RowId = 1,
                    ColumnId = i + 1,
                    HoleId = _count++
                });
            }
        }

        /// <summary>
        /// 处理第二排炮孔的底部坐标
        /// </summary>
        private void ProcessSecondRowBottomPosition()
        {
            var sortedTopHoles = HoleSort.Sort(_blastPolygons[1], _blastHolePositions[1]);
            int holeCount = sortedTopHoles.Count;

            if (!sortedTopHoles[0].Equals(_blastPolygons[1].StartPoint, Constant.ErrorThreshold))
            {
                holeCount++;
            }
            if (!sortedTopHoles[^1].Equals(_blastPolygons[1].EndPoint, Constant.ErrorThreshold))
            {
                holeCount++;
            }

            var bufferPolygon = (BufferPolygon)_bottomPolygon.Offset(_config.PreSplitHoleOffset);
            bufferPolygon.ArrangeHoles(holeCount, _config.IsContourLineEndHoleEnabled);

            var sortedBottomHoles = HoleSort.Sort(bufferPolygon,
                new HashSet<Point3D>(bufferPolygon.HolePoints, new Point3DEqualityComparer()));

            ValidateHoleCount(sortedTopHoles, sortedBottomHoles, "第二排");
            _holePositionLists.Add([]);

            for (int i = 0; i < sortedBottomHoles.Count; i++)
            {
                _holePositionLists[1].Add(new()
                {
                    Top = sortedTopHoles[i],
                    Bottom = sortedBottomHoles[i],
                    HoleStyle = HolePosition.HoleType.BufferHole,
                    RowId = 2,
                    ColumnId = i + 1,
                    HoleId = _count++
                });
            }
        }

        /// <summary>
        /// 验证顶部和底部炮孔数量是否一致
        /// </summary>
        private static void ValidateHoleCount(ICollection<Point3D> top, ICollection<Point3D> bottom, string rowName)
        {
            if (top.Count == bottom.Count) return;

            throw new ArgumentException(
                $"{rowName}顶部炮孔({top.Count})和底部炮孔({bottom.Count})数量不一致");
        }

        /// <summary>
        /// 找到 x 的范围
        /// </summary>
        private void FindXRange()
        {
            _minX = int.MaxValue;
            _maxX = int.MinValue;

            foreach (var edge in _blastPolygons[0].Edges)
            {
                _minX = Math.Min(_minX, (int)Math.Min(edge.Start.X, edge.End.X));
                _maxX = Math.Max(_maxX, (int)Math.Max(edge.Start.X, edge.End.X));
            }
        }
        #endregion

        #region Helper Classes
        /// <summary>
        /// 3D点相等性比较器
        /// </summary>
        private class Point3DEqualityComparer : IEqualityComparer<Point3D>
        {
            public bool Equals(Point3D v1, Point3D v2) =>
                v1.Equals(v2, Constant.ErrorThreshold);

            public int GetHashCode(Point3D v) =>
                HashCode.Combine(
                    (int)(v.X / Constant.ErrorThreshold),
                    (int)(v.Y / Constant.ErrorThreshold),
                    (int)(v.Z / Constant.ErrorThreshold));
        }
        #endregion
    }
}