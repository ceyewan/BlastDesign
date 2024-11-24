using MathNet.Spatial.Euclidean;

public class Config
{
    public bool IsContourLineEndHoleEnabled { get; set; } = true; // 设置轮廓线两端是否布孔
    public bool IsPlumBlossomHoleEnabled { get; set; } = false; // 设置是否布置梅花孔
    public double MinDistanceToFreeLine { get; set; } = 1.0; // 炮孔到自由线的最短距离
    public double PreSplitHoleOffset { get; set; } = -1.1; // 预裂孔和缓冲孔的偏移距离
    public double BufferHoleOffset { get; set; } = -1.7; // 缓冲孔和主爆孔的偏移距离
    public double MainBlastHoleOffset { get; set; } = -2.6; // 主爆孔之间的偏移距离
    public double PreSplitHoleSpacing { get; set; } = 0.9; // 预裂孔间距
    public double BufferHoleSpacing { get; set; } = 2.6; // 缓冲孔间距
    public double MainBlastHoleSpacing { get; set; } = 3.9; // 主爆孔间距
    public int InterColumnDelay { get; set; } = 25; // 列延时（孔间延时）
    public int InterRowDelay { get; set; } = 25; // 行延时（排间延时）
    public int PreSplitHoleCount { get; set; } = 6; // 预裂孔几孔同时起爆
    public string[] TopName { get; set; } = new string[] { "6", "Surface2", "Surface3", "Surface4", "Surface5", "Surface6", "1", "2", "3", "4", "5", "6" }; // 顶部轮廓控制点名称
    public string[] TopPoints { get; set; } = new string[] {
        "(1192.29179517837,1814.2950239428,10)",
        "(1203.37736472149,1804.77694287797,10)",
        "(1209.84997016126,1812.32075943659,10)",
        "(1238.71817747134,1815.93036567953,10)",
        "(1269.71929117736,1814.70565848252,10)",
        "(1272.42836373365,1810.69526344462,10)",
        "(1284.1376372666,1818.60502576662,10)",
        "(1278.20976190496,1828.97172699636,10)",
        "(1238.09667495072,1830.56634945151,10)",
        "(1199.36858805073,1825.72238097234,10)",
        "(1196.43959186318,1819.14487048819,10)",
        "(1192.29179517837,1814.2950239428,10)"
    }; // 顶部控制点坐标
    public string[] TopStyle { get; set; } = new string[] { "1", "1", "1", "1", "1", "1", "3", "3", "3", "3", "3", "1" }; // 顶部控制点类型
    public string[] BottomPoints { get; set; } = new string[] {
        "(1193.40811158726,1812.6001589988,0)",
        "(1203.35585616727,1804.05901558199,0)",
        "(1209.74849259074,1811.50962831489,0)",
        "(1238.70966073648,1815.13085814114,0)",
        "(1269.82834957778,1813.90150610732,0)",
        "(1272.48214112917,1809.97294670251,0)",
        "(1282.95782446729,1817.04940325055,0)",
        "(1277.4386568771,1826.7013540683,0)",
        "(1238.15189807875,1828.26312735794,0)",
        "(1200.25777379537,1823.52346789455,0)",
        "(1197.55032608976,1817.44347853004,0)",
        "(1193.40811158726,1812.6001589988,0)"
    };//底部控制点坐标
    public string[] BottomStyle { get; set; } = new string[] { "1", "1", "1", "1", "1", "1", "3", "3", "3", "3", "3", "1" }; // 底部控制点类型
    public double[] CrossSectionXCoordinates { get; set; } = new double[] { 1220, 1250, 1280 }; // 剖面图的x坐标
    public double[] BlastHoleDiameters { get; set; } = new double[] { 0.1, 0.2, 0.2 }; // 三种炮孔的直径
    public double Depth { get; set; } = 2; // 超深
    public double InclinationAngle { get; set; } = 100; // 倾角


    // 炮孔装药结构参数，包括总长度、孔底加强段长度、炮孔堵塞段长度、装药段间隔长度、装药块长度
    public int[] PreSplitHoleChargeConfig { get; set; } = { 800, 60, 100, 25, 15 };
    public int[] BufferHoleChargeConfig { get; set; } = { 800, 60, 100, 25, 15 };
    public int[] MainBlastHoleChargeConfig { get; set; } = { 800, 60, 100, 25, 15 };
}