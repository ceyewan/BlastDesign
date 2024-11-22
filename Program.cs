using System;

class Program
{
    static void Main(string[] args)
    {
        // Config config = new Config
        // {
        //     IsContourLineEndHoleEnabled = false,
        //     IsPlumBlossomHoleEnabled = true,
        //     MinDistanceToFreeLine = 2.5,
        //     PreSplitHoleOffset = -1.5,
        //     BufferHoleOffset = -2.5,
        //     MainBlastHoleOffset = -3.0,
        //     PreSplitHoleSpacing = 1.0,
        //     BufferHoleSpacing = 3.0,
        //     MainBlastHoleSpacing = 4.0,
        //     InterColumnDelay = 30,
        //     InterRowDelay = 30,
        //     PreSplitHoleCount = 8,
        //     TopName = new string[] { "7", "Surface7", "Surface8" },
        //     TopPoints = new string[] { "Point1", "Point2", "Point3" }
        // };
        Config config = new Config();
        BlastFactory polygonFactory = new BlastFactory(config);
        polygonFactory.PrintHoles();            // 打印炮孔信息
        polygonFactory.DrawHoleDesign();        // 绘制炮孔设计
        polygonFactory.DrawGif();               // 绘制 Gif 帧
        polygonFactory.DrawTiming();            // 绘制起爆网络
        polygonFactory.DrawCrossSection();      // 绘制剖面图
    }
}
