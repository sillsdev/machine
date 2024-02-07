using static SIL.Machine.AspNetCore.Utils.SharedFileUtils;

namespace SIL.Machine.AspNetCore.Services;

public class InMemoryStorage : DisposableBase, IFileStorage
{
    public class Entry : Stream
    {
        public MemoryStream MemoryStream { get; }
        public string Path { get; }

        private readonly InMemoryStorage _parent;

        public override bool CanRead => MemoryStream.CanRead;

        public override bool CanSeek => MemoryStream.CanSeek;

        public override bool CanWrite => MemoryStream.CanWrite;

        public override long Length => MemoryStream.Length;

        public override long Position
        {
            get => MemoryStream.Position;
            set => MemoryStream.Position = value;
        }

        public Entry(string path, InMemoryStorage parent)
        {
            Path = path;
            MemoryStream = new();
            _parent = parent;
        }

        public Entry(Entry other)
        {
            Path = other.Path;
            MemoryStream = other.MemoryStream;
            _parent = other._parent;
        }

        protected override void Dispose(bool disposing)
        {
            _parent._memoryStreams[Path] = new Entry(this);
        }

        public override void Flush()
        {
            MemoryStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return MemoryStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return MemoryStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            MemoryStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            MemoryStream.Write(buffer, offset, count);
        }
    }

    private readonly ConcurrentDictionary<string, Entry> _memoryStreams = new();

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_memoryStreams.TryGetValue(Normalize(path), out _));
    }

    public Task<IReadOnlyCollection<string>> ListFilesAsync(
        string? path,
        bool recurse = false,
        CancellationToken cancellationToken = default
    )
    {
        path = string.IsNullOrEmpty(path) ? "" : Normalize(path, includeTrailingSlash: true);
        if (recurse)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(
                _memoryStreams.Keys.Where(p => p.StartsWith(path)).ToList()
            );
        }

        return Task.FromResult<IReadOnlyCollection<string>>(
            _memoryStreams.Keys.Where(p => p.StartsWith(path) && !p[path.Length..].Contains('/')).ToList()
        );
    }

    public Task<string> GetDownloadUrlAsync(
        string path,
        DateTime expiresAt,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!_memoryStreams.TryGetValue(Normalize(path), out Entry? ret))
            throw new FileNotFoundException($"Unable to find file {path}");
        ret.Position = 0;
        return Task.FromResult<Stream>(ret);
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Stream>(new Entry(Normalize(path), this));
    }

    public async Task DeleteAsync(string path, bool recurse, CancellationToken cancellationToken = default)
    {
        if (_memoryStreams.ContainsKey(Normalize(path)))
        {
            _memoryStreams.Remove(Normalize(path), out _);
        }
        else
        {
            IEnumerable<string> filesToRemove = await ListFilesAsync(path, recurse, cancellationToken);
            foreach (string filePath in filesToRemove)
                _memoryStreams.Remove(Normalize(filePath), out _);
        }
    }

    protected override void DisposeManagedResources()
    {
        foreach (Entry stream in _memoryStreams.Values)
            stream.Dispose();
        _memoryStreams.Clear();
    }
}
