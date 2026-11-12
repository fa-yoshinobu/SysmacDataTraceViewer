using System.IO;
using System.IO.Compression;
using System.Xml;
using SysmacDataTraceViewer.Services;
using SysmacDataTraceViewer.ViewModels;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class SmcCommentImporterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private string ProjectPath => Path.Combine(_directory, "fictional.smc2");

    public SmcCommentImporterTests()
    {
        Directory.CreateDirectory(_directory);
        using var zip = ZipFile.Open(ProjectPath, ZipArchiveMode.Create);
        Write(zip, "p/project.oem", """
            <Root><Entity type="Device" name="Fictional PLC"><ChildEntities>
              <Entity type="Variables" subtype="Global" id="global" />
              <Entity type="Program" name="Demo"><ChildEntities><Entity type="Variables" id="local" /></ChildEntities></Entity>
              <Entity type="FunctionBlock" name="Block"><ChildEntities><Entity type="Variables" id="block" /></ChildEntities></Entity>
              <Entity type="DataType" id="types" />
              <Entity type="Device" name="Unsupported HMI"><ChildEntities><Entity type="Variables" subtype="Global" id="hmi" /></ChildEntities></Entity>
              <Entity type="Device" subtype="NA"><Entity type="Variables" subtype="Global" id="not-a-plc-table" /></Entity>
              <Entity type="Device" subtype="NES"><Entity type="Program" name="Safety"><ChildEntities><Entity type="Variables" id="not-a-plc-table" /></ChildEntities></Entity></Entity>
            </ChildEntities></Entity></Root>
            """);
        Write(zip, "p/global.xml", "[SLWD version=1.0]\n" +
            "++D=ARRAY[0..3] OF BOOL\tN=Flags\tG=VAR_GLOBAL\tCom=配列の親\tEC=<ECs><EC EK=\"[1]\" C=\"要素 &amp; コメント\" /></ECs>\n" +
            "++D=BOOL\tN=Shared\tG=VAR_GLOBAL\tCom=Global$tcomment$$n\n" +
            "++D=BOOL\tN=Empty\tG=VAR_GLOBAL\tCom=\n");
        Write(zip, "p/local.xml", "[SLWD version=1.0]\n" +
            "++D=BOOL\tN=Shared\tG=VAR\tCom=Local comment\n" +
            "++D=DemoStruct\tN=Record\tG=VAR\tCom=構造体の親\n" +
            "++D=Block\tN=Instance\tG=VAR\tCom=FB instance\n" +
            "++D=BOOL\tN=Alias\tG=VAR_EXTERNAL\tCom=Do not import external declarations\n");
        Write(zip, "p/block.xml", "[SLWD version=1.0]\n++D=BOOL\tN=Count\tG=VAR\tCom=Counter comment\n");
        Write(zip, "p/types.xml", "<data><DataType Name=\"DemoStruct\" BaseType=\"STRUCT\"><DataType Name=\"Field\" BaseType=\"BOOL\" Comment=\"Member comment\" /></DataType></data>");
        Write(zip, "p/hmi.xml", "<UnsupportedTable />");
    }

    [Theory]
    [InlineData("Flags[1]:BOOL", "要素 & コメント")]
    [InlineData("Flags[0]:BOOL", "配列の親")]
    [InlineData("Shared:BOOL", "Global\tcomment$n")]
    [InlineData("Demo.Shared:BOOL", "Local comment")]
    [InlineData("Demo.Record.Field:BOOL", "構造体の親 / Member comment")]
    [InlineData("Demo.Instance.Instance.Count:BOOL", "FB instance / Counter comment")]
    public void ResolvesGlobalLocalArrayAndMemberComments(string signal, string expected)
    {
        var result = SmcCommentImporter.Load(ProjectPath, [signal]);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(expected, result.Comments[signal]);
    }

    [Fact]
    public void ReportsMissingAndEmptyCommentsWithoutUsingPartialMemberOrExternalComments()
    {
        var result = SmcCommentImporter.Load(ProjectPath, ["Flags[1]:BOOL", "Empty:BOOL", "Demo.Record.Missing:BOOL", "Demo.Alias:BOOL"]);
        Assert.Equal(2, result.MatchedCount);
        Assert.Equal(1, result.NoCommentCount);
        Assert.Single(result.Comments);
        Assert.Contains("unmatched: 2", result.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAmbiguousDevicesInsteadOfChoosingTheFirst()
    {
        using (var zip = ZipFile.Open(ProjectPath, ZipArchiveMode.Update))
            Write(zip, "p/other.oem", "<Root><Entity type=\"Device\"><Entity type=\"Variables\" subtype=\"Global\" id=\"global\" /></Entity></Root>");
        var error = Assert.Throws<InvalidDataException>(() => SmcCommentImporter.Load(ProjectPath, ["Flags[1]:BOOL"]));
        Assert.Contains("Multiple project devices", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnmatchedProjectsAndSupportsCancellation()
    {
        Assert.Throws<InvalidDataException>(() => SmcCommentImporter.Load(ProjectPath, ["Unknown:BOOL"]));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => SmcCommentImporter.Load(ProjectPath, ["Flags[1]:BOOL"], cancellation.Token));
    }

    [Fact]
    public void RejectsDtdInCommentXml()
    {
        using (var zip = ZipFile.Open(ProjectPath, ZipArchiveMode.Update))
        {
            zip.GetEntry("p/global.xml")!.Delete();
            Write(zip, "p/global.xml", "[SLWD version=1.0]\n++D=BOOL\tN=Flags\tG=VAR_GLOBAL\tEC=<!DOCTYPE ECs [<!ENTITY cell 'private'>]><ECs><EC EK=\"[1]\" C=\"&cell;\" /></ECs>\n");
        }
        Assert.Throws<XmlException>(() => SmcCommentImporter.Load(ProjectPath, ["Flags[1]:BOOL"]));
    }

    [Fact]
    public async Task ImportsThroughCommentsDialogAndKeepsDisplaySettingsAndUnresolvedComments()
    {
        var csv = Path.Combine(_directory, "fictional.csv");
        await File.WriteAllTextAsync(csv, "Index,ClockTime,Flags[1]:BOOL,Unknown:BOOL\n0,00:00:00,0,1\n1,00:00:01,1,0\n").ConfigureAwait(true);
        var vm = new MainViewModel();
        var dialogs = new TestDialogs { Path = ProjectPath };
        var files = new TraceFileOperations(vm, dialogs);
        await files.LoadAsync(csv).ConfigureAwait(true);
        vm.BoolSignals[0].ColorHex = "#112233";
        vm.BoolSignals[0].IsVisible = false;
        vm.BoolSignals[1].CommentText = "Keep existing comment";
        vm.ReorderBoolRows(["Unknown:BOOL", "Flags[1]:BOOL"]);
        await files.LoadCommentsAsync().ConfigureAwait(true);
        Assert.Equal("Unknown:BOOL", vm.BoolSignals[0].Name);
        Assert.Equal("Keep existing comment", vm.BoolSignals[0].CommentText);
        Assert.Equal("要素 & コメント", vm.BoolSignals[1].CommentText);
        Assert.False(vm.BoolSignals[1].IsVisible);
        Assert.Equal("#112233", vm.BoolSignals[1].ColorHex);
        Assert.Contains("smc2 comments: 1/2", vm.StatusText, StringComparison.Ordinal);
        Assert.False(vm.IsLoading);
        Assert.Empty(dialogs.Errors);
        dialogs.Path = ProjectPath + ".missing.smc2";
        await files.LoadCommentsAsync().ConfigureAwait(true);
        Assert.Single(dialogs.Errors);
        Assert.Equal("要素 & コメント", vm.BoolSignals[1].CommentText);
        Assert.False(vm.IsLoading);
        var saved = Path.Combine(_directory, "saved.csv");
        dialogs.Path = saved;
        files.SaveComments();
        Assert.Equal("要素 & コメント", CommentCsvService.Load(saved).BoolSignals[1].Comment);
    }

    private static void Write(ZipArchive archive, string path, string text)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open());
        writer.Write(text);
    }

    public void Dispose() => Directory.Delete(_directory, true);

    private sealed class TestDialogs : IDialogService
    {
        public required string Path { get; set; }
        public List<string> Errors { get; } = [];
        public string? ShowOpenCsvFileDialog() => null;
        public string? ShowOpenCommentsFileDialog() => Path;
        public string? ShowSaveCsvFileDialog(string fileName) => Path;
        public string? ShowSavePngFileDialog(string fileName) => null;
        public void ShowError(string message, string title) => Errors.Add(title);
        public void ShowInformation(string message, string title) { }
    }
}
