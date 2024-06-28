namespace SIL.Machine.AspNetCore.Services;

public interface ISharedFileService
{
    Uri GetBaseUri();

    Uri GetResolvedUri(string path);

    Task<string> GetDownloadUrlAsync(string path, DateTime expiresAt);

    Task<IReadOnlyCollection<string>> ListFilesAsync(
        string path,
        bool recurse = false,
        CancellationToken cancellationToken = default
    );

    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
