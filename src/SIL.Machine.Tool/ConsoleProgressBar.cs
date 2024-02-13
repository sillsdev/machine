using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using SIL.Machine.Utils;

namespace SIL.Machine;

/// <summary>
/// An ASCII progress bar
/// </summary>
public class ConsoleProgressBar : IDisposable, IProgress<ProgressStatus>
{
    private const int BlockCount = 10;
    private static readonly TimeSpan s_animationInterval = TimeSpan.FromSeconds(1.0 / 8);
    private const string Animation = @"|/-\";

    private readonly Timer _timer;
    private readonly TextWriter _outWriter;

    private double _currentProgress;
    private string _currentMessage = string.Empty;
    private string _currentText = string.Empty;
    private bool _disposed;
    private int _animationIndex;

    public ConsoleProgressBar(TextWriter outWriter)
    {
        _outWriter = outWriter;
        _timer = new Timer(TimerHandler, null, Timeout.Infinite, Timeout.Infinite);
        ResetTimer();
    }

    public void Report(ProgressStatus value)
    {
        // Make sure value is in [0..1] range
        double percentCompleted = Math.Max(0, Math.Min(1, value.PercentCompleted ?? 0));
        Interlocked.Exchange(ref _currentProgress, percentCompleted);
        _currentMessage = value.Message;
    }

    private void TimerHandler(object state)
    {
        lock (_timer)
        {
            if (_disposed)
                return;

            int progressBlockCount = (int)(_currentProgress * BlockCount);
            int percent = (int)Math.Round(_currentProgress * 100, MidpointRounding.AwayFromZero);
            string text = string.Format(
                CultureInfo.InvariantCulture,
                "[{0}{1}] {2,3}% {3} {4}",
                new string('#', progressBlockCount),
                new string('-', BlockCount - progressBlockCount),
                percent,
                Animation[_animationIndex++ % Animation.Length],
                _currentMessage
            );
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
        var outputBuilder = new StringBuilder();
        outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

        // Output new suffix
        outputBuilder.Append(text.AsSpan(commonPrefixLength));

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
        _timer.Change(s_animationInterval, TimeSpan.FromMilliseconds(-1));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        lock (_timer)
        {
            _disposed = true;
            UpdateText(string.Empty);
        }
    }
}
