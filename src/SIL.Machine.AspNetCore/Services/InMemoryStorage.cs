using Newtonsoft.Json.Serialization;

namespace SIL.Machine.AspNetCore.Services;

public class InMemoryStorage : FileStorage
{
    public class Entry : Stream
    {
        public MemoryStream MemoryStream;
        public string Path;
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
            bool alreadyExisted = !_parent._memoryStreams.TryAdd(Path, new Entry(this));
            if (alreadyExisted)
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

    public ConcurrentDictionary<string, Entry> _memoryStreams;

    public InMemoryStorage()
    {
        _memoryStreams = new();
    }

    public override Task<bool> Exists(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_memoryStreams.TryGetValue(Normalize(path), out _));
    }

    public override Task<IReadOnlyCollection<string>> Ls(
        string? path,
        bool recurse = false,
        CancellationToken cancellationToken = default
    )
    {
        if (recurse)
            return Task.FromResult(
                (IReadOnlyCollection<string>)
                    _memoryStreams
                        .Where(kvPair => kvPair.Key.StartsWith(Normalize(path, true, true)))
                        .Select(kvPair => kvPair.Key)
                        .ToList()
            );
        return Task.FromResult(
            (IReadOnlyCollection<string>)
                _memoryStreams
                    .Where(
                        kvPair =>
                            kvPair.Key.StartsWith(Normalize(path, true, true))
                            && !kvPair.Key.Remove(0, Normalize(path, true, true).Length).Contains("/")
                    )
                    .Select(kvPair => kvPair.Key)
                    .ToList()
        );
    }

    public override Task<Stream> OpenRead(string path, CancellationToken cancellationToken = default)
    {
        if (!_memoryStreams.TryGetValue(Normalize(path), out Entry? ret))
            throw new FileNotFoundException($"Unable to find file {path}");
        ret.Position = 0;
        return Task.FromResult<Stream>(ret);
    }

    public override Task<Stream> OpenWrite(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Stream>(new Entry(Normalize(path), this));
    }

    public override async Task Rm(string path, bool recurse, CancellationToken cancellationToken = default)
    {
        if (_memoryStreams.ContainsKey(Normalize(path)))
        {
            _memoryStreams.Remove(Normalize(path), out _);
        }
        else
        {
            IEnumerable<string> filesToRemove = await Ls(path, recurse, cancellationToken);
            foreach (string filePath in filesToRemove)
                _memoryStreams.Remove(Normalize(filePath), out _);
        }
    }

    public override void Dispose()
    {
        foreach (Entry stream in _memoryStreams.Select(kvPair => kvPair.Value))
        {
            stream.Dispose();
        }
    }
}
