using System.IO;
using System.Windows;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Wpf;
using SysmacDataTraceViewer.Services;

namespace SysmacDataTraceViewer;

public partial class MainWindow
{
    // File I/O actions: load/save CSV, comments, PNG export.
    private void LoadCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = CreateCsvOpenDialog();

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _traceData = CsvTraceParser.Parse(dialog.FileName);
            ApplyAutoColors(_traceData.BoolSignals.Count);
            InitializeBoolRows(_traceData);
            InitializeValueRows(_traceData);
            TryAutoLoadComments(dialog.FileName);
            DrawTrace(_traceData, GetVisibleBoolIndexes());
            _viewModel.CursorTimeText = "-";
            _viewModel.CursorClockText = "-";
            _viewModel.CursorSampleText = "-";
            _viewModel.CursorDeltaText = "-";
            _viewModel.HoverStateText = "-";
            _viewModel.HoverDurationText = "-";
            _viewModel.StatusText =
                UiFormattingService.BuildStatusText(_traceData, _traceData.BoolSignals.Count, _viewModel.ValueSignals.Count);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, CsvLoadErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SavePng_Click(object sender, RoutedEventArgs e)
    {
        ExportPng(visibleRangeOnly: true);
    }

    private void SavePngFullRange_Click(object sender, RoutedEventArgs e)
    {
        ExportPng(visibleRangeOnly: false);
    }

    private void ExportPng(bool visibleRangeOnly)
    {
        if (TracePlot.Model is null)
        {
            return;
        }

        var defaultName = visibleRangeOnly ? "trace_visible.png" : "trace_full.png";
        var dialog = CreatePngSaveDialog(defaultName);
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var model = TracePlot.Model;
            var yAxis = model.DefaultYAxis;
            var xAxis = model.DefaultXAxis;
            Func<double, string>? originalFormatter = null;
            var originalMargins = model.PlotMargins;
            var originalMin = xAxis?.ActualMinimum ?? 0;
            var originalMax = xAxis?.ActualMaximum ?? 0;

            if (yAxis is not null && _visibleBoolSignalIndexes.Count > 0)
            {
                originalFormatter = yAxis.LabelFormatter;
                yAxis.LabelFormatter = value =>
                {
                    var laneIndex = (int)Math.Round(value);
                    if (laneIndex < 0 || laneIndex >= _visibleBoolSignalIndexes.Count)
                    {
                        return string.Empty;
                    }

                    var signalIndex = _visibleBoolSignalIndexes[laneIndex];
                    return GetBoolLaneLabel(signalIndex);
                };
            }

            model.PlotMargins = new OxyThickness(
                Math.Max(originalMargins.Left, 280),
                originalMargins.Top,
                originalMargins.Right,
                originalMargins.Bottom);

            if (!visibleRangeOnly && xAxis is not null)
            {
                _suppressAxisEvent = true;
                try
                {
                    xAxis.Zoom(_dataMinX, _dataMaxX);
                }
                finally
                {
                    _suppressAxisEvent = false;
                }
            }

            try
            {
                using var stream = File.Create(dialog.FileName);
                var exporter = new PngExporter { Width = 1800, Height = 900 };
                exporter.Export(model, stream);
            }
            finally
            {
                if (!visibleRangeOnly && xAxis is not null)
                {
                    _suppressAxisEvent = true;
                    try
                    {
                        xAxis.Zoom(originalMin, originalMax);
                    }
                    finally
                    {
                        _suppressAxisEvent = false;
                    }
                }

                if (yAxis is not null)
                {
                    yAxis.LabelFormatter = originalFormatter;
                }

                model.PlotMargins = originalMargins;
                model.InvalidatePlot(false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, PngExportErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveComments_Click(object sender, RoutedEventArgs e)
    {
        if (_traceData is null)
        {
            MessageBox.Show(this, LoadTraceFirstMessage, SaveCommentsTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var suggestedName = $"{Path.GetFileNameWithoutExtension(_traceData.FileName)}_comments.csv";
        var dialog = CreateCsvSaveDialog(suggestedName);

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var boolStates = _viewModel.BoolSignals
            .Select((row, index) => new CommentSignalState(row.Name, row.CommentText, row.IsVisible, row.ColorHex, index))
            .ToList();
        var valueStates = _viewModel.ValueSignals
            .Select((row, index) => new CommentSignalState(row.Name, row.CommentText, row.IsVisible, string.Empty, index))
            .ToList();

        CommentCsvService.Save(dialog.FileName, boolStates, valueStates);
    }

    private void LoadComments_Click(object sender, RoutedEventArgs e)
    {
        if (_traceData is null)
        {
            MessageBox.Show(this, LoadTraceFirstMessage, LoadCommentsTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = CreateCsvOpenDialog();

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        ApplyCommentsFromFile(dialog.FileName, showErrorDialog: true);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutWindow
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static OpenFileDialog CreateCsvOpenDialog() =>
        new()
        {
            Filter = CsvFileFilter,
            CheckFileExists = true
        };

    private static SaveFileDialog CreateCsvSaveDialog(string fileName) =>
        new()
        {
            Filter = CsvFileFilter,
            FileName = fileName,
            OverwritePrompt = true
        };

    private static SaveFileDialog CreatePngSaveDialog(string fileName) =>
        new()
        {
            Filter = PngFileFilter,
            FileName = fileName,
            OverwritePrompt = true
        };

    private void TryAutoLoadComments(string traceCsvPath)
    {
        var directory = Path.GetDirectoryName(traceCsvPath);
        var baseName = Path.GetFileNameWithoutExtension(traceCsvPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName))
        {
            return;
        }

        var commentPath = Path.Combine(directory, $"{baseName}_comments.csv");
        if (!File.Exists(commentPath))
        {
            return;
        }

        ApplyCommentsFromFile(commentPath, showErrorDialog: false);
    }

    private void ApplyCommentsFromFile(string commentFilePath, bool showErrorDialog)
    {
        if (_traceData is null)
        {
            return;
        }

        try
        {
            var data = CommentCsvService.Load(commentFilePath);
            ApplyCommentData(data);

            UpdateDisplayLabels();
            RefreshVisibleValueRows();
            RedrawWithVisibility();
        }
        catch (Exception ex)
        {
            if (showErrorDialog)
            {
                MessageBox.Show(this, ex.Message, LoadCommentsErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ApplyCommentData(CommentCsvData data)
    {
        var boolMap = data.BoolSignals.ToDictionary(static s => s.Name, StringComparer.Ordinal);
        var valueMap = data.ValueSignals.ToDictionary(static s => s.Name, StringComparer.Ordinal);

        _suspendCommentUpdate = true;
        _suspendVisibilityUpdate = true;
        try
        {
            ApplyCommentOrdering(data);

            foreach (var row in _viewModel.BoolSignals)
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

            foreach (var row in _viewModel.ValueSignals)
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

    private void ApplyCommentOrdering(CommentCsvData data)
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
