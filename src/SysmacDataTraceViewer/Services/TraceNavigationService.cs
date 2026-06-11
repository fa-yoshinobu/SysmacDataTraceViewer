using SysmacDataTraceViewer.Models;

namespace SysmacDataTraceViewer.Services;

internal static class TraceNavigationService
{
    public static List<int> BuildChangePointSampleIndexes(TraceData traceData, IReadOnlyList<int> signalIndexes)
    {
        var points = new HashSet<int>();
        foreach (var signalIndex in signalIndexes)
        {
            var values = traceData.BoolSignals[signalIndex].Values;
            bool? lastValue = null;
            var hasLast = false;

            for (var i = 0; i < values.Count; i++)
            {
                var current = values[i];
                if (!current.HasValue)
                {
                    continue;
                }

                if (!hasLast)
                {
                    lastValue = current.Value;
                    hasLast = true;
                    continue;
                }

                if (lastValue != current.Value)
                {
                    points.Add(i);
                }

                lastValue = current.Value;
            }
        }

        return points.OrderBy(static i => i).ToList();
    }

    public static int FindClosestSample(IReadOnlyList<double> values, double x)
    {
        if (values.Count == 0)
        {
            return -1;
        }

        var lo = 0;
        var hi = values.Count - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) / 2);
            var cmp = values[mid].CompareTo(x);
            if (cmp == 0)
            {
                return mid;
            }

            if (cmp < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        var next = lo;
        if (next <= 0)
        {
            return 0;
        }

        if (next >= values.Count)
        {
            return values.Count - 1;
        }

        var prev = next - 1;
        return Math.Abs(values[prev] - x) <= Math.Abs(values[next] - x) ? prev : next;
    }

    public static int? FindPreviousChangePoint(IReadOnlyList<int> changePointSampleIndexes, int currentSampleIndex)
    {
        for (var i = changePointSampleIndexes.Count - 1; i >= 0; i--)
        {
            if (changePointSampleIndexes[i] < currentSampleIndex)
            {
                return changePointSampleIndexes[i];
            }
        }

        return null;
    }

    public static int? FindNextChangePoint(IReadOnlyList<int> changePointSampleIndexes, int currentSampleIndex)
    {
        for (var i = 0; i < changePointSampleIndexes.Count; i++)
        {
            if (changePointSampleIndexes[i] > currentSampleIndex)
            {
                return changePointSampleIndexes[i];
            }
        }

        return null;
    }
}
