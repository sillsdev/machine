using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.Translation
{
	public static class TokenProcessors
	{
		public static readonly ITokenProcessor Lowercase = new LowercaseTokenProcessor();
		public static readonly ITokenProcessor EscapeSpaces = new EscapeSpacesTokenProcessor();
		public static readonly ITokenProcessor UnescapeSpaces = new UnescapeSpacesTokenProcessor();
		public static readonly ITokenProcessor Null = new NullTokenProcessor();

		public static ITokenProcessor Pipeline(params ITokenProcessor[] processors)
		{
			return new PipelineTokenProcessor(processors);
		}

		public static ITokenProcessor Pipeline(IEnumerable<ITokenProcessor> processors)
		{
			return new PipelineTokenProcessor(processors);
		}

		public static ITokenProcessor Normalize(NormalizationForm normalizationForm = NormalizationForm.FormC)
		{
			return new NormalizeTokenProcessor(normalizationForm);
		}

		private class LowercaseTokenProcessor : ITokenProcessor
		{
			public IReadOnlyList<string> Process(IReadOnlyList<string> tokens)
			{
				return tokens.Select(t => t.ToLowerInvariant()).ToArray();
			}
		}

		private class EscapeSpacesTokenProcessor : ITokenProcessor
		{
			public IReadOnlyList<string> Process(IReadOnlyList<string> tokens)
			{
				return tokens.Select(t => t.Length > 0 && t.All(char.IsWhiteSpace) ? "<space>" : t).ToArray();
			}
		}

		private class UnescapeSpacesTokenProcessor : ITokenProcessor
		{
			public IReadOnlyList<string> Process(IReadOnlyList<string> tokens)
			{
				return tokens.Select(t => t == "<space>" ? " " : t).ToArray();
			}
		}

		private class NullTokenProcessor : ITokenProcessor
		{
			public IReadOnlyList<string> Process(IReadOnlyList<string> tokens)
			{
				return tokens;
			}
		}

		private class NormalizeTokenProcessor : ITokenProcessor
		{
			private readonly NormalizationForm _normalizationForm;

			public NormalizeTokenProcessor(NormalizationForm normalizationForm)
			{
				_normalizationForm = normalizationForm;
			}

			public IReadOnlyList<string> Process(IReadOnlyList<string> tokens)
			{
				return tokens.Select(t => t.Normalize(_normalizationForm)).ToArray();
			}
		}

		private class PipelineTokenProcessor : ITokenProcessor
		{
			private readonly ITokenProcessor[] _processors;

			public PipelineTokenProcessor(IEnumerable<ITokenProcessor> processors)
			{
				_processors = processors.ToArray();
			}

			public IReadOnlyList<string> Process(IReadOnlyList<string> tokens)
			{
				foreach (ITokenProcessor processor in _processors)
					tokens = (processor ?? Null).Process(tokens);
				return tokens;
			}
		}
	}
}
