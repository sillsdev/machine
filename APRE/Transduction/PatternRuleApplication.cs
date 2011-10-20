namespace SIL.APRE.Transduction
{
	public struct PatternRuleApplication<TOffset>
	{
		private readonly IData<TOffset> _output;
		private readonly Annotation<TOffset> _resumeAnn;

		public PatternRuleApplication(IData<TOffset> output, Annotation<TOffset> resumeAnn)
		{
			_output = output;
			_resumeAnn = resumeAnn;
		}

		public IData<TOffset> Output
		{
			get { return _output; }
		}

		public Annotation<TOffset> ResumeAnnotation
		{
			get { return _resumeAnn; }
		}
	}
}
