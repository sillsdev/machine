namespace SIL.Machine.WebApi.Services;

public class S3WriteStream : Stream
{
    private readonly S3FileStorage _parent;
    private readonly string _key;
    private readonly string _uploadId;
    private readonly List<string> _partTags = new List<string>();
    private long _length;

    public S3WriteStream(S3FileStorage parent, string key, string uploadId)
    {
        _parent = parent;
        _key = key;
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
        int partNumber = _partTags.Count + 1;
        string eTag = _parent.UploadPart(_key, _uploadId, partNumber, buffer, count);
        _partTags.Add(eTag);
        _length += count;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int partNumber = _partTags.Count + 1;
        string eTag = await _parent.UploadPartAsync(_key, _uploadId, partNumber, buffer, count);
        _partTags.Add(eTag);
        _length += count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _parent.CompleteMultipartUpload(_key, _uploadId, _partTags);
        base.Dispose(disposing);
    }

    public async override ValueTask DisposeAsync()
    {
        await _parent.CompleteMultipartUploadAsync(_key, _uploadId, _partTags);

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }
}
