using SIL.ObjectModel;
using static SIL.Machine.AspNetCore.Utils.SharedFileUtils;

namespace SIL.Machine.AspNetCore.Services;

public class LocalStorage : DisposableBase, IFileStorage
{
    private readonly Uri _basePath;

    public LocalStorage(string basePath)
    {
        _basePath = new Uri(basePath);
        if (!_basePath.AbsoluteUri.EndsWith("/"))
            _basePath = new Uri(_basePath.AbsoluteUri + "/");
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        Uri pathUri = new(_basePath, Normalize(path));
        return Task.FromResult(File.Exists(pathUri.LocalPath));
    }

    public Task<IReadOnlyCollection<string>> ListFilesAsync(
        string path = "",
        bool recurse = false,
        CancellationToken cancellationToken = default
    )
    {
        Uri pathUri = new(_basePath, Normalize(path));
        string[] files = Directory.GetFiles(
            pathUri.LocalPath,
            "*",
            new EnumerationOptions { RecurseSubdirectories = recurse }
        );
        return Task.FromResult<IReadOnlyCollection<string>>(
            files.Select(f => _basePath.MakeRelativeUri(new Uri(f)).ToString()).ToArray()
        );
    }

    public Task<string> GetDownloadUrlAsync(
        string path,
        DateTime expiresAt,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        Uri pathUri = new(_basePath, Normalize(path));
        return Task.FromResult<Stream>(File.OpenRead(pathUri.LocalPath));
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        Uri pathUri = new(_basePath, Normalize(path));
        Directory.CreateDirectory(Path.GetDirectoryName(pathUri.LocalPath)!);
        return Task.FromResult<Stream>(File.OpenWrite(pathUri.LocalPath));
    }

    public async Task DeleteAsync(string path, bool recurse, CancellationToken cancellationToken = default)
    {
        Uri pathUri = new(_basePath, Normalize(path));

        if (File.Exists(pathUri.LocalPath))
        {
            File.Delete(pathUri.LocalPath);
        }
        else if (Directory.Exists(pathUri.LocalPath))
        {
            foreach (string filePath in await ListFilesAsync(path, recurse, cancellationToken))
            {
                await DeleteAsync(filePath, false, cancellationToken);
            }
        }
    }
}
