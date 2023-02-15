namespace SIL.Machine.AspNetCore.Configuration;

public class SharedFileOptions
{
    public const string Key = "SharedFile";

    public string Uri { get; set; } = "file:///var/lib/machine/";
    public string S3AccessKeyId { get; set; } = "";
    public string S3SecretAccessKey { get; set; } = "";
    public string S3Region { get; set; } = "us-east-1";
}
