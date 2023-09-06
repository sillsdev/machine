namespace SIL.Machine.AspNetCore.Utils;

public static class SharedFileUtils
{
    public static string Normalize(string path, bool includeLeadingSlash = false, bool includeTrailingSlash = false)
    {
        string normalizedPath = path;
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
            normalizedPath += "/";
        }
        return normalizedPath;
    }
}
