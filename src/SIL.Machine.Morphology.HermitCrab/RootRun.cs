namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>A root found as a run of segments [Start, End) of an analysis shape.</summary>
    internal sealed class RootRun
    {
        public RootRun(int start, int end)
        {
            Start = start;
            End = end;
        }

        /// <summary>Stands for no usable location: nothing lies inside it.</summary>
        public static readonly RootRun None = new RootRun(-1, -1);

        public int Start { get; }
        public int End { get; }
    }
}
