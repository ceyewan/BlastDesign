using SkiaSharp;

public class HoleChargeDrawing
{
    // 总长度(cm)
    public int TotalLength { get; set; } = 800;
    // 孔底加强段长度(cm)
    public int BottomChargeLength { get; set; } = 60;
    // 炮孔堵塞段长度(cm)
    public int StemmingLength { get; set; } = 100;
    // 装药段间隔长度(cm)
    public int IntervalLength { get; set; } = 25;
    // 装药块长度(cm)
    public int ChargeBlockLength { get; set; } = 15;
    // 起始X坐标
    private int StartX { get; set; } = 50;
    // 基准线Y坐标
    private int BaselineY { get; set; } = 100;

    private SKCanvas? canvas;

    #region Paint Definitions
    private static readonly SKPaint LinePaint = new()
    {
        Color = SKColors.Black,
        StrokeWidth = 2,
        IsStroke = true,
        IsAntialias = true
    };

    private static readonly SKPaint BambooLinePaint = new()
    {
        Color = SKColors.Green,
        StrokeWidth = 1,
        IsStroke = true,
        IsAntialias = true
    };

    private static readonly SKPaint DetonatingCordPaint = new()
    {
        Color = SKColors.Red,
        StrokeWidth = 1,
        IsStroke = true,
        IsAntialias = true
    };

    private static readonly SKPaint ChargeBlockPaint = new()
    {
        Color = SKColors.Red,
        IsStroke = false,
        IsAntialias = true
    };

    private static readonly SKPaint StemmingBlockPaint = new()
    {
        Color = SKColors.Gray,
        IsStroke = false,
        IsAntialias = true
    };

    private static readonly SKPaint StrokePaint = new()
    {
        Color = SKColors.Black,
        StrokeWidth = 1,
        IsStroke = true,
        IsAntialias = true
    };

    private SKPaint TextPaint = new SKPaint();

