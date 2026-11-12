using System.Windows;
using System.Windows.Threading;
using System.Diagnostics.CodeAnalysis;
using SysmacDataTraceViewer.Services;

namespace SysmacDataTraceViewer;

internal sealed partial class App : System.Windows.Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherError;
        AppDomain.CurrentDomain.UnhandledException += OnProcessError;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
        if (e.Args.Length > 0)
        {
            await window.LoadCsvAsync(e.Args[0]).ConfigureAwait(true);
        }
    }

    private void OnDispatcherError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppErrorLog.Record("Unhandled UI exception", e.Exception);
        ShowUnexpectedError();
        if (MainWindow?.IsLoaded == true && e.Exception is not OutOfMemoryException and not AccessViolationException
            and not System.Runtime.InteropServices.SEHException)
        {
            e.Handled = true;
        }
    }

    private static void OnProcessError(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception error)
        {
            AppErrorLog.Record("Unhandled process exception", error);
        }

        ShowUnexpectedError();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Failure notification is best effort when the UI or process is already failing.")]
    private static void ShowUnexpectedError()
    {
        try
        {
            MessageBox.Show("An unexpected error occurred. Details were saved in:" + Environment.NewLine + AppErrorLog.DirectoryPath,
                "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception) { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherError;
        AppDomain.CurrentDomain.UnhandledException -= OnProcessError;
        base.OnExit(e);
    }
}
