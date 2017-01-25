using System.IO;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public class TransferEngineFactory : ITranslationEngineFactory
	{
		public ITranslationEngine Create(EngineContext engineContext)
		{
			string hcSrcConfigFileName = Path.Combine(engineContext.ConfigDirectory, string.Format("{0}-hc.xml", engineContext.SourceLanguageTag));
			string hcTrgConfigFileName = Path.Combine(engineContext.ConfigDirectory, string.Format("{0}-hc.xml", engineContext.TargetLanguageTag));
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
