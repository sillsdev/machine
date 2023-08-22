namespace SIL.Machine.AspNetCore.Services;

public class SharedFileService : ISharedFileService
{
    private readonly Uri? _baseUri;
    private readonly FileStorage _fileStorage;
    private readonly bool _supportFolderDelete = true;

    public SharedFileService(IOptions<SharedFileOptions>? options = null)
    {
        if (options?.Value.Uri is null)
        {
            _fileStorage = new InMemoryStorage();
        }
        else
        {
            string baseUri = options.Value.Uri;
            if (!baseUri.EndsWith("/"))
                baseUri += "/";
            _baseUri = new Uri(baseUri);
            switch (_baseUri.Scheme)
            {
                case "file":
                    _fileStorage = new LocalStorage(_baseUri.LocalPath);
                    Directory.CreateDirectory(_baseUri.LocalPath);
                    break;
                case "s3":
                    _fileStorage = new S3FileStorage(
                        _baseUri.Host,
                        _baseUri.AbsolutePath,
                        options.Value.S3AccessKeyId,
                        options.Value.S3SecretAccessKey,
                        options.Value.S3Region
                    );
                    _supportFolderDelete = false;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported URI scheme: {_baseUri.Scheme}");
            }
        }
    }

    public Uri GetBaseUri()
    {
        return GetResolvedUri("");
    }

    public Uri GetResolvedUri(string path)
    {
        if (_baseUri is null)
            return new Uri($"memory://{path}");
        return new Uri(_baseUri, path);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        return _fileStorage.OpenRead(path, cancellationToken);
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        return _fileStorage.OpenWrite(path, cancellationToken);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!_supportFolderDelete && path.EndsWith("/"))
        {
            IReadOnlyCollection<string> files = await _fileStorage.Ls(path, recurse: true, cancellationToken);
            foreach (string file in files)
                await _fileStorage.Rm(file, cancellationToken: cancellationToken);
        }
        else
        {
            await _fileStorage.Rm(path, recurse: true, cancellationToken: cancellationToken);
        }
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return _fileStorage.Exists(path, cancellationToken);
    }

    public Task<IReadOnlyCollection<string>> Ls(
        string path,
        bool recurse = false,
        CancellationToken cancellationToken = default
    )
    {
        return _fileStorage.Ls(path, recurse, cancellationToken);
    }
}
