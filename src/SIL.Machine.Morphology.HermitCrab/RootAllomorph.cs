using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph in a lexical entry.
	/// </summary>
	public class RootAllomorph : Allomorph
	{
		private readonly Segments _segments;

		/// <summary>
		/// Initializes a new instance of the <see cref="RootAllomorph"/> class.
		/// </summary>
		public RootAllomorph(Segments segments)
		{
			_segments = segments;
		}

		/// <summary>
		/// Gets the segments.
		/// </summary>
		public Segments Segments
		{
			get { return _segments; }
		}

		public StemName StemName { get; set; }

		public bool IsBound { get; set; }

		protected override bool ConstraintsEqual(Allomorph other)
		{
			var otherAllo = other as RootAllomorph;
			if (otherAllo == null)
				return false;

			return base.ConstraintsEqual(other) && IsBound == otherAllo.IsBound;
		}

		internal override bool IsWordValid(Morpher morpher, Word word)
		{
			if (!base.IsWordValid(morpher, word))
				return false;

			if (IsBound && word.Allomorphs.Count == 1)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.BoundRoot, this, null);
				return false;
			}

			if (StemName != null && !StemName.IsRequiredMatch(word.SyntacticFeatureStruct))
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.RequiredStemName, this, StemName);
				return false;
			}

			foreach (RootAllomorph otherAllo in ((LexEntry) Morpheme).Allomorphs.Where(a => a != this && a.StemName != null))
			{
				if (!otherAllo.StemName.IsExcludedMatch(word.SyntacticFeatureStruct, StemName))
				{
					if (morpher.TraceManager.IsTracing)
						morpher.TraceManager.Failed(morpher.Language, word, FailureReason.ExcludedStemName, this, otherAllo.StemName);
					return false;
				}
			}

			return true;
		}
	}
}
