using SIL.ObjectModel;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SIL.Machine.Utils
{
    public class TempDirectory : DisposableBase
    {
        public TempDirectory(string name)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);
            DeleteFolderThatMayBeInUse();
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        protected override void DisposeManagedResources()
        {
            DeleteFolderThatMayBeInUse();
        }

        private void DeleteFolderThatMayBeInUse()
        {
            if (Directory.Exists(Path))
            {
                try
                {
                    Directory.Delete(Path, true);
                }
                catch (Exception)
                {
                    try
                    {
                        //maybe we can at least clear it out a bit
                        string[] files = Directory.GetFiles(Path, "*.*", SearchOption.AllDirectories);
                        foreach (string s in files)
                        {
                            File.Delete(s);
                        }
                        //sleep and try again (seems to work)
                        Task.Delay(1000).Wait();
                        Directory.Delete(Path, true);
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
