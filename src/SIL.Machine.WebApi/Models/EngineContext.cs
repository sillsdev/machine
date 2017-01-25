using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Services;

namespace SIL.Machine.WebApi.Models
{
	public class EngineContext
	{
		private static readonly HashSet<string> MergeRightTokens = new HashSet<string> {"‘", "“", "(", "¿", "¡", "«"};
		private static readonly HashSet<string> MergeRightFirstLeftSecondTokens = new HashSet<string> {"\"", "'"};
		private static readonly SpanFactory<int> SpanFactory = new IntegerSpanFactory();

		private IInteractiveSmtModel _smtModel;
		private bool _isUpdated;

		public EngineContext(string configDir, string sourceLanguageTag, string targetLanguageTag)
		{
			ConfigDirectory = configDir;
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			LastUsedTime = DateTime.Now;
			Tokenizer = RegexTokenizer.CreateLatinTokenizer(SpanFactory);
			Detokenizer = new SimpleStringDetokenizer(GetDetokenizeOperation);
		}

		private static DetokenizeOperation GetDetokenizeOperation(string token)
		{
			if (token.Any(char.IsPunctuation))
			{
				if (MergeRightTokens.Contains(token))
					return DetokenizeOperation.MergeRight;
				if (MergeRightFirstLeftSecondTokens.Contains(token))
					return DetokenizeOperation.MergeRightFirstLeftSecond;
				return DetokenizeOperation.MergeLeft;
			}

			return DetokenizeOperation.NoOperation;
		}

		public string ConfigDirectory { get; }
		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public bool IsLoaded { get; private set; }
		public bool IsRemoved { get; private set; }
		public DateTime LastUsedTime { get; set; }
		public HybridTranslationEngine Engine { get; set; }
		public ITokenizer<string, int> Tokenizer { get; }
		public IDetokenizer<string, string> Detokenizer { get; }

		public void MarkUpdated()
		{
			_isUpdated = true;
		}

		public void MarkRemoved()
		{
			IsRemoved = true;
		}

		public void Save()
		{
			if (_isUpdated)
			{
				_smtModel.Save();
				_isUpdated = false;
			}
		}

		public void Load(ISmtModelFactory smtModelFactory, ITranslationEngineFactory ruleEngineFactory)
		{
			if (IsLoaded)
				return;

			_smtModel = smtModelFactory.Create(this);
			ITranslationEngine ruleEngine = ruleEngineFactory.Create(this);

			Engine = new HybridTranslationEngine(_smtModel.CreateInteractiveEngine(), ruleEngine);
			IsLoaded = true;
		}

		public void Unload()
		{
			if (!IsLoaded)
				return;

			Engine.Dispose();
			Engine = null;
			Save();
			_smtModel.Dispose();
			_smtModel = null;
			IsLoaded = false;
		}
	}
}
