using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Laboratory8;

public class AsyncRelayCommand : ICommand
{
    readonly Action<object?> execute;
    readonly Predicate<object?>? canExecute;
    private CancellationTokenSource? cancellationTokenSource;

    /// <summary>
    /// Creates a new command.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    public AsyncRelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    [DebuggerStepThrough]
    public bool CanExecute(object? parameters)
    {
        return canExecute?.Invoke(parameters) ?? true;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public async void Execute(object? parameters)
    {
        try
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Token.WaitHandle.WaitOne();
            }

            cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() => { execute(parameters); }, cancellationTokenSource.Token);
        }
        catch
        {
            // ignored
        }
    }
}