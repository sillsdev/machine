namespace SIL.Machine.AspNetCore.Services;

public interface IFileStorage : IDisposable
{
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListFilesAsync(
        string path,
        bool recurse = false,
        CancellationToken cancellationToken = default
    );

    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default);

    Task<string> GetDownloadUrlAsync(string path, DateTime expiresAt, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, bool recurse = false, CancellationToken cancellationToken = default);
}
