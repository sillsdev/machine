namespace SIL.Machine.AspNetCore.Services;

public class S3FileStorage : FileStorage
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private readonly string _basePath;

    public S3FileStorage(string bucketName, string basePath, string accessKeyId, string secretAccessKey, string region)
    {
        _client = new AmazonS3Client(
            accessKeyId,
            secretAccessKey,
            new AmazonS3Config
            {
                RetryMode = Amazon.Runtime.RequestRetryMode.Standard,
                MaxErrorRetry = 3,
                RegionEndpoint = RegionEndpoint.GetBySystemName(region)
            }
        );

        _bucketName = bucketName;
        _basePath = basePath.EndsWith("/") ? basePath.Remove(basePath.Length - 1, 1) : basePath;
    }

    public override void Dispose() { }

    public override async Task<bool> Exists(string path, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = _basePath + Normalize(path, includeTrailingSlash: path.EndsWith("/")),
            MaxKeys = 1
        };

        ListObjectsV2Response response = await _client.ListObjectsV2Async(request, cancellationToken);

        return response.S3Objects.Any();
    }

    public override async Task<IReadOnlyCollection<string>> Ls(
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
            Prefix = _basePath + Normalize(path, includeTrailingSlash: true),
            MaxKeys = 1,
            Delimiter = recurse ? "" : "/"
        };

        ListObjectsV2Response response = await _client.ListObjectsV2Async(request, cancellationToken);
        return response.S3Objects.Select(s3Obj => s3Obj.Key).ToList();
    }

    public override async Task<Stream> OpenRead(string path, CancellationToken cancellationToken = default)
    {
        string objectId = _basePath + Normalize(path);
        GetObjectRequest request = new() { BucketName = _bucketName, Key = objectId };
        GetObjectResponse response = await _client.GetObjectAsync(request, cancellationToken);
        if (response.HttpStatusCode != HttpStatusCode.OK)
            throw new FileNotFoundException($"File {objectId} does not exist");
        return response.ResponseStream;
    }

    public override async Task<Stream> OpenWrite(string path, CancellationToken cancellationToken = default)
    {
        string objectId = _basePath + Normalize(path);
        InitiateMultipartUploadRequest request = new() { BucketName = _bucketName, Key = objectId };
        InitiateMultipartUploadResponse response = await _client.InitiateMultipartUploadAsync(request);
        return new BufferedStream(
            new S3WriteStream(_client, objectId, _bucketName, response.UploadId),
            1024 * 1024 * 5
        );
    }

    public override async Task Rm(string path, bool recurse = false, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        string objectId = _basePath + Normalize(path);
        DeleteObjectRequest request = new() { BucketName = _bucketName, Key = objectId };
        DeleteObjectResponse response = await _client.DeleteObjectAsync(request, cancellationToken);
        if (!response.HttpStatusCode.Equals(HttpStatusCode.OK))
            new HttpRequestException(
                $"Received status code {response.HttpStatusCode} when attempting to delete {path}"
            );
    }
}
