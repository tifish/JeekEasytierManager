using System;
using Avalonia;
using Avalonia.Controls;

namespace JeekEasyTierManager;

public class SizeToContentSafePanel : Panel
{
    public Control? FallbackWidthSource { get; set; }

    public double FallbackWidth { get; set; }

    public double FallbackWidthAdjustment { get; set; }

    public double FallbackHeight { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var fallbackWidth = GetFallbackWidth();
        var measureSize = new Size(
            GetChildMeasureSize(availableSize.Width, Bounds.Width, fallbackWidth),
            GetChildMeasureSize(availableSize.Height, Bounds.Height, FallbackHeight)
        );
        var desiredSize = new Size(
            GetDesiredSize(availableSize.Width, Bounds.Width, fallbackWidth),
            GetDesiredSize(availableSize.Height, Bounds.Height, FallbackHeight)
        );

        foreach (var child in Children)
            child.Measure(measureSize);

        return desiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
            child.Arrange(new Rect(finalSize));

        return finalSize;
    }

    private double GetFallbackWidth()
    {
        var sourceWidth = FallbackWidthSource?.DesiredSize.Width;
        if (sourceWidth is > 0)
            return Math.Max(sourceWidth.Value + FallbackWidthAdjustment, 0);

        sourceWidth = FallbackWidthSource?.Bounds.Width;
        if (sourceWidth is > 0)
            return Math.Max(sourceWidth.Value + FallbackWidthAdjustment, 0);

        return FallbackWidth;
    }

    private static double GetChildMeasureSize(double available, double current, double minimum)
    {
        if (!double.IsInfinity(available))
            return available;

        return GetStableSize(current, minimum);
    }

    private static double GetDesiredSize(double available, double current, double minimum)
    {
        var stableSize = GetStableSize(current, minimum);

        if (!double.IsInfinity(available))
            return Math.Min(available, stableSize);

        return stableSize;
    }

    private static double GetStableSize(double current, double minimum)
    {
        if (current > 0)
            return Math.Max(current, minimum);

        return Math.Max(minimum, 0);
    }
}
