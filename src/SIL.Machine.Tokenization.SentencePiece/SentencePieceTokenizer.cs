using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.Machine.Annotations;
using SIL.ObjectModel;

namespace SIL.Machine.Tokenization.SentencePiece
{
    public class SentencePieceTokenizer : DisposableBase, ITokenizer<string, int, string>
    {
        private readonly IntPtr _processorHandle;

        public SentencePieceTokenizer(string modelFilename)
        {
            _processorHandle = SentencePieceApi.sp_createProcessor();
            StatusCode code = SentencePieceApi.sp_loadProcessor(_processorHandle, modelFilename);
            if (code != StatusCode.Ok)
                throw new InvalidOperationException($"Error occurred while loading processor, code: {code}.");
        }

        public IEnumerable<string> Tokenize(string data)
        {
            return Tokenize(data, Range<int>.Create(0, data.Length));
        }

        public IEnumerable<string> Tokenize(string data, Range<int> range)
        {
            IntPtr inputPtr = SentencePieceApi.ConvertStringToNativeUtf8(data.Substring(range.Start, range.Length));
            uint capacity = (uint)range.Length * 2;
            IntPtr outputPtr = Marshal.AllocHGlobal((int)capacity);
            try
            {
                StatusCode code = SentencePieceApi.sp_encodeAsPieces(
                    _processorHandle,
                    inputPtr,
                    outputPtr,
                    capacity,
                    out uint length
                );
                if (code != StatusCode.Ok)
                    throw new InvalidOperationException($"Error occurred while encoding, code: {code}.");
                if (length > capacity)
                {
                    capacity = length;
                    outputPtr = Marshal.ReAllocHGlobal(outputPtr, (IntPtr)capacity);
                    code = SentencePieceApi.sp_encodeAsPieces(
                        _processorHandle,
                        inputPtr,
                        outputPtr,
                        capacity,
                        out length
                    );
                    if (code != StatusCode.Ok)
                        throw new InvalidOperationException($"Error occurred while encoding, code: {code}.");
                }

                string output = SentencePieceApi.ConvertNativeUtf8ToString(outputPtr, length);
                if (output.Length == 0)
                    return Enumerable.Empty<string>();
                return output.Split();
            }
            finally
            {
                Marshal.FreeHGlobal(outputPtr);
                Marshal.FreeHGlobal(inputPtr);
            }
        }

        protected override void DisposeUnmanagedResources()
        {
            SentencePieceApi.sp_destroyProcessor(_processorHandle);
        }
    }
}
