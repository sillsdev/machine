using System;
using System.IO;

namespace SIL.Machine.Corpora
{
    public interface IStreamContainer : IDisposable
    {
        Stream OpenStream();
    }
}
