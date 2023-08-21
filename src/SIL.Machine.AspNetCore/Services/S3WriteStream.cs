namespace SIL.Machine.AspNetCore.Services;

public class S3WriteStream : Stream
{
    private readonly AmazonS3Client _client;
    private readonly string _key;
    private readonly string _uploadId;

    private readonly string _bucketName;
    private readonly List<UploadPartResponse> uploadResponses = new();
    private long _length;

    public S3WriteStream(AmazonS3Client client, string key, string bucketName, string uploadId)
    {
        _client = client;
        _key = key;
        _bucketName = bucketName;
        _uploadId = uploadId;
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
        try
        {
            int partNumber = uploadResponses.Count + 1;
            UploadPartRequest request =
                new()
                {
                    BucketName = _bucketName,
                    Key = _key,
                    UploadId = _uploadId,
                    PartNumber = partNumber,
                    FilePosition = _length
                };
            _length += count;
            uploadResponses.Add(_client.UploadPartAsync(request).Result);
        }
        catch (Exception)
        {
            Abort().Wait(10_000);
        }
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        try
        {
            int partNumber = uploadResponses.Count + 1;
            UploadPartRequest request =
                new()
                {
                    BucketName = _bucketName,
                    Key = _key,
                    UploadId = _uploadId,
                    PartNumber = partNumber,
                    FilePosition = _length
                };
            _length += count;
            uploadResponses.Add(await _client.UploadPartAsync(request));
        }
        catch (Exception)
        {
            await Abort();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    public async override ValueTask DisposeAsync()
    {
        try
        {
            CompleteMultipartUploadRequest request =
                new()
                {
                    BucketName = _bucketName,
                    Key = _key,
                    UploadId = _uploadId
                };
            request.AddPartETags(uploadResponses);
            await _client.CompleteMultipartUploadAsync(request);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
        catch (Exception)
        {
            await Abort();
        }
    }

    private async Task Abort()
    {
        // Logging?
        AbortMultipartUploadRequest abortMPURequest =
            new()
            {
                BucketName = _bucketName,
                Key = _key,
                UploadId = _uploadId
            };
        await _client.AbortMultipartUploadAsync(abortMPURequest);
    }
}
