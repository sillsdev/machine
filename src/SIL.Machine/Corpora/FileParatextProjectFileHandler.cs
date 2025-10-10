using System.IO;

namespace SIL.Machine.Corpora
{
    public class FileParatextProjectFileHandler : IParatextProjectFileHandler
    {
        private readonly string _projectDir;
        private readonly ParatextProjectSettings _settings;

        public FileParatextProjectFileHandler(string projectDir)
        {
            _projectDir = projectDir;
            _settings = new FileParatextProjectSettingsParser(projectDir).Parse();
        }

        public bool Exists(string fileName)
        {
            return File.Exists(Path.Combine(_projectDir, fileName));
        }

        public Stream Open(string fileName)
        {
            return File.OpenRead(Path.Combine(_projectDir, fileName));
        }

        public ParatextProjectSettings GetSettings()
        {
            return _settings;
        }
    }
}
