using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;
using SysmacDataTraceViewer.ViewModels;
using Input = System.Windows.Input;

namespace SysmacDataTraceViewer;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by WPF XAML startup and window navigation.")]
internal sealed partial class MainWindow : Window
{
    // Core plotting/state orchestration for the main window.
    private const double PlotLeftMargin = 16;
    private const double PlotTopMargin = 8;
    private const double PlotRightMargin = 12;
    private const double PlotBottomMargin = 50;
    private const string CsvFileFilter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
    private const string PngFileFilter = "PNG image (*.png)|*.png";
    private const string NoChangeSuffix = " [No Change]";
    private const string HideVariableSettingsText = "Hide Variable Settings";
    private const string ShowVariableSettingsText = "Show Variable Settings";
    private const string CsvLoadErrorTitle = "CSV Load Error";
    private const string PngExportErrorTitle = "PNG Export Error";
    private const string SaveCommentsTitle = "Save Comments";
    private const string LoadCommentsTitle = "Load Comments";
    private const string LoadCommentsErrorTitle = "Load Comments Error";
    private const string LoadTraceFirstMessage = "Load a trace CSV first.";

    private readonly MainViewModel _viewModel = new();
    private TraceData? _traceData;
    private LineAnnotation? _cursorAnnotation;
    private LineAnnotation? _deltaCursorAnnotation;
    private RectangleAnnotation? _cursorRangeAnnotation;
    private RectangleAnnotation? _hoverSegmentAnnotation;
    private bool _hoverSegmentActive;
    private bool _suspendVisibilityUpdate;
    private bool _isBoolPanelVisible = true;
    private bool _isBottomPanelVisible = true;
    private bool _suppressScrollEvent;
    private bool _suppressNameLaneScrollEvent;
    private bool _suppressAxisEvent;
    private bool _suspendCommentUpdate;
    private bool _isCursorDragging;
    private bool _isDeltaCursorDragging;
    private Point _boolListDragStartPoint;
    private Point _valueListDragStartPoint;
    private bool _allowBoolSelectionFromNameLane;
    private bool _suppressBoolSelectionEvent;
    private double _dataMinX;
    private double _dataMaxX;
    private double _windowSizeX = 1.0;
    private bool _showCommentLabels;
    private bool _showTypeSuffix;
    private bool _showCursorRangeBand;
    private double? _cursorX;
    private double? _deltaCursorX;
    private int _lastPrimarySampleIndex = -1;
    private int _lastDeltaSampleIndex = -1;
    private int _lastHoverSignalIndex = -1;
    private int _lastHoverStartIndex = -1;
    private int _lastHoverEndExclusive = -1;
    private bool? _lastHoverState;
    private readonly Dictionary<int, OxyColor> _boolColors = new();
    private List<int> _changePointSampleIndexes = [];
    private List<int> _visibleBoolSignalIndexes = [];
    private bool _jumpScopeSelectedBoolOnly;
    private int? _selectedJumpBoolSignalIndex;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        CreateEmptyPlot();
        TracePlot.PreviewMouseLeftButtonDown += TracePlot_PreviewMouseLeftButtonDown;
        TracePlot.PreviewMouseMove += TracePlot_PreviewMouseMove;
        TracePlot.PreviewMouseLeftButtonUp += TracePlot_PreviewMouseLeftButtonUp;
        TracePlot.PreviewMouseRightButtonDown += TracePlot_PreviewMouseRightButtonDown;
        TracePlot.PreviewMouseRightButtonUp += TracePlot_PreviewMouseRightButtonUp;
        TracePlot.MouseLeave += TracePlot_MouseLeave;
        NameLaneScrollViewer.SizeChanged += (_, _) => UpdateNameLaneScrollBar();
    }

    private void ReorderBoolRows(List<string> orderedNames)
    {
        if (orderedNames.Count == 0)
        {
            return;
        }

        var map = _viewModel.BoolSignals.ToDictionary(static row => row.Name, StringComparer.Ordinal);
        var ordered = new List<BoolSignalRow>(_viewModel.BoolSignals.Count);

        foreach (var name in orderedNames)
        {
            if (map.Remove(name, out var row))
            {
                ordered.Add(row);
            }
        }

        ordered.AddRange(map.Values);

        if (ordered.Count != _viewModel.BoolSignals.Count)
        {
            return;
        }

        _viewModel.BoolSignals.Clear();
        foreach (var row in ordered)
        {
            _viewModel.BoolSignals.Add(row);
        }
    }

    private void ReorderValueRows(List<string> orderedNames)
    {
        if (orderedNames.Count == 0)
        {
            return;
        }

        var map = _viewModel.ValueSignals.ToDictionary(static row => row.Name, StringComparer.Ordinal);
        var ordered = new List<ValueSignalRow>(_viewModel.ValueSignals.Count);

        foreach (var name in orderedNames)
        {
            if (map.Remove(name, out var row))
            {
                ordered.Add(row);
            }
        }

        ordered.AddRange(map.Values);

        if (ordered.Count != _viewModel.ValueSignals.Count)
        {
            return;
        }

        _viewModel.ValueSignals.Clear();
        foreach (var row in ordered)
        {
            _viewModel.ValueSignals.Add(row);
        }
    }
    private void TracePlot_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_traceData is null || TracePlot.Model is null)
        {
            return;
        }

        _isCursorDragging = true;
        _isDeltaCursorDragging = false;
        TracePlot.CaptureMouse();
        UpdatePrimaryCursorAtPosition(e.GetPosition(TracePlot));
        e.Handled = true;
    }

    private void TracePlot_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isCursorDragging)
        {
            UpdatePrimaryCursorAtPosition(e.GetPosition(TracePlot));
            return;
        }

        if (_isDeltaCursorDragging)
        {
            UpdateDeltaCursorAtPosition(e.GetPosition(TracePlot));
            return;
        }

        UpdateHoverSegmentAtPosition(e.GetPosition(TracePlot));
    }

    private void TracePlot_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_isCursorDragging)
        {
            return;
        }

        UpdatePrimaryCursorAtPosition(e.GetPosition(TracePlot));
        _isCursorDragging = false;
        TracePlot.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void TracePlot_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_traceData is null || TracePlot.Model is null)
        {
            return;
        }

        _isDeltaCursorDragging = true;
        _isCursorDragging = false;
        TracePlot.CaptureMouse();
        UpdateDeltaCursorAtPosition(e.GetPosition(TracePlot));
        e.Handled = true;
    }

    private void TracePlot_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_isDeltaCursorDragging)
        {
            return;
        }

        UpdateDeltaCursorAtPosition(e.GetPosition(TracePlot));
        _isDeltaCursorDragging = false;
        TracePlot.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void TracePlot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isCursorDragging && !_isDeltaCursorDragging)
        {
            return;
        }

        _isCursorDragging = false;
        _isDeltaCursorDragging = false;
        TracePlot.ReleaseMouseCapture();
        ClearHoverSegment();
    }

    private void DrawTrace(TraceData traceData, List<int> visibleSignalIndexes)
    {
        var model = new PlotModel
        {
            PlotMargins = new OxyThickness(PlotLeftMargin, PlotTopMargin, PlotRightMargin, PlotBottomMargin),
            IsLegendVisible = false,
            Background = OxyColors.White
        };

        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Elapsed Time [s]",
            StringFormat = "0.000",
            AbsoluteMinimum = traceData.ElapsedSeconds[0],
            AbsoluteMaximum = traceData.ElapsedSeconds[^1],
            IsPanEnabled = false,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        };
