using System.Windows;
using System.Windows.Controls;

namespace SysmacDataTraceViewer;

public partial class MainWindow
{
    // Left name-lane rendering, selection sync, and horizontal scrolling.
    private void UpdateNameLanePanel(IReadOnlyList<string> laneNames)
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
            var label = new TextBlock
            {
                Text = laneNames[i],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 6, 0),
                Tag = signalIndex,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            label.MouseLeftButtonDown += NameLaneLabel_MouseLeftButtonDown;
            Grid.SetRow(label, i);
            NameLaneGrid.Children.Add(label);
        }

        UpdateNameLaneHighlight();
        UpdateNameLaneScrollBar();
    }

    private void NameLaneScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressNameLaneScrollEvent)
        {
            return;
        }

        NameLaneScrollViewer.ScrollToHorizontalOffset(e.NewValue);
    }

    private void UpdateNameLaneScrollBar()
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
        if (sender is not TextBlock label || label.Tag is not int signalIndex || BoolSignalsListBox is null)
        {
            return;
        }

        var row = _viewModel.BoolSignals.FirstOrDefault(r => r.Index == signalIndex);
        if (row is null)
        {
            return;
        }

        _selectedJumpBoolSignalIndex = row.Index;
        _allowBoolSelectionFromNameLane = true;
        BoolSignalsListBox.SelectedItem = row;
        BoolSignalsListBox.ScrollIntoView(row);
        e.Handled = true;
    }

    private void UpdateNameLaneHighlight()
    {
        if (NameLaneGrid is null || BoolSignalsListBox is null)
        {
            return;
        }

        if (!_jumpScopeSelectedBoolOnly || !_selectedJumpBoolSignalIndex.HasValue)
        {
            foreach (var child in NameLaneGrid.Children.OfType<TextBlock>())
            {
                child.Foreground = System.Windows.Media.Brushes.Black;
                child.FontWeight = FontWeights.Normal;
            }

            return;
        }

        var selected = _selectedJumpBoolSignalIndex.Value;
        foreach (var child in NameLaneGrid.Children.OfType<TextBlock>())
        {
            if (child.Tag is int signalIndex && signalIndex == selected)
            {
                child.Foreground = System.Windows.Media.Brushes.Red;
                child.FontWeight = FontWeights.Bold;
            }
            else
            {
                child.Foreground = System.Windows.Media.Brushes.Black;
                child.FontWeight = FontWeights.Normal;
            }
        }
    }
}
