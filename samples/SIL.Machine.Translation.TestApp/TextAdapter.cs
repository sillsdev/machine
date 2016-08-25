using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.TestApp
{
	public class TextAdapter : IText
	{
		private readonly TextViewModel _textViewModel;
		private readonly bool _isSource;

		public TextAdapter(TextViewModel textViewModel, bool isSource)
		{
			_textViewModel = textViewModel;
			_isSource = isSource;
		}

		public string Id => _textViewModel.Name;

		public IEnumerable<TextSegment> Segments
		{
			get
			{
				IList<Segment> segments = _isSource ? _textViewModel.SourceSegments : _textViewModel.TargetSegments;
				int segmentNum = 1;
				foreach (Segment segment in segments.Where(s => s.IsApproved))
					yield return new TextSegment(new TextSegmentRef(1, segmentNum), segment.Text);
			}
		}
	}
}
