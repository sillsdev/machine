using SIL.Collections;

namespace SIL.Machine
{
	public class StringData : IData<int>, IDeepCloneable<StringData>
	{
		private readonly SpanFactory<int> _spanFactory;
		private readonly string _str;
		private readonly Span<int> _span; 
		private readonly AnnotationList<int> _annotations; 

		public StringData(SpanFactory<int> spanFactory, string str)
		{
			_spanFactory = spanFactory;
			_str = str;
			_span = _spanFactory.Create(0, _str.Length);
			_annotations = new AnnotationList<int>(spanFactory);
		}

		protected StringData(StringData sd)
		{
			_spanFactory = sd._spanFactory;
			_str = sd._str;
			_span = sd._span;
			_annotations = sd._annotations.DeepClone();
		}

		public string String
		{
			get { return _str; }
		}

		public Span<int> Span
		{
			get { return _span; }
		}

		public AnnotationList<int> Annotations
		{
			get { return _annotations; }
		}

		public StringData DeepClone()
		{
			return new StringData(this);
		}

		public override string ToString()
		{
			return _str;
		}
	}
}
