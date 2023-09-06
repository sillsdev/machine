using static SIL.Machine.AspNetCore.Utils.SharedFileUtils;

namespace SIL.Machine.AspNetCore.Services;

public class S3FileStorage : DisposableBase, IFileStorage
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private readonly string _basePath;
    private readonly ILoggerFactory _loggerFactory;

    public S3FileStorage(
        string bucketName,
        string basePath,
        string accessKeyId,
        string secretAccessKey,
        string region,
        ILoggerFactory loggerFactory
    )
    {
        _client = new AmazonS3Client(
            accessKeyId,
            secretAccessKey,
            new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(region) }
        );

        _bucketName = bucketName;
        // Ultimately, object keys can neither begin nor end with slashes; this is what broke the earlier low-level
        // implementation
        _basePath = Normalize(basePath, includeTrailingSlash: true);
        _loggerFactory = loggerFactory;
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = _basePath + Normalize(path),
            MaxKeys = 1
        };

        ListObjectsV2Response response = await _client.ListObjectsV2Async(request, cancellationToken);

        return response.S3Objects.Any();
    }

    public async Task<IReadOnlyCollection<string>> ListFilesAsync(
        string? path = null,
        bool recurse = false,
        CancellationToken cancellationToken = default
    )
    {
        if (path != null && !path.EndsWith("/"))
            throw new ArgumentException("Path must be a folder (ending with '/')", nameof(path));

        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = _basePath + (string.IsNullOrEmpty(path) ? "" : Normalize(path, includeTrailingSlash: true)),
            Delimiter = recurse ? "" : "/"
        };

        ListObjectsV2Response response = await _client.ListObjectsV2Async(request, cancellationToken);
        return response.S3Objects.Select(s3Obj => s3Obj.Key[_basePath.Length..]).ToList();
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        GetObjectRequest request = new() { BucketName = _bucketName, Key = _basePath + Normalize(path) };
        GetObjectResponse response = await _client.GetObjectAsync(request, cancellationToken);
        if (response.HttpStatusCode != HttpStatusCode.OK)
            throw new FileNotFoundException($"File {path} does not exist");
        return response.ResponseStream;
    }

    public async Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = _basePath + Normalize(path);
        InitiateMultipartUploadRequest request = new() { BucketName = _bucketName, Key = fullPath };
        InitiateMultipartUploadResponse response = await _client.InitiateMultipartUploadAsync(
            request,
            cancellationToken
        );
        return new BufferedStream(
            new S3WriteStream(_client, fullPath, _bucketName, response.UploadId, _loggerFactory),
            S3WriteStream.MaxPartSize
        );
    }

    public async Task DeleteAsync(string path, bool recurse = false, CancellationToken cancellationToken = default)
    {
        DeleteObjectRequest request = new() { BucketName = _bucketName, Key = _basePath + Normalize(path) };
        DeleteObjectResponse response = await _client.DeleteObjectAsync(request, cancellationToken);
        if (!response.HttpStatusCode.Equals(HttpStatusCode.NoContent))
            throw new HttpRequestException(
                $"Received status code {response.HttpStatusCode} when attempting to delete {path}"
            );
    }
}
