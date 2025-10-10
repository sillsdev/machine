using System.IO;

namespace SIL.Machine.Corpora
{
    public class FileParatextProjectVersificationMismatchDetector : ParatextProjectVersificationMismatchDetector
    {
        private readonly string _projectDir;

        public FileParatextProjectVersificationMismatchDetector(string projectDir)
            : base(new FileParatextProjectSettingsParser(projectDir))
        {
            _projectDir = projectDir;
        }

        protected override bool Exists(string fileName)
        {
            return File.Exists(Path.Combine(_projectDir, fileName));
        }

        protected override Stream Open(string fileName)
        {
            return File.OpenRead(Path.Combine(_projectDir, fileName));
        }
    }
}
