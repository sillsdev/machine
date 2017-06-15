using System.IO;
using Microsoft.Extensions.Options;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Options;

namespace SIL.Machine.WebApi.Services
{
	public class TransferEngineFactory : IRuleEngineFactory
	{
		private readonly string _rootDir;

		public TransferEngineFactory(IOptions<EngineOptions> engineOptions)
		{
			_rootDir = engineOptions.Value.RootDir;
		}

		public ITranslationEngine Create(string engineId)
		{
			string engineDir = Path.Combine(_rootDir, engineId);
			string hcSrcConfigFileName = Path.Combine(engineDir, "src-hc.xml");
			string hcTrgConfigFileName = Path.Combine(engineDir, "trg-hc.xml");
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
