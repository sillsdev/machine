using System;

namespace SIL.Machine.Utils
{
    public struct ProgressStatus : IEquatable<ProgressStatus>
    {
        public ProgressStatus(int step, int stepCount, string message = null)
            : this(step, stepCount == 0 ? 1.0 : (double)step / stepCount, message) { }

        public ProgressStatus(int step, double? percentCompleted = null, string message = null)
        {
            Step = step;
            PercentCompleted = percentCompleted;
            Message = message;
        }

        public int Step { get; }
        public double? PercentCompleted { get; }
        public string Message { get; }

        public bool Equals(ProgressStatus other)
        {
            return Step == other.Step && PercentCompleted == other.PercentCompleted && Message == other.Message;
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressStatus other && Equals(other);
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + Step.GetHashCode();
            code = code * 31 + (PercentCompleted?.GetHashCode() ?? 0);
            code = code * 31 + (Message?.GetHashCode() ?? 0);
            return code;
        }
    }
}
