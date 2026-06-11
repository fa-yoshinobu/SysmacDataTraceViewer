using System.Windows.Input;

namespace SysmacDataTraceViewer.ViewModels;

internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public static RelayCommand NoOp { get; } = new(static () => { });

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
