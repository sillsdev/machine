namespace SIL.Machine.AspNetCore.Configuration;

public class BuildJobOptions
{
    public const string Key = "BuildJob";

    public ClearMLBuildJobOptions SmtTransferOptions = new() { Queue = "default", DockerImage = "" };
    public ClearMLBuildJobOptions NmtOptions = new() { Queue = "default", DockerImage = "" };
}
