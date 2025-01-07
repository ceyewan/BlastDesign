using System.Drawing;
using MathNet.Spatial.Euclidean;

namespace BlastDesign.tool.BlackBoxTest
{
    /// <summary>
    /// 炮孔计时类
    /// </summary>
    public class HoleTiming : IDisposable
    {
        #region Fields
        private readonly Config _config;
        private bool flag = true;  // 标记是否是首次读取用户输入的孔坐标
        private HolePosition blastStartPoint;  // 起爆点位置
        private List<List<HolePosition>> holePositions;  // 所有炮孔位置
        private List<List<Point3D>> blastLines;  // 爆破连接线
        private bool disposed = false;  // 用于 IDisposable 实现
        #endregion

        #region Constructor
        public HoleTiming(List<List<HolePosition>> holePositions, Config config)
        {
            _config = config;
            this.holePositions = holePositions;
            this.blastLines = new List<List<Point3D>>();
            this.blastStartPoint = new HolePosition();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 计算孔的起爆时间和连接线
        /// </summary>
        /// <param name="hasNoPermanentEdge">是否没有永久边坡</param>
        /// <returns>起爆时间字典和连接线列表</returns>
        public (Dictionary<Point3D, double>, List<List<Point3D>>) TimingHoles(bool hasNoPermanentEdge)
        {
            return hasNoPermanentEdge ? TimingHolesWithoutPermanentEdge() : TimingHoles();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 计算预裂孔分组大小
        /// </summary>
        private int GetGroupSize() => _config.PreSplitHoleCount;

        /// <summary>
        /// 设计孔的分组方案
        /// </summary>
        private List<int> DesignGroup(List<HolePosition> holePositions)
        {
            int groupSize = GetGroupSize();
            int holeCount = holePositions.Count;
            List<int> group = new List<int>();

            // 计算完整组的数量
            int fullGroups = holeCount / groupSize;
            for (int i = 0; i < fullGroups; i++)
            {
                group.Add(groupSize);
            }

            // 处理剩余的孔
            int remainingHoles = holeCount % groupSize;
            if (remainingHoles > 0)
            {
                group.Add(remainingHoles);
            }

            return group;
        }

        /// <summary>
        /// 将孔按组进行分组，返回虚拟孔位置
        /// </summary>
        private List<Point3D> GroupHoles(List<HolePosition> holePositions, bool virtualOnly)
        {
            List<int> group = DesignGroup(holePositions);
            List<Point3D> virtualHoles = new List<Point3D>();
            int index = 0;

            foreach (var size in group)
            {
                var currentGroup = holePositions.GetRange(index, size);
                var groupHoles = currentGroup.Select(hp => hp.Top).ToList();

                // 计算虚拟孔的位置（组内孔的平均位置）
                double avgX = groupHoles.Average(h => h.X);
                double avgY = groupHoles.Average(h => h.Y) - _config.PreSplitHoleOffset;
                double avgZ = groupHoles.Average(h => h.Z);

                var virtualHole = new Point3D(avgX, avgY, avgZ);
                virtualHoles.Add(virtualHole);

                // 将虚拟孔和组内所有孔连接
                foreach (var hole in groupHoles)
                {
                    blastLines.Add(new List<Point3D> { virtualHole, hole });
                }

                index += size;
            }
            return virtualHoles;
        }

        /// <summary>
        /// 获取起爆孔的索引
        /// </summary>
        private int GetFirstBlastHoleIndex(List<HolePosition> holePositions)
        {
            if (flag)
            {
                flag = false;
                // 首次使用用户指定的起爆孔
                int blastHoleIndex = Math.Clamp(_config.BlastHoleIndex - 1, 0, holePositions.Count - 1);
                blastStartPoint = holePositions[blastHoleIndex];
                return blastHoleIndex;
            }

            // 后续使用距离上一个起爆点最近的孔
            int index = holePositions.IndexOf(
                holePositions.OrderBy(h => (h.Top - blastStartPoint.Top).Length).First()
            );
            blastStartPoint = holePositions[index];
            return index;
        }

        /// <summary>
        /// 添加起爆时间到字典
        /// </summary>
        private void AddToTiming(Dictionary<Point3D, double> timing, Point3D key, double value)
        {
            if (!timing.ContainsKey(key))
            {
                timing.Add(key, value);
            }
            else
            {
                timing[key] = value;
            }
        }

        /// <summary>
        /// 计算有永久边坡时的起爆时间
        /// </summary>
        private (Dictionary<Point3D, double>, List<List<Point3D>>) TimingHoles()
        {
            Dictionary<Point3D, double> timing = new Dictionary<Point3D, double>();
            var startPoint = new Point3D(0, 0, 0);

            // 处理预裂孔
            var holes = GroupHoles(holePositions[0], true);
            for (int i = 0; i < holes.Count; i++)
            {
                AddToTiming(timing, holes[i], i * _config.InterColumnDelay);
                blastLines.Add(new List<Point3D> { startPoint, holes[i] });
                startPoint = holes[i];
            }
            startPoint = new Point3D(0, 0, 0);
            // 处理其他孔
            int count = 0;
            for (int i = holePositions.Count - 1; i > 0; i--)
            {
                if (holePositions[i].Count == 0)
                {
                    count++;
                    continue;
                }
                ProcessRowTiming(timing, holePositions[i], startPoint, i, count);
                startPoint = holePositions[i][GetFirstBlastHoleIndex(holePositions[i])].Top;
            }
            return (timing.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value), blastLines);
        }

        /// <summary>
        /// 计算无永久边坡时的起爆时间
        /// </summary>
        private (Dictionary<Point3D, double>, List<List<Point3D>>) TimingHolesWithoutPermanentEdge()
        {
            Dictionary<Point3D, double> timing = new Dictionary<Point3D, double>();
            var startPoint = new Point3D(0, 0, 0);
            int count = 0;

            for (int i = holePositions.Count - 1; i >= 0; i--)
            {
                if (holePositions[i].Count == 0)
                {
                    count++;
                    continue;
                }
                ProcessRowTiming(timing, holePositions[i], startPoint, i, count);
                startPoint = holePositions[i][GetFirstBlastHoleIndex(holePositions[i])].Top;
            }

            return (timing.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value), blastLines);
        }

