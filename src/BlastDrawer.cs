using BlastDesign.tool;
using SkiaSharp;
using MathNet.Spatial.Euclidean;

public class BlastDrawer
{
    private static float canvasWidth { get; set; } = 1500;     // 画布的 weith
    private static float canvasHeight { get; set; } = 900;    // 画布的 height
    private static double holeSpacing { get; set; } = 0;         // 孔的间距
    private static double rowOffset { get; set; } = 0;           // 行的偏移量
    private static float scale, offsetX, offsetY;                // 缩放比例，偏移量
    private static double maxX, maxY, minX, minY;                 // 最大最小坐标
    private static Point3D startPoint;

    public BlastDrawer(double maxX, double maxY, double minX, double minY, double spacing, double offset)
    {
        BlastDrawer.maxX = maxX; BlastDrawer.maxY = maxY; BlastDrawer.minX = minX; BlastDrawer.minY = minY;
        scale = (float)Math.Min(canvasWidth / (maxX - minX), canvasHeight / (maxY - minY)) * 0.9f;
        // offsetX = (float)(canvasWidth - ((canvasWidth - (maxX - minX) * scale) / 2 - minX * scale)); // 修改X轴偏移计算
        offsetY = (float)(canvasHeight - ((canvasHeight - (maxY - minY) * scale) / 2 - minY * scale)); // 修改Y轴偏移计算
        offsetX = (float)((canvasWidth - (maxX - minX) * scale) / 2 - minX * scale);
        // offsetY = (float)((canvasHeight - (maxY - minY) * scale) / 2 - minY * scale);
        holeSpacing = spacing;
        rowOffset = Math.Abs(offset);
    }

