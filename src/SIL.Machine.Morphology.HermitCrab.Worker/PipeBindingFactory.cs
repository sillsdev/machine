using System;
using System.ServiceModel;

namespace SIL.Machine.Morphology.HermitCrab.Worker
{
    /// <summary>
    /// Shared net.pipe binding config for both sides of the worker channel (this project's own
    /// ServiceHost in Program.cs, and any client in-process here like the bench harness). The
    /// FieldWorks-side client (HCWorkerClient.cs, a separate repo/build) keeps its own duplicate
    /// of these exact values in sync by hand - see the comment there.
    /// </summary>
    public static class PipeBindingFactory
    {
        // Real grammars can be several MB once serialized to the HC.NET XML input format (Sena is
        // ~1.4 MB) - discovered by actually running the worker against it (the unit tests only use
        // tiny synthetic grammars, so this limit never got exercised there). NetNamedPipeBinding's
        // 64 KB default, even with LexicalProviderManager.cs's *4 multiplier (fine for whatever
        // that consumes), is nowhere near enough and fails with a low-level "pipe is being closed"
        // error rather than a clear quota-exceeded one. Size generously since grammars only grow
        // as FieldWorks projects grow.
        private const long MaxMessageSize = 256L * 1024 * 1024;

        public static NetNamedPipeBinding Create()
        {
            var pipeBinding = new NetNamedPipeBinding();
            pipeBinding.Security.Mode = NetNamedPipeSecurityMode.None;
            pipeBinding.MaxBufferSize = ClampToInt(MaxMessageSize);
            pipeBinding.MaxReceivedMessageSize = MaxMessageSize;
            pipeBinding.MaxBufferPoolSize = MaxMessageSize;
            pipeBinding.ReaderQuotas.MaxArrayLength = ClampToInt(MaxMessageSize);
            pipeBinding.ReaderQuotas.MaxStringContentLength = ClampToInt(MaxMessageSize);
            pipeBinding.ReaderQuotas.MaxBytesPerRead = 65536;
            pipeBinding.ReaderQuotas.MaxDepth = 64;
            pipeBinding.ReaderQuotas.MaxNameTableCharCount = 65536;
            pipeBinding.SendTimeout = TimeSpan.FromMinutes(10);
            pipeBinding.ReceiveTimeout = TimeSpan.FromMinutes(10);
            return pipeBinding;
        }

        private static int ClampToInt(long value) => (int)Math.Min(value, int.MaxValue);
    }
}
