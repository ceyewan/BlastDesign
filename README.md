## 使用说明

1. **配置参数**
    - 在 `Program.cs` 文件中创建 Config 类，根据需要调整爆破参数。

2. **运行程序**
    ```bash
    dotnet run .
    ```

    程序将：
    - 打印炮孔信息
    - 绘制炮孔设计图
    - 生成爆破动画GIF
    - 绘制起爆网络图

3. **生成动画GIF**
    - 运行 `createGif.sh` 脚本：
      ```bash
      bash createGif.sh
      ```
    - 按提示输入帧率，脚本将生成 `output.gif`。

## 项目结构

- `Config.cs`：配置参数类。
- `BlastFactory.cs`：爆破孔布局生成逻辑。
- `Program.cs`：程序入口。
- `createGif.sh`：生成动画GIF的Shell脚本。
- `frames/`：存放生成的图片帧。
- `README.md`：项目说明文档。