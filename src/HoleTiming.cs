using System.Drawing;
using MathNet.Spatial.Euclidean;
using HolePosition = BlastDesign.tool.HolePosition;

public class HoleTiming
{
    private readonly Config _config;
    private bool flag = true; // 用于标记是否是读取用户输入的孔坐标
    private HolePosition blastStartPoint = new HolePosition();
    List<List<HolePosition>> holePositions;
    public HoleTiming(List<List<HolePosition>> holePositions, Config config)
    {
        _config = config;
        this.holePositions = holePositions;
    }

    // 计算几个一组
    private int GetGroupSize() => _config.PreSplitHoleCount;

    // 通过组大小和孔的数量计算分组方案
    List<int> DesignGroup(List<HolePosition> holePositions)
    {
        int groupSize = GetGroupSize();
        int holeCount = holePositions.Count;
        List<int> group = new List<int>();
        int remainingHoles = holeCount % groupSize;
        for (int i = 0; i < holeCount / groupSize; i++)
        {
            group.Add(groupSize);
        }
        if (remainingHoles > 0)
        {
            group.Add(remainingHoles);
        }
        return group;
    }

    // 根据分组方案和孔的排序，将孔分组 
    public List<List<Point3D>> GroupHoles(List<HolePosition> holePositions)
    {
        List<int> group = DesignGroup(holePositions);
        List<List<Point3D>> holeGroups = new List<List<Point3D>>();
        int index = 0;
        foreach (var size in group)
        {
            holeGroups.Add(holePositions.GetRange(index, size).Select(hp => hp.Top).ToList());
            index += size;
        }
        return holeGroups;
    }

    // 获取一排中起爆孔的索引
    public int GetFirstBlastHoleIndex(List<HolePosition> holePositions)
    {
        if (flag)
        {
            flag = false;
            int blastHoleIndex = 0;
            if (_config.BlastHoleIndex - 1 < 0)
            {
                blastHoleIndex = 0;
            }
            else if (_config.BlastHoleIndex - 1 >= holePositions.Count)
            {
                blastHoleIndex = holePositions.Count - 1;
            }
            else
            {
                blastHoleIndex = _config.BlastHoleIndex - 1;
            }
            blastStartPoint = holePositions[blastHoleIndex];
            return _config.BlastHoleIndex - 1;
        }
        // 返回炮孔中距离 blastStartPoint 最近的孔的索引
        int index = holePositions.IndexOf(holePositions.OrderBy(h => (h.Top - blastStartPoint.Top).Length).First());
        blastStartPoint = holePositions[index];
        return index;
    }

    // 添加到 timing 字典的方法
    public void AddToTiming(Dictionary<Point3D, double> timing, Point3D key, double value)
    {
        if (!timing.ContainsKey(key))
        {
            timing.Add(key, value);
        }
        else
        {
            // 处理键已存在的情况，例如更新值或忽略
            timing[key] = value; // 这里选择更新值
        }
    }

    public (Dictionary<Point3D, double>, List<List<Point3D>>) TimingHoles(bool hasNoPermanentEdge)
    {
        if (!hasNoPermanentEdge)
        {
            return TimingHoles();
        }
        return TimingHolesWithoutPermanentEdge();
    }

    private (Dictionary<Point3D, double>, List<List<Point3D>>) TimingHoles()
    {
        Dictionary<Point3D, double> timing = new Dictionary<Point3D, double>();
        List<List<Point3D>> blastLines = new List<List<Point3D>>();
        HolePosition blastStartPoint = new HolePosition();
        blastStartPoint.Top = new Point3D(0, 0, 0);
        var startPoint = blastStartPoint.Top;
        var holes = GroupHoles(holePositions[0]);
        for (int i = 0; i < holes.Count; i++)
        {
            for (int j = 0; j < holes[i].Count; j++)
            {
                AddToTiming(timing, holes[i][j], i * _config.InterColumnDelay);
                blastLines.Add(new List<Point3D> { startPoint, holes[i][j] });
                startPoint = holes[i][j];
            }
        }
        startPoint = blastStartPoint.Top;
        int count = 0;
        for (int i = holePositions.Count - 1; i > 0; i--)
        {
            if (holePositions[i].Count == 0)
            {
                count++;
                continue;
            }
            int index = GetFirstBlastHoleIndex(holePositions[i]);
            blastLines.Add(new List<Point3D> { startPoint, holePositions[i][index].Top });
            startPoint = holePositions[i][index].Top;
            AddToTiming(timing, holePositions[i][index].Top, (holePositions.Count - i - 1 - count) * _config.InterRowDelay);
            for (int j = index - 1; j >= 0; j--)
            {
                blastLines.Add(new List<Point3D> { holePositions[i][j + 1].Top, holePositions[i][j].Top });
                AddToTiming(timing, holePositions[i][j].Top, (holePositions.Count - i - 1 - count) * _config.InterRowDelay + (index - j) * _config.InterColumnDelay);
            }
            for (int j = index + 1; j < holePositions[i].Count; j++)
            {
                blastLines.Add(new List<Point3D> { holePositions[i][j - 1].Top, holePositions[i][j].Top });
                AddToTiming(timing, holePositions[i][j].Top, (holePositions.Count - i - 1 - count) * _config.InterRowDelay + (j - index) * _config.InterColumnDelay);
            }
        }
        timing = timing.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        return (timing, blastLines);
    }

    private (Dictionary<Point3D, double>, List<List<Point3D>>) TimingHolesWithoutPermanentEdge()
    {
        Dictionary<Point3D, double> timing = new Dictionary<Point3D, double>();
        List<List<Point3D>> blastLines = new List<List<Point3D>>();
        HolePosition blastStartPoint = new HolePosition();
        blastStartPoint.Top = new Point3D(0, 0, 0);
        var startPoint = blastStartPoint.Top;
        int count = 0;
        for (int i = holePositions.Count - 1; i >= 0; i--)
        {
            if (holePositions[i].Count == 0)
            {
                count++;
                continue;
            }
            int index = GetFirstBlastHoleIndex(holePositions[i]);
            blastLines.Add(new List<Point3D> { startPoint, holePositions[i][index].Top });
            startPoint = holePositions[i][index].Top;
            AddToTiming(timing, holePositions[i][index].Top, (holePositions.Count - i - 1 - count) * _config.InterRowDelay);
            for (int j = index - 1; j >= 0; j--)
            {
                blastLines.Add(new List<Point3D> { holePositions[i][j + 1].Top, holePositions[i][j].Top });
                AddToTiming(timing, holePositions[i][j].Top, (holePositions.Count - i - 1 - count) * _config.InterRowDelay + (index - j) * _config.InterColumnDelay);
            }
            for (int j = index + 1; j < holePositions[i].Count; j++)
            {
                blastLines.Add(new List<Point3D> { holePositions[i][j - 1].Top, holePositions[i][j].Top });
                AddToTiming(timing, holePositions[i][j].Top, (holePositions.Count - i - 1 - count) * _config.InterRowDelay + (j - index) * _config.InterColumnDelay);
            }
        }
        timing = timing.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        return (timing, blastLines);
    }

    // 实现 IDisposable 接口
    private bool disposed = false;
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
            // 释放非托管资源
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~HoleTiming()
    {
        Dispose(false);
    }
}