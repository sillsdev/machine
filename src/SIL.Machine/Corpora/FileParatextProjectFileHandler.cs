using System.IO;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class FileParatextProjectFileHandler : IParatextProjectFileHandler
    {
        private readonly string _projectDir;

        public FileParatextProjectFileHandler(string projectDir)
        {
            _projectDir = projectDir;
        }

        public bool Exists(string fileName)
        {
            return File.Exists(Path.Combine(_projectDir, fileName));
        }

        public Stream Open(string fileName)
        {
            return File.OpenRead(Path.Combine(_projectDir, fileName));
        }

        public UsfmStylesheet CreateStylesheet(string fileName)
        {
            string customStylesheetFileName = Path.Combine(_projectDir, "custom.sty");
            return new UsfmStylesheet(
                fileName,
                File.Exists(customStylesheetFileName) ? customStylesheetFileName : null
            );
        }

        public string Find(string extension)
        {
            return Directory.EnumerateFiles(_projectDir, "*" + extension).FirstOrDefault();
        }
    }
}
