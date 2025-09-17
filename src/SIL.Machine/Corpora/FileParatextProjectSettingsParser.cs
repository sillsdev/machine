using System.IO;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class FileParatextProjectSettingsParser : ParatextProjectSettingsParserBase
    {
        private readonly string _projectDir;

        public FileParatextProjectSettingsParser(string projectDir)
        {
            _projectDir = projectDir;
        }

        protected override UsfmStylesheet CreateStylesheet(string fileName)
        {
            string customStylesheetFileName = Path.Combine(_projectDir, "custom.sty");
            return new UsfmStylesheet(
                fileName,
                File.Exists(customStylesheetFileName) ? customStylesheetFileName : null
            );
        }

        protected override bool Exists(string fileName)
        {
            return File.Exists(Path.Combine(_projectDir, fileName));
        }

        protected override string Find(string extension)
        {
            return Directory.EnumerateFiles(_projectDir, "*" + extension).FirstOrDefault();
        }

        protected override Stream Open(string fileName)
        {
            return File.OpenRead(Path.Combine(_projectDir, fileName));
        }
    }
}
