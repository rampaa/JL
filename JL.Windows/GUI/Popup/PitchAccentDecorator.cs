using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using TextBox = System.Windows.Controls.TextBox;

namespace JL.Windows.GUI.Popup;

internal sealed class PitchAccentDecorator : Decorator
{
    private readonly string[] _readings;
    private readonly string[] _splitReadingsWithRInfo;
    private readonly byte[] _pitchPositions;
    private readonly Pen _pen;

    public PitchAccentDecorator(FrameworkElement child, string[] reading, string[] splitReadingsWithRInfo, byte[] pitchPositions, Pen pen)
    {
        Child = child;
        VerticalAlignment = child.VerticalAlignment;
        HorizontalAlignment = child.HorizontalAlignment;
        Margin = child.Margin;
        UseLayoutRounding = true;
        _readings = reading;
        _splitReadingsWithRInfo = splitReadingsWithRInfo;
        _pitchPositions = pitchPositions;
        _pen = pen;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        FrameworkElement childElement = (FrameworkElement)Child;
        double fontSize = childElement switch
        {
            TextBlock textBlock => textBlock.FontSize,
            TextBox textBox => textBox.FontSize,
            _ => (double)childElement.GetValue(TextElement.FontSizeProperty)
        };

        double horizontalOffsetForReading = childElement.Margin.Left;
        double uniformCharHeight = WindowsUtils.GetMaxHeight(WindowsUtils.PopupFontTypeFace, fontSize);
        for (int i = 0; i < _readings.Length; i++)
        {
            if (i > 0)
            {
                horizontalOffsetForReading += WindowsUtils.MeasureTextSize($"{_splitReadingsWithRInfo[i - 1]}、", fontSize).Width;
            }

            byte pitchPosition = _pitchPositions[i];
            if (pitchPosition is byte.MaxValue)
            {
                continue;
            }

            StreamGeometry lineGeometry = BuildPitchAccentGeometry(_readings[i], pitchPosition, horizontalOffsetForReading, uniformCharHeight, fontSize);
            lineGeometry.Freeze();
            drawingContext.DrawGeometry(null, _pen, lineGeometry);
        }
    }

    private static StreamGeometry BuildPitchAccentGeometry(string expression, byte pitchPosition, double horizontalOffsetForReading, double uniformCharHeight, double fontSize)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext streamGeometryContext = geometry.Open();

        bool lowPitch = false;
        double horizontalOffsetForChar = horizontalOffsetForReading;
        ReadOnlySpan<string> combinedFormList = JapaneseUtils.CreateCombinedForm(expression);

        for (int i = 0; i < combinedFormList.Length; i++)
        {
            double charWidth = WindowsUtils.MeasureTextSize(combinedFormList[i], fontSize).Width;

            if (pitchPosition - 1 == i)
            {
                Point point = new(horizontalOffsetForChar, 0);
                if (i is 0)
                {
                    streamGeometryContext.BeginFigure(point, false, false);
                }
                else
                {
                    streamGeometryContext.LineTo(point, true, false);
                }

                streamGeometryContext.LineTo(new Point(horizontalOffsetForChar + charWidth, 0), true, false);
                streamGeometryContext.LineTo(new Point(horizontalOffsetForChar + charWidth, uniformCharHeight), true, false);
                lowPitch = true;
            }
            else if (i is 0)
            {
                streamGeometryContext.BeginFigure(new Point(horizontalOffsetForChar, uniformCharHeight), false, false);
                streamGeometryContext.LineTo(new Point(horizontalOffsetForChar + charWidth, uniformCharHeight), true, false);
                streamGeometryContext.LineTo(new Point(horizontalOffsetForChar + charWidth, 0), true, false);
            }
            else
            {
                double yPosition = lowPitch ? uniformCharHeight : 0;
                streamGeometryContext.LineTo(new Point(horizontalOffsetForChar, yPosition), true, false);
                streamGeometryContext.LineTo(new Point(horizontalOffsetForChar + charWidth, yPosition), true, false);
            }

            horizontalOffsetForChar += charWidth;
        }

        return geometry;
    }
}
