using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SIL.Machine.Translation
{
	/// <summary>
	/// An ASCII progress bar
	/// </summary>
	public class ConsoleProgressBar : ConsoleProgressBar<double>
	{
		public ConsoleProgressBar(TextWriter outWriter)
			: base(outWriter, value => value)
		{
		}
	}

	public class ConsoleProgressBar<T> : IDisposable, IProgress<T>
	{
		private const int BlockCount = 10;
		private static readonly TimeSpan AnimationInterval = TimeSpan.FromSeconds(1.0 / 8);
		private const string Animation = @"|/-\";

		private readonly Timer _timer;
		private readonly TextWriter _outWriter;
		private readonly Func<T, double> _percentCompletedSelector;

		private double _currentProgress;
		private string _currentText = string.Empty;
		private bool _disposed;
		private int _animationIndex;

		public ConsoleProgressBar(TextWriter outWriter, Func<T, double> percentCompletedSelector)
		{
			_outWriter = outWriter;
			_percentCompletedSelector = percentCompletedSelector;
			_timer = new Timer(TimerHandler, null, Timeout.Infinite, Timeout.Infinite);
			ResetTimer();
		}

		public void Report(T value)
		{
			double percentCompleted = _percentCompletedSelector(value);
			// Make sure value is in [0..1] range
			percentCompleted = Math.Max(0, Math.Min(1, percentCompleted));
			Interlocked.Exchange(ref _currentProgress, percentCompleted);
		}

		private void TimerHandler(object state)
		{
			lock (_timer)
			{
				if (_disposed)
					return;

				int progressBlockCount = (int) (_currentProgress * BlockCount);
				int percent = (int) (_currentProgress * 100);
				string text = string.Format("[{0}{1}] {2,3}% {3}",
					new string('#', progressBlockCount), new string('-', BlockCount - progressBlockCount),
					percent,
					Animation[_animationIndex++ % Animation.Length]);
				UpdateText(text);

				ResetTimer();
			}
		}

		private void UpdateText(string text)
		{
			// Get length of common portion
			int commonPrefixLength = 0;
			int commonLength = Math.Min(_currentText.Length, text.Length);
			while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength])
				commonPrefixLength++;

			// Backtrack to the first differing character
			StringBuilder outputBuilder = new StringBuilder();
			outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

			// Output new suffix
			outputBuilder.Append(text.Substring(commonPrefixLength));

			// If the new text is shorter than the old one: delete overlapping characters
			int overlapCount = _currentText.Length - text.Length;
			if (overlapCount > 0)
			{
				outputBuilder.Append(' ', overlapCount);
				outputBuilder.Append('\b', overlapCount);
			}

			_outWriter.Write(outputBuilder);
			_currentText = text;
		}

		private void ResetTimer()
		{
			_timer.Change(AnimationInterval, TimeSpan.FromMilliseconds(-1));
		}

		public void Dispose()
		{
			lock (_timer)
			{
				_disposed = true;
				UpdateText(string.Empty);
			}
		}

	}
}
