using System.Collections.Generic;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;

namespace SIL.Machine
{
	public class PreprocessCommandSpec : ICommandSpec
	{
		private const string Nfc = "nfc";
		private const string Nfd = "nfd";
		private const string Nfkc = "nfkc";
		private const string Nfkd = "nfkd";

		private CommandOption _lowercaseOption;
		private CommandOption _normalizeOption;

		public bool EscapeSpaces { get; set; } = false;

		public void AddParameters(CommandBase command)
		{
			_lowercaseOption = command.Option("-l|--lowercase", "Convert text to lowercase.",
				CommandOptionType.NoValue);
			_normalizeOption = command.Option("-nf|--normalization-form <FORM>",
				$"Normalizes text to the specified form.\nForms: \"{Nfc}\", \"{Nfd}\", \"{Nfkc}\", \"{Nfkd}\".",
				CommandOptionType.SingleValue);
		}

		public bool Validate(TextWriter outWriter)
		{
			if (!ValidateNormalizeOption(_normalizeOption.Value()))
			{
				outWriter.WriteLine("The specified normalization form is invalid.");
				return false;
			}

			return true;
		}

		public ITokenProcessor GetProcessor()
		{
			var processors = new List<ITokenProcessor>();
			switch (_normalizeOption.Value())
			{
				case Nfc:
					processors.Add(TokenProcessors.Normalize(NormalizationForm.FormC));
					break;
				case Nfd:
					processors.Add(TokenProcessors.Normalize(NormalizationForm.FormD));
					break;
				case Nfkc:
					processors.Add(TokenProcessors.Normalize(NormalizationForm.FormKC));
					break;
				case Nfkd:
					processors.Add(TokenProcessors.Normalize(NormalizationForm.FormKD));
					break;
			}

			if (EscapeSpaces)
				processors.Add(TokenProcessors.EscapeSpaces);

			if (_lowercaseOption.HasValue())
				processors.Add(TokenProcessors.Lowercase);

			if (processors.Count == 0)
				return TokenProcessors.NoOp;
			return TokenProcessors.Pipeline(processors);
		}

		private static bool ValidateNormalizeOption(string value)
		{
			var validForms = new HashSet<string>
			{
				Nfc,
				Nfd,
				Nfkc,
				Nfkd
			};
			return string.IsNullOrEmpty(value) || validForms.Contains(value.ToLowerInvariant());
		}
	}
}
