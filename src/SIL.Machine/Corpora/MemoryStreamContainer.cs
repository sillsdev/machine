using System.IO;
using System.Text;
using SIL.ObjectModel;

namespace SIL.Machine.Corpora
{
    public class MemoryStreamContainer : DisposableBase, IStreamContainer
    {
        private readonly string _usfm;
        private readonly Encoding _encoding;

        public MemoryStreamContainer(string usfm, Encoding encoding)
        {
            _usfm = usfm;
            _encoding = encoding;
        }

        public Stream OpenStream()
        {
            return new MemoryStream(_encoding.GetBytes(_usfm));
        }
    }
}
