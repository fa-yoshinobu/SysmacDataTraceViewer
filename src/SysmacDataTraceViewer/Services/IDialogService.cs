namespace SysmacDataTraceViewer.Services;

internal interface IDialogService
{
    string? ShowOpenCsvFileDialog();
    string? ShowSaveCsvFileDialog(string fileName);
    string? ShowSavePngFileDialog(string fileName);
    void ShowError(string message, string title);
    void ShowInformation(string message, string title);
}
