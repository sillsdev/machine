using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Statistics;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation
{
	public class UnigramTruecaser : ITruecaser
	{
		private ConditionalFrequencyDistribution<string, string> _casing;
		private Dictionary<string, (string Token, int Count)> _bestTokens;
		private string _modelPath;

		public UnigramTruecaser(string modelPath = null)
		{
			_casing = new ConditionalFrequencyDistribution<string, string>();
			_bestTokens = new Dictionary<string, (string Token, int Count)>();
			if (!string.IsNullOrEmpty(modelPath))
				Load(modelPath);
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
					ParseLine(line);
			}
		}

		public void Load(string path)
		{
			Reset();
			_modelPath = path;
			if (!File.Exists(path))
				return;

			using (var reader = new StreamReader(path))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
					ParseLine(line);
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

		public void Save(string modelPath)
		{
			_modelPath = modelPath;
			Save();
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

		private void ParseLine(string line)
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

		private class Trainer : UnigramTruecaserTrainer
		{
			private readonly UnigramTruecaser _truecaser;

			public Trainer(UnigramTruecaser truecaser, IEnumerable<TextRow> corpus)
				: base(corpus)
			{
				_truecaser = truecaser;
			}

			public override async Task SaveAsync()
			{
				_truecaser._casing = NewTruecaser._casing;
				_truecaser._bestTokens = NewTruecaser._bestTokens;
				await _truecaser.SaveAsync();
			}

			public override void Save()
			{
				_truecaser._casing = NewTruecaser._casing;
				_truecaser._bestTokens = NewTruecaser._bestTokens;
				_truecaser.Save();
			}
		}
	}
}
