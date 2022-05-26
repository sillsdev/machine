using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
    public class ThotLanguageModel : DisposableBase
    {
        private readonly IntPtr _handle;

        public ThotLanguageModel(string lmPrefix)
        {
            _handle = Thot.langModel_open(lmPrefix);
        }

        public double GetSegmentLog10Probability(IReadOnlyList<string> segment)
        {
            IntPtr nativeSegment = Thot.ConvertSegmentToNativeUtf8(segment);
            try
            {
                return Thot.langModel_getSentenceProbability(_handle, nativeSegment);
            }
            finally
            {
                Marshal.FreeHGlobal(nativeSegment);
            }
        }

        protected override void DisposeUnmanagedResources()
        {
            Thot.langModel_close(_handle);
        }
    }
}
