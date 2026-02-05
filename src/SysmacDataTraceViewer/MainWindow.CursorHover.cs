using System.Windows;
using OxyPlot;
using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;

namespace SysmacDataTraceViewer;

public partial class MainWindow
{
    // Cursor movement, delta cursor, and hover segment interactions.
    private void UpdatePrimaryCursorAtPosition(Point plotPosition)
    {
        if (_traceData is null || TracePlot.Model is null || _traceData.SampleCount == 0)
        {
            return;
        }

        var model = TracePlot.Model;
        var xAxis = model.DefaultXAxis;
        if (xAxis is null)
        {
            return;
        }

        var screenPoint = new ScreenPoint(plotPosition.X, plotPosition.Y);
        var x = xAxis.InverseTransform(screenPoint.X);
        var sampleIndex = TraceNavigationService.FindClosestSample(_traceData.ElapsedSeconds, x);
        if (sampleIndex < 0)
        {
            return;
        }

        ApplyPrimaryCursorSample(sampleIndex);
    }

    private void UpdateDeltaCursorAtPosition(Point plotPosition)
    {
        if (_traceData is null || TracePlot.Model is null || _traceData.SampleCount == 0)
        {
            return;
        }

        var model = TracePlot.Model;
        var xAxis = model.DefaultXAxis;
        if (xAxis is null)
        {
            return;
        }

        var screenPoint = new ScreenPoint(plotPosition.X, plotPosition.Y);
        var x = xAxis.InverseTransform(screenPoint.X);
        var sampleIndex = TraceNavigationService.FindClosestSample(_traceData.ElapsedSeconds, x);
        if (sampleIndex < 0)
        {
            return;
        }

        if (sampleIndex == _lastDeltaSampleIndex)
        {
            return;
        }

        _lastDeltaSampleIndex = sampleIndex;
        _deltaCursorX = _traceData.ElapsedSeconds[sampleIndex];
        if (_deltaCursorAnnotation is not null)
        {
            _deltaCursorAnnotation.X = _deltaCursorX.Value;
            model.InvalidatePlot(false);
        }

        UpdateCursorDeltaText();
        UpdateCursorRangeBand();
    }

    private void UpdateCursorDeltaText()
    {
        if (!_cursorX.HasValue || !_deltaCursorX.HasValue)
        {
            _viewModel.CursorDeltaText = "-";
            return;
        }

        var delta = Math.Abs(_deltaCursorX.Value - _cursorX.Value);
        var span = TimeSpan.FromSeconds(delta);
        _viewModel.CursorDeltaText = span.ToString(@"hh\:mm\:ss\.fff");
    }

    private void UpdateCursorRangeBand()
    {
        if (_cursorRangeAnnotation is null || !_cursorX.HasValue || !_deltaCursorX.HasValue || _visibleBoolSignalIndexes.Count == 0)
        {
            return;
        }

        if (!_showCursorRangeBand)
        {
            _cursorRangeAnnotation.Fill = OxyColors.Transparent;
            TracePlot.Model?.InvalidatePlot(false);
            return;
        }

        var minX = Math.Min(_cursorX.Value, _deltaCursorX.Value);
        var maxX = Math.Max(_cursorX.Value, _deltaCursorX.Value);
        _cursorRangeAnnotation.MinimumX = minX;
        _cursorRangeAnnotation.MaximumX = Math.Max(maxX, minX + 1e-6);
        _cursorRangeAnnotation.MinimumY = -0.5;
        _cursorRangeAnnotation.MaximumY = Math.Max(_visibleBoolSignalIndexes.Count - 0.5, 0.5);
        _cursorRangeAnnotation.Fill = OxyColor.FromAColor(35, OxyColors.SteelBlue);
        TracePlot.Model?.InvalidatePlot(false);
    }

