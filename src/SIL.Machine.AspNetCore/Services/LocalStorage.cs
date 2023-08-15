namespace SIL.Machine.AspNetCore.Services;

public class LocalStorage : FileStorage
{
    private readonly string _basePath;

    public LocalStorage(string basePath)
    {
        _basePath = basePath.EndsWith("/") ? basePath.Remove(basePath.Length - 1, 1) : basePath;
    }

    public override Task<bool> Exists(string path, CancellationToken cancellationToken)
    {
        return Task.FromResult(File.Exists(_basePath + Normalize(path)));
    }

    public override Task<IReadOnlyCollection<string>> Ls(
        string? path,
        bool recurse,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult((IReadOnlyCollection<string>)Directory.GetFiles(_basePath + Normalize(path)).ToList());
    }

    public override Task<Stream> OpenRead(string path, CancellationToken cancellationToken)
    {
        Stream? ret = File.OpenRead(_basePath + Normalize(path));
        if (ret is null)
            throw new FileNotFoundException($"Unable to locate file {_basePath + Normalize(path)}");
        return Task.FromResult(ret);
    }

    public override Task<Stream> OpenWrite(string path, CancellationToken cancellationToken)
    {
        return Task.FromResult((Stream)File.OpenWrite(_basePath + Normalize(path)));
    }

    public override Task Rm(string path, bool recurse, CancellationToken cancellationToken)
    {
        if (!path.EndsWith("/"))
        {
            File.Delete(_basePath + Normalize(path));
        }
        else
        {
            foreach (string filePath in Ls(path, recurse, cancellationToken).Result)
            {
                Rm(filePath, false, cancellationToken);
            }
        }
        return Task.CompletedTask;
    }
}
