using System.IO;

namespace SIL.Machine.Corpora
{
    public class FileVersificationTable : VersificationTableBase
    {
        private readonly string _projectDir;

        public FileVersificationTable(string projectDir)
        {
            _projectDir = projectDir;
        }

        protected override string ProjectName => Path.GetFileName(_projectDir);

        protected override bool FileExists(string fileName)
        {
            return File.Exists(Path.Combine(_projectDir, fileName));
        }

        protected override Stream OpenFile(string fileName)
        {
            return File.OpenRead(Path.Combine(_projectDir, fileName));
        }
    }
}
