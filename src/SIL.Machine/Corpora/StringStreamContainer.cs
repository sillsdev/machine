using System.IO;
using SIL.ObjectModel;

namespace SIL.Machine.Corpora
{
    public class StringStreamContainer : DisposableBase, IStreamContainer
    {
        private readonly string _content;

        public StringStreamContainer(string content)
        {
            _content = content;
        }

        public Stream OpenStream()
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(_content);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
