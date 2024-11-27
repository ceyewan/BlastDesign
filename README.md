## 爆破设计项目使用指南

### 项目结构

- `Config.cs`：配置参数类。
- `BlastFactory.cs`：爆破孔布局生成逻辑。
- `Program.cs`：程序入口。
- `createGif.sh`：生成动画 GIF 的 Shell 脚本。
- `frames/`：存放生成的图片帧。
- `images/`：存放生成的图片文件。
- `src/`：存放源代码文件。
- `README.md`：项目说明文档。

### 使用方法

1. **安装依赖**：确保您已经安装了 Clipper2、SkiaSharp 和 MathNet.Spatial 库。您可以使用 NuGet 包管理器来安装这些库。

2. ** 打开项目 **：使用 Visual Studio 或其他 C# 开发环境打开 `BlastDesign.sln` 解决方案文件。

3. ** 运行程序 **：在终端切换到当前目录下，执行 `dotnet run .`，或者运行 `Program.cs` 文件。

4. ** 执行脚本得到动图 **：在当前路径下执行 `bash createGif.sh` 脚本得到动图，需要提前安装 `ffmpeg`。

### 配置参数

在 `Config.cs` 文件中，您可以找到配置参数。这些参数包括：

- `PreSplitHoleSpacing`：预裂孔间距。
- `BufferHoleSpacing`：缓冲孔间距。
- `MainBlastHoleSpacing`：主爆孔间距。
- `PreSplitHoleOffset`：预裂孔偏移。
- `BufferHoleOffset`：缓冲孔偏移。
- `MainBlastHoleOffset`：主爆孔偏移。
- `TopPoints`：顶部多边形点坐标。
- `TopStyle`：顶部多边形点类型。
- `BottomPoints`：底部多边形点坐标。
- `BottomStyle`：底部多边形点类型。
- `MinDistanceToFreeLine`：最小距离到自由线。
- `CrossSectionXCoordinates`：剖面图 X 坐标。
- `BlastHoleDiameters`：爆破孔直径。
- `InclinationAngle`：倾角。
- `Depth`：深度，垂直距离的超深。
- `PreSplitHoleChargeConfig`：预裂孔装药配置。
- `BufferHoleChargeConfig`：缓冲孔装药配置。
- `MainBlastHoleChargeConfig`：主爆孔装药配置。

### 功能

- ** 获取炮孔坐标 **：使用 `BlastDrawer.cs` 类中的 `GetHoles` 方法获取炮孔信息。
- ** 绘制平面图 **：使用 `BlastDrawer.cs` 类中的 `DrawHoleDesign` 方法绘制爆破孔平面图。
- ** 绘制爆破网络图 **：使用 `BlastDrawer.cs` 类中的 `DrawTiming` 方法绘制爆破网络图。
- ** 生成动画 GIF**：使用 `BlastFactory.cs` 类中的 `DrawGif` 方法生成动画 GIF。
- ** 绘制剖面图 **：使用 `BlastFactory.cs` 类中的 `DrawCrossSection` 方法绘制剖面图。
- ** 绘制炮孔装药结构图 **：使用 `BlastFactory.cs` 类中的 `DrawChargeStructure` 方法绘制炮孔装药结构图。

### 注意事项

- 请确保已安装 Clipper2、SkiaSharp 和 MathNet.Spatial 库。
- 项目中的 `Config.cs` 文件包含了配置参数，可以根据需要进行修改。
- 如果装药结构图不显示中文，修改 `HoleChargeDrawing.cs` 文件的第 74 行，设置一个系统支持的中文字体，例如 `SimHei` 或 `Microsoft YaHei`。