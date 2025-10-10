using System.IO;

namespace SIL.Machine.Corpora
{
    public interface IParatextProjectFileHandler
    {
        bool Exists(string fileName);
        Stream Open(string fileName);
        ParatextProjectSettings GetSettings();
    }
}
