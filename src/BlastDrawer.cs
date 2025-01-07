using BlastDesign.tool;
using SkiaSharp;
using MathNet.Spatial.Euclidean;

namespace BlastDesign.tool.BlackBoxTest
{
    /// <summary>
    /// 爆破图绘制类
    /// </summary>
    public class BlastDrawer
    {
        #region Properties
        private static float CanvasWidth { get; } = 1500;     // 画布宽度
        private static float CanvasHeight { get; } = 900;     // 画布高度
        private double HoleSpacing { get; } = 0;              // 孔的间距  
        private double RowOffset { get; } = 0;                // 行的偏移量
        #endregion
        #region Fields
        private readonly float _scale, _offsetX, _offsetY;    // 缩放比例、X轴偏移、Y轴偏移
        private readonly double _maxX, _maxY, _minX, _minY;   // X、Y轴的最大最小值
        private Point3D _startPoint;                          // 起始点
        #endregion

        #region Constructor
        public BlastDrawer(double rightBoundaryX, double rightBoundaryY,
            double leftBoundaryX, double leftBoundaryY,
            double spacing, double offset)
        {
            _maxX = rightBoundaryX;
            _maxY = rightBoundaryY + (rightBoundaryY - leftBoundaryY) / 10;
            _minX = leftBoundaryX;
            _minY = leftBoundaryY - (rightBoundaryY - leftBoundaryY) / 10;

            _scale = (float)Math.Min(CanvasWidth / (_maxX - _minX),
                CanvasHeight / (_maxY - _minY)) * 0.9f;
            _offsetX = (float)((CanvasWidth - (_maxX - _minX) * _scale) / 2 - _minX * _scale);
            _offsetY = (float)(CanvasHeight - ((CanvasHeight - (_maxY - _minY) * _scale) / 2 - _minY * _scale));

            HoleSpacing = spacing;
            RowOffset = Math.Abs(offset);
        }
        #endregion

