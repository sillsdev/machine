namespace SIL.Machine.AspNetCore.Services;

public interface ISharedFileService
{
    public const string ModelDirectory = "models/";

    Uri GetBaseUri();

    Uri GetResolvedUri(string path);

    Task<Uri> GetPresignedUrlAsync(string path, int minutesToExpire);

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
