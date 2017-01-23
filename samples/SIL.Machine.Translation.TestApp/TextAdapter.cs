using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation.TestApp
{
	public class TextAdapter : IText
	{
		private readonly TextViewModel _textViewModel;
		private readonly bool _isSource;
		private readonly ITokenizer<string, int> _tokenizer;

		public TextAdapter(TextViewModel textViewModel, bool isSource, ITokenizer<string, int> tokenizer)
		{
			_textViewModel = textViewModel;
			_isSource = isSource;
			_tokenizer = tokenizer;
		}

		public string Id => _textViewModel.Name;

		public IEnumerable<TextSegment> Segments
		{
			get
			{
				IList<Segment> segments = _isSource ? _textViewModel.SourceSegments : _textViewModel.TargetSegments;
				int segmentNum = 1;
				foreach (Segment segment in segments.Where(s => s.IsApproved))
					yield return new TextSegment(new TextSegmentRef(1, segmentNum), _tokenizer.TokenizeToStrings(segment.Text).ToArray());
			}
		}
	}
}
