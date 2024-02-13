using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SIL.Machine.Tokenization.SentencePiece
{
    internal enum StatusCode
    {
        Ok = 0,
        Cancelled = 1,
        Unknown = 2,
        InvalidArgument = 3,
        DeadlineExceeded = 4,
        NotFound = 5,
        AlreadyExists = 6,
        PermissionDenied = 7,
        ResourceExhausted = 8,
        FailedPrecondition = 9,
        Aborted = 10,
        OutOfRange = 11,
        Unimplemented = 12,
        Internal = 13,
        Unavailable = 14,
        DataLoss = 15,
        Unauthenticated = 16
    }

    internal static class SentencePieceApi
    {
        [DllImport("sentencepiece4c", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sp_createProcessor();

        [DllImport("sentencepiece4c", CallingConvention = CallingConvention.Cdecl)]
        public static extern StatusCode sp_loadProcessor(IntPtr processorHandle, string filename);

        [DllImport("sentencepiece4c", CallingConvention = CallingConvention.Cdecl)]
        public static extern StatusCode sp_encodeAsPieces(
            IntPtr processorHandle,
            IntPtr input,
            IntPtr output,
            uint capacity,
            out uint length
        );

        [DllImport("sentencepiece4c", CallingConvention = CallingConvention.Cdecl)]
        public static extern void sp_destroyProcessor(IntPtr processorHandle);

        [DllImport("sentencepiece4c", CallingConvention = CallingConvention.Cdecl)]
        public static extern StatusCode sp_train(string inputFilenames, string modelPrefix, string args);

        public static IntPtr ConvertStringToNativeUtf8(string managedString)
        {
            int len = Encoding.UTF8.GetByteCount(managedString);
            byte[] buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }

        public static string ConvertNativeUtf8ToString(IntPtr nativeUtf8, uint len)
        {
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }
    }
}
