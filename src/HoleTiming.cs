using System.Drawing;
using MathNet.Spatial.Euclidean;
using HolePosition = BlastDesign.tool.HolePosition;

public class HoleTiming
{
    private readonly Config _config;
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

    // 获取中间孔的坐标
    public int GetMiddleHoleIndex(List<HolePosition> holePositions)
    {
        return holePositions.Count / 2;
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

    public (Dictionary<Point3D, double>, List<List<Point3D>>) TimingHoles()
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
        for (int i = holePositions.Count - 1; i > 0; i--)
        {
            if (holePositions[i].Count == 0)
            {
                continue;
            }
            int index = GetMiddleHoleIndex(holePositions[i]);
            blastLines.Add(new List<Point3D> { startPoint, holePositions[i][index].Top });
            startPoint = holePositions[i][index].Top;
            AddToTiming(timing, holePositions[i][index].Top, (holePositions.Count - i - 1) * _config.InterRowDelay);
            for (int j = index - 1; j >= 0; j--)
            {
                blastLines.Add(new List<Point3D> { holePositions[i][j + 1].Top, holePositions[i][j].Top });
                AddToTiming(timing, holePositions[i][j].Top, (holePositions.Count - i - 1) * _config.InterRowDelay + (index - j) * _config.InterColumnDelay);
            }
            for (int j = index + 1; j < holePositions[i].Count; j++)
            {
                blastLines.Add(new List<Point3D> { holePositions[i][j - 1].Top, holePositions[i][j].Top });
                AddToTiming(timing, holePositions[i][j].Top, (holePositions.Count - i - 1) * _config.InterRowDelay + (j - index) * _config.InterColumnDelay);
            }
        }
        timing = timing.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        return (timing, blastLines);
    }
}