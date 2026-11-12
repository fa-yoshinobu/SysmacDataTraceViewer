using System.ComponentModel;
using OxyPlot;
using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;

namespace SysmacDataTraceViewer.ViewModels;

internal sealed partial class MainViewModel
{
    private const string NoChangeSuffix = " [No Change]";
    private bool _suspendVisibilityUpdate;
    private bool _suspendCommentUpdate;
    private readonly Dictionary<int, OxyColor> _boolColors = new();
    public bool IsBoolPanelVisible { get; private set; } = true;
    public bool IsBottomPanelVisible { get; private set; } = true;
    public void ToggleBoolPanel()
    {
        IsBoolPanelVisible = !IsBoolPanelVisible;
        BoolPanelToggleText = IsBoolPanelVisible ? "Hide Right Panel" : "Show Right Panel";
    }
    public void ToggleBottomPanel()
    {
        IsBottomPanelVisible = !IsBottomPanelVisible;
        BottomPanelToggleText = IsBottomPanelVisible ? "Hide Variable Settings" : "Show Variable Settings";
    }
    public TraceData? TraceData { get; private set; }
    public bool ShowCommentLabels { get; set; }
    public event Action? RedrawRequested;

    private void RequestRedraw() => RedrawRequested?.Invoke();

    public void InitializeTrace(TraceData trace)
    {
        TraceData = trace;
        HasReadIssues = trace.Diagnostics.HasIssues;
        ApplyAutoColors(trace.BoolSignals.Count);
        InitializeBoolRows(trace);
        InitializeValueRows(trace);
        ResetCursorText();
    }

    public void ResetCursorText() => CursorTimeText = CursorClockText = CursorSampleText = CursorDeltaText = HoverStateText = HoverDurationText = "-";

    public CommentCsvData CaptureComments()
    {
        var data = new CommentCsvData();
        data.BoolSignals.AddRange(BoolSignals.Select((row, index) => new CommentSignalState(row.Name, row.CommentText, row.IsVisible, row.ColorHex, index)));
        data.ValueSignals.AddRange(ValueSignals.Select((row, index) => new CommentSignalState(row.Name, row.CommentText, row.IsVisible, string.Empty, index)));
        return data;
    }

    public void ReorderBoolRows(List<string> orderedNames)
    {
        var ordered = SignalOrderingService.BuildOrderedRows(BoolSignals, orderedNames, static row => row.Name);
        if (ordered is null)
        {
            return;
        }

        BoolSignals.Clear();
        foreach (var row in ordered)
        {
            BoolSignals.Add(row);
        }
    }

    public void ReorderValueRows(List<string> orderedNames)
    {
        var ordered = SignalOrderingService.BuildOrderedRows(ValueSignals, orderedNames, static row => row.Name);
        if (ordered is null)
        {
            return;
        }

        ValueSignals.Clear();
        foreach (var row in ordered)
        {
            ValueSignals.Add(row);
        }
    }
    public void InitializeValueRows(TraceData traceData)
    {
        foreach (var row in ValueSignals)
        {
            row.PropertyChanged -= ValueSignalRow_PropertyChanged;
        }

        ValueSignals.Clear();
        foreach (var signal in traceData.ValueSignals)
        {
            var row = new ValueSignalRow
            {
                Name = signal.Name,
                SignalIndex = ValueSignals.Count,
                CommentText = UiFormattingService.BuildDefaultComment(signal.Name),
                DisplayLabel = signal.Name,
                IsUnchanged = !signal.HasChange,
                IsVisible = true,
                ValueText = "-"
            };
            row.PropertyChanged += ValueSignalRow_PropertyChanged;
            ValueSignals.Add(row);
        }

        UpdateDisplayLabels();
        RefreshVisibleValueRows();
    }

