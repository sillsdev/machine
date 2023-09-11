namespace SIL.Machine.AspNetCore.Services;

public class S3WriteStream : Stream
{
    private readonly AmazonS3Client _client;
    private readonly string _key;
    private readonly string _bucketName;
    private long _length;

    public S3WriteStream(AmazonS3Client client, string key, string bucketName)
    {
        _client = client;
        _key = key;
        _bucketName = bucketName;
        _length = 0;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override void Write(byte[] buffer, int offset, int count)
    {
        using Stream inputStream = new MemoryStream(buffer, offset, count);
        using var transferUtility = new TransferUtility(_client);
        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = _bucketName,
            InputStream = inputStream,
            Key = _key,
            PartSize = count
        };
        transferUtility.Upload(uploadRequest);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        using Stream inputStream = new MemoryStream(buffer, offset, count);
        using var transferUtility = new TransferUtility(_client);
        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = _bucketName,
            InputStream = inputStream,
            Key = _key,
            PartSize = count
        };
        await transferUtility.UploadAsync(uploadRequest);
    }

    public override ValueTask DisposeAsync()
    {
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
