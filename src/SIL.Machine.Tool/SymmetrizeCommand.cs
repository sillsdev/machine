using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class SymmetrizeCommand : CommandBase
	{
		private const string Pharaoh = "pharaoh";
		private const string Giza = "giza";

		private CommandArgument _directArgument;
		private CommandArgument _inverseArgument;
		private CommandArgument _outputArgument;
		private CommandOption _outputFormatOption;
		private CommandOption _symHeuristicOption;

		public SymmetrizeCommand()
		{
			Name = "symmetrize";
			Description = "Symmetrizes alignments from direct and inverse word alignment models.";

			_directArgument = Argument("DIRECT_PATH", "The direct alignments file (GIZA format).").IsRequired();
			_inverseArgument = Argument("INVERSE_PATH", "The inverse alignments file (GIZA format).").IsRequired();
			_outputArgument = Argument("OUTPUT_PATH", "The output alignments file.").IsRequired();

			_outputFormatOption = Option("-of|--output-format <ALIGNMENT_FORMAT>",
				"The output alignment format.\nFormats: \"pharaoh\" (default), \"giza\".",
				CommandOptionType.SingleValue);
			_symHeuristicOption = Option("-sh|--sym-heuristic <SYM_HEURISTIC>",
				$"The symmetrization heuristic.\nHeuristics: \"{ToolHelpers.Och}\" (default), \"{ToolHelpers.Union}\", \"{ToolHelpers.Intersection}\", \"{ToolHelpers.Grow}\", \"{ToolHelpers.GrowDiag}\", \"{ToolHelpers.GrowDiagFinal}\", \"{ToolHelpers.GrowDiagFinalAnd}\".",
				CommandOptionType.SingleValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!File.Exists(_directArgument.Value))
			{
				Out.WriteLine("The specified direct alignment file does not exist.");
				return 1;
			}

			if (!File.Exists(_inverseArgument.Value))
			{
				Out.WriteLine("The specified inverse alignment file does not exist.");
				return 1;
			}

			if (!ValidateAlignmentFormatOption(_outputFormatOption.Value()))
			{
				Out.WriteLine("The specified output format is invalid.");
				return 1;
			}

			if (!ToolHelpers.ValidateSymmetrizationHeuristicOption(_symHeuristicOption.Value(), noneAllowed: false))
			{
				Out.WriteLine("The specified symmetrization heuristic is invalid.");
				return 1;
			}

			string outputFormat = _outputFormatOption.Value() ?? Pharaoh;
			SymmetrizationHeuristic heuristic = ToolHelpers.GetSymmetrizationHeuristic(_symHeuristicOption.Value());

			using var directReader = new StreamReader(_directArgument.Value);
			using var inverseReader = new StreamReader(_inverseArgument.Value);
			using var outputWriter = new StreamWriter(_outputArgument.Value);

			int index = 1;
			foreach ((WordAlignmentMatrix matrix, WordAlignmentMatrix invMatrix, IReadOnlyList<string> source, IReadOnlyList<string> target) in ParseGizaAlignments(
				directReader, inverseReader))
			{
				invMatrix.Transpose();
				matrix.SymmetrizeWith(invMatrix, heuristic);

				switch (outputFormat)
				{
					case Giza:
						outputWriter.WriteLine($"# Sentence pair ({index}) source length {source.Count} target length {target.Count}");
						outputWriter.Write(matrix.ToGizaFormat(source, target));
						break;

					case Pharaoh:
						outputWriter.WriteLine(matrix.ToString());
						break;
				}
				index++;
			}

			return 0;
		}

		private IEnumerable<(WordAlignmentMatrix, WordAlignmentMatrix, IReadOnlyList<string>, IReadOnlyList<string>)> ParseGizaAlignments(
			StreamReader directReader, StreamReader inverseReader)
		{
			return ParseGizaAlignments(directReader)
				.Zip(ParseGizaAlignments(inverseReader), (da, ia) => (da.Item1, ia.Item1, da.Item2, da.Item3));
		}

		private IEnumerable<(WordAlignmentMatrix, IReadOnlyList<string>, IReadOnlyList<string>)> ParseGizaAlignments(
			StreamReader reader)
		{
			int lineIndex = 0;
			string[] target = null;
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				line = line.Trim();
				if (line.StartsWith("#"))
				{
					lineIndex = 0;
				}
				else if (lineIndex == 1)
				{
					target = line.Split();
				}
				else if (lineIndex == 2)
				{
					int start = line.IndexOf("({");
					int end = line.IndexOf("})");
					int srcIndex = -1;
					var source = new List<string>();
					var pairs = new List<(int, int)>();
					while (start != -1 && end != -1)
					{
						if (srcIndex > -1)
						{
							string trgIndicesStr = line.Substring(start + 3, end - start - 4).Trim();
							string[] trgIndices = trgIndicesStr.Split();
							foreach (string trgIndex in trgIndices)
								pairs.Add((srcIndex, int.Parse(trgIndex) - 1));
						}
						start = line.IndexOf("({", start + 2);
						string srcWord = line.Substring(end + 3, start - end - 4);
						source.Add(srcWord);
						end = line.IndexOf("})", end + 2);
						srcIndex++;
					}

					var alignment = new WordAlignmentMatrix(srcIndex + 1, target.Length);
					foreach ((int i, int j) in pairs)
						alignment[i, j] = true;
					yield return (alignment, source, target);
				}
			}
		}

		private static bool ValidateAlignmentFormatOption(string value)
		{
			var validFormats = new HashSet<string>
			{
				Pharaoh,
				Giza
			};
			return string.IsNullOrEmpty(value) || validFormats.Contains(value.ToLowerInvariant());
		}
	}
}