        /// <summary>
        /// 处理单排孔的起爆时间
        /// </summary>
        private void ProcessRowTiming(Dictionary<Point3D, double> timing, List<HolePosition> row, Point3D startPoint, int rowIndex, int emptyRowCount)
        {
            int index = GetFirstBlastHoleIndex(row);
            blastLines.Add(new List<Point3D> { startPoint, row[index].Top });

            double baseDelay = (holePositions.Count - rowIndex - 1 - emptyRowCount) * _config.InterRowDelay;
            AddToTiming(timing, row[index].Top, baseDelay);

            // 处理左侧孔
            for (int j = index - 1; j >= 0; j--)
            {
                blastLines.Add(new List<Point3D> { row[j + 1].Top, row[j].Top });
                AddToTiming(timing, row[j].Top, baseDelay + (index - j) * _config.InterColumnDelay);
            }

            // 处理右侧孔
            for (int j = index + 1; j < row.Count; j++)
            {
                blastLines.Add(new List<Point3D> { row[j - 1].Top, row[j].Top });
                AddToTiming(timing, row[j].Top, baseDelay + (j - index) * _config.InterColumnDelay);
            }
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var holePosition in holePositions)
                    {
                        holePosition.Clear();
                    }
                    holePositions.Clear();
                }
                disposed = true;
            }
        }
        #endregion

        #region Destructor
        ~HoleTiming()
        {
            Dispose(false);
        }
        #endregion
    }
}