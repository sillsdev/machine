namespace SIL.APRE
{
	public class StringData : IData<int>
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

		public override string ToString()
		{
			return _str;
		}
	}
}
