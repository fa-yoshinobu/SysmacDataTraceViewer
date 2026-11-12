using System.Windows;
using Microsoft.Win32;

namespace SysmacDataTraceViewer.Services;

internal sealed class WpfDialogService(Window owner) : IDialogService
{
    private const string CsvFileFilter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
    private const string PngFileFilter = "PNG image (*.png)|*.png";

    public string? ShowOpenCsvFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = CsvFileFilter,
            CheckFileExists = true
        };

        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public string? ShowOpenCommentsFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Comments from CSV or Sysmac Project",
            Filter = "Comment sources (*.csv;*.smc2)|*.csv;*.smc2|Comment CSV (*.csv)|*.csv|Sysmac Studio project (*.smc2)|*.smc2",
            CheckFileExists = true
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public string? ShowSaveCsvFileDialog(string fileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = CsvFileFilter,
            FileName = fileName,
            OverwritePrompt = true
        };

        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public string? ShowSavePngFileDialog(string fileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = PngFileFilter,
            FileName = fileName,
            OverwritePrompt = true
        };

        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public void ShowError(string message, string title) =>
        MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInformation(string message, string title) =>
        MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
