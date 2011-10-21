using SIL.APRE;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class should be extended by all phonological rules.
	/// </summary>
	public abstract class PhonologicalRule : IDBearerBase
	{
		protected PhonologicalRule(string id)
			: base(id)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of this phonological rule
		/// during analysis is on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceAnalysis { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tracing of this phonological rule
		/// during synthesis is on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceSynthesis { get; set; }

		public abstract IRule<Word, ShapeNode> AnalysisRule { get; }

		public abstract IRule<Word, ShapeNode> SynthesisRule { get; }

		public abstract void Compile();
	}
}
