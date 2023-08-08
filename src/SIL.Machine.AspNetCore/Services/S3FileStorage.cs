namespace SIL.Machine.AspNetCore.Services;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

public class S3FileStorage : DisposableBase, IFileStorage
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private readonly string _directory;

    public S3FileStorage(string bucketName, string directory, string accessKeyId, string secretAccessKey, string region)
    {
        _client = new AmazonS3Client(
            accessKeyId,
            secretAccessKey,
            new AmazonS3Config //Best retry configuration?
            {
                RetryMode = Amazon.Runtime.RequestRetryMode.Standard,
                MaxErrorRetry = 3,
                RegionEndpoint = RegionEndpoint.GetBySystemName(region)
            }
        );
        _bucketName = bucketName;
        _directory = directory;
    }

    public async Task<bool> Exists(IOPath path, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = _directory,
            MaxKeys = 1
        };

        var response = await _client.ListObjectsV2Async(request, cancellationToken);

        return response.S3Objects.Any();
    }

    public async Task<IReadOnlyCollection<IOEntry>> Ls(
        IOPath? path = null,
        bool recurse = false,
        CancellationToken cancellationToken = default
    )
    {
        if (path != null && !path.IsFolder)
            throw new ArgumentException("Path must be a folder", nameof(path));

        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix =
                _directory
                + (
                    IOPath.IsRoot(path)
                        ? string.Empty
                        : IOPath.Normalize(path, removeLeadingSlash: true, appendTrailingSlash: true)
                ),
            MaxKeys = 1
        };

        var response = await _client.ListObjectsV2Async(request, cancellationToken);
        var result = new List<IOEntry>();
        foreach (var s3obj in response.S3Objects)
        {
            var entry = new IOEntry(s3obj.Key) { LastModificationTime = s3obj.LastModified, Size = s3obj.Size };
            entry.TryAddProperties("ETag", s3obj.ETag, "StorageClass", s3obj.StorageClass);
        }
        return (IReadOnlyCollection<IOEntry>)result;
    }

    public async Task<Stream?> OpenRead(IOPath path, CancellationToken cancellationToken = default)
    {
        var objectId = IOPath.IsRoot(path)
            ? string.Empty
            : IOPath.Normalize(path, removeLeadingSlash: true, appendTrailingSlash: true);
        GetObjectRequest request = new() { BucketName = _bucketName, Key = objectId };
        GetObjectResponse response = await _client.GetObjectAsync(request, cancellationToken);
        return response.ResponseStream;
    }

    public async Task<Stream> OpenWrite(IOPath path, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("PATH ||| " + path.ToString());
        var objectId = IOPath.IsRoot(path)
            ? string.Empty
            : IOPath.Normalize(path, removeLeadingSlash: true, appendTrailingSlash: true);
        InitiateMultipartUploadRequest request = new() { BucketName = _bucketName, Key = objectId };
        InitiateMultipartUploadResponse response = await _client.InitiateMultipartUploadAsync(request);
        return new BufferedStream(
            new S3WriteStream(_client, objectId, _bucketName, response.UploadId),
            1024 * 1024 * 5
        );
    }

    public async Task<T?> ReadAsJson<T>(IOPath path, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        string? json = await ReadText(path, null, cancellationToken);
        if (json == null)
            return default;

        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task<string?> ReadText(
        IOPath path,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default
    )
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        if (!path.IsFile)
            throw new ArgumentException($"{nameof(path)} needs to be a file", nameof(path));

        using Stream? src = await OpenRead(path, cancellationToken).ConfigureAwait(false);
        if (src == null)
            return null;

        var ms = new MemoryStream();
        using (src)
        {
            await src.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        }

        return (encoding ?? Encoding.UTF8).GetString(ms.ToArray());
    }

    public async Task Ren(IOPath oldPath, IOPath newPath, CancellationToken cancellationToken = default)
    {
        if (oldPath is null)
            throw new ArgumentNullException(nameof(oldPath));
        if (newPath is null)
            throw new ArgumentNullException(nameof(newPath));

        // when file moves to a folder
        if (oldPath.IsFile && newPath.IsFolder)
        {
            // now it's a file-to-file rename
            throw new NotImplementedException();
        }
        else if (oldPath.IsFolder && newPath.IsFile)
        {
            throw new ArgumentException($"attempted to rename folder to file", nameof(newPath));
        }
        else if (oldPath.IsFolder)
        {
            // folder-to-folder ren
            throw new NotImplementedException();
        }
        else
        {
            // file-to-file ren
            await RenFile(oldPath, newPath, cancellationToken);
        }
    }

    public async Task Rm(IOPath path, bool recurse = false, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        var objectId = IOPath.IsRoot(path)
            ? string.Empty
            : IOPath.Normalize(path, removeLeadingSlash: true, appendTrailingSlash: true);

        DeleteObjectRequest request = new() { BucketName = _bucketName, Key = objectId };
        DeleteObjectResponse response = await _client.DeleteObjectAsync(request, cancellationToken);
        if (!response.HttpStatusCode.Equals(HttpStatusCode.OK))
            new HttpRequestException(
                $"Received status code {response.HttpStatusCode} when attempting to delete {path}"
            );
    }

    public async Task WriteAsJson(IOPath path, object value, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        string json = JsonSerializer.Serialize(value);
        await WriteText(path, json, null, cancellationToken);
    }

    public async Task WriteText(
        IOPath path,
        string contents,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default
    )
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        if (contents is null)
            throw new ArgumentNullException(nameof(contents));

        if (!path.IsFile)
            throw new ArgumentException($"{nameof(path)} needs to be a file", nameof(path));

        using Stream ws = await OpenWrite(path, cancellationToken);
        Stream rs = new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(contents));
        await rs.CopyToAsync(ws, cancellationToken);
    }

    private async Task RenFile(IOPath oldPath, IOPath newPath, CancellationToken cancellationToken = default)
    {
        using Stream? src = await OpenRead(oldPath, cancellationToken);
        if (src != null)
        {
            using (Stream dest = await OpenWrite(newPath, cancellationToken))
            {
                await src.CopyToAsync(dest, cancellationToken);
            }

            await Rm(oldPath, cancellationToken: cancellationToken);
        }
    }
}
