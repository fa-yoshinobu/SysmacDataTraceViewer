using System.IO;
using SysmacDataTraceViewer.Services;
using SysmacDataTraceViewer.ViewModels;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class TraceFileOperationsTests
{
    [Fact]
    public async Task LoadsAndInitializesTraceAndKeepsPreviousDataWhenNextLoadFails()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "Index,ClockTime,FictionalFlag:BOOL,FictionalValue:INT\n0,00:00:00,0,1\n1,00:00:01,1,2\n").ConfigureAwait(true);
            var vm = new MainViewModel();
            var dialogs = new TestDialogs { OpenPath = path };
            var files = new TraceFileOperations(vm, dialogs);
            var loaded = 0;
            files.TraceLoaded += () => { loaded++; vm.CursorDeltaText = "rendered"; };
            await files.OpenAsync().ConfigureAwait(true);
            var trace = vm.TraceData;
            Assert.NotNull(trace);
            Assert.Single(vm.BoolSignals);
            Assert.Single(vm.VisibleValueSignals);
            Assert.Equal("-", vm.CursorSampleText);
            Assert.Equal("-", vm.CursorDeltaText);
            Assert.False(vm.IsLoading);
            Assert.Equal(1, loaded);
            await files.LoadAsync(path + ".missing").ConfigureAwait(true);
            Assert.Same(trace, vm.TraceData);
            Assert.False(vm.IsLoading);
            Assert.Single(dialogs.Errors);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task AutoLoadsCommentsAndRestoresSavedOrderVisibilityAndColors()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "fictional.csv");
            var comments = Path.Combine(directory, "fictional_comments.csv");
            await File.WriteAllTextAsync(path, "Index,ClockTime,A:BOOL,B:BOOL,V:INT\n0,00:00:00,0,1,2\n1,00:00:01,1,0,3\n").ConfigureAwait(true);
            CommentCsvService.Save(comments,
                [new("A:BOOL", "架空A", false, "#112233", 1), new("B:BOOL", "架空B", true, "#445566", 0)],
                [new("V:INT", "架空値", true, "", 0)]);
            var vm = new MainViewModel();
            var dialogs = new TestDialogs { SavePath = comments };
            var files = new TraceFileOperations(vm, dialogs);
            await files.LoadAsync(path).ConfigureAwait(true);
            Assert.Equal(["B:BOOL", "A:BOOL"], vm.BoolSignals.Select(row => row.Name));
            Assert.False(vm.BoolSignals[1].IsVisible);
            Assert.Equal("#112233", vm.BoolSignals[1].ColorHex);
            vm.BoolSignals[0].CommentText = "更新した架空コメント";
            files.SaveComments();
            vm.ReorderBoolRows(["A:BOOL", "B:BOOL"]);
            dialogs.OpenPath = comments;
            files.LoadComments();
            Assert.Equal("B:BOOL", vm.BoolSignals[0].Name);
            Assert.Equal("更新した架空コメント", vm.BoolSignals[0].CommentText);
            Assert.Empty(dialogs.Errors);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void TogglesPanelStateWithoutChangingOtherDisplayOptions()
    {
        var vm = new MainViewModel();
        vm.ToggleBoolPanel();
        vm.ToggleBottomPanel();
        Assert.False(vm.IsBoolPanelVisible);
        Assert.False(vm.IsBottomPanelVisible);
        Assert.Equal("Show Right Panel", vm.BoolPanelToggleText);
        Assert.Equal("Show Variable Settings", vm.BottomPanelToggleText);
        vm.ToggleBoolPanel();
        vm.ToggleBottomPanel();
        Assert.True(vm.IsBoolPanelVisible);
        Assert.True(vm.IsBottomPanelVisible);
        Assert.False(vm.ShowTypeSuffix);
    }

    private sealed class TestDialogs : IDialogService
    {
        public string? OpenPath { get; set; }
        public string? SavePath { get; set; }
        public List<string> Errors { get; } = [];
        public string? ShowOpenCsvFileDialog() => OpenPath;
        public string? ShowSaveCsvFileDialog(string suggestedName) => SavePath;
        public string? ShowSavePngFileDialog(string suggestedName) => null;
        public void ShowError(string message, string title) => Errors.Add(title);
        public void ShowInformation(string message, string title) { }
    }
}
