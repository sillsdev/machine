namespace SIL.Machine.FiniteState
{
    /// <summary>
    /// Registers are conceptually a <c>registerCount x 2</c> table (register index, start-or-end), but are stored
    /// as a flat 1-D array to avoid the slow general-purpose multi-dimensional array allocator
    /// (<see cref="System.Array.CreateInstance(System.Type, int[])"/>) that <c>Register&lt;TOffset&gt;[,]</c> goes
    /// through on every allocation.
    /// </summary>
    internal static class RegisterArray
    {
        public const int Start = 0;
        public const int End = 1;

        public static int Idx(int registerIndex, int startOrEnd) => (registerIndex * 2) + startOrEnd;
    }
}
