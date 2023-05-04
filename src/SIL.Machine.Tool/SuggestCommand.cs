using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace SIL.Machine
{
    public class SuggestCommand : CommandBase
    {
        private readonly TranslationModelCommandSpec _modelSpec;
        private readonly ParallelCorpusCommandSpec _corpusSpec;
        private readonly CommandOption _confidenceOption;
        private readonly CommandOption _traceOption;
        private readonly CommandOption _nOption;
        private readonly CommandOption _approveAlignedOption;
        private readonly PreprocessCommandSpec _preprocessSpec;
        private readonly CommandOption _quietOption;

        private int _actionCount;
        private int _charCount;
        private int _totalAcceptedSuggestionCount;
        private int _totalSuggestionCount;
        private int _fullSuggestionCount;
        private int _initSuggestionCount;
        private int _finalSuggestionCount;
        private int _middleSuggestionCount;
        private int[] _acceptedSuggestionCounts;

        public SuggestCommand()
        {
            Name = "suggest";
            Description =
                "Simulates the generation of translation suggestions during an interactive translation session.";

            _modelSpec = AddSpec(new TranslationModelCommandSpec());
            _corpusSpec = AddSpec(new ParallelCorpusCommandSpec { SupportAlignmentsCorpus = false });
            _confidenceOption = Option(
                "-c|--confidence <PERCENTAGE>",
                "The confidence threshold. Default: 0.2.",
                CommandOptionType.SingleValue
            );
            _nOption = Option(
                "-n <NUMBER>",
                "The number of suggestions to generate. Default: 1.",
                CommandOptionType.SingleValue
            );
            _traceOption = Option("-t|--trace <TRACE_FILE>", "The trace file.", CommandOptionType.SingleValue);
            _approveAlignedOption = Option(
                "-aa|--approve-aligned",
                "Approve aligned part of source segment.",
                CommandOptionType.NoValue
            );
            _preprocessSpec = AddSpec(new PreprocessCommandSpec());
            _quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
        }

        protected override async Task<int> ExecuteCommandAsync(CancellationToken cancellationToken)
        {
            int code = await base.ExecuteCommandAsync(cancellationToken);
            if (code != 0)
                return code;

            double confidenceThreshold = 0.2;
            if (_confidenceOption.HasValue())
            {
                if (!double.TryParse(_confidenceOption.Value(), out confidenceThreshold))
                {
                    Out.WriteLine("The specified confidence is invalid.");
                    return 1;
                }
            }

            int n = 1;
            if (_nOption.HasValue())
            {
                if (!int.TryParse(_nOption.Value(), out n))
                {
                    Out.WriteLine("The specified number of suggestions is invalid.");
                    return 1;
                }
            }

            if (_traceOption.HasValue())
            {
                if (!Directory.Exists(_traceOption.Value()))
                    Directory.CreateDirectory(_traceOption.Value());
            }

            var suggester = new PhraseTranslationSuggester() { ConfidenceThreshold = confidenceThreshold };

            if (!_quietOption.HasValue())
                Out.Write("Loading model... ");
            var watch = new Stopwatch();
            int segmentCount = 0;
            _acceptedSuggestionCounts = new int[n];
            using (var smtModel = (IInteractiveTranslationModel)_modelSpec.CreateModel())
            {
                if (!_quietOption.HasValue())
                {
                    Out.WriteLine("done.");
                    Out.Write("Suggesting... ");
                }
                watch.Start();
                using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
                using (StreamWriter traceWriter = CreateTraceWriter())
                {
                    int corpusCount = Math.Min(
                        _corpusSpec.MaxCorpusCount,
                        _corpusSpec.ParallelCorpus.Count(includeEmpty: false)
                    );
                    IParallelTextCorpus corpus = _preprocessSpec.Preprocess(_corpusSpec.ParallelCorpus.WhereNonempty());
                    var translatorFactory = new InteractiveTranslatorFactory(smtModel);
                    progress?.Report(new ProgressStatus(segmentCount, corpusCount));
                    foreach (ParallelTextRow row in corpus)
                    {
                        await TestSegmentAsync(translatorFactory, suggester, n, row, traceWriter, cancellationToken);
                        segmentCount++;
                        progress?.Report(new ProgressStatus(segmentCount, corpusCount));
                        if (segmentCount == _corpusSpec.MaxCorpusCount)
                            break;
                    }
                }
                watch.Stop();
                if (!_quietOption.HasValue())
                    Out.WriteLine("done.");
            }

            Out.WriteLine($"Execution time: {watch.Elapsed:c}");
            Out.WriteLine($"# of Segments: {segmentCount}");
            Out.WriteLine($"# of Suggestions: {_totalSuggestionCount}");
            Out.WriteLine($"# of Correct Suggestions: {_totalAcceptedSuggestionCount}");
            Out.WriteLine("Correct Suggestion Types");
            double fullPcnt = (double)_fullSuggestionCount / _totalAcceptedSuggestionCount;
            Out.WriteLine($"-Full: {fullPcnt:0.0000}");
            double initPcnt = (double)_initSuggestionCount / _totalAcceptedSuggestionCount;
            Out.WriteLine($"-Initial: {initPcnt:0.0000}");
            double finalPcnt = (double)_finalSuggestionCount / _totalAcceptedSuggestionCount;
            Out.WriteLine($"-Final: {finalPcnt:0.0000}");
            double middlePcnt = (double)_middleSuggestionCount / _totalAcceptedSuggestionCount;
            Out.WriteLine($"-Middle: {middlePcnt:0.0000}");
            Out.WriteLine("Correct Suggestion N");
            for (int i = 0; i < _acceptedSuggestionCounts.Length; i++)
            {
                double pcnt = (double)_acceptedSuggestionCounts[i] / _totalAcceptedSuggestionCount;
                Out.WriteLine($"-{i + 1}: {pcnt:0.0000}");
            }
            double ksmr = (double)_actionCount / _charCount;
            Out.WriteLine($"KSMR: {ksmr:0.0000}");
            double precision = (double)_totalAcceptedSuggestionCount / _totalSuggestionCount;
            Out.WriteLine($"Precision: {precision:0.0000}");
            return 0;
        }

        private StreamWriter CreateTraceWriter()
        {
            if (_traceOption.HasValue())
                return ToolHelpers.CreateStreamWriter(_traceOption.Value());

            return null;
        }

        private async Task TestSegmentAsync(
            InteractiveTranslatorFactory translatorFactory,
            ITranslationSuggester suggester,
            int n,
            ParallelTextRow row,
            StreamWriter traceWriter,
            CancellationToken cancellationToken
        )
        {
            traceWriter?.WriteLine($"Segment:      {row.Ref}");
            traceWriter?.WriteLine($"Source:       {row.SourceText}");
            traceWriter?.WriteLine($"Target:       {row.TargetText}");
            traceWriter?.WriteLine(new string('=', 120));
            string[][] prevSuggestionWords = null;
            bool isLastWordSuggestion = false;
            string suggestionResult = null;
            InteractiveTranslator translator = await translatorFactory.CreateAsync(row.SourceText, cancellationToken);
            while (translator.PrefixWordRanges.Count < row.TargetSegment.Count || !translator.IsLastWordComplete)
            {
                int targetIndex = translator.PrefixWordRanges.Count;
                if (!translator.IsLastWordComplete)
                    targetIndex--;

                bool match = false;
                IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(n, translator);
                string[][] suggestionWords = suggestions.Select(s => s.TargetWords.ToArray()).ToArray();
                if (prevSuggestionWords == null || !SuggestionsAreEqual(prevSuggestionWords, suggestionWords))
                {
                    WritePrefix(traceWriter, suggestionResult, translator.Prefix);
                    WriteSuggestions(traceWriter, suggestions);
                    suggestionResult = null;
                    if (suggestions.Any(s => s.TargetWordIndices.Count > 0))
                        _totalSuggestionCount++;
                }
                for (int k = 0; k < suggestions.Count; k++)
                {
                    TranslationSuggestion suggestion = suggestions[k];
                    var accepted = new List<int>();
                    for (int i = 0, j = targetIndex; i < suggestionWords[k].Length && j < row.TargetSegment.Count; i++)
                    {
                        if (suggestionWords[k][i] == row.TargetSegment[j])
                        {
                            accepted.Add(suggestion.TargetWordIndices[i]);
                            j++;
                        }
                        else if (accepted.Count == 0)
                        {
                            j = targetIndex;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (accepted.Count > 0)
                    {
                        translator.AppendToPrefix(
                            string.Join(" ", accepted.Select(j => suggestion.Result.TargetTokens[j]))
                        );
                        isLastWordSuggestion = true;
                        _actionCount++;
                        _totalAcceptedSuggestionCount++;
                        if (accepted.Count == suggestion.TargetWordIndices.Count)
                        {
                            suggestionResult = "ACCEPT_FULL";
                            _fullSuggestionCount++;
                        }
                        else if (accepted[0] == suggestion.TargetWordIndices[0])
                        {
                            suggestionResult = "ACCEPT_INIT";
                            _initSuggestionCount++;
                        }
                        else if (
                            accepted[accepted.Count - 1]
                            == suggestion.TargetWordIndices[suggestion.TargetWordIndices.Count - 1]
                        )
                        {
                            suggestionResult = "ACCEPT_FIN";
                            _finalSuggestionCount++;
                        }
                        else
                        {
                            suggestionResult = "ACCEPT_MID";
                            _middleSuggestionCount++;
                        }
                        _acceptedSuggestionCounts[k]++;
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    if (isLastWordSuggestion)
                    {
                        _actionCount++;
                        isLastWordSuggestion = false;
                        WritePrefix(traceWriter, suggestionResult, translator.Prefix);
                        suggestionResult = null;
                    }

                    int len = translator.IsLastWordComplete
                        ? 0
                        : translator.PrefixWordRanges[translator.PrefixWordRanges.Count - 1].Length;
                    string targetWord = row.TargetSegment[targetIndex];
                    if (len == targetWord.Length)
                    {
                        translator.AppendToPrefix(" ");
                    }
                    else
                    {
                        string c = targetWord.Substring(len, 1);
                        translator.AppendToPrefix(c);
                    }

                    suggestionResult = suggestions.Any(s => s.TargetWordIndices.Count > 0) ? "REJECT" : "NONE";
                    _actionCount++;
                }

                prevSuggestionWords = suggestionWords;
            }

            WritePrefix(traceWriter, suggestionResult, translator.Prefix);

            await translator.ApproveAsync(_approveAlignedOption.HasValue(), cancellationToken);

            _charCount += row.TargetSegment.Sum(w => w.Length + 1);
            traceWriter?.WriteLine();
        }

        private void WritePrefix(StreamWriter traceWriter, string suggestionResult, string prefix)
        {
            if (traceWriter == null || suggestionResult == null)
                return;

            traceWriter.Write(("-" + suggestionResult).PadRight(14));
            traceWriter.WriteLine(prefix);
        }

        private void WriteSuggestions(StreamWriter traceWriter, IReadOnlyList<TranslationSuggestion> suggestions)
        {
            if (traceWriter == null)
                return;

            for (int k = 0; k < suggestions.Count; k++)
            {
                TranslationSuggestion suggestion = suggestions[k];
                bool inSuggestion = false;
                traceWriter.Write($"SUGGESTION {k + 1}  ");
                for (int j = 0; j < suggestion.Result.TargetTokens.Count; j++)
                {
                    if (suggestion.TargetWordIndices.Contains(j))
                    {
                        if (j > 0)
                            traceWriter.Write(" ");
                        if (!inSuggestion)
                            traceWriter.Write("[");
                        inSuggestion = true;
                    }
                    else if (inSuggestion)
                    {
                        traceWriter.Write("] ");
                        inSuggestion = false;
                    }
                    else if (j > 0)
                    {
                        traceWriter.Write(" ");
                    }

                    traceWriter.Write(suggestion.Result.TargetTokens[j]);
                }
                if (inSuggestion)
                    traceWriter.Write("]");
                traceWriter.WriteLine();
            }
        }

        private bool SuggestionsAreEqual(string[][] x, string[][] y)
        {
            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (!x[i].SequenceEqual(y[i]))
                    return false;
            }

            return true;
        }
    }
}
