namespace SIL.Machine.AspNetCore.Services;

public interface IFileSystem
{
    void DeleteFile(string path);
    void CreateDirectory(string path);
    Stream OpenWrite(string path);
    Stream OpenRead(string path);
}
