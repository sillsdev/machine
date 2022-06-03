using System;

namespace SIL.Machine.Translation.Thot
{
    internal interface IThotSmtModelInternal
    {
        IntPtr Handle { get; }
        ThotSmtParameters Parameters { get; }
        IWordAligner WordAligner { get; }
        SymmetrizedWordAlignmentModel SymmetrizedWordAlignmentModel { get; }

        void RemoveEngine(ThotSmtEngine engine);
    }
}
