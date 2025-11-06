using System.IO;

namespace SIL.Machine.Corpora
{
    public interface IParatextProjectFileHandler
    {
        bool Exists(string fileName);
        Stream Open(string fileName);
        string Find(string extension);
        UsfmStylesheet CreateStylesheet(string fileName);
    }
}