        /// <summary>
        /// 绘制孔设计图
        /// </summary>
        /// <param name="polygons">多边形列表</param>
        /// <param name="blastHoles">炮孔位置列表</param>
        /// <param name="crossSectionPositions">横断面位置数组</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="hasNoPermanentEdge">是否无永久边坡</param>
        public void DrawHoleDesign(List<BasePolygon> polygons, List<HashSet<Point3D>> blastHoles, double[] CrossSectionX, string outputPath = "output.svg", bool HasNoPermanentEdge = false)
        {
            using var stream = new SKFileWStream(outputPath);
            var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, CanvasWidth, CanvasHeight), stream);
            const float CrossSectionRatio = 0.1f; // 剖面图占总高度的比例
            try
            {
                InitializeCanvas(svgCanvas);
                // 绘制竖线，即在 X = x 处沿着 Y 轴方向绘制一条线，表示剖面
                var paint = CreateBasicPaint(SKColors.Black, (float)HoleSpacing / 20);
                foreach (var x in CrossSectionX)
                {
                    svgCanvas.DrawLine((float)x, (float)_minY, (float)x, (float)(_minY + (_maxY - _minY) * CrossSectionRatio), paint);
                    svgCanvas.DrawLine((float)x, (float)(_maxY - (_maxY - _minY) * CrossSectionRatio), (float)x, (float)_maxY, paint);
                    DrawCrossSectionLabel(svgCanvas, x, CrossSectionX);
                }
                // 绘制多边形
                DrawPolygons(svgCanvas, polygons);
                // 绘制各类炮孔
                DrawBlastHoles(svgCanvas, blastHoles, HasNoPermanentEdge);
                svgCanvas.Restore();
            }
            finally
            {
                svgCanvas.Dispose();
            }
        }

        /// <summary>
        /// 绘制起爆网络图
        /// </summary>
        /// <param name="timing">起爆时间字典</param>
        /// <param name="blastLines">爆破连接线</param>
        /// <param name="preSplitHoles">预裂孔位置</param>
        /// <param name="outputPath">输出文件路径</param>
        public void DrawTimingNetwork(Dictionary<Point3D, double> timing, List<List<Point3D>> blastLines, List<Point3D> preSplitHoles, string outputPath = "timing.svg")
        {
            if (timing == null) throw new ArgumentNullException(nameof(timing));
            if (blastLines == null) throw new ArgumentNullException(nameof(blastLines));
            if (preSplitHoles == null) throw new ArgumentNullException(nameof(preSplitHoles));

            const float StartPointCircleRatio = 2f;  // 起始点圆圈大小比例
            const float HoleCircleRatio = 5f;        // 炮孔圆圈大小比例
            using var stream = new SKFileWStream(outputPath);
            var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, CanvasWidth, CanvasHeight), stream);
            try
            {
                InitializeCanvas(svgCanvas);
                // 绘制连接线和箭头
                DrawBlastLines(svgCanvas, blastLines);
                // 绘制起始点
                using var startPointPaint = CreateBasicPaint(SKColors.Red, 0f, SKPaintStyle.Fill);
                startPointPaint.Style = SKPaintStyle.Fill;
                svgCanvas.DrawCircle(
                    (float)_startPoint.X,
                    (float)_startPoint.Y,
                    (float)HoleSpacing / StartPointCircleRatio,
                    startPointPaint);

                // 绘制孔位和时间标注
                DrawTimingHoles(svgCanvas, timing, HoleCircleRatio);
                // 单独绘制预裂孔
                var preSplitPaint = CreateBasicPaint(SKColors.Red, 0f, SKPaintStyle.Fill);
                foreach (var hole in preSplitHoles)
                {
                    svgCanvas.DrawCircle((float)hole.X, (float)hole.Y, (float)HoleSpacing / HoleCircleRatio, preSplitPaint);
                }
                svgCanvas.Restore();
            }
            finally
            {
                svgCanvas.Dispose();
            }
        }

        /// <summary>
        /// 绘制横断面图
        /// </summary>
        /// <param name="edges">边界点列表</param>
        /// <param name="lines">炮孔线条列表</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="hasNoPermanentEdge">是否无永久边坡</param>
        /// <exception cref="ArgumentNullException">当必要参数为 null 时抛出</exception>
        public void DrawCrossSection(List<Point3D> edges, List<List<Point3D>> lines, int count, string outputPath = "cross_section.svg", bool HasNoPermanentEdge = false)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentException("输出路径不能为空", nameof(outputPath));
            // 计算坐标范围
            var (minX, maxX, minY, maxY) = CalculateBoundingBox(edges, lines);
            var _scale = (float)Math.Min(CanvasWidth / (maxX - minX), CanvasHeight / (maxY - minY)) * 0.9f;
            var _offsetX = (float)(CanvasWidth - ((CanvasWidth - (maxX - minX) * _scale) / 2 - minX * _scale)); // 修改X轴偏移计算
            var _offsetY = (float)(CanvasHeight - ((CanvasHeight - (maxY - minY) * _scale) / 2 - minY * _scale)); // 修改Y轴偏移计算

            using var stream = new SKFileWStream(outputPath);
            var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, CanvasWidth, CanvasHeight), stream);
            svgCanvas.Clear(SKColors.White);
            try
            {
                svgCanvas.Save();
                svgCanvas.Translate(_offsetX, _offsetY); // 先平移
                svgCanvas.Scale(-_scale, -_scale);        // 在Y轴方向上使用负缩放来实现翻转

                // 绘制剖面图
                DrawCrossSectionEdges(svgCanvas, edges);
                DrawCrossSectionLines(svgCanvas, lines, HasNoPermanentEdge);

                // 绘制图标题，即剖面编号，如 “剖面 1”,要支持中文，需要设置字体
                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = (float)((maxY - minY) / 20),
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("PingFang SC", SKFontStyle.Normal)
                };
                svgCanvas.Save();
                svgCanvas.Scale(-1, -1);
                svgCanvas.DrawText($"剖面 {count}", -(float)(minX + (maxX - minX) / 2), -(float)maxY, paint);
                svgCanvas.Restore();
            }
            finally
            {
                svgCanvas.Dispose();
            }
        }

        // 生成动画帧
        public void DrawAnimationFrame(List<BasePolygon> polygons, Dictionary<Point3D, double> timing,
            double currentTime, double previousTime, string outputPath)
        {
            using var bitmap = new SKBitmap((int)CanvasWidth, (int)CanvasHeight);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            canvas.Save();
            canvas.Translate(_offsetX, _offsetY);
            canvas.Scale(_scale, -_scale);

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


        /// <summary>
        /// 初始化画布设置
        /// </summary>
        private void InitializeCanvas(SKCanvas canvas)
        {
            canvas.Clear(SKColors.White);
            canvas.Save();
            canvas.Translate(_offsetX, _offsetY);
            canvas.Scale(_scale, -_scale);
        }

        /// <summary>
        /// 创建基础画笔
        /// </summary>
        private static SKPaint CreateBasicPaint(SKColor color, float strokeWidth, SKPaintStyle style = SKPaintStyle.Stroke)
        {
            return new SKPaint
            {
                IsAntialias = true,
                Color = color,
                StrokeWidth = strokeWidth,
                Style = style
            };
        }


        private void DrawBoundaryPolygon(SKCanvas canvas, BasePolygon polygon)
        {
            var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black,
                StrokeWidth = (float)HoleSpacing / 20,
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
                canvas.DrawCircle((float)holePosition.X, (float)holePosition.Y, (float)HoleSpacing / 5, paint);
            }
        }

        /// <summary>
        /// 绘制边界多边形
        /// </summary>
        /// <param name="canvas">画布对象</param>
        /// <param name="polygons">多边形列表</param>
        /// <exception cref="ArgumentNullException">当canvas或polygons为null时抛出</exception>
        private void DrawPolygons(SKCanvas canvas, List<BasePolygon> polygons)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (polygons == null) throw new ArgumentNullException(nameof(polygons));
            const float MainBoundaryWidth = 20f;    // 主边界线宽度比例
            const float SecondaryBoundaryWidth = 40f;  // 次边界线宽度比例
            var paint = CreateBasicPaint(SKColors.Black, (float)HoleSpacing / MainBoundaryWidth);
            for (int polyIndex = 0; polyIndex < polygons.Count; polyIndex++)
            {
                // 绘制边界线
                var currentPolygon = polygons[polyIndex];
                var polygonPath = CreatePolygonPath(currentPolygon);
                // 次要边界使用灰色细线
                if (polyIndex > 0)
                {
                    paint.Color = SKColors.Gray;
                    paint.StrokeWidth = (float)HoleSpacing / SecondaryBoundaryWidth;
                }
                canvas.DrawPath(polygonPath, paint);
            }
        }

        /// <summary>
        /// 为多边形创建绘制路径
        /// </summary>
        /// <param name="polygon">多边形对象</param>
        /// <returns>绘制路径</returns>
        private static SKPath CreatePolygonPath(BasePolygon polygon)
        {
            var path = new SKPath();
            var firstPoint = true;
            foreach (var edge in polygon.Edges)
            {
                var point = new SKPoint((float)edge.Start.X, (float)edge.Start.Y);
                if (firstPoint)
                {
                    path.MoveTo(point);
                    firstPoint = false;
                }
                else
                {
                    path.LineTo(point);
                }
            }
            path.Close();
            return path;
        }

        /// <summary>
        /// 绘制炮孔
        /// </summary>
        private void DrawBlastHoles(SKCanvas canvas, List<HashSet<Point3D>> blastHoles, bool HasNoPermanentEdge)
        {
            var paint = CreateBasicPaint(SKColors.Black, (float)HoleSpacing / 20);
            for (int i = 0; i < blastHoles.Count; i++)
            {
                if (!HasNoPermanentEdge)
                {
                    paint.Color = (i == 0 ? SKColors.Red : (i == 1 ? SKColors.Purple : SKColors.Black));
                }
                DrawHoleSet(canvas, blastHoles[i], paint);
            }
        }

        /// <summary>
        /// 绘制爆破连接线
        /// </summary>
        private void DrawBlastLines(SKCanvas canvas, List<List<Point3D>> blastLines)
        {
            bool isFirstLine = true;
            foreach (var line in blastLines)
            {
                if (IsZeroPoint(line[0]))
                {
                    ProcessZeroPoint(canvas, line, ref isFirstLine);
                }

                DrawArrow(canvas,
                    new SKPoint((float)line[0].X, (float)line[0].Y),
                    new SKPoint((float)line[1].X, (float)line[1].Y));
            }
        }

        /// <summary>
        /// 检查是否为零点
        /// </summary>
        private static bool IsZeroPoint(Point3D point) =>
            point.X == 0 && point.Y == 0;

        /// <summary>
        /// 处理零点情况
        /// </summary>
        private void ProcessZeroPoint(SKCanvas canvas, List<Point3D> line, ref bool isFirstLine)
        {
            if (isFirstLine)
            {
                _startPoint = new Point3D(line[1].X, _minY, 0);
                line[0] = _startPoint;
                isFirstLine = false;
            }
            else
            {
                DrawArrow(canvas,
                    new SKPoint((float)_startPoint.X, (float)_startPoint.Y),
                    new SKPoint((float)line[1].X, (float)_minY));
                line[0] = new Point3D(line[1].X, _minY, 0);
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
                    (float)HoleSpacing / 5,
                    paint);
            }
        }

        // 绘制起爆线及箭头
        private void DrawArrow(SKCanvas canvas, SKPoint startPoint, SKPoint endPoint)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.DimGray,
                StrokeWidth = (float)HoleSpacing / 20,
                Style = SKPaintStyle.Stroke
            };
            var midPoint = new SKPoint(
                (startPoint.X + endPoint.X) / 2,
                (startPoint.Y + endPoint.Y) / 2);
            var direction = new SKPoint(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
            var length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
            var unitDirection = new SKPoint(direction.X / length, direction.Y / length);
            float arrowHeadLength = (float)HoleSpacing / 4;
            float arrowHeadWidth = (float)HoleSpacing / 8;
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

        /// <summary>
        /// 绘制爆破网络中的孔位和时间标注
        /// </summary>
        private void DrawTimingHoles(SKCanvas canvas, Dictionary<Point3D, double> timing, float HoleCircleRatio)
        {
            const float TextSizeRatio = 2f;         // 文字大小与间距的比例
            const float TextOffsetRatio = 2f;       // 文字偏移与行间距的比例
            var holePaint = CreateBasicPaint(SKColors.Black, (float)HoleSpacing / HoleCircleRatio, SKPaintStyle.Fill);
            var textPaint = new SKPaint { Color = SKColors.Black, TextSize = (float)HoleSpacing / TextSizeRatio, Typeface = SKTypeface.Default, TextAlign = SKTextAlign.Center, Style = SKPaintStyle.Fill };
            foreach (var (holePosition, blastTime) in timing)
            {
                // 绘制孔
                canvas.DrawCircle((float)holePosition.X, (float)holePosition.Y, (float)HoleSpacing / HoleCircleRatio, holePaint);
                // 绘制时间标注
                canvas.Save();
                canvas.Scale(1, -1);
                canvas.DrawText($"{blastTime}",
                    (float)holePosition.X,
                    -(float)holePosition.Y - (float)RowOffset / TextOffsetRatio,
                    textPaint);
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
            canvas.DrawText($"Time: {time} ms", 100, CanvasHeight - 50, paint);
        }

        private void DrawCrossSectionEdges(SKCanvas canvas, List<Point3D> newEdges)
        {
            var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black,
                StrokeWidth = (float)HoleSpacing / 20,
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

        private void DrawCrossSectionLabel(SKCanvas canvas, double x, double[] crossSectionX)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = (float)HoleSpacing * 0.75f,
                Typeface = SKTypeface.Default,
                TextAlign = SKTextAlign.Center,
                Style = SKPaintStyle.Fill
            };

            int index = Array.IndexOf(crossSectionX, x) + 1;
            canvas.Save();
            canvas.Scale(1, -1);

            // 在顶部和底部绘制标签
            float xPos = (float)(x - 1);
            canvas.DrawText($"{index}", xPos, -(float)_minY, paint);
            canvas.DrawText($"{index}", xPos, -(float)_maxY, paint);

            canvas.Restore();
        }

        private void DrawCrossSectionLines(SKCanvas canvas, List<List<Point3D>> newLines, bool HasNoPermanentEdge)
        {
            var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black,
                StrokeWidth = (float)HoleSpacing / 20,
                Style = SKPaintStyle.Stroke
            };
            // 前两条红色、后两条紫色，其他黑色
            int count = 0;
            foreach (var line in newLines)
            {
                if (count < 2 && !HasNoPermanentEdge)
                {
                    paint.Color = SKColors.Red;
                }
                else if (count < 4 && !HasNoPermanentEdge)
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

        /// <summary>
        /// 计算边界框
        /// </summary>
        private static (double MinX, double MaxX, double MinY, double MaxY) CalculateBoundingBox(
            List<Point3D> edges,
            List<List<Point3D>> lines)
        {
            double maxX = double.MinValue, maxY = double.MinValue;
            double minX = double.MaxValue, minY = double.MaxValue;

            // 处理边界点
            foreach (var point in edges)
            {
                maxX = Math.Max(maxX, point.Y);
                maxY = Math.Max(maxY, point.Z);
                minX = Math.Min(minX, point.Y);
                minY = Math.Min(minY, point.Z);
            }

            // 处理线条点
            foreach (var line in lines)
            {
                foreach (var point in line)
                {
                    maxX = Math.Max(maxX, point.Y);
                    maxY = Math.Max(maxY, point.Z);
                    minX = Math.Min(minX, point.Y);
                    minY = Math.Min(minY, point.Z);
                }
            }

            return (minX, maxX, minY, maxY);
        }
    }
}