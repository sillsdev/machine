namespace SIL.Machine.AspNetCore.Services;

public class FileSystem : IFileSystem
{
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    public Stream OpenWrite(string path)
    {
        return File.OpenWrite(path);
    }

    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }
}
