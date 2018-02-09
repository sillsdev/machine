using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public abstract class TextBase : IText
	{
		protected TextBase(ITokenizer<string, int> wordTokenizer, string id)
		{
			WordTokenizer = wordTokenizer;
			Id = id;
		}

		public string Id { get; }

		protected ITokenizer<string, int> WordTokenizer { get; }

		public abstract IEnumerable<TextSegment> Segments { get; }

		protected TextSegment CreateTextSegment(string text, IComparable segRef)
		{
			string[] segment = WordTokenizer.TokenizeToStrings(text.Trim()).ToArray();
			return new TextSegment(segRef, segment);
		}

		protected TextSegment CreateTextSegment(string text, params int[] indices)
		{
			return CreateTextSegment(text, new TextSegmentRef(indices));
		}
	}
}
