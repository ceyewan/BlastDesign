using MathNet.Spatial.Euclidean;

public class Config
{
    public bool IsContourLineEndHoleEnabled { get; set; } = false; // 设置轮廓线两端是否布孔
    public bool IsPlumBlossomHoleEnabled { get; set; } = false; // 设置是否布置梅花孔
    public double MinDistanceToFreeLine { get; set; } = 1.0; // 炮孔到自由线的最短距离
    public double PreSplitHoleOffset { get; set; } = -1.1; // 预裂孔和缓冲孔的偏移距离
    public double BufferHoleOffset { get; set; } = -1.7; // 缓冲孔和主爆孔的偏移距离
    public double MainBlastHoleOffset { get; set; } = -2.0; // 主爆孔之间的偏移距离
    public double PreSplitHoleSpacing { get; set; } = 0.9; // 预裂孔间距
    public double BufferHoleSpacing { get; set; } = 2.6; // 缓冲孔间距
    public double MainBlastHoleSpacing { get; set; } = 3.9; // 主爆孔间距
    public int InterColumnDelay { get; set; } = 25; // 列延时（孔间延时）
    public int InterRowDelay { get; set; } = 50; // 行延时（排间延时）
    public int PreSplitHoleCount { get; set; } = 6; // 预裂孔几孔同时起爆
    public string[] TopPoints { get; set; } = new string[] {
        "(1181.0449398198234,1018.8790263922049,1951.0240478515625)",
        "(1130.7950548678843,1023.8240149833414,1951.0240478515625)",
        "(1140.4530354822111,1032.329997626429,1951.0240478515625)",
        "(1171.8889576284473,1028.2720074378565,1951.0240478515625)",
        "(1181.0449398198234,1018.8790263922049,1951.0240478515625)"
    }; // 顶部控制点坐标
    public string[] TopStyle { get; set; } = new string[] { "1", "4", "3", "4", "1" }; // 顶部控制点类型
    public string[] BottomPoints { get; set; } = new string[] {
        "(1181.0449398198234,1018.8790263922049,1936.428955078125)",
        "(1130.7950548678843,1023.8240149833414,1936.428955078125)",
        "(1140.4530354822111,1032.329997626429,1936.428955078125)",
        "(1171.8889576284473,1028.2720074378565,1936.428955078125)",
        "(1181.0449398198234,1018.8790263922049,1936.428955078125)"
    };//底部控制点坐标
    public string[] BottomStyle { get; set; } = new string[] { "1", "4", "3", "4", "1" }; // 底部控制点类型
    public double[] CrossSectionXCoordinates { get; set; } = new double[] { 1, 2, 3, 4 }; // 剖面图的x坐标
    public double[] BlastHoleDiameters { get; set; } = new double[] { 0.1, 0.2, 0.2 }; // 三种炮孔的直径
    public double Depth { get; set; } = 2; // 超深
    public double bottomResistanceLine { get; set; } = 0.8; // 底面抵抗线的距离
    public double InclinationAngle { get; set; } = 95; // 倾角
    // 炮孔装药结构参数，包括总长度、孔底加强段长度、炮孔堵塞段长度、装药段间隔长度、装药块长度
    public int[] PreSplitHoleChargeConfig { get; set; } = { 800, 60, 100, 25, 15 };
    public int[] BufferHoleChargeConfig { get; set; } = { 800, 60, 100, 25, 15 };
    public int[] MainBlastHoleChargeConfig { get; set; } = { 800, 60, 100, 25, 15 };
}