using System;

namespace BlastDesign.tool.BlackBoxTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // 初始化配置参数
            Config config = new Config
            {
                // 设置炮孔到自由线的最短距离
                MinDistanceToFreeLine = 1.0,
                // 设置预裂孔、缓冲孔和主爆孔的偏移距离
                PreSplitHoleOffset = -1.1,
                BufferHoleOffset = -1.7,
                MainBlastHoleOffset = -2.6,
                // 设置各类型炮孔的间距
                PreSplitHoleSpacing = 0.9,
                BufferHoleSpacing = 2.6,
                MainBlastHoleSpacing = 3.9,
            };
            BlastFactory polygonFactory = new BlastFactory(config);
            polygonFactory.PrintHoles();                                    // 打印炮孔信息
            var holes = polygonFactory.GetHoles();                          // 获取炮孔信息
            polygonFactory.DrawHoleDesign("./images/hole_design.svg");      // 绘制炮孔设计
                                                                            // 提前设置好孔间延时和排间延时，预裂孔同时起爆数量
            polygonFactory.DrawTiming("./images/timing_network.svg");       // 绘制起爆网络
            polygonFactory.DrawGif();                                       // 绘制 Gif 帧
                                                                            // 提前设置好剖面图的 x 坐标，超深（垂直长度）和倾角
            polygonFactory.DrawCrossSection("./images");                    // 绘制剖面图
                                                                            // 提前设置好三种孔的装药结构参数
            polygonFactory.DrawChargeStructure("./images");                 // 绘制装药结构
            // var lastRowHoleDistance = polygonFactory.GetLastRowHoleDistance(); // 获取最后一排炮孔到自由线的最大距离和最小距离
            // double maxDistance = lastRowHoleDistance.Item1;
            // double minDistance = lastRowHoleDistance.Item2;
            // Console.WriteLine("最后一排主爆孔到自由线的最大距离: {0}, 最后一排主爆孔到自由线的最小距离: {1}", maxDistance, minDistance);
        }
    }
}
