using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MultipleTextCorpus : ITextCorpus
	{
		public MultipleTextCorpus(IEnumerable<(string Key, ITextCorpus Corpus)> corpora)
		{
			Corpora = corpora.ToDictionary(c => c.Key, c => c.Corpus);
		}

		public MultipleTextCorpus(IEnumerable<KeyValuePair<string, ITextCorpus>> corpora)
		{
			Corpora = corpora.ToDictionary(c => c.Key, c => c.Value);
		}

		public IReadOnlyDictionary<string, ITextCorpus> Corpora { get; }

		public IText this[string id]
		{
			get
			{
				(ITextCorpus corpus, string key, string textId) = GetCorpus(id);
				return new MultipleText(key, corpus.GetText(textId));
			}
		}

		public IEnumerable<IText> Texts => Corpora.SelectMany(kvp =>
			kvp.Value.Texts.Select(t => new MultipleText(kvp.Key, t))).OrderBy(t => t.SortKey);

		public IText CreateNullText(string id)
		{
			(ITextCorpus corpus, string key, string textId) = GetCorpus(id);
			return new MultipleText(key, corpus.CreateNullText(textId));
		}

		private (ITextCorpus, string, string) GetCorpus(string id)
		{
			int index = id.IndexOf('_');
			if (index >= 0)
			{
				string key = id.Substring(0, index);
				if (Corpora.TryGetValue(key, out ITextCorpus corpus))
					return (corpus, key, id.Substring(index + 1));
			}

			throw new ArgumentException("The specified id is invalid.", nameof(id));
		}
	}
}
