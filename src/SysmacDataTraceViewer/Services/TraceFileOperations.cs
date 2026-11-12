using System.IO;
using System.Security;
using OxyPlot;
using SysmacDataTraceViewer.ViewModels;

namespace SysmacDataTraceViewer.Services;

internal sealed class TraceFileOperations(MainViewModel viewModel, IDialogService dialogs)
{
    private CancellationTokenSource? _loadCancellation;
    public event Action? TraceLoaded;

    public Task OpenAsync()
    {
        var path = viewModel.IsLoading ? null : dialogs.ShowOpenCsvFileDialog();
        return path is null ? Task.CompletedTask : LoadAsync(path);
    }

    public async Task LoadAsync(string path)
    {
        if (viewModel.IsLoading) return;
        using var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        viewModel.IsLoading = true;
        viewModel.StatusText = "Loading... 0%";
        var progress = new Progress<int>(percent =>
        {
            if (ReferenceEquals(_loadCancellation, cancellation)) viewModel.StatusText = $"Loading... {percent}%";
        });
        try
        {
            var trace = await Task.Run(() => CsvTraceParser.Parse(path, progress, cancellation.Token), cancellation.Token).ConfigureAwait(true);
            cancellation.Token.ThrowIfCancellationRequested();
            viewModel.InitializeTrace(trace);
            var directory = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrWhiteSpace(directory) && !string.IsNullOrWhiteSpace(name))
            {
                var comments = Path.Combine(directory, $"{name}_comments.csv");
                if (File.Exists(comments)) ApplyComments(comments, false);
            }
            TraceLoaded?.Invoke();
            viewModel.ResetCursorText();
            viewModel.UpdateStatusForVisibility();
        }
        catch (OperationCanceledException) { viewModel.StatusText = "Loading canceled."; }
        catch (Exception ex) when (IsFileError(ex))
        {
            dialogs.ShowError(ex.Message, "CSV Load Error");
            viewModel.StatusText = "Loading failed.";
        }
        finally
        {
            _loadCancellation = null;
            viewModel.IsLoading = false;
        }
    }

    public void CancelLoad() => _loadCancellation?.Cancel();
    public void ShowReadDetails()
    {
        if (viewModel.TraceData is { } trace) dialogs.ShowInformation(trace.Diagnostics.Describe(), "CSV Read Details");
    }

    public void SaveComments()
    {
        if (!RequireTrace("Save Comments")) return;
        var suggested = $"{Path.GetFileNameWithoutExtension(viewModel.TraceData!.FileName)}_comments.csv";
        if (dialogs.ShowSaveCsvFileDialog(suggested) is not { } path) return;
        RunFileAction(() =>
        {
            var data = viewModel.CaptureComments();
            CommentCsvService.Save(path, data.BoolSignals, data.ValueSignals);
        }, "Save Comments Error");
    }

    public void LoadComments()
    {
        if (RequireTrace("Load Comments") && dialogs.ShowOpenCsvFileDialog() is { } path) ApplyComments(path, true);
    }

    private void ApplyComments(string path, bool showError)
    {
        RunFileAction(() =>
        {
            var data = CommentCsvService.Load(path);
            viewModel.TraceData!.CommentEncodingName = data.EncodingName;
            viewModel.ApplyCommentData(data);
            viewModel.UpdateDisplayLabels();
            viewModel.UpdateStatusForVisibility();
            TraceLoaded?.Invoke();
        }, "Load Comments Error", showError);
    }

    public void ExportPng(PlotModel model, bool visibleRangeOnly, IReadOnlyList<string> laneLabels, double dataMin, double dataMax)
    {
        var suggested = visibleRangeOnly ? "trace_visible.png" : "trace_full.png";
        if (dialogs.ShowSavePngFileDialog(suggested) is not { } path) return;
        RunFileAction(() => TracePngExporter.Export(model, path, visibleRangeOnly, laneLabels, dataMin, dataMax), "PNG Export Error");
    }

    private bool RequireTrace(string title)
    {
        if (viewModel.TraceData is not null) return true;
        dialogs.ShowInformation("Load a trace CSV first.", title);
        return false;
    }

    private void RunFileAction(Action action, string title, bool showError = true)
    {
        try { action(); }
        catch (Exception ex) when (IsFileError(ex))
        {
            if (showError) dialogs.ShowError(ex.Message, title);
        }
    }

    private static bool IsFileError(Exception error) => error is InvalidDataException or IOException or UnauthorizedAccessException
        or SecurityException or FormatException or ArgumentException or NotSupportedException or InvalidOperationException;
}
