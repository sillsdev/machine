using System.IO;
using System.Text;
using SIL.ObjectModel;

namespace SIL.Machine.Corpora
{
    public class MemoryStreamContainer : DisposableBase, IStreamContainer
    {
        private readonly string _usfm;

        public MemoryStreamContainer(string usfm)
        {
            _usfm = usfm;
        }

        public Stream OpenStream()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(_usfm));
        }
    }
}
