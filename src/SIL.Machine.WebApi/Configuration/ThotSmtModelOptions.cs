namespace SIL.Machine.WebApi.Configuration;

public class ThotSmtModelOptions
{
	public ThotSmtModelOptions()
	{
		string installDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
		NewModelFile = Path.Combine(installDir, "thot-new-model.zip");
	}

	public string NewModelFile { get; set; }
}
