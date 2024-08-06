using System.IO;

namespace SIL.Machine.Corpora
{
    public class FileParatextProjectTextUpdater : ParatextProjectTextUpdaterBase
    {
        private readonly string _projectDir;

        public FileParatextProjectTextUpdater(string projectDir)
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
