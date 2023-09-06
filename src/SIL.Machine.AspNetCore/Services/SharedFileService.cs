namespace SIL.Machine.AspNetCore.Services;

public class SharedFileService : ISharedFileService
{
    private readonly Uri? _baseUri;
    private readonly IFileStorage _fileStorage;
    private readonly bool _supportFolderDelete = true;
    private readonly ILoggerFactory _loggerFactory;

    public SharedFileService(ILoggerFactory loggerFactory, IOptions<SharedFileOptions>? options = null)
    {
        _loggerFactory = loggerFactory;

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
                    break;
                case "s3":
                    _fileStorage = new S3FileStorage(
                        _baseUri.Host,
                        _baseUri.AbsolutePath,
                        options.Value.S3AccessKeyId,
                        options.Value.S3SecretAccessKey,
                        options.Value.S3Region,
                        _loggerFactory
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
        return _fileStorage.OpenReadAsync(path, cancellationToken);
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        return _fileStorage.OpenWriteAsync(path, cancellationToken);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!_supportFolderDelete && path.EndsWith("/"))
        {
            IReadOnlyCollection<string> files = await _fileStorage.ListFilesAsync(
                path,
                recurse: true,
                cancellationToken
            );
            foreach (string file in files)
                await _fileStorage.DeleteAsync(file, cancellationToken: cancellationToken);
        }
        else
        {
            await _fileStorage.DeleteAsync(path, recurse: true, cancellationToken: cancellationToken);
        }
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return _fileStorage.ExistsAsync(path, cancellationToken);
    }
}
