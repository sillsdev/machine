using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIL.Machine.Translation.TestApp
{
	public class AsyncRelayCommand : AsyncRelayCommand<object>
	{
		public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
			: base(o => execute(), canExecute == null ? (Func<object, bool>) null : o => canExecute())
		{
		}
	}

	public class AsyncRelayCommand<T> : ICommand
	{
		private readonly Func<T, Task> _execute;
		private readonly Func<T, bool> _canExecute;

		public event EventHandler CanExecuteChanged;

		public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool> canExecute = null)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute((T) parameter);
		}

		public async void Execute(object parameter)
		{
			await ExecuteAsync(parameter);
		}

		public Task ExecuteAsync(object parameter)
		{
			return _execute((T) parameter);
		}

		public void UpdateCanExecute()
		{
			OnCanExecuteChanged(EventArgs.Empty);
		}

		protected virtual void OnCanExecuteChanged(EventArgs e)
		{
			CanExecuteChanged?.Invoke(this, e);
		}
	}
}