#pragma warning disable CS0618
        xAxis.AxisChanged += XAxis_AxisChanged;
#pragma warning restore CS0618
        model.Axes.Add(xAxis);

        var laneNames = visibleSignalIndexes.Select(GetBoolLaneLabel).ToList();
        _visibleBoolSignalIndexes = [.. visibleSignalIndexes];
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = -0.5,
            Maximum = Math.Max(visibleSignalIndexes.Count - 0.5, 0.5),
            StartPosition = 1,
            EndPosition = 0,
            MajorStep = 1.0,
            MinorStep = 1.0,
            TickStyle = TickStyle.Outside,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            LabelFormatter = value =>
            {
                return string.Empty;
            }
        };
        model.Axes.Add(yAxis);

        for (var laneIndex = 0; laneIndex < visibleSignalIndexes.Count; laneIndex++)
        {
            var signalIndex = visibleSignalIndexes[laneIndex];
            var signal = traceData.BoolSignals[signalIndex];
            var series = new StairStepSeries
            {
                StrokeThickness = 2.4,
                Color = GetSignalColor(signalIndex)
            };

            for (var i = 0; i < traceData.SampleCount; i++)
            {
                var value = signal.Values[i];
                if (!value.HasValue)
                {
                    continue;
                }

                // Keep ON state above OFF state in the rendered lane.
                var y = laneIndex + (value.Value ? -0.32 : 0.32);
                series.Points.Add(new DataPoint(traceData.ElapsedSeconds[i], y));
            }

            model.Series.Add(series);
        }

        var firstX = traceData.ElapsedSeconds[0];
        _cursorX = ClampToTraceRange(_cursorX ?? firstX, traceData);
        _deltaCursorX = ClampToTraceRange(_deltaCursorX ?? firstX, traceData);

        _cursorAnnotation = new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            X = _cursorX.Value,
            Color = OxyColors.OrangeRed,
            StrokeThickness = 1.0,
            LineStyle = LineStyle.Solid
        };
        model.Annotations.Add(_cursorAnnotation);

        _deltaCursorAnnotation = new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            X = _deltaCursorX.Value,
            Color = OxyColors.MediumBlue,
            StrokeThickness = 1.0,
            LineStyle = LineStyle.Solid
        };
        model.Annotations.Add(_deltaCursorAnnotation);
        _cursorRangeAnnotation = new RectangleAnnotation
        {
            MinimumX = _cursorX.Value,
            MaximumX = _cursorX.Value,
            MinimumY = -0.5,
            MaximumY = Math.Max(visibleSignalIndexes.Count - 0.5, 0.5),
            Fill = OxyColors.Transparent,
            Stroke = OxyColors.Transparent,
            Layer = AnnotationLayer.BelowSeries
        };
        model.Annotations.Add(_cursorRangeAnnotation);
        _hoverSegmentAnnotation = new RectangleAnnotation
        {
            MinimumX = firstX,
            MaximumX = firstX + 1e-6,
            MinimumY = -0.45,
            MaximumY = 0.45,
            Fill = OxyColors.Transparent,
            Stroke = OxyColors.Transparent,
            Layer = AnnotationLayer.AboveSeries
        };
        model.Annotations.Add(_hoverSegmentAnnotation);
        _hoverSegmentActive = false;

        model.ResetAllAxes();
        TracePlot.Model = model;
        _dataMinX = traceData.ElapsedSeconds[0];
        _dataMaxX = traceData.ElapsedSeconds[^1];
        _windowSizeX = _dataMaxX - _dataMinX;
        NameLaneGrid.Margin = new Thickness(2, PlotTopMargin, 2, PlotBottomMargin);
        _lastPrimarySampleIndex = TraceNavigationService.FindClosestSample(traceData.ElapsedSeconds, _cursorX.Value);
        _lastDeltaSampleIndex = TraceNavigationService.FindClosestSample(traceData.ElapsedSeconds, _deltaCursorX.Value);
        _lastHoverSignalIndex = -1;
        _lastHoverStartIndex = -1;
        _lastHoverEndExclusive = -1;
        _lastHoverState = null;
        UpdateTimeScrollBar(_dataMinX, _dataMaxX);
        UpdateNameLanePanel(laneNames);
        RefreshChangePointIndexes(traceData, visibleSignalIndexes);
        UpdateCursorDeltaText();
        UpdateCursorRangeBand();
        ClearHoverSegment();
    }

    private void JumpPrevChange_Click(object sender, RoutedEventArgs e)
    {
        JumpPrevChangeCore();
    }

    private void JumpNextChange_Click(object sender, RoutedEventArgs e)
    {
        JumpNextChangeCore();
    }

    private void SwapCursors_Click(object sender, RoutedEventArgs e)
    {
        if (_traceData is null || !_cursorX.HasValue || !_deltaCursorX.HasValue)
        {
            return;
        }

        var temp = _cursorX.Value;
        _cursorX = _deltaCursorX.Value;
        _deltaCursorX = temp;

        if (_deltaCursorAnnotation is not null)
        {
            _deltaCursorAnnotation.X = _deltaCursorX.Value;
        }

        // Primary cursor drives sampled values, so refresh it from swapped position.
        var sampleIndex = TraceNavigationService.FindClosestSample(_traceData.ElapsedSeconds, _cursorX.Value);
        ApplyPrimaryCursorSample(sampleIndex);
    }

    private bool JumpPrevChangeCore()
    {
        if (_traceData is null)
        {
            return false;
        }

        RefreshChangePointIndexes();
        if (_changePointSampleIndexes.Count == 0)
        {
            return false;
        }

        var currentIndex = _cursorX.HasValue ? TraceNavigationService.FindClosestSample(_traceData.ElapsedSeconds, _cursorX.Value) : 0;
        int? target = null;
        for (var i = _changePointSampleIndexes.Count - 1; i >= 0; i--)
        {
            if (_changePointSampleIndexes[i] < currentIndex)
            {
                target = _changePointSampleIndexes[i];
                break;
            }
        }

        if (target.HasValue)
        {
            ApplyPrimaryCursorSample(target.Value);
            return true;
        }

        return false;
    }

    private bool JumpNextChangeCore()
    {
        if (_traceData is null)
        {
            return false;
        }

        RefreshChangePointIndexes();
        if (_changePointSampleIndexes.Count == 0)
        {
            return false;
        }

        var currentIndex = _cursorX.HasValue ? TraceNavigationService.FindClosestSample(_traceData.ElapsedSeconds, _cursorX.Value) : 0;
        int? target = null;
        for (var i = 0; i < _changePointSampleIndexes.Count; i++)
        {
            if (_changePointSampleIndexes[i] > currentIndex)
            {
                target = _changePointSampleIndexes[i];
                break;
            }
        }

        if (target.HasValue)
        {
            ApplyPrimaryCursorSample(target.Value);
            return true;
        }

        return false;
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((Input.Keyboard.Modifiers & Input.ModifierKeys.Control) != 0)
        {
            if (e.Key == Input.Key.O)
            {
                LoadCsv_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Input.Key.E)
            {
                SavePng_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
        }

        // Keep arrow behavior for text editing controls.
        if (e.OriginalSource is TextBox)
        {
            return;
        }

        if ((Input.Keyboard.Modifiers & Input.ModifierKeys.Shift) == 0)
        {
            return;
        }

        if (e.Key == Input.Key.Left)
        {
            _ = JumpPrevChangeCore();
            // Always handle to prevent focused controls (e.g., ComboBox) from consuming Shift+Arrow.
            e.Handled = true;
            return;
        }

        if (e.Key == Input.Key.Right)
        {
            _ = JumpNextChangeCore();
            // Always handle to prevent focused controls (e.g., ComboBox) from consuming Shift+Arrow.
            e.Handled = true;
        }
    }

    private void CreateEmptyPlot()
    {
        var model = new PlotModel { Background = OxyColors.White };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Elapsed Time" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Signals" });
        TracePlot.Model = model;
    }

    private void InitializeValueRows(TraceData traceData)
    {
        foreach (var row in _viewModel.ValueSignals)
        {
            row.PropertyChanged -= ValueSignalRow_PropertyChanged;
        }

        _viewModel.ValueSignals.Clear();
        foreach (var signal in traceData.ValueSignals)
        {
            var row = new ValueSignalRow
            {
                Name = signal.Name,
                SignalIndex = _viewModel.ValueSignals.Count,
                CommentText = UiFormattingService.BuildDefaultComment(signal.Name),
                DisplayLabel = signal.Name,
                IsUnchanged = !signal.HasChange,
                IsVisible = true,
                ValueText = "-"
            };
            row.PropertyChanged += ValueSignalRow_PropertyChanged;
            _viewModel.ValueSignals.Add(row);
        }

        UpdateDisplayLabels();
        RefreshVisibleValueRows();
    }

    private void UpdateValueRows(TraceData traceData, int sampleIndex)
    {
        foreach (var row in _viewModel.ValueSignals)
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

    private void InitializeBoolRows(TraceData traceData)
    {
        foreach (var row in _viewModel.BoolSignals)
        {
            row.PropertyChanged -= BoolSignalRow_PropertyChanged;
        }

        _viewModel.BoolSignals.Clear();
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
            _viewModel.BoolSignals.Add(row);
        }

        UpdateDisplayLabels();
    }

    private void BoolSignalRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_traceData is null || sender is not BoolSignalRow row)
        {
            return;
        }

        if (e.PropertyName == nameof(BoolSignalRow.IsVisible))
        {
            if (_suspendVisibilityUpdate)
            {
                return;
            }

            RedrawWithVisibility();
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

                RedrawWithVisibility();
            }
            return;
        }

        if (e.PropertyName == nameof(BoolSignalRow.CommentText) && _showCommentLabels)
        {
            if (_suspendCommentUpdate)
            {
                return;
            }

            UpdateDisplayLabels();
            RedrawWithVisibility();
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

    private void AllBoolOn_Click(object sender, RoutedEventArgs e)
    {
        SetAllBoolVisibility(true);
    }

    private void AllBoolOff_Click(object sender, RoutedEventArgs e)
    {
        SetAllBoolVisibility(false);
    }

    private void SetAllBoolVisibility(bool isVisible)
    {
        if (_traceData is null)
        {
            return;
        }

        _suspendVisibilityUpdate = true;
        try
        {
            foreach (var row in _viewModel.BoolSignals)
            {
                row.IsVisible = isVisible;
            }
        }
        finally
        {
            _suspendVisibilityUpdate = false;
        }

        RedrawWithVisibility();
    }

    private void HideNoChangeBool_Click(object sender, RoutedEventArgs e)
    {
        if (_traceData is null)
        {
            return;
        }

        _suspendVisibilityUpdate = true;
        try
        {
            foreach (var row in _viewModel.BoolSignals)
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

        RedrawWithVisibility();
    }

    private void AutoBoolColors_Click(object sender, RoutedEventArgs e)
    {
        if (_traceData is null)
        {
            return;
        }

        var visibleRows = _viewModel.BoolSignals.Where(static row => row.IsVisible).ToList();
        for (var i = 0; i < visibleRows.Count; i++)
        {
            var row = visibleRows[i];
            var color = SignalColorService.GetDefaultPaletteColor(i);
            _boolColors[row.Index] = color;
            row.ColorHex = SignalColorService.ToHex(color);
        }

        RedrawWithVisibility();
    }

    private void AllValueOn_Click(object sender, RoutedEventArgs e) => SetAllValueVisibility(true);

    private void AllValueOff_Click(object sender, RoutedEventArgs e) => SetAllValueVisibility(false);

    private void SetAllValueVisibility(bool isVisible)
    {
        foreach (var row in _viewModel.ValueSignals)
        {
            row.IsVisible = isVisible;
        }

        RefreshVisibleValueRows();
        UpdateStatusForVisibility();
    }

    private void HideNoChangeValue_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _viewModel.ValueSignals)
        {
            if (row.IsUnchanged)
            {
                row.IsVisible = false;
            }
        }

        RefreshVisibleValueRows();
        UpdateStatusForVisibility();
    }

    private void RedrawWithVisibility()
    {
        if (_traceData is null)
        {
            return;
        }

        var visible = GetVisibleBoolIndexes();
        DrawTrace(_traceData, visible);
        _viewModel.StatusText = UiFormattingService.BuildStatusText(_traceData, visible.Count, _viewModel.VisibleValueSignals.Count);
    }

    private void ToggleBoolPanel_Click(object sender, RoutedEventArgs e)
    {
        _isBoolPanelVisible = !_isBoolPanelVisible;
        BoolPanelBorder.Visibility = _isBoolPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        BoolPanelSplitter.Visibility = _isBoolPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        BoolPanelSplitterColumn.Width = _isBoolPanelVisible ? new GridLength(6) : new GridLength(0);
        BoolPanelColumn.Width = _isBoolPanelVisible ? new GridLength(320) : new GridLength(0);
        ToggleBoolPanelButton.Content = _isBoolPanelVisible ? "Hide Right Panel" : "Show Right Panel";
    }

    private void ToggleBottomPanel_Click(object sender, RoutedEventArgs e)
    {
        _isBottomPanelVisible = !_isBottomPanelVisible;
        BottomPanelBorder.Visibility = _isBottomPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        BottomPanelSplitter.Visibility = _isBottomPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        BottomPanelSplitterRow.Height = _isBottomPanelVisible ? new GridLength(6) : new GridLength(0);
        BottomPanelRow.MinHeight = _isBottomPanelVisible ? 140 : 0;
        BottomPanelRow.Height = _isBottomPanelVisible ? new GridLength(2, GridUnitType.Star) : new GridLength(0);
        ToggleBottomPanelButton.Content = _isBottomPanelVisible ? HideVariableSettingsText : ShowVariableSettingsText;
    }

    private void BoolSignalsListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _boolListDragStartPoint = e.GetPosition(BoolSignalsListBox);
    }

    private void BoolSignalsListBox_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source &&
            FindVisualParent<TextBox>(source) is not null)
        {
            // Allow normal text selection/editing drag inside cell editors.
            return;
        }

        var currentPos = e.GetPosition(BoolSignalsListBox);
        var delta = currentPos - _boolListDragStartPoint;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (ItemsControl.ContainerFromElement(BoolSignalsListBox, e.OriginalSource as DependencyObject) is not ListViewItem item ||
            item.DataContext is not BoolSignalRow row)
        {
            return;
        }

        DragDrop.DoDragDrop(item, row, DragDropEffects.Move);
    }

    private void BoolSignalsListBox_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(BoolSignalRow)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void BoolSignalsListBox_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(BoolSignalRow)) || e.Data.GetData(typeof(BoolSignalRow)) is not BoolSignalRow sourceRow)
        {
            return;
        }

        var sourceIndex = _viewModel.BoolSignals.IndexOf(sourceRow);
        if (sourceIndex < 0)
        {
            return;
        }

        if (ItemsControl.ContainerFromElement(BoolSignalsListBox, e.OriginalSource as DependencyObject) is not ListViewItem targetItem ||
            targetItem.DataContext is not BoolSignalRow targetRow)
        {
            // Ignore drops on empty area to avoid unintended "move to end" behavior.
            return;
        }

        var targetIndex = _viewModel.BoolSignals.IndexOf(targetRow);

        if (targetIndex < 0 || targetIndex == sourceIndex)
        {
            return;
        }

        _viewModel.BoolSignals.RemoveAt(sourceIndex);
        if (sourceIndex < targetIndex)
        {
            targetIndex--;
        }

        _viewModel.BoolSignals.Insert(targetIndex, sourceRow);
        RedrawWithVisibility();
    }

    private void ValueSignalsListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _valueListDragStartPoint = e.GetPosition(ValueSignalsListBox);
    }

    private void ValueSignalsListBox_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source &&
            FindVisualParent<TextBox>(source) is not null)
        {
            // Allow normal text selection/editing drag inside cell editors.
            return;
        }

        var currentPos = e.GetPosition(ValueSignalsListBox);
        var delta = currentPos - _valueListDragStartPoint;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (ItemsControl.ContainerFromElement(ValueSignalsListBox, e.OriginalSource as DependencyObject) is not ListViewItem item ||
            item.DataContext is not ValueSignalRow row)
        {
            return;
        }

        DragDrop.DoDragDrop(item, row, DragDropEffects.Move);
    }

    private void ValueSignalsListBox_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(ValueSignalRow)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void ValueSignalsListBox_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(ValueSignalRow)) || e.Data.GetData(typeof(ValueSignalRow)) is not ValueSignalRow sourceRow)
        {
            return;
        }

        var sourceIndex = _viewModel.ValueSignals.IndexOf(sourceRow);
        if (sourceIndex < 0)
        {
            return;
        }

        if (ItemsControl.ContainerFromElement(ValueSignalsListBox, e.OriginalSource as DependencyObject) is not ListViewItem targetItem ||
            targetItem.DataContext is not ValueSignalRow targetRow)
        {
            // Ignore drops on empty area to avoid unintended "move to end" behavior.
            return;
        }

        var targetIndex = _viewModel.ValueSignals.IndexOf(targetRow);

        if (targetIndex < 0 || targetIndex == sourceIndex)
        {
            return;
        }

        _viewModel.ValueSignals.RemoveAt(sourceIndex);
        if (sourceIndex < targetIndex)
        {
            targetIndex--;
        }

        _viewModel.ValueSignals.Insert(targetIndex, sourceRow);
        RefreshVisibleValueRows();
    }

    private void LabelModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _showCommentLabels = LabelModeComboBox.SelectedIndex == 1;
        UpdateDisplayLabels();
        if (_traceData is not null)
        {
            RedrawWithVisibility();
        }
    }

    private void ShowTypeSuffixCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _showTypeSuffix = ShowTypeSuffixMenuItem.IsChecked;
        UpdateDisplayLabels();
        if (_traceData is not null)
        {
            RedrawWithVisibility();
        }
    }

    private void JumpScopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (JumpScopeComboBox is null)
        {
            return;
        }

        _jumpScopeSelectedBoolOnly = JumpScopeComboBox.SelectedIndex == 1;
        _viewModel.IsSelectedBoolJumpMode = _jumpScopeSelectedBoolOnly;
        EnsureJumpScopeSelection();
        UpdateNameLaneHighlight();
        if (_traceData is null)
        {
            return;
        }

        RefreshChangePointIndexes();
    }

    private void BoolSignalsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressBoolSelectionEvent)
        {
            return;
        }

        if (!_allowBoolSelectionFromNameLane && BoolSignalsListBox.SelectedItem is not null)
        {
            _suppressBoolSelectionEvent = true;
            try
            {
                BoolSignalsListBox.SelectedItem = null;
            }
            finally
            {
                _suppressBoolSelectionEvent = false;
            }
        }

        if (BoolSignalsListBox.SelectedItem is BoolSignalRow selectedRow)
        {
            _selectedJumpBoolSignalIndex = selectedRow.Index;
        }

        _allowBoolSelectionFromNameLane = false;
        UpdateNameLaneHighlight();

        if (_traceData is null || !_jumpScopeSelectedBoolOnly)
        {
            return;
        }

        RefreshChangePointIndexes();
    }

    private void RangeBandCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _showCursorRangeBand = ShowRangeBandMenuItem.IsChecked;
        UpdateCursorRangeBand();
    }

    private void TimeScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_traceData is null || _suppressScrollEvent || TracePlot.Model?.DefaultXAxis is not Axis xAxis)
        {
            return;
        }

        var left = Math.Max(_dataMinX, Math.Min(e.NewValue, _dataMaxX - _windowSizeX));
        var right = left + _windowSizeX;

        _suppressAxisEvent = true;
        try
        {
            xAxis.Zoom(left, right);
            TracePlot.Model.InvalidatePlot(false);
        }
        finally
        {
            _suppressAxisEvent = false;
        }
    }

