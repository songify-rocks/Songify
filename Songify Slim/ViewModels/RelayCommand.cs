using System;
using System.Windows.Input;

namespace Songify_Slim.ViewModels;

/// <summary>
/// A simple ICommand implementation for MVVM bindings.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
        : this(p => execute(), canExecute != null ? p => canExecute() : (Predicate<object>)null)
    {
    }

    public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public static void InvalidateRequerySuggested()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}