namespace SIL.Machine.WebApi.Configuration;

public class SharedFileOptions
{
    public const string Key = "SharedFile";

    public string? Uri { get; set; }
    public string? S3AccessKeyId { get; set; }
    public string? S3SecretAccessKey { get; set; }
    public string? S3Region { get; set; }
}
