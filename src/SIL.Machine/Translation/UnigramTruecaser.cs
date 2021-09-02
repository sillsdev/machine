using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Statistics;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class UnigramTruecaser : ITruecaser
	{
		private ConditionalFrequencyDistribution<string, string> _casing;
		private Dictionary<string, (string Token, int Count)> _bestTokens;
		private string _modelPath;

		public UnigramTruecaser()
		{
			_casing = new ConditionalFrequencyDistribution<string, string>();
			_bestTokens = new Dictionary<string, (string Token, int Count)>();
		}

		public async Task LoadAsync(string path)
		{
			Reset();
			_modelPath = path;
			if (!File.Exists(path))
				return;

			using (var reader = new StreamReader(path))
			{
				string line;
				while ((line = await reader.ReadLineAsync()) != null)
				{
					string[] parts = line.Split(' ');
					for (int i = 0; i < parts.Length; i += 2)
					{
						string token = parts[i];
						string lowerToken = token.ToLowerInvariant();
						int count = int.Parse(parts[i + 1]);
						_casing[lowerToken].Increment(token, count);
						int bestCount = 0;
						if (_bestTokens.TryGetValue(lowerToken, out (string Token, int Count) bestCase))
							bestCount = bestCase.Count;
						if (count > bestCount)
							_bestTokens[lowerToken] = (token, count);
					}

				}
			}
		}

		public ITrainer CreateTrainer(ITextCorpus corpus)
		{
			return new Trainer(this, corpus);
		}

		public void TrainSegment(IReadOnlyList<string> segment, bool sentenceStart = true)
		{
			for (int i = 0; i < segment.Count; i++)
			{
				string token = segment[i];
				if (token.IsDelayedSentenceStart())
					continue;

				if (!sentenceStart && token.IsSentenceTerminal())
				{
					sentenceStart = true;
					continue;
				}

				if (token.All(c => !char.IsUpper(c) && !char.IsLower(c)))
				{
					sentenceStart = false;
					continue;
				}

				bool increment = false;
				if (!sentenceStart)
					increment = true;
				else if (char.IsLower(token[0]))
					increment = true;

				sentenceStart = false;

				if (increment)
				{
					string lowerToken = token.ToLowerInvariant();
					int newCount = _casing[lowerToken].Increment(token);
					int bestCount = 0;
					if (_bestTokens.TryGetValue(lowerToken, out (string Token, int Count) bestCase))
						bestCount = bestCase.Count;
					if (newCount > bestCount)
						_bestTokens[lowerToken] = (token, newCount);
				}
			}
		}

		public async Task SaveAsync(string modelPath)
		{
			_modelPath = modelPath;
			await SaveAsync();
		}

		public async Task SaveAsync()
		{
			using (var writer = new StreamWriter(_modelPath))
			{
				foreach (string lowerToken in _casing.Conditions)
				{
					FrequencyDistribution<string> counts = _casing[lowerToken];
					string line = string.Join(" ", counts.ObservedSamples.Select(t => $"{t} {counts[t]}"));
					await writer.WriteAsync($"{line}\n");
				}
			}
		}

		public void Save()
		{
			using (var writer = new StreamWriter(_modelPath))
			{
				foreach (string lowerToken in _casing.Conditions)
				{
					FrequencyDistribution<string> counts = _casing[lowerToken];
					string line = string.Join(" ", counts.ObservedSamples.Select(t => $"{t} {counts[t]}"));
					writer.Write($"{line}\n");
				}
			}
		}

		public TranslationResult Truecase(IReadOnlyList<string> sourceSegment, TranslationResult result)
		{
			return new TranslationResult(sourceSegment, Truecase(result.TargetSegment), result.WordConfidences,
				result.WordSources, result.Alignment, result.Phrases);
		}

		public WordGraph Truecase(IReadOnlyList<string> sourceSegment, WordGraph wordGraph)
		{
			var newArcs = new List<WordGraphArc>();
			foreach (WordGraphArc arc in wordGraph.Arcs)
			{
				newArcs.Add(new WordGraphArc(arc.PrevState, arc.NextState, arc.Score, Truecase(arc.Words),
					arc.Alignment, arc.SourceSegmentRange, arc.WordSources, arc.WordConfidences));
			}
			return new WordGraph(newArcs, wordGraph.FinalStates, wordGraph.InitialStateScore);
		}

		public IReadOnlyList<string> Truecase(IReadOnlyList<string> segment)
		{
			var result = new string[segment.Count];
			for (int i = 0; i < segment.Count; i++)
			{
				string token = segment[i];
				string lowerToken = token.ToLowerInvariant();
				if (_bestTokens.TryGetValue(lowerToken, out (string Token, int Count) bestCase))
					token = bestCase.Token;
				result[i] = token;
			}
			return result;
		}

		private void Reset()
		{
			_casing.Reset();
			_bestTokens.Clear();
		}

		private class Trainer : DisposableBase, ITrainer
		{
			private readonly UnigramTruecaser _truecaser;
			private readonly ITextCorpus _corpus;
			private readonly UnigramTruecaser _newTruecaser;

			public TrainStats Stats { get; } = new TrainStats();

			public Trainer(UnigramTruecaser truecaser, ITextCorpus corpus)
			{
				_truecaser = truecaser;
				_corpus = corpus;
				_newTruecaser = new UnigramTruecaser();
			}

			public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
			{
				int stepCount = 0;
				if (progress != null)
					stepCount = _corpus.GetSegments().Count();
				int currentStep = 0;
				foreach (TextSegment segment in _corpus.GetSegments())
				{
					checkCanceled?.Invoke();
					_newTruecaser.TrainSegment(segment);
					currentStep++;
					if (progress != null)
						progress.Report(new ProgressStatus(currentStep, stepCount));
				}
				Stats.TrainedSegmentCount = currentStep;
			}

			public async Task SaveAsync()
			{
				_truecaser._casing = _newTruecaser._casing;
				_truecaser._bestTokens = _newTruecaser._bestTokens;
				await _truecaser.SaveAsync();
			}

			public void Save()
			{
				_truecaser._casing = _newTruecaser._casing;
				_truecaser._bestTokens = _newTruecaser._bestTokens;
				_truecaser.Save();
			}
		}
	}
}
