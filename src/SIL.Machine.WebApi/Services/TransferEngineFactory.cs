using System.IO;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Services
{
	public class TransferEngineFactory : IRuleEngineFactory
	{
		public ITranslationEngine Create(string configDir)
		{
			string hcSrcConfigFileName = Path.Combine(configDir, "src-hc.xml");
			string hcTrgConfigFileName = Path.Combine(configDir, "trg-hc.xml");
			TransferEngine transferEngine = null;
			if (File.Exists(hcSrcConfigFileName) && File.Exists(hcTrgConfigFileName))
			{
				var spanFactory = new ShapeSpanFactory();
				var hcTraceManager = new TraceManager();

				Language srcLang = XmlLanguageLoader.Load(hcSrcConfigFileName);
				var srcMorpher = new Morpher(spanFactory, hcTraceManager, srcLang);

				Language trgLang = XmlLanguageLoader.Load(hcTrgConfigFileName);
				var trgMorpher = new Morpher(spanFactory, hcTraceManager, trgLang);

				transferEngine = new TransferEngine(srcMorpher, new SimpleTransferer(new GlossMorphemeMapper(trgMorpher)), trgMorpher);
			}
			return transferEngine;
		}
	}
}
