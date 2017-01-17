using System.IO;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.WebApi.Models
{
	public class ThotSmtModelFactory : ISmtModelFactory
	{
		public IInteractiveSmtModel Create(EngineContext engineContext)
		{
			string smtConfigFileName = Path.Combine(engineContext.ConfigDirectory, "smt.cfg");
			return new ThotSmtModel(smtConfigFileName);
		}
	}
}