#pragma warning disable CS0618
    private void XAxis_AxisChanged(object? sender, AxisChangedEventArgs e)
    {
        if (_traceData is null || _suppressAxisEvent || sender is not Axis xAxis)
        {
            return;
        }

        var min = xAxis.ActualMinimum;
        var max = xAxis.ActualMaximum;
        var currentWindow = Math.Max(max - min, 0.0001);
        _windowSizeX = Math.Min(currentWindow, _dataMaxX - _dataMinX);

        var clampedMin = Math.Max(_dataMinX, min);
        var clampedMax = Math.Min(_dataMaxX, max);
        if (clampedMax - clampedMin < _windowSizeX)
        {
            if (clampedMin <= _dataMinX)
            {
                clampedMax = Math.Min(_dataMaxX, _dataMinX + _windowSizeX);
                clampedMin = _dataMinX;
            }
            else
            {
                clampedMin = Math.Max(_dataMinX, _dataMaxX - _windowSizeX);
                clampedMax = _dataMaxX;
            }
        }

        if (Math.Abs(clampedMin - min) > 1e-9 || Math.Abs(clampedMax - max) > 1e-9)
        {
            _suppressAxisEvent = true;
            try
            {
                xAxis.Zoom(clampedMin, clampedMax);
            }
            finally
            {
                _suppressAxisEvent = false;
            }
        }

        UpdateTimeScrollBar(clampedMin, clampedMax);
    }