    public void UpdateValueRows(TraceData traceData, int sampleIndex)
    {
        foreach (var row in ValueSignals)
        {
            if (row.SignalIndex < 0 || row.SignalIndex >= traceData.ValueSignals.Count)
            {
                row.ValueText = "-";
                continue;
            }

            var value = traceData.ValueSignals[row.SignalIndex].Values[sampleIndex];
            row.ValueText = string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }

    public void InitializeBoolRows(TraceData traceData)
    {
        foreach (var row in BoolSignals)
        {
            row.PropertyChanged -= BoolSignalRow_PropertyChanged;
        }

        BoolSignals.Clear();
        for (var i = 0; i < traceData.BoolSignals.Count; i++)
        {
            var colorHex = SignalColorService.ToHex(GetSignalColor(i));
            var row = new BoolSignalRow
            {
                Index = i,
                Name = traceData.BoolSignals[i].Name,
                CommentText = UiFormattingService.BuildDefaultComment(traceData.BoolSignals[i].Name),
                DisplayLabel = traceData.BoolSignals[i].Name,
                IsUnchanged = !traceData.BoolSignals[i].HasChange,
                IsVisible = true,
                ColorHex = colorHex
            };
            row.PropertyChanged += BoolSignalRow_PropertyChanged;
            BoolSignals.Add(row);
        }

        UpdateDisplayLabels();
    }

    private void BoolSignalRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (TraceData is null || sender is not BoolSignalRow row)
        {
            return;
        }

        if (e.PropertyName == nameof(BoolSignalRow.IsVisible))
        {
            if (_suspendVisibilityUpdate)
            {
                return;
            }

            RequestRedraw();
            return;
        }

        if (e.PropertyName == nameof(BoolSignalRow.ColorHex))
        {
            if (SignalColorService.TryParseHexColor(row.ColorHex, out var color))
            {
                _boolColors[row.Index] = color;
                // Bulk-apply paths (comments/profile load) set suspension flags, so skip per-row redraw.
                if (_suspendVisibilityUpdate || _suspendCommentUpdate)
                {
                    return;
                }

                RequestRedraw();
            }
            return;
        }

        if (e.PropertyName == nameof(BoolSignalRow.CommentText) && ShowCommentLabels)
        {
            if (_suspendCommentUpdate)
            {
                return;
            }

            UpdateDisplayLabels();
            RequestRedraw();
        }
    }

