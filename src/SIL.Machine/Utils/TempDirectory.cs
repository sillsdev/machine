using System.IO;
using SIL.ObjectModel;

namespace SIL.Machine.Utils
{
    public class TempDirectory : DisposableBase
    {
        public TempDirectory(string name)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);
            DirectoryHelper.DeleteDirectoryRobust(Path);
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        protected override void DisposeManagedResources()
        {
            DirectoryHelper.DeleteDirectoryRobust(Path);
        }
    }
}