#pragma warning restore CS0618

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T found)
            {
                return found;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
    private void UpdateTimeScrollBar(double viewMin, double viewMax)
    {
        var total = Math.Max(_dataMaxX - _dataMinX, 0.0001);
        var window = Math.Max(viewMax - viewMin, 0.0001);
        var maxLeft = _dataMaxX - window;
        var left = Math.Max(_dataMinX, Math.Min(viewMin, maxLeft));

        _suppressScrollEvent = true;
        try
        {
            TimeScrollBar.Minimum = _dataMinX;
            TimeScrollBar.Maximum = Math.Max(maxLeft, _dataMinX);
            TimeScrollBar.ViewportSize = Math.Min(window, total);
            TimeScrollBar.SmallChange = Math.Max(window / 20.0, 0.0001);
            TimeScrollBar.LargeChange = Math.Max(window / 2.0, 0.0001);
            TimeScrollBar.Value = left;
        }
        finally
        {
            _suppressScrollEvent = false;
        }
    }

    private OxyColor GetSignalColor(int signalIndex)
    {
        if (_boolColors.TryGetValue(signalIndex, out var color))
        {
            return color;
        }

        return OxyColors.DodgerBlue;
    }

    private void ApplyAutoColors(int signalCount)
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

    private void UpdateBoolRowColor(int signalIndex)
    {
        var row = _viewModel.BoolSignals.FirstOrDefault(r => r.Index == signalIndex);
        if (row is null)
        {
            return;
        }

        row.ColorHex = SignalColorService.ToHex(GetSignalColor(signalIndex));
    }

    private void UpdateDisplayLabels()
    {
        foreach (var row in _viewModel.BoolSignals)
        {
            // BOOL labels stay fixed to variable names to keep signal-row identity stable.
            row.DisplayLabel = UiFormattingService.FormatVariableName(row.Name, _showTypeSuffix);
        }

        foreach (var row in _viewModel.ValueSignals)
        {
            // Value labels follow BOOL Label Mode (Variable Name / Comment).
            var baseText = _showCommentLabels && !string.IsNullOrWhiteSpace(row.CommentText)
                ? row.CommentText
                : UiFormattingService.FormatVariableName(row.Name, _showTypeSuffix);
            row.DisplayLabel = baseText;
            row.SettingsDisplayName = UiFormattingService.FormatVariableName(row.Name, _showTypeSuffix);
        }

        RefreshVisibleValueRows();
    }

    private void RefreshVisibleValueRows()
    {
        _viewModel.VisibleValueSignals.Clear();
        foreach (var row in _viewModel.ValueSignals.Where(static r => r.IsVisible))
        {
            _viewModel.VisibleValueSignals.Add(row);
        }
    }

    private void UpdateStatusForVisibility()
    {
        if (_traceData is null)
        {
            return;
        }

        _viewModel.StatusText = UiFormattingService.BuildStatusText(_traceData, GetVisibleBoolIndexes().Count, _viewModel.VisibleValueSignals.Count);
    }

    private void RefreshChangePointIndexes()
    {
        if (_traceData is null)
        {
            _changePointSampleIndexes.Clear();
            return;
        }

        EnsureJumpScopeSelection();
        RefreshChangePointIndexes(_traceData, _visibleBoolSignalIndexes);
    }

    private void RefreshChangePointIndexes(TraceData traceData, IReadOnlyList<int> visibleSignalIndexes)
    {
        _changePointSampleIndexes =
            TraceNavigationService.BuildChangePointSampleIndexes(traceData, GetJumpTargetSignalIndexes(visibleSignalIndexes));
    }

    private List<int> GetJumpTargetSignalIndexes(IReadOnlyList<int> visibleSignalIndexes)
    {
        if (!_jumpScopeSelectedBoolOnly)
        {
            return visibleSignalIndexes.ToList();
        }

        if (_selectedJumpBoolSignalIndex.HasValue &&
            _viewModel.BoolSignals.FirstOrDefault(r => r.Index == _selectedJumpBoolSignalIndex.Value) is { IsVisible: true } selected)
        {
            return [selected.Index];
        }

        // Selected BOOL mode: jump is disabled until a visible BOOL is selected.
        return [];
    }

    private void EnsureJumpScopeSelection()
    {
        if (!_jumpScopeSelectedBoolOnly || BoolSignalsListBox is null)
        {
            return;
        }

        if (_selectedJumpBoolSignalIndex.HasValue &&
            _viewModel.BoolSignals.FirstOrDefault(r => r.Index == _selectedJumpBoolSignalIndex.Value) is { IsVisible: true } current)
        {
            if (!ReferenceEquals(BoolSignalsListBox.SelectedItem, current))
            {
                _allowBoolSelectionFromNameLane = true;
                BoolSignalsListBox.SelectedItem = current;
                BoolSignalsListBox.ScrollIntoView(current);
            }

            return;
        }

        if (BoolSignalsListBox.SelectedItem is BoolSignalRow selected && selected.IsVisible)
        {
            _selectedJumpBoolSignalIndex = selected.Index;
            return;
        }

        var firstVisible = _viewModel.BoolSignals.FirstOrDefault(static row => row.IsVisible);
        if (firstVisible is null)
        {
            _selectedJumpBoolSignalIndex = null;
            BoolSignalsListBox.SelectedItem = null;
            return;
        }

        _selectedJumpBoolSignalIndex = firstVisible.Index;
        _allowBoolSelectionFromNameLane = true;
        BoolSignalsListBox.SelectedItem = firstVisible;
        BoolSignalsListBox.ScrollIntoView(firstVisible);
    }

    private string GetBoolLaneLabel(int signalIndex)
    {
        var row = _viewModel.BoolSignals.FirstOrDefault(r => r.Index == signalIndex);
        if (row is null)
        {
            return _traceData?.BoolSignals[signalIndex].Name ?? string.Empty;
        }

        var baseText = _showCommentLabels && !string.IsNullOrWhiteSpace(row.CommentText)
            ? row.CommentText
            : UiFormattingService.FormatVariableName(row.Name, _showTypeSuffix);
        return row.IsUnchanged ? $"{baseText}{NoChangeSuffix}" : baseText;
    }

    private List<int> GetVisibleBoolIndexes() =>
        [.. _viewModel.BoolSignals.Where(static s => s.IsVisible).Select(static s => s.Index)];


}

