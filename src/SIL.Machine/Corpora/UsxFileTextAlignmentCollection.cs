using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsxFileTextAlignmentCollection : ITextAlignmentCollection
	{
		private readonly IRangeTokenizer<string, int, string> _srcWordTokenizer;
		private readonly IRangeTokenizer<string, int, string> _trgWordTokenizer;
		private readonly string _srcFileName;
		private readonly string _trgFileName;
		private readonly ScrVers _srcVersification;
		private readonly ScrVers _trgVersification;
		private readonly UsxVerseParser _parser;

		public UsxFileTextAlignmentCollection(IRangeTokenizer<string, int, string> srcWordTokenizer,
			IRangeTokenizer<string, int, string> trgWordTokenizer, string srcFileName, string trgFileName,
			ScrVers srcVersification = null, ScrVers trgVersification = null)
		{
			_srcWordTokenizer = srcWordTokenizer;
			_trgWordTokenizer = trgWordTokenizer;
			_srcFileName = srcFileName;
			_trgFileName = trgFileName;
			_srcVersification = srcVersification;
			_trgVersification = trgVersification;

			Id = CorporaHelpers.GetUsxId(_srcFileName);
			SortKey = CorporaHelpers.GetScriptureTextSortKey(Id);

			_parser = new UsxVerseParser();
		}

		public string Id { get; }

		public string SortKey { get; }

		public IEnumerable<TextAlignment> Alignments
		{
			get
			{
				using (var srcStream = new FileStream(_srcFileName, FileMode.Open, FileAccess.Read))
				using (var trgStream = new FileStream(_trgFileName, FileMode.Open, FileAccess.Read))
				{
					IEnumerable<UsxVerse> srcVerses = _parser.Parse(srcStream);
					IEnumerable<UsxVerse> trgVerses = _parser.Parse(trgStream);

					using (IEnumerator<UsxVerse> enumerator1 = srcVerses.GetEnumerator())
					using (IEnumerator<UsxVerse> enumerator2 = trgVerses.GetEnumerator())
					{
						var rangeInfo = new RangeInfo();

						bool sourceCompleted = !enumerator1.MoveNext();
						bool targetCompleted = !enumerator2.MoveNext();
						while (!sourceCompleted && !targetCompleted)
						{
							UsxVerse srcVerse = enumerator1.Current;
							UsxVerse trgVerse = enumerator2.Current;

							var srcVerseRef = new VerseRef(Id, srcVerse.Chapter, srcVerse.Verse, _srcVersification);
							var trgVerseRef = new VerseRef(Id, trgVerse.Chapter, trgVerse.Verse, _trgVersification);

							int compare = srcVerseRef.CompareTo(trgVerseRef);
							if (compare < 0)
							{
								sourceCompleted = !enumerator1.MoveNext();
							}
							else if (compare > 0)
							{
								targetCompleted = !enumerator2.MoveNext();
							}
							else
							{
								if (srcVerseRef.HasMultiple || trgVerseRef.HasMultiple)
								{
									if (rangeInfo.IsInRange
										&& ((srcVerseRef.HasMultiple && !trgVerseRef.HasMultiple
											&& srcVerse.Text.Length > 0)
										|| (!srcVerseRef.HasMultiple && trgVerseRef.HasMultiple
											&& trgVerse.Text.Length > 0)
										|| (srcVerseRef.HasMultiple && trgVerseRef.HasMultiple
											&& srcVerse.Text.Length > 0 && trgVerse.Text.Length > 0)))
									{
										TextAlignment rangeAlignment = CreateTextAlignment((VerseRef)rangeInfo.VerseRef,
											rangeInfo.SourceNodes, rangeInfo.TargetNodes);
										if (rangeAlignment.AlignedWordPairs.Count > 0)
											yield return rangeAlignment;
									}

									if (!rangeInfo.IsInRange)
										rangeInfo.VerseRef = srcVerseRef;
									rangeInfo.SourceNodes.AddRange(srcVerse.Nodes);
									rangeInfo.TargetNodes.AddRange(trgVerse.Nodes);
								}
								else
								{
									if (rangeInfo.IsInRange)
									{
										TextAlignment rangeAlignment = CreateTextAlignment((VerseRef)rangeInfo.VerseRef,
											rangeInfo.SourceNodes, rangeInfo.TargetNodes);
										if (rangeAlignment.AlignedWordPairs.Count > 0)
											yield return rangeAlignment;
									}

									TextAlignment alignment = CreateTextAlignment(srcVerseRef, srcVerse.Nodes,
										trgVerse.Nodes);
									if (alignment.AlignedWordPairs.Count > 0)
										yield return alignment;
								}
								sourceCompleted = !enumerator1.MoveNext();
								targetCompleted = !enumerator2.MoveNext();
							}
						}
					}
				}
			}
		}

		public ITextAlignmentCollection Invert()
		{
			return new UsxFileTextAlignmentCollection(_trgWordTokenizer, _srcWordTokenizer, _trgFileName, _srcFileName,
				_trgVersification, _srcVersification);
		}

		private TextAlignment CreateTextAlignment(VerseRef verseRef, IReadOnlyList<XNode> srcNodes,
			IReadOnlyList<XNode> trgNodes)
		{
			Dictionary<string, HashSet<int>> srcLinks = GetLinks(_srcWordTokenizer, srcNodes);
			Dictionary<string, HashSet<int>> trgLinks = GetLinks(_trgWordTokenizer, trgNodes);

			var wordPairs = new List<AlignedWordPair>();
			foreach (KeyValuePair<string, HashSet<int>> srcLink in srcLinks)
			{
				if (trgLinks.TryGetValue(srcLink.Key, out HashSet<int> trgIndices))
				{
					foreach (int srcIndex in srcLink.Value)
					{
						foreach (int trgIndex in trgIndices)
							wordPairs.Add(new AlignedWordPair(srcIndex, trgIndex));
					}
				}
			}
			return new TextAlignment(verseRef, wordPairs.OrderBy(wp => wp.TargetIndex).ThenBy(wp => wp.SourceIndex));
		}

		private Dictionary<string, HashSet<int>> GetLinks(
			IRangeTokenizer<string, int, string> wordTokenizer, IReadOnlyList<XNode> nodes)
		{
			XElement prevParent = null;
			var sb = new StringBuilder();
			var linkStrs = new List<(Range<int>, string)>();
			foreach (XNode node in nodes)
			{
				XElement parent = node.Parent;
				while (parent != null && parent.Name != "para")
					parent = parent.Parent;

				if (parent != prevParent && sb.Length > 0)
					sb.Append(" ");

				int start = sb.Length;
				sb.Append(node);
				if (node is XElement e && e.Name == "wg")
					linkStrs.Add((Range<int>.Create(start, sb.Length), (string)e.Attribute("target_links")));
				prevParent = parent;
			}
			string text = sb.ToString().Trim();

			int i = 0;
			var segmentLinks = new Dictionary<string, HashSet<int>>();
			using (IEnumerator<Range<int>> tokenEnumerator = wordTokenizer.TokenizeAsRanges(text).GetEnumerator())
			using (IEnumerator<(Range<int>, string)> linkStrEnumerator = linkStrs.GetEnumerator())
			{
				bool tokensCompleted = !tokenEnumerator.MoveNext();
				bool linksCompleted = !linkStrEnumerator.MoveNext();
				while (!tokensCompleted && !linksCompleted)
				{
					Range<int> tokenRange = tokenEnumerator.Current;
					(Range<int> linkRange, string linkStr) = linkStrEnumerator.Current;
					string[] links = linkStr.Split(';');

					int compare = tokenRange.CompareTo(linkRange);
					if (compare < 0)
					{
						if (tokenRange.Contains(linkRange))
						{
							foreach (string link in links)
								segmentLinks.GetOrCreate(link).Add(i);
						}
						else
						{
							tokensCompleted = !tokenEnumerator.MoveNext();
							i++;
						}
					}
					else if (compare > 0)
					{
						if (linkRange.Contains(tokenRange))
						{
							foreach (string link in links)
								segmentLinks.GetOrCreate(link).Add(i);
						}
						else
						{
							linksCompleted = !linkStrEnumerator.MoveNext();
						}
					}
					else
					{
						foreach (string link in links)
							segmentLinks.GetOrCreate(link).Add(i);

						tokensCompleted = !tokenEnumerator.MoveNext();
						i++;
						linksCompleted = !linkStrEnumerator.MoveNext();
					}
				}
			}
			return segmentLinks;
		}

		private class RangeInfo
		{
			public VerseRef? VerseRef { get; set; }
			public List<XNode> SourceNodes { get; } = new List<XNode>();
			public List<XNode> TargetNodes { get; } = new List<XNode>();
			public bool IsInRange => VerseRef != null;
		}
	}
}
