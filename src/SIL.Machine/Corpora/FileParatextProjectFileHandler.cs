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
            return GetFileName(fileName) != null;
        }

        public Stream Open(string fileName)
        {
            fileName = GetFileName(fileName) ?? fileName;
            return File.OpenRead(Path.Combine(_projectDir, fileName));
        }

        public UsfmStylesheet CreateStylesheet(string fileName)
        {
            string customStylesheetFileName = GetFileName("custom.sty");
            return new UsfmStylesheet(
                fileName,
                customStylesheetFileName != null ? Path.Combine(_projectDir, customStylesheetFileName) : null
            );
        }

        public string Find(string extension)
        {
            return Directory.EnumerateFiles(_projectDir, "*" + extension).FirstOrDefault();
        }

        private string GetFileName(string caseInsensitiveFileName)
        {
            return Directory
                .EnumerateFiles(_projectDir)
                .Select(p => Path.GetFileName(p))
                .FirstOrDefault(f =>
                    f.Equals(caseInsensitiveFileName, System.StringComparison.InvariantCultureIgnoreCase)
                );
        }
    }
}
