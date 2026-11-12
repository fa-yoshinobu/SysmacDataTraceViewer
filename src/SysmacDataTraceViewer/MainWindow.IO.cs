using System.Windows;
using SysmacDataTraceViewer.Services;

namespace SysmacDataTraceViewer;

internal partial class MainWindow
{
    private void TraceFile_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        e.Effects = !_viewModel.IsLoading && e.Data.GetData(DataFormats.FileDrop) is string[] paths &&
            TraceFileInput.TryGetDropPath(paths, out _, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private async void TraceFile_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        e.Handled = true;
        if (_viewModel.IsLoading)
        {
            _dialogService.ShowInformation("Wait for the current CSV load to finish or cancel it first.", "Open CSV");
            return;
        }
        if (!TraceFileInput.TryGetDropPath(e.Data.GetData(DataFormats.FileDrop) as string[] ?? [], out var path, out var reason))
        {
            _dialogService.ShowInformation(reason, "Open CSV");
            return;
        }
        await LoadCsvAsync(path).ConfigureAwait(true);
    }
    private async void LoadCsv_Click(object sender, RoutedEventArgs e) => await _files.OpenAsync().ConfigureAwait(true);
    internal Task LoadCsvAsync(string fileName) => _files.LoadAsync(fileName);
    private void CancelLoad_Click(object sender, RoutedEventArgs e) => _files.CancelLoad();
    private void ReadDetails_Click(object sender, RoutedEventArgs e) => _files.ShowReadDetails();
    protected override void OnClosed(EventArgs e)
    {
        _files.CancelLoad();
        base.OnClosed(e);
    }
    private void SavePng_Click(object sender, RoutedEventArgs e) => ExportPng(true);
    private void SavePngFullRange_Click(object sender, RoutedEventArgs e) => ExportPng(false);
    private void ExportPng(bool visibleRangeOnly)
    {
        if (TracePlot.Model is not { } model) return;
        _suppressAxisEvent = true;
        try
        {
            _files.ExportPng(model, visibleRangeOnly, _visibleBoolSignalIndexes.Select(_viewModel.GetBoolLaneLabel).ToList(), _dataMinX, _dataMaxX);
        }
        finally { _suppressAxisEvent = false; }
    }
    private void SaveComments_Click(object sender, RoutedEventArgs e) => _files.SaveComments();
    private async void LoadComments_Click(object sender, RoutedEventArgs e) => await _files.LoadCommentsAsync().ConfigureAwait(true);
    private void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
