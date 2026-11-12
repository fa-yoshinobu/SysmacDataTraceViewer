using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SysmacDataTraceViewer.Views;

using SysmacDataTraceViewer.Models;

internal sealed class NameLanePresenter(Grid NameLaneGrid, ScrollViewer NameLaneScrollViewer, System.Windows.Controls.Primitives.ScrollBar NameLaneScrollBar)
{
    // Same stripe color as the chart lanes (TracePlotModelBuilder.LaneStripeColor).
    private static readonly Brush LaneStripeBrush = CreateFrozenBrush(0xF3, 0xF5, 0xF8);
    private static readonly Brush OnValueBrush = CreateFrozenBrush(0x1B, 0x7F, 0x3B);
    private static readonly Brush OffValueBrush = CreateFrozenBrush(0x8A, 0x93, 0x9F);
    private static readonly Brush LabelBrush = CreateFrozenBrush(0x1F, 0x29, 0x37);

    private bool _suppressNameLaneScrollEvent;
    private List<int> _visibleBoolSignalIndexes = [];
    private TraceData? _traceData;
    private int _sampleIndex = -1;
    private int? _selectedIndex;
    public event Action<int>? SignalSelected;

    public void SetTrace(TraceData? trace, List<int> visibleIndexes, int sampleIndex)
    {
        _traceData = trace;
        _visibleBoolSignalIndexes = visibleIndexes;
        _sampleIndex = sampleIndex;
    }

    public void UpdateNameLanePanel(List<string> laneNames)
    {
        NameLaneGrid.RowDefinitions.Clear();
        NameLaneGrid.Children.Clear();

        if (laneNames.Count == 0)
        {
            NameLaneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var emptyText = new TextBlock
            {
                Text = "(no BOOL selected)",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(6, 0, 6, 0)
            };
            Grid.SetRow(emptyText, 0);
            NameLaneGrid.Children.Add(emptyText);
            UpdateNameLaneScrollBar();
            return;
        }

        for (var i = 0; i < laneNames.Count; i++)
        {
            NameLaneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var signalIndex = _visibleBoolSignalIndexes.Count > i ? _visibleBoolSignalIndexes[i] : -1;
            var rowGrid = new Grid
            {
                Margin = new Thickness(2, 0, 2, 0),
                Background = i % 2 == 1 ? LaneStripeBrush : Brushes.Transparent
            };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = laneNames[i],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0),
                Tag = signalIndex,
                Cursor = System.Windows.Input.Cursors.Hand,
                Foreground = LabelBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = laneNames[i]
            };
            label.MouseLeftButtonDown += NameLaneLabel_MouseLeftButtonDown;
            Grid.SetColumn(label, 0);
            rowGrid.Children.Add(label);

            var valueText = new TextBlock
            {
                Text = "-",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 2, 0),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.SemiBold,
                Foreground = OffValueBrush,
                Tag = $"value:{signalIndex}"
            };
            Grid.SetColumn(valueText, 1);
            rowGrid.Children.Add(valueText);

            Grid.SetRow(rowGrid, i);
            NameLaneGrid.Children.Add(rowGrid);
        }

        UpdateNameLaneHighlight(_selectedIndex);
        UpdateNameLaneValues(_sampleIndex);
        UpdateNameLaneScrollBar();
    }

    public void UpdateNameLaneValues(int sampleIndex)
    {
        if (_traceData is null || sampleIndex < 0 || sampleIndex >= _traceData.SampleCount)
        {
            foreach (var rowGrid in NameLaneGrid.Children.OfType<Grid>())
            {
                var valueBlock = rowGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag is string tag && tag.StartsWith("value:", StringComparison.Ordinal));
                if (valueBlock is not null)
                {
                    valueBlock.Text = "-";
                    valueBlock.Foreground = OffValueBrush;
                }
            }

            return;
        }

        foreach (var rowGrid in NameLaneGrid.Children.OfType<Grid>())
        {
            var valueBlock = rowGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag is string tag && tag.StartsWith("value:", StringComparison.Ordinal));
            if (valueBlock is null)
            {
                continue;
            }

            if (valueBlock.Tag is not string tagValue || !int.TryParse(tagValue.AsSpan(6), out var signalIndex))
            {
                continue;
            }

            if (signalIndex < 0 || signalIndex >= _traceData.BoolSignals.Count)
            {
                valueBlock.Text = "-";
                valueBlock.Foreground = OffValueBrush;
                continue;
            }

            var value = _traceData.BoolSignals[signalIndex].Values[sampleIndex];
            valueBlock.Text = value.HasValue ? (value.Value ? "1" : "0") : "-";
            valueBlock.Foreground = value == true ? OnValueBrush : OffValueBrush;
        }
    }

    public void NameLaneScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressNameLaneScrollEvent)
        {
            return;
        }

        NameLaneScrollViewer.ScrollToHorizontalOffset(e.NewValue);
    }

    public void UpdateNameLaneScrollBar()
    {
        if (NameLaneScrollViewer is null || NameLaneScrollBar is null)
        {
            return;
        }

        var extent = NameLaneScrollViewer.ExtentWidth;
        var viewport = NameLaneScrollViewer.ViewportWidth;
        var max = Math.Max(0, extent - viewport);

        _suppressNameLaneScrollEvent = true;
        try
        {
            NameLaneScrollBar.Minimum = 0;
            NameLaneScrollBar.Maximum = Math.Max(max, 1);
            NameLaneScrollBar.ViewportSize = Math.Max(1, viewport);
            NameLaneScrollBar.SmallChange = 16;
            NameLaneScrollBar.LargeChange = Math.Max(32, viewport * 0.8);
            NameLaneScrollBar.Visibility = max > 0.5 ? Visibility.Visible : Visibility.Collapsed;

            var clamped = Math.Max(0, Math.Min(NameLaneScrollViewer.HorizontalOffset, max));
            NameLaneScrollBar.Value = clamped;
            NameLaneScrollViewer.ScrollToHorizontalOffset(clamped);
        }
        finally
        {
            _suppressNameLaneScrollEvent = false;
        }
    }

    private void NameLaneLabel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { Tag: int signalIndex }) return;
        SignalSelected?.Invoke(signalIndex);
        e.Handled = true;
    }

    public void UpdateNameLaneHighlight(int? selectedIndex)
    {
        _selectedIndex = selectedIndex;
        if (NameLaneGrid is null)
        {
            return;
        }

        if (!selectedIndex.HasValue)
        {
            foreach (var rowGrid in NameLaneGrid.Children.OfType<Grid>())
            {
                var label = rowGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag is int);
                if (label is null)
                {
                    continue;
                }

                label.Foreground = LabelBrush;
                label.FontWeight = FontWeights.Normal;
            }

            return;
        }

        var selected = selectedIndex.Value;
        foreach (var rowGrid in NameLaneGrid.Children.OfType<Grid>())
        {
            var label = rowGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag is int);
            if (label is null)
            {
                continue;
            }

            if (label.Tag is int signalIndex && signalIndex == selected)
            {
                label.Foreground = System.Windows.Media.Brushes.Red;
                label.FontWeight = FontWeights.Bold;
            }
            else
            {
                label.Foreground = LabelBrush;
                label.FontWeight = FontWeights.Normal;
            }
        }
    }

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
