namespace SIL.Machine.AspNetCore.Configuration;

public class ThotSmtModelOptions
{
    public const string ThotSmtModel = "ThotSmtModel";

    public ThotSmtModelOptions()
    {
        string installDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        NewModelFile = Path.Combine(installDir, "thot-new-model.zip");
    }

    public string NewModelFile { get; set; }
}
