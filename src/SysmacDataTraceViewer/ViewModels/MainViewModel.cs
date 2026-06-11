using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SysmacDataTraceViewer.ViewModels;

internal sealed class MainViewModel : INotifyPropertyChanged
{
    private string _statusText = "Load a CSV file to display BOOL timeline and value variables.";
    private string _cursorTimeText = "-";
    private string _cursorClockText = "-";
    private string _cursorSampleText = "-";
    private string _cursorDeltaText = "-";
    private string _hoverStateText = "-";
    private string _hoverDurationText = "-";
    private string _boolPanelToggleText = "Hide Right Panel";
    private string _bottomPanelToggleText = "Hide Variable Settings";
    private bool _isSelectedBoolJumpMode;
    private bool _showTypeSuffix;
    private bool _showRangeBand;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string CursorTimeText
    {
        get => _cursorTimeText;
        set => SetProperty(ref _cursorTimeText, value);
    }

    public string CursorClockText
    {
        get => _cursorClockText;
        set => SetProperty(ref _cursorClockText, value);
    }

    public string CursorSampleText
    {
        get => _cursorSampleText;
        set => SetProperty(ref _cursorSampleText, value);
    }

    public string CursorDeltaText
    {
        get => _cursorDeltaText;
        set => SetProperty(ref _cursorDeltaText, value);
    }

    public string HoverStateText
    {
        get => _hoverStateText;
        set => SetProperty(ref _hoverStateText, value);
    }

    public string HoverDurationText
    {
        get => _hoverDurationText;
        set => SetProperty(ref _hoverDurationText, value);
    }

    public bool IsSelectedBoolJumpMode
    {
        get => _isSelectedBoolJumpMode;
        set => SetProperty(ref _isSelectedBoolJumpMode, value);
    }

    public bool ShowTypeSuffix
    {
        get => _showTypeSuffix;
        set => SetProperty(ref _showTypeSuffix, value);
    }

    public bool ShowRangeBand
    {
        get => _showRangeBand;
        set => SetProperty(ref _showRangeBand, value);
    }

    public string BoolPanelToggleText
    {
        get => _boolPanelToggleText;
        set => SetProperty(ref _boolPanelToggleText, value);
    }

    public string BottomPanelToggleText
    {
        get => _bottomPanelToggleText;
        set => SetProperty(ref _bottomPanelToggleText, value);
    }

    public ObservableCollection<ValueSignalRow> ValueSignals { get; } = new();
    public ObservableCollection<ValueSignalRow> VisibleValueSignals { get; } = new();
    public ObservableCollection<BoolSignalRow> BoolSignals { get; } = new();

    public ICommand OpenTraceCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand ExportVisiblePngCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand ExportFullPngCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand LoadCommentsCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand SaveCommentsCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand CloseCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand AboutCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand ToggleBoolPanelCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand ToggleBottomPanelCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand SwapCursorsCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand JumpPrevChangeCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand JumpNextChangeCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand SelectAllBoolCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand ClearAllBoolCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand HideNoChangeBoolCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand AutoBoolColorsCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand SelectAllValueCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand ClearAllValueCommand { get; private set; } = RelayCommand.NoOp;
    public ICommand HideNoChangeValueCommand { get; private set; } = RelayCommand.NoOp;

