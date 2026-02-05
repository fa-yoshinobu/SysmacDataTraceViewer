using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SysmacDataTraceViewer.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _statusText = "Load a CSV file to display BOOL timeline and value variables.";
    private string _cursorTimeText = "-";
    private string _cursorClockText = "-";
    private string _cursorSampleText = "-";
    private string _cursorDeltaText = "-";
    private string _hoverStateText = "-";
    private string _hoverDurationText = "-";
    private bool _isSelectedBoolJumpMode;

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

    public ObservableCollection<ValueSignalRow> ValueSignals { get; } = new();
    public ObservableCollection<ValueSignalRow> VisibleValueSignals { get; } = new();
    public ObservableCollection<BoolSignalRow> BoolSignals { get; } = new();

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ValueSignalRow : INotifyPropertyChanged
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

public sealed class BoolSignalRow : INotifyPropertyChanged
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
