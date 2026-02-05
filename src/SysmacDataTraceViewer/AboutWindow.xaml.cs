using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using OxyPlot;
using OxyPlot.Wpf;

namespace SysmacDataTraceViewer;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var appVersion = GetAssemblyVersionText(Assembly.GetExecutingAssembly());
        VersionTextBlock.Text = $"Version: {appVersion}";

        LibrariesListView.ItemsSource = new[]
        {
            new LibraryInfo("Sysmac Data Trace Viewer", appVersion, "MIT", "Application itself"),
            new LibraryInfo("OxyPlot.Core", GetAssemblyVersionText(typeof(PlotModel).Assembly), "MIT", "Chart rendering"),
            new LibraryInfo("OxyPlot.Wpf", GetAssemblyVersionText(typeof(PlotView).Assembly), "MIT", "WPF integration"),
            new LibraryInfo(".NET Runtime", Environment.Version.ToString(), "MIT", "Application runtime")
        };

        LicenseTextBox.Text = LoadLicenseText();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore browser launch failures.
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static string GetAssemblyVersionText(Assembly assembly)
    {
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plusIndex = info.IndexOf('+');
            return plusIndex >= 0 ? info[..plusIndex] : info;
        }

        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private static string LoadLicenseText()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("SysmacDataTraceViewer.LICENSE");
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }
        catch
        {
            // Ignore and fall back below.
        }

        return "Embedded LICENSE resource was not found.";
    }

    private sealed record LibraryInfo(string Name, string Version, string License, string Notes);
}
