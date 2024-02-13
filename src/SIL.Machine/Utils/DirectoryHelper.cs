using System;
using System.IO;
using System.Threading;

namespace SIL.Machine.Utils
{
    public static class DirectoryHelper
    {
        public static void DeleteDirectoryRobust(string path)
        {
            // Catch and retry logic based off of this post:
            //   https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true

            if (Directory.Exists(path))
            {
                foreach (string directory in Directory.GetDirectories(path))
                    DeleteDirectoryRobust(directory);

                try
                {
                    Thread.Sleep(0);
                    Directory.Delete(path, true);
                }
                catch (IOException)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        Directory.Delete(path, true);
                    }
                    catch (Exception) { }
                }
                catch (UnauthorizedAccessException)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        Directory.Delete(path, true);
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
