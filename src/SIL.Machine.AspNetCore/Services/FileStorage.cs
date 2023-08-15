namespace SIL.Machine.AspNetCore.Services;

public abstract class FileStorage
{
    public abstract Task<bool> Exists(string path, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyCollection<string>> Ls(
        string? path = null,
        bool recurse = false,
        CancellationToken cancellationToken = default
    );

    public abstract Task<Stream> OpenRead(string path, CancellationToken cancellationToken = default);

    public abstract Task<Stream> OpenWrite(string path, CancellationToken cancellationToken = default);

    public abstract Task Rm(string path, bool recurse = false, CancellationToken cancellationToken = default);

    protected string Normalize(string? path, bool includeLeadingSlash = true, bool includeTrailingSlash = false)
    {
        string normalizedPath = path ?? "";
        if (normalizedPath == "/")
            return normalizedPath;
        if (!includeLeadingSlash && normalizedPath.StartsWith("/"))
        {
            normalizedPath = normalizedPath.Remove(0, 1);
        }
        else if (includeLeadingSlash && !normalizedPath.StartsWith("/"))
        {
            normalizedPath = "/" + normalizedPath;
        }
        if (!includeTrailingSlash && normalizedPath.EndsWith("/"))
        {
            normalizedPath = normalizedPath.Remove(normalizedPath.Length - 1, 1);
        }
        else if (includeTrailingSlash && !normalizedPath.EndsWith("/"))
        {
            normalizedPath = normalizedPath + "/";
        }
        return normalizedPath;
    }
}
