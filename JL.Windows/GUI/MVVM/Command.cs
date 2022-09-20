using System.Windows.Input;

namespace JL.Windows.GUI.MVVM;

public class Command : ICommand
{
    private readonly Action _action;

    public Command(Action action)
    {
        _action = action;
    }

    public virtual bool CanExecute(object? parameter)
    {
        return true;
    }

    public virtual event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public virtual void Execute(object? parameter)
    {
        if (CanExecute(null))
        {
            _action();
        }
    }
}