    private void TextInit()
    {
        // 设置字体，修改为系统中存在的中文字体
        var typeface = SKTypeface.FromFamilyName("PingFang SC", SKFontStyle.Normal);
        // var typeface = SKTypeface.FromFile("/路径/到/你的/字体文件.ttf"); // 从文件加载字体
        TextPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 8,
            IsAntialias = true,
            TextAlign = SKTextAlign.Left,
            Typeface = typeface,
            SubpixelText = true
        };
    }
    #endregion

    public HoleChargeDrawing(double totalLength, double bottomChargeLength, double stemmingLength, double intervalLength, double chargeBlockLength)
    {
        TextInit();
        TotalLength = (int)totalLength;
        BottomChargeLength = (int)bottomChargeLength;
        StemmingLength = (int)stemmingLength;
        IntervalLength = (int)intervalLength;
        ChargeBlockLength = (int)chargeBlockLength;
        // 总长度-孔底加强段长度-炮孔堵塞段长度是装药段间隔长度+装药块长度的整数倍，否则会有装药块无法绘制的问题
        if ((TotalLength - BottomChargeLength - StemmingLength) % (IntervalLength + ChargeBlockLength) != 0)
        {
            throw new ArgumentException("Invalid parameters: TotalLength, BottomChargeLength, StemmingLength, IntervalLength, ChargeBlockLength");
        }
    }

    public void DrawAndSave(string filePath)
    {
        using var stream = new SKFileWStream(filePath);
        var rect = SKRect.Create(1000, 300);
        canvas = SKSvgCanvas.Create(rect, stream);
        canvas.Clear(SKColors.White);

        DrawBaseDiagram();
        DrawDimensionLine(StartX, StartX + TotalLength, BaselineY + 35, $"{TotalLength}cm");
        DrawRedRectangles(BottomChargeLength, StemmingLength, IntervalLength, ChargeBlockLength);
        DrawTextWithLine(StartX + TotalLength + 25, BaselineY - 7, "导爆索", true);
        DrawTextWithLine(StartX + TotalLength + 30, BaselineY - 4, "竹片", false);
        canvas.Dispose();
    }

    public void DrawRedRectangles(float startLength, float endLength, float intervalLength, float rectLength)
    {
        if (canvas == null)
        {
            return;
        }
        float currentX = StartX;
        // 绘制孔底加强矩形
        var startRect = new SKRect(currentX, BaselineY - 15, currentX + startLength, BaselineY);
        canvas.DrawRect(startRect, ChargeBlockPaint);
        canvas.DrawRect(startRect, StrokePaint);
        DrawDimensionLine(currentX, currentX + startLength, BaselineY + 20, $"{startLength}cm 孔底加强");

        // 绘制药包矩形
        currentX += startLength + intervalLength;
        int rectCount = (int)((TotalLength - startLength - endLength) / (intervalLength + rectLength));
        for (int i = 0; i < rectCount; i++)
        {
            var rect = new SKRect(currentX, BaselineY - 10, currentX + rectLength, BaselineY);
            canvas.DrawRect(rect, ChargeBlockPaint);
            canvas.DrawRect(rect, StrokePaint);
            DrawDimensionLine(currentX - intervalLength, currentX, BaselineY + 20, intervalLength.ToString());
            DrawDimensionLine(currentX, currentX + rectLength, BaselineY + 20, rectLength.ToString());
            currentX += rectLength + intervalLength;
        }

        // 绘制炮孔堵塞矩形
        currentX -= intervalLength;
        var endRect = new SKRect(currentX, BaselineY - 15, currentX + endLength, BaselineY);
        canvas.DrawRect(endRect, StemmingBlockPaint);
        canvas.DrawRect(endRect, StrokePaint);
        DrawDimensionLine(currentX, currentX + endLength, BaselineY + 20, $"{endLength}cm 炮孔堵塞");
    }

    public void DrawDimensionLine(float startX, float endX, float y, string dimensionText)
    {
        if (canvas == null)
        {
            return;
        }
        // 绘制尺寸线
        canvas.DrawLine(new SKPoint(startX, y), new SKPoint(endX, y), StrokePaint);

        // 绘制起始箭头和竖线
        DrawArrow(startX, y, true);
        canvas.DrawLine(new SKPoint(startX, y - 3), new SKPoint(startX, y + 3), StrokePaint);

        // 绘制结束箭头和竖线
        DrawArrow(endX, y, false);
        canvas.DrawLine(new SKPoint(endX, y - 3), new SKPoint(endX, y + 3), StrokePaint);

        // 计算文字位置，使其居中显示
        float textWidth = TextPaint.MeasureText(dimensionText);
        float centerX = startX + (endX - startX - textWidth) / 2;
        // 绘制尺寸文字，稍微调整Y坐标以避免与线重叠
        canvas.DrawText(dimensionText, centerX, y - 5, TextPaint);
    }

    private void DrawArrow(float x, float y, bool isStart)
    {
        if (canvas == null)
        {
            return;
        }
        var path = new SKPath();
        if (isStart)
        {
            path.MoveTo(x, y);
            path.LineTo(x + 3, y - 1.5f);
            path.LineTo(x + 3, y + 1.5f);
        }
        else
        {
            path.MoveTo(x, y);
            path.LineTo(x - 3, y - 1.5f);
            path.LineTo(x - 3, y + 1.5f);
        }
        path.Close();
        canvas.DrawPath(path, StrokePaint);
    }

    public void DrawBaseDiagram()
    {
        if (canvas == null)
        {
            return;
        }
        // 绘制底部基线
        canvas.DrawLine(
            new SKPoint(StartX, BaselineY),
            new SKPoint(StartX + TotalLength, BaselineY),
            LinePaint);

        // 绘制导爆索和竹片
        canvas.DrawLine(
            new SKPoint(StartX, BaselineY - 4),
            new SKPoint(StartX + TotalLength + 50, BaselineY - 4),
            DetonatingCordPaint);
        canvas.DrawLine(
            new SKPoint(StartX, BaselineY - 7),
            new SKPoint(StartX + TotalLength + 50, BaselineY - 7),
            BambooLinePaint);

        // 绘制上方基线
        canvas.DrawLine(
            new SKPoint(StartX, BaselineY - 15),
            new SKPoint(StartX + TotalLength, BaselineY - 15),
            StrokePaint);
    }

    public void DrawTextWithLine(float x, float y, string text, bool slantUpwards)
    {
        if (canvas == null)
        {
            return;
        }
        float slantEndX = x + 30;
        float slantEndY = slantUpwards ? y - 20 : y + 20;
        float horizontalEndX = slantEndX + 20;
        // 绘制斜线
        canvas.DrawLine(
            new SKPoint(x, y),
            new SKPoint(slantEndX, slantEndY),
            StrokePaint);
        // 绘制水平线
        canvas.DrawLine(
            new SKPoint(slantEndX, slantEndY),
            new SKPoint(horizontalEndX, slantEndY),
            StrokePaint);
        // 绘制文字
        canvas.DrawText(text, slantEndX, slantEndY - 5, TextPaint);
    }
}