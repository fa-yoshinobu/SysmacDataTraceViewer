using System.Windows;

namespace SysmacDataTraceViewer;

internal partial class MainWindow
{
    private void UpdateNameLanePanel(List<string> laneNames)
    {
        _nameLane.SetTrace(_traceData, _visibleBoolSignalIndexes, _cursorState.LastPrimarySampleIndex);
        _nameLane.UpdateNameLanePanel(laneNames);
    }
    private void UpdateNameLaneValues(int sampleIndex) => _nameLane.UpdateNameLaneValues(sampleIndex);
    private void NameLaneScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _nameLane?.NameLaneScrollBar_ValueChanged(sender, e);
    private void UpdateNameLaneScrollBar() => _nameLane.UpdateNameLaneScrollBar();
    private void UpdateNameLaneHighlight() => _nameLane?.UpdateNameLaneHighlight(_jumpScopeSelectedBoolOnly ? _selectedJumpBoolSignalIndex : null);
    private void SelectBoolFromNameLane(int signalIndex)
    {
        var row = _viewModel.BoolSignals.FirstOrDefault(r => r.Index == signalIndex);
        if (row is null) return;
        _selectedJumpBoolSignalIndex = row.Index;
        _allowBoolSelectionFromNameLane = true;
        BoolSignalsListBox.SelectedItem = row;
        BoolSignalsListBox.ScrollIntoView(row);
    }
}
