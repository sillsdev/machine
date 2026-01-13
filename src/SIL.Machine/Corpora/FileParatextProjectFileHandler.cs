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
            return Directory
                .EnumerateFiles(_projectDir)
                .Any(f => Path.GetFileName(f).Equals(fileName, System.StringComparison.InvariantCultureIgnoreCase));
        }

        public Stream Open(string fileName)
        {
            return File.OpenRead(
                Path.Combine(
                    _projectDir,
                    Directory
                        .EnumerateFiles(_projectDir)
                        .FirstOrDefault(f =>
                            Path.GetFileName(f).Equals(fileName, System.StringComparison.InvariantCultureIgnoreCase)
                        )
                )
            );
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