    private static double ClampToTraceRange(double value, TraceData traceData)
    {
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

    private void ApplyPrimaryCursorSample(int sampleIndex)
    {
        if (_traceData is null || TracePlot.Model is null || sampleIndex < 0 || sampleIndex >= _traceData.SampleCount)
        {
            return;
        }

        if (sampleIndex == _lastPrimarySampleIndex)
        {
            return;
        }

        _lastPrimarySampleIndex = sampleIndex;
        var elapsed = TimeSpan.FromSeconds(_traceData.ElapsedSeconds[sampleIndex]);
        _viewModel.CursorTimeText = elapsed.ToString(@"hh\:mm\:ss\.fff");
        _viewModel.CursorClockText = UiFormattingService.BuildOriginalTimeText(_traceData, sampleIndex);
        _viewModel.CursorSampleText = sampleIndex.ToString();
        UpdateValueRows(_traceData, sampleIndex);
        _cursorX = _traceData.ElapsedSeconds[sampleIndex];

        if (_cursorAnnotation is not null)
        {
            _cursorAnnotation.X = _cursorX.Value;
            TracePlot.Model.InvalidatePlot(false);
        }

        UpdateCursorDeltaText();
        UpdateCursorRangeBand();
    }

    private void UpdateHoverSegmentAtPosition(Point plotPosition)
    {
        if (_traceData is null || TracePlot.Model is null || _visibleBoolSignalIndexes.Count == 0)
        {
            ClearHoverSegment();
            return;
        }

        var model = TracePlot.Model;
        if (!model.PlotArea.Contains(plotPosition.X, plotPosition.Y))
        {
            ClearHoverSegment();
            return;
        }

        var xAxis = model.DefaultXAxis;
        var yAxis = model.DefaultYAxis;
        if (xAxis is null || yAxis is null)
        {
            ClearHoverSegment();
            return;
        }

        var screenPoint = new ScreenPoint(plotPosition.X, plotPosition.Y);
        var x = xAxis.InverseTransform(screenPoint.X);
        if (x < xAxis.ActualMinimum || x > xAxis.ActualMaximum)
        {
            ClearHoverSegment();
            return;
        }

        var y = yAxis.InverseTransform(screenPoint.Y);
        var laneIndex = (int)Math.Round(y);
        if (laneIndex < 0 || laneIndex >= _visibleBoolSignalIndexes.Count)
        {
            ClearHoverSegment();
            return;
        }

        if (Math.Abs(y - laneIndex) > 0.48)
        {
            ClearHoverSegment();
            return;
        }

        var sampleIndex = TraceNavigationService.FindClosestSample(_traceData.ElapsedSeconds, x);
        if (sampleIndex < 0 || sampleIndex >= _traceData.SampleCount)
        {
            ClearHoverSegment();
            return;
        }

        var signalIndex = _visibleBoolSignalIndexes[laneIndex];
        var signal = _traceData.BoolSignals[signalIndex];
        var current = signal.Values[sampleIndex];
        if (!current.HasValue)
        {
            ClearHoverSegment();
            return;
        }

        var start = sampleIndex;
        while (start > 0 && signal.Values[start - 1].HasValue && signal.Values[start - 1] == current)
        {
            start--;
        }

        var endExclusive = sampleIndex + 1;
        while (endExclusive < signal.Values.Length && signal.Values[endExclusive].HasValue && signal.Values[endExclusive] == current)
        {
            endExclusive++;
        }

        var startSec = _traceData.ElapsedSeconds[start];
        var endSec = endExclusive < _traceData.SampleCount ? _traceData.ElapsedSeconds[endExclusive] : _traceData.ElapsedSeconds[^1];
        var duration = Math.Max(0, endSec - startSec);

        var hoverState = current.Value;
        if (_lastHoverSignalIndex == signalIndex &&
            _lastHoverStartIndex == start &&
            _lastHoverEndExclusive == endExclusive &&
            _lastHoverState == hoverState)
        {
            return;
        }

        _lastHoverSignalIndex = signalIndex;
        _lastHoverStartIndex = start;
        _lastHoverEndExclusive = endExclusive;
        _lastHoverState = hoverState;
        _viewModel.HoverStateText = current.Value ? "ON" : "OFF";
        _viewModel.HoverDurationText = TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss\.fff");

        if (_hoverSegmentAnnotation is not null)
        {
            _hoverSegmentAnnotation.MinimumX = startSec;
            _hoverSegmentAnnotation.MaximumX = Math.Max(endSec, startSec + 1e-6);
            _hoverSegmentAnnotation.MinimumY = laneIndex - 0.45;
            _hoverSegmentAnnotation.MaximumY = laneIndex + 0.45;
            _hoverSegmentAnnotation.Fill = OxyColor.FromAColor(70, current.Value ? OxyColors.MediumSeaGreen : OxyColors.IndianRed);
            _hoverSegmentActive = true;
            TracePlot.Model?.InvalidatePlot(false);
        }
    }

    private void ClearHoverSegment()
    {
        _lastHoverSignalIndex = -1;
        _lastHoverStartIndex = -1;
        _lastHoverEndExclusive = -1;
        _lastHoverState = null;
        if (_viewModel.HoverStateText != "-")
        {
            _viewModel.HoverStateText = "-";
        }

        if (_viewModel.HoverDurationText != "-")
        {
            _viewModel.HoverDurationText = "-";
        }

        if (_hoverSegmentAnnotation is not null && _hoverSegmentActive)
        {
            _hoverSegmentAnnotation.Fill = OxyColors.Transparent;
            _hoverSegmentActive = false;
            TracePlot.Model?.InvalidatePlot(false);
        }
    }
}
