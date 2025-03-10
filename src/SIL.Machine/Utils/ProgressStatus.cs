using System;

namespace SIL.Machine.Utils
{
    public struct ProgressStatus : IEquatable<ProgressStatus>
    {
        public ProgressStatus(int step, int stepCount, string message = null)
            : this(step, stepCount == 0 ? (double?)1.0 : (double)step / stepCount, null, null, message) { }

        public ProgressStatus(
            int step,
            double? percentCompleted = null,
            double? fineTuneProgress = null,
            double? inferenceProgress = null,
            string message = null
        )
        {
            Step = step;
            PercentCompleted = percentCompleted ?? 0.0;
            FineTuneProgress = fineTuneProgress ?? 0.0;
            InferenceProgress = inferenceProgress ?? 0.0;
            Message = message ?? string.Empty;
        }

        public int Step { get; }
        public double? PercentCompleted { get; }
        public double? FineTuneProgress { get; }
        public double? InferenceProgress { get; }
        public string Message { get; }

        public bool Equals(ProgressStatus other)
        {
            return Step == other.Step
                && PercentCompleted == other.PercentCompleted
                && FineTuneProgress == other.FineTuneProgress
                && InferenceProgress == other.InferenceProgress
                && Message == other.Message;
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressStatus other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 31 + Step.GetHashCode();
            hash = hash * 31 + (PercentCompleted?.GetHashCode() ?? 0);
            hash = hash * 31 + (FineTuneProgress?.GetHashCode() ?? 0);
            hash = hash * 31 + (InferenceProgress?.GetHashCode() ?? 0);
            hash = hash * 31 + (Message?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