    private void ValueSignalRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ValueSignalRow)
        {
            return;
        }

        if (e.PropertyName == nameof(ValueSignalRow.IsVisible))
        {
            RefreshVisibleValueRows();
            UpdateStatusForVisibility();
            return;
        }

        if (e.PropertyName == nameof(ValueSignalRow.CommentText))
        {
            if (_suspendCommentUpdate)
            {
                return;
            }

            UpdateDisplayLabels();
            RefreshVisibleValueRows();
            UpdateStatusForVisibility();
        }
    }

    public void SetAllBoolVisibility(bool isVisible)
    {
        if (TraceData is null)
        {
            return;
        }

        _suspendVisibilityUpdate = true;
        try
        {
            foreach (var row in BoolSignals)
            {
                row.IsVisible = isVisible;
            }
        }
        finally
        {
            _suspendVisibilityUpdate = false;
        }

        RequestRedraw();
    }

    public void HideNoChangeBool()
    {
        if (TraceData is null)
        {
            return;
        }

        _suspendVisibilityUpdate = true;
        try
        {
            foreach (var row in BoolSignals)
            {
                if (row.IsUnchanged)
                {
                    row.IsVisible = false;
                }
            }
        }
        finally
        {
            _suspendVisibilityUpdate = false;
        }

        RequestRedraw();
    }

    public void AutoBoolColors()
    {
        if (TraceData is null)
        {
            return;
        }

        var visibleRows = BoolSignals.Where(static row => row.IsVisible).ToList();
        for (var i = 0; i < visibleRows.Count; i++)
        {
            var row = visibleRows[i];
            var color = SignalColorService.GetDefaultPaletteColor(i);
            _boolColors[row.Index] = color;
            row.ColorHex = SignalColorService.ToHex(color);
        }

        RequestRedraw();
    }

    public void SetAllValueVisibility(bool isVisible)
    {
        foreach (var row in ValueSignals)
        {
            row.IsVisible = isVisible;
        }

        RefreshVisibleValueRows();
        UpdateStatusForVisibility();
    }


    public void HideNoChangeValue()
    {
        foreach (var row in ValueSignals)
        {
            if (row.IsUnchanged)
            {
                row.IsVisible = false;
            }
        }

        RefreshVisibleValueRows();
        UpdateStatusForVisibility();
    }

    public OxyColor GetSignalColor(int signalIndex)
    {
        if (_boolColors.TryGetValue(signalIndex, out var color))
        {
            return color;
        }

        return OxyColors.DodgerBlue;
    }

    public void ApplyAutoColors(int signalCount)
    {
        _boolColors.Clear();
        if (signalCount <= 0)
        {
            return;
        }

        for (var i = 0; i < signalCount; i++)
        {
            _boolColors[i] = SignalColorService.GetDefaultPaletteColor(i);
        }
    }

    public void UpdateBoolRowColor(int signalIndex)
    {
        var row = BoolSignals.FirstOrDefault(r => r.Index == signalIndex);
        if (row is null)
        {
            return;
        }

        row.ColorHex = SignalColorService.ToHex(GetSignalColor(signalIndex));
    }

    public void UpdateDisplayLabels()
    {
        foreach (var row in BoolSignals)
        {
            // BOOL labels stay fixed to variable names to keep signal-row identity stable.
            row.DisplayLabel = UiFormattingService.FormatVariableName(row.Name, ShowTypeSuffix);
        }

        foreach (var row in ValueSignals)
        {
            // Value labels follow BOOL Label Mode (Variable Name / Comment).
            var baseText = ShowCommentLabels && !string.IsNullOrWhiteSpace(row.CommentText)
                ? row.CommentText
                : UiFormattingService.FormatVariableName(row.Name, ShowTypeSuffix);
            row.DisplayLabel = baseText;
            row.SettingsDisplayName = UiFormattingService.FormatVariableName(row.Name, ShowTypeSuffix);
        }

        RefreshVisibleValueRows();
    }

    public void RefreshVisibleValueRows()
    {
        VisibleValueSignals.Clear();
        foreach (var row in ValueSignals.Where(static r => r.IsVisible))
        {
            VisibleValueSignals.Add(row);
        }
    }

    public void UpdateStatusForVisibility()
    {
        if (TraceData is null)
        {
            return;
        }

        StatusText = UiFormattingService.BuildStatusText(TraceData, GetVisibleBoolIndexes().Count, VisibleValueSignals.Count);
    }

    public string GetBoolLaneLabel(int signalIndex)
    {
        var row = BoolSignals.FirstOrDefault(r => r.Index == signalIndex);
        if (row is null)
        {
            return TraceData?.BoolSignals[signalIndex].Name ?? string.Empty;
        }

        var baseText = ShowCommentLabels && !string.IsNullOrWhiteSpace(row.CommentText)
            ? row.CommentText
            : UiFormattingService.FormatVariableName(row.Name, ShowTypeSuffix);
        return row.IsUnchanged ? $"{baseText}{NoChangeSuffix}" : baseText;
    }

    public List<int> GetVisibleBoolIndexes() =>
        [.. BoolSignals.Where(static s => s.IsVisible).Select(static s => s.Index)];

    public void ApplyCommentData(CommentCsvData data)
    {
        var boolMap = data.BoolSignals.ToDictionary(static s => s.Name, StringComparer.Ordinal);
        var valueMap = data.ValueSignals.ToDictionary(static s => s.Name, StringComparer.Ordinal);

        _suspendCommentUpdate = true;
        _suspendVisibilityUpdate = true;
        try
        {
            ApplyCommentOrdering(data);

            foreach (var row in BoolSignals)
            {
                if (!boolMap.TryGetValue(row.Name, out var state))
                {
                    continue;
                }

                row.CommentText = state.Comment;
                row.IsVisible = state.IsVisible;
                row.ColorHex = state.ColorHex;
                if (SignalColorService.TryParseHexColor(state.ColorHex, out var color))
                {
                    _boolColors[row.Index] = color;
                }
            }

            foreach (var row in ValueSignals)
            {
                if (!valueMap.TryGetValue(row.Name, out var state))
                {
                    continue;
                }

                row.CommentText = state.Comment;
                row.IsVisible = state.IsVisible;
            }
        }
        finally
        {
            _suspendCommentUpdate = false;
            _suspendVisibilityUpdate = false;
        }
    }

    public void ApplyCommentOrdering(CommentCsvData data)
    {
        if (data.BoolSignals.Count > 0)
        {
            var boolNames = data.BoolSignals
                .OrderBy(static s => s.Order)
                .Select(static s => s.Name)
                .ToList();
            ReorderBoolRows(boolNames);
        }

        if (data.ValueSignals.Count > 0)
        {
            var valueNames = data.ValueSignals
                .OrderBy(static s => s.Order)
                .Select(static s => s.Name)
                .ToList();
            ReorderValueRows(valueNames);
        }
    }
}
