using System;
using System.Collections.Generic;
using System.Threading;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>Captures runtime evidence for semantic paths without coupling the engine to the test harness.</summary>
    public static class SemanticBranch
    {
        private static readonly AsyncLocal<ISet<string>> Listener = new AsyncLocal<ISet<string>>();

        public static IDisposable BeginCapture(ISet<string> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            ISet<string> prior = Listener.Value;
            Listener.Value = destination;
            return new Capture(prior);
        }

        public static void Hit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("A semantic branch ID is required.", nameof(id));
            }

            ISet<string> listener = Listener.Value;
            if (listener != null)
            {
                listener.Add(id);
            }
        }

        private sealed class Capture : IDisposable
        {
            private readonly ISet<string> _prior;
            private bool _disposed;

            public Capture(ISet<string> prior)
            {
                _prior = prior;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Listener.Value = _prior;
            }
        }
    }
}