using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;

namespace SIL.Machine.Translation.TestApp
{
	public class TaskViewModel<TProgress> : ViewModelBase
	{
		private readonly Func<IProgress<TProgress>, CancellationToken, Task> _execute;
		private readonly Func<TProgress, int> _pcntCompletedSelector;
		private readonly Func<TProgress, string> _msgSelector;
		private readonly AsyncRelayCommand _startTaskCommand;
		private readonly RelayCommand _cancelCommand;
		private CancellationTokenSource _cts;

		private bool _isExecuting;
		private int _percentCompleted;
		private string _message;

		public TaskViewModel(Func<IProgress<TProgress>, CancellationToken, Task> execute, Func<bool> canExecute,
			Func<TProgress, int> pcntCompletedSelector, Func<TProgress, string> msgSelector)
		{
			_execute = execute;
			_pcntCompletedSelector = pcntCompletedSelector;
			_msgSelector = msgSelector;
			_startTaskCommand = new AsyncRelayCommand(ExecuteAsync, canExecute);
			_cancelCommand = new RelayCommand(Cancel, CanCancel);
		}

		private async Task ExecuteAsync()
		{
			using (_cts = new CancellationTokenSource())
			{
				IsExecuting = true;
				_cancelCommand.UpdateCanExecute();
				var progress = new Progress<TProgress>(p =>
					{
						PercentCompleted = _pcntCompletedSelector(p);
						Message = _msgSelector(p);
					});
				CancellationToken token = _cts.Token;
				Task task = null;
				try
				{
					task = _execute(progress, token);
					await task;
				}
				catch
				{
				}

				if (task != null && task.IsFaulted && task.Exception != null)
					throw task.Exception;

				IsExecuting = false;
				_cancelCommand.UpdateCanExecute();
			}
		}

		private bool CanCancel()
		{
			return IsExecuting && !_cts.IsCancellationRequested;
		}

		private void Cancel()
		{
			_cts.Cancel();
			_cancelCommand.UpdateCanExecute();
			Message = "Canceling";
		}

		public int PercentCompleted
		{
			get => _percentCompleted;
			private set => Set(nameof(PercentCompleted), ref _percentCompleted, value);
		}

		public string Message
		{
			get => _message;
			private set => Set(nameof(Message), ref _message, value);
		}

		public bool IsExecuting
		{
			get => _isExecuting;
			private set
			{
				if (Set(nameof(IsExecuting), ref _isExecuting, value))
					RaisePropertyChanged(nameof(IsNotExecuting));
			}
		}

		public bool IsNotExecuting => !_isExecuting;

		public ICommand StartTaskCommand => _startTaskCommand;
		public ICommand CancelCommand => _cancelCommand;

		public void UpdateCanExecute()
		{
			_startTaskCommand.UpdateCanExecute();
		}
	}
}
