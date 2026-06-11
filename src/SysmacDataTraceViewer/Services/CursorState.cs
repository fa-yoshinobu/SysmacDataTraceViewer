using SysmacDataTraceViewer.Models;

namespace SysmacDataTraceViewer.Services;

internal sealed class CursorState
{
    public double? PrimaryX { get; private set; }
    public double? DeltaX { get; private set; }
    public int LastPrimarySampleIndex { get; private set; } = -1;
    public int LastDeltaSampleIndex { get; private set; } = -1;
    public bool ShowRangeBand { get; set; }

    public void InitializeForTrace(TraceData traceData)
    {
        ArgumentNullException.ThrowIfNull(traceData);

        var firstX = traceData.ElapsedSeconds[0];
        PrimaryX = ClampToTraceRange(PrimaryX ?? firstX, traceData);
        DeltaX = ClampToTraceRange(DeltaX ?? firstX, traceData);
        LastPrimarySampleIndex = TraceNavigationService.FindClosestSample(traceData.ElapsedSeconds, PrimaryX.Value);
        LastDeltaSampleIndex = TraceNavigationService.FindClosestSample(traceData.ElapsedSeconds, DeltaX.Value);
    }

    public bool TryMovePrimaryToSample(TraceData traceData, int sampleIndex)
    {
        ArgumentNullException.ThrowIfNull(traceData);

        if (sampleIndex < 0 || sampleIndex >= traceData.SampleCount || sampleIndex == LastPrimarySampleIndex)
        {
            return false;
        }

        LastPrimarySampleIndex = sampleIndex;
        PrimaryX = traceData.ElapsedSeconds[sampleIndex];
        return true;
    }

    public bool TryMoveDeltaToSample(TraceData traceData, int sampleIndex)
    {
        ArgumentNullException.ThrowIfNull(traceData);

        if (sampleIndex < 0 || sampleIndex >= traceData.SampleCount || sampleIndex == LastDeltaSampleIndex)
        {
            return false;
        }

        LastDeltaSampleIndex = sampleIndex;
        DeltaX = traceData.ElapsedSeconds[sampleIndex];
        return true;
    }

    public bool TrySwap(TraceData traceData)
    {
        ArgumentNullException.ThrowIfNull(traceData);

        if (!PrimaryX.HasValue || !DeltaX.HasValue)
        {
            return false;
        }

        (PrimaryX, DeltaX) = (DeltaX.Value, PrimaryX.Value);
        LastPrimarySampleIndex = TraceNavigationService.FindClosestSample(traceData.ElapsedSeconds, PrimaryX.Value);
        LastDeltaSampleIndex = TraceNavigationService.FindClosestSample(traceData.ElapsedSeconds, DeltaX.Value);
        return true;
    }

    public static double ClampToTraceRange(double value, TraceData traceData)
    {
        ArgumentNullException.ThrowIfNull(traceData);

        var min = traceData.ElapsedSeconds[0];
        var max = traceData.ElapsedSeconds[^1];
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
