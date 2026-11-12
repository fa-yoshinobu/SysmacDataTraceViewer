using System.Windows;
using SysmacDataTraceViewer.Services;

namespace SysmacDataTraceViewer;

internal partial class MainWindow
{
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
    private void LoadComments_Click(object sender, RoutedEventArgs e) => _files.LoadComments();
    private void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