    public void ConfigureCommands(MainViewModelCommands commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        OpenTraceCommand = new RelayCommand(commands.OpenTrace);
        ExportVisiblePngCommand = new RelayCommand(commands.ExportVisiblePng);
        ExportFullPngCommand = new RelayCommand(commands.ExportFullPng);
        LoadCommentsCommand = new RelayCommand(commands.LoadComments);
        SaveCommentsCommand = new RelayCommand(commands.SaveComments);
        CloseCommand = new RelayCommand(commands.Close);
        AboutCommand = new RelayCommand(commands.About);
        ToggleBoolPanelCommand = new RelayCommand(commands.ToggleBoolPanel);
        ToggleBottomPanelCommand = new RelayCommand(commands.ToggleBottomPanel);
        SwapCursorsCommand = new RelayCommand(commands.SwapCursors);
        JumpPrevChangeCommand = new RelayCommand(commands.JumpPrevChange);
        JumpNextChangeCommand = new RelayCommand(commands.JumpNextChange);
        SelectAllBoolCommand = new RelayCommand(commands.SelectAllBool);
        ClearAllBoolCommand = new RelayCommand(commands.ClearAllBool);
        HideNoChangeBoolCommand = new RelayCommand(commands.HideNoChangeBool);
        AutoBoolColorsCommand = new RelayCommand(commands.AutoBoolColors);
        SelectAllValueCommand = new RelayCommand(commands.SelectAllValue);
        ClearAllValueCommand = new RelayCommand(commands.ClearAllValue);
        HideNoChangeValueCommand = new RelayCommand(commands.HideNoChangeValue);

        OnPropertyChanged(nameof(OpenTraceCommand));
        OnPropertyChanged(nameof(ExportVisiblePngCommand));
        OnPropertyChanged(nameof(ExportFullPngCommand));
        OnPropertyChanged(nameof(LoadCommentsCommand));
        OnPropertyChanged(nameof(SaveCommentsCommand));
        OnPropertyChanged(nameof(CloseCommand));
        OnPropertyChanged(nameof(AboutCommand));
        OnPropertyChanged(nameof(ToggleBoolPanelCommand));
        OnPropertyChanged(nameof(ToggleBottomPanelCommand));
        OnPropertyChanged(nameof(SwapCursorsCommand));
        OnPropertyChanged(nameof(JumpPrevChangeCommand));
        OnPropertyChanged(nameof(JumpNextChangeCommand));
        OnPropertyChanged(nameof(SelectAllBoolCommand));
        OnPropertyChanged(nameof(ClearAllBoolCommand));
        OnPropertyChanged(nameof(HideNoChangeBoolCommand));
        OnPropertyChanged(nameof(AutoBoolColorsCommand));
        OnPropertyChanged(nameof(SelectAllValueCommand));
        OnPropertyChanged(nameof(ClearAllValueCommand));
        OnPropertyChanged(nameof(HideNoChangeValueCommand));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class MainViewModelCommands
{
    public required Action OpenTrace { get; init; }
    public required Action ExportVisiblePng { get; init; }
    public required Action ExportFullPng { get; init; }
    public required Action LoadComments { get; init; }
    public required Action SaveComments { get; init; }
    public required Action Close { get; init; }
    public required Action About { get; init; }
    public required Action ToggleBoolPanel { get; init; }
    public required Action ToggleBottomPanel { get; init; }
    public required Action SwapCursors { get; init; }
    public required Action JumpPrevChange { get; init; }
    public required Action JumpNextChange { get; init; }
    public required Action SelectAllBool { get; init; }
    public required Action ClearAllBool { get; init; }
    public required Action HideNoChangeBool { get; init; }
    public required Action AutoBoolColors { get; init; }
    public required Action SelectAllValue { get; init; }
    public required Action ClearAllValue { get; init; }
    public required Action HideNoChangeValue { get; init; }
}

internal sealed class ValueSignalRow : INotifyPropertyChanged
{
    private string _valueText = "-";
    private bool _isVisible = true;
    private string _commentText = string.Empty;
    private string _displayLabel = string.Empty;
    private string _settingsDisplayName = string.Empty;

    public required string Name { get; init; }
    public required int SignalIndex { get; init; }
    public bool IsUnchanged { get; init; }
    public string CommentText
    {
        get => _commentText;
        set
        {
            if (_commentText == value)
            {
                return;
            }

            _commentText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CommentText)));
        }
    }

    public string DisplayLabel
    {
        get => _displayLabel;
        set
        {
            if (_displayLabel == value)
            {
                return;
            }

            _displayLabel = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLabel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
        }
    }

    public string SettingsDisplayName
    {
        get => _settingsDisplayName;
        set
        {
            if (_settingsDisplayName == value)
            {
                return;
            }

            _settingsDisplayName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SettingsDisplayName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SettingsDisplayNameWithState)));
        }
    }

    public string SettingsDisplayNameWithState => IsUnchanged ? $"{SettingsDisplayName} [No Change]" : SettingsDisplayName;

    public string DisplayName => IsUnchanged ? $"{DisplayLabel} [No Change]" : DisplayLabel;

    public string ValueText
    {
        get => _valueText;
        set
        {
            if (_valueText == value)
            {
                return;
            }

            _valueText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueText)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class BoolSignalRow : INotifyPropertyChanged
{
    private bool _isVisible = true;
    private string _colorHex = "#1E90FF";
    private string _commentText = string.Empty;
    private string _displayLabel = string.Empty;

    public required int Index { get; init; }
    public required string Name { get; init; }
    public string CommentText
    {
        get => _commentText;
        set
        {
            if (_commentText == value)
            {
                return;
            }

            _commentText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CommentText)));
        }
    }

    public string DisplayLabel
    {
        get => _displayLabel;
        set
        {
            if (_displayLabel == value)
            {
                return;
            }

            _displayLabel = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLabel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
        }
    }

    public string DisplayName => IsUnchanged ? $"{DisplayLabel} [No Change]" : DisplayLabel;
    public bool IsUnchanged { get; init; }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
        }
    }

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            if (_colorHex == value)
            {
                return;
            }

            _colorHex = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorHex)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