    // 绘制孔设计图
    public void DrawHoleDesign(List<BasePolygon> polygons, List<HashSet<Point3D>> blastHoles, string outputPath = "output.svg")
    {
        using var stream = new SKFileWStream(outputPath);
        var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, canvasWidth, canvasHeight), stream);
        svgCanvas.Clear(SKColors.White);
        svgCanvas.Save();
        svgCanvas.Translate(offsetX, offsetY);
        svgCanvas.Scale(scale, -scale);
        // 绘制多边形
        DrawPolygons(svgCanvas, polygons);
        // 绘制各类炮孔
        DrawBlastHoles(svgCanvas, blastHoles);
        svgCanvas.Restore();
        svgCanvas.Dispose();
    }

    // 绘制起爆网络图
    public void DrawTimingNetwork(Dictionary<Point3D, double> timing, List<List<Point3D>> blastLines, string outputPath = "timing.svg")
    {
        using var stream = new SKFileWStream(outputPath);
        var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, canvasWidth, canvasHeight), stream);
        svgCanvas.Clear(SKColors.White);
        svgCanvas.Save();
        svgCanvas.Translate(offsetX, offsetY);
        svgCanvas.Scale(scale, -scale);
        // 绘制连接线
        bool firstFlag = true;
        foreach (var line in blastLines)
        {
            if (line[0].X == 0 && line[0].Y == 0)
            {
                if (firstFlag)
                {
                    firstFlag = false;
                    startPoint = new Point3D(line[1].X, minY, 0);
                    line[0] = startPoint;
                }
                else
                {
                    DrawArrow(svgCanvas,
                        new SKPoint((float)startPoint.X, (float)startPoint.Y),
                        new SKPoint((float)line[1].X, (float)minY));
                    line[0] = new Point3D(line[1].X, minY, 0);
                }
            }
            DrawArrow(svgCanvas,
                new SKPoint((float)line[0].X, (float)line[0].Y),
                new SKPoint((float)line[1].X, (float)line[1].Y));
        }
        var paint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Red, IsAntialias = true };
        svgCanvas.DrawCircle((float)startPoint.X, (float)startPoint.Y, (float)holeSpacing / 2, paint);
        // 绘制孔位和时间标注
        DrawTimingHoles(svgCanvas, timing);
        svgCanvas.Restore();
        svgCanvas.Dispose();
    }

    // 生成动画帧
    public void DrawAnimationFrame(List<BasePolygon> polygons, Dictionary<Point3D, double> timing,
        double currentTime, double previousTime, string outputPath)
    {
        using var bitmap = new SKBitmap((int)canvasWidth, (int)canvasHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        canvas.Save();
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale, -scale);

        // 绘制边界多边形
        DrawBoundaryPolygon(canvas, polygons[0]);
        // 绘制炮孔状态
        DrawHolesWithTiming(canvas, timing, currentTime, previousTime);
        canvas.Restore();
        canvas.Flush();

        // 绘制时间标注
        DrawTimeLabel(canvas, currentTime);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        data.SaveTo(stream);
    }

    private void DrawBoundaryPolygon(SKCanvas canvas, BasePolygon polygon)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
            StrokeWidth = (float)holeSpacing / 20,
            Style = SKPaintStyle.Stroke
        };
        var path = new SKPath();
        var firstEdge = true;
        foreach (var edge in polygon.Edges)
        {
            if (firstEdge)
            {
                path.MoveTo((float)edge.Start.X, (float)edge.Start.Y);
                firstEdge = false;
            }
            path.LineTo((float)edge.Start.X, (float)edge.Start.Y);
        }
        path.Close();
        canvas.DrawPath(path, paint);
    }

    private void DrawHolesWithTiming(SKCanvas canvas, Dictionary<Point3D, double> timing, double currentTime, double previousTime)
    {
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        // 绘制所有孔
        foreach (var kvp in timing)
        {
            var holePosition = kvp.Key;
            var blastTime = kvp.Value;
            if (blastTime == currentTime)
            {
                paint.Color = SKColors.Red;
            }
            else if (blastTime == previousTime)
            {
                paint.Color = new SKColor(255, 192, 192); // 浅红色
            }
            else
            {
                paint.Color = SKColors.Gray;
            }
            canvas.DrawCircle((float)holePosition.X, (float)holePosition.Y, (float)holeSpacing / 5, paint);
        }
    }

    // 绘制边界多边形
    private void DrawPolygons(SKCanvas canvas, List<BasePolygon> polygons)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
            StrokeWidth = (float)holeSpacing / 20,
            Style = SKPaintStyle.Stroke
        };
        for (int i = 0; i < polygons.Count; i++)
        {
            var path = new SKPath();
            var firstEdge = true;
            foreach (var edge in polygons[i].Edges)
            {
                if (firstEdge)
                {
                    path.MoveTo((float)edge.Start.X, (float)edge.Start.Y);
                    firstEdge = false;
                }
                path.LineTo((float)edge.Start.X, (float)edge.Start.Y);
            }
            path.Close();
            if (i > 0)
            {
                paint.Color = SKColors.Gray;
                paint.StrokeWidth = (float)holeSpacing / 40;
            }
            canvas.DrawPath(path, paint);
        }
    }

    // 绘制炮孔
    private void DrawBlastHoles(SKCanvas canvas, List<HashSet<Point3D>> blastHoles)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)holeSpacing / 20
        };
        // 绘制预裂孔
        paint.Color = SKColors.Red;
        DrawHoleSet(canvas, blastHoles[0], paint);
        // 绘制缓冲孔
        paint.Color = SKColors.Purple;
        DrawHoleSet(canvas, blastHoles[1], paint);
        // 绘制主爆孔
        paint.Color = SKColors.Black;
        for (int i = 2; i < blastHoles.Count; i++)
        {
            DrawHoleSet(canvas, blastHoles[i], paint);
        }
    }

    // 用于平面图中绘制炮孔
    private void DrawHoleSet(SKCanvas canvas, HashSet<Point3D> holes, SKPaint paint)
    {
        foreach (var hole in holes)
        {
            canvas.DrawCircle(
                (float)hole.X,
                (float)hole.Y,
                (float)holeSpacing / 5,
                paint);
        }
    }

    // 绘制起爆线及箭头
    private void DrawArrow(SKCanvas canvas, SKPoint startPoint, SKPoint endPoint)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.DimGray,
            StrokeWidth = (float)holeSpacing / 20,
            Style = SKPaintStyle.Stroke
        };
        var midPoint = new SKPoint(
            (startPoint.X + endPoint.X) / 2,
            (startPoint.Y + endPoint.Y) / 2);
        var direction = new SKPoint(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
        var length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        var unitDirection = new SKPoint(direction.X / length, direction.Y / length);
        float arrowHeadLength = (float)holeSpacing / 4;
        float arrowHeadWidth = (float)holeSpacing / 4;
        var arrowPoint1 = new SKPoint(
            midPoint.X - arrowHeadLength * unitDirection.X + arrowHeadWidth * unitDirection.Y,
            midPoint.Y - arrowHeadLength * unitDirection.Y - arrowHeadWidth * unitDirection.X);

        var arrowPoint2 = new SKPoint(
            midPoint.X - arrowHeadLength * unitDirection.X - arrowHeadWidth * unitDirection.Y,
            midPoint.Y - arrowHeadLength * unitDirection.Y + arrowHeadWidth * unitDirection.X);
        canvas.DrawLine(startPoint, endPoint, paint);
        var path = new SKPath();
        path.MoveTo(midPoint);
        path.LineTo(arrowPoint1);
        path.LineTo(arrowPoint2);
        path.Close();
        paint.Style = SKPaintStyle.Fill;
        canvas.DrawPath(path, paint);
    }

    // 绘制爆破网络中的孔位和时间标注
    private void DrawTimingHoles(SKCanvas canvas, Dictionary<Point3D, double> timing)
    {
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Black,
            IsAntialias = true
        };
        foreach (var (holePosition, blastTime) in timing)
        {
            // 绘制孔
            canvas.DrawCircle((float)holePosition.X, (float)holePosition.Y, (float)holeSpacing / 10, paint);
            // 绘制时间标注
            paint.Color = SKColors.Black;
            paint.TextSize = (float)holeSpacing / 5;
            paint.Typeface = SKTypeface.Default; // 设置默认字体
            paint.TextAlign = SKTextAlign.Center;
            canvas.Save();
            canvas.Scale(1, -1);
            canvas.DrawText($"{blastTime} ms",
                (float)holePosition.X,
                -(float)holePosition.Y - (float)rowOffset / 5,
                paint);
            canvas.Restore();
        }
    }

    // 用于 gif 动画添加时间 label
    private void DrawTimeLabel(SKCanvas canvas, double time)
    {
        var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 24,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true
        };
        canvas.DrawText($"Time: {time} ms", 100, canvasHeight - 50, paint);
    }

    public void DrawCrossSection(List<Point3D> newEdges, List<List<Point3D>> newLines, string outputPath = "cross_section.svg")
    {
        using var stream = new SKFileWStream(outputPath);
        var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, canvasWidth, canvasHeight), stream);
        svgCanvas.Clear(SKColors.White);
        var maxX = double.MinValue;
        var maxY = double.MinValue;
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        foreach (var edge in newEdges)
        {
            maxX = Math.Max(maxX, edge.Y);
            maxY = Math.Max(maxY, edge.Z);
            minX = Math.Min(minX, edge.Y);
            minY = Math.Min(minY, edge.Z);
        }
        foreach (var line in newLines)
        {
            maxX = Math.Max(maxX, line[0].Y);
            maxY = Math.Max(maxY, line[0].Z);
            minX = Math.Min(minX, line[0].Y);
            minY = Math.Min(minY, line[0].Z);
            maxX = Math.Max(maxX, line[1].Y);
            maxY = Math.Max(maxY, line[1].Z);
            minX = Math.Min(minX, line[1].Y);
            minY = Math.Min(minY, line[1].Z);
        }
        var scale = (float)Math.Min(canvasWidth / (maxX - minX), canvasHeight / (maxY - minY)) * 0.9f;
        var offsetX = (float)(canvasWidth - ((canvasWidth - (maxX - minX) * scale) / 2 - minX * scale)); // 修改X轴偏移计算
        var offsetY = (float)(canvasHeight - ((canvasHeight - (maxY - minY) * scale) / 2 - minY * scale)); // 修改Y轴偏移计算

        // Console.WriteLine($"maxX: {maxX}, maxY: {maxY}, minX: {minX}, minY: {minY}");
        // Console.WriteLine($"scale: {scale}, offsetX: {offsetX}, offsetY: {offsetY}");

        svgCanvas.Save();
        svgCanvas.Translate(offsetX, offsetY); // 先平移
        svgCanvas.Scale(-scale, -scale);        // 在Y轴方向上使用负缩放来实现翻转

        // 绘制剖面图
        DrawCrossSectionEdges(svgCanvas, newEdges);
        DrawCrossSectionLines(svgCanvas, newLines);
        svgCanvas.Restore();
        svgCanvas.Dispose();
    }

    private void DrawCrossSectionEdges(SKCanvas canvas, List<Point3D> newEdges)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
            StrokeWidth = (float)holeSpacing / 20,
            Style = SKPaintStyle.Stroke
        };
        var path = new SKPath();
        var firstEdge = true;
        foreach (var edge in newEdges)
        {
            if (firstEdge)
            {
                path.MoveTo((float)edge.Y, (float)edge.Z);
                firstEdge = false;
            }
            path.LineTo((float)edge.Y, (float)edge.Z);
        }
        path.LineTo((float)newEdges[0].Y, (float)newEdges[0].Z);
        canvas.DrawPath(path, paint);
    }

    private void DrawCrossSectionLines(SKCanvas canvas, List<List<Point3D>> newLines)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
            StrokeWidth = (float)holeSpacing / 20,
            Style = SKPaintStyle.Stroke
        };
        // 前两条红色、后两条紫色，其他黑色
        int count = 0;
        foreach (var line in newLines)
        {
            if (count < 2)
            {
                paint.Color = SKColors.Red;
            }
            else if (count < 4)
            {
                paint.Color = SKColors.Purple;
            }
            else
            {
                paint.Color = SKColors.Black;
            }
            var path = new SKPath();
            path.MoveTo((float)line[0].Y, (float)line[0].Z);
            path.LineTo((float)line[1].Y, (float)line[1].Z);
            canvas.DrawPath(path, paint);
            count++;
        }
    }
}
