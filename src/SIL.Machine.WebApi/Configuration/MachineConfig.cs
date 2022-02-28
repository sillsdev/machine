namespace SIL.Machine.WebApi.Configuration;

public class MachineConfig
{
	public string Namespace { get; set; } = "";
	public ICollection<string> AuthenticationSchemes { get; set; } = new List<string>();
}
