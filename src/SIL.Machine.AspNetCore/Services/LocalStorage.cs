namespace SIL.Machine.AspNetCore.Services;

public class LocalStorage : FileStorage
{
    private readonly string _basePath;

    public LocalStorage(string basePath)
    {
        _basePath = basePath.EndsWith("/") ? basePath.Remove(basePath.Length - 1, 1) : basePath;
        Random r = new Random(Guid.NewGuid().GetHashCode());
        while (Directory.Exists(_basePath + "/"))
        {
            _basePath += r.Next();
        }
        Directory.CreateDirectory(_basePath + "/");
    }

    public override void Dispose()
    {
        DeleteRecursive();
        Directory.Delete(_basePath + "/");
    }

    private void DeleteRecursive(string? path = null)
    {
        path ??= _basePath + "/";
        foreach (var subDir in Directory.GetDirectories(path))
        {
            DeleteRecursive(subDir);
            Directory.Delete(subDir);
        }
        foreach (var subPath in Directory.GetFiles(path))
        {
            File.Delete(subPath);
        }
    }

    public override Task<bool> Exists(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(_basePath + Normalize(path)));
    }

    public override async Task<IReadOnlyCollection<string>> Ls(
        string path = "",
        bool recurse = false,
        CancellationToken cancellationToken = default
    )
    {
        if (path.Contains(_basePath))
            path = path.Replace(_basePath, "");
        if (recurse)
        {
            List<string> files = Directory.GetFiles(_basePath + Normalize(path)).ToList();
            foreach (var subDir in Directory.GetDirectories(_basePath + Normalize(path)))
            {
                var subFiles = await Ls(subDir, recurse: true);
                foreach (var file in subFiles)
                    files.Add(file);
            }
            return files;
        }
        if (Directory.Exists(_basePath + Normalize(path)))
            return Directory.GetFiles(_basePath + Normalize(path));
        return new List<string>();
    }

    public override Task<Stream> OpenRead(string path, CancellationToken cancellationToken)
    {
        Stream? ret = File.OpenRead(_basePath + Normalize(path));
        if (ret is null)
            throw new FileNotFoundException($"Unable to locate file {_basePath + Normalize(path)}");
        return Task.FromResult(ret);
    }

    public override Task<Stream> OpenWrite(string path, CancellationToken cancellationToken = default)
    {
        Stream s;
        try
        {
            s = File.OpenWrite(_basePath + Normalize(path));
        }
        catch (IOException)
        {
            string accumulator = _basePath;
            List<string> segments = path.Split("/").ToList();
            foreach (string segment in segments.Take(segments.Count() - 1))
            {
                accumulator += Normalize(segment);
                if (!Directory.Exists(accumulator))
                {
                    Directory.CreateDirectory(accumulator);
                }
            }
            s = File.OpenWrite(_basePath + Normalize(path));
        }
        return Task.FromResult(s);
    }

    public async override Task Rm(string path, bool recurse, CancellationToken cancellationToken = default)
    {
        if (path.Contains(_basePath))
            path = path.Replace(_basePath, "");

        if (File.Exists(_basePath + Normalize(path)))
        {
            File.Delete(_basePath + Normalize(path));
        }
        else
        {
            foreach (string filePath in await Ls(path, recurse, cancellationToken))
            {
                await Rm(filePath, false, cancellationToken);
            }
        }
    }
}
