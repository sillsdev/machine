using System.Linq;
using SIL.Machine.Annotations;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph in a lexical entry.
	/// </summary>
	public class RootAllomorph : Allomorph
	{
		private readonly Shape _shape;

		/// <summary>
		/// Initializes a new instance of the <see cref="RootAllomorph"/> class.
		/// </summary>
		/// <param name="shape">The shape.</param>
		public RootAllomorph(Shape shape)
		{
			_shape = shape;
		}

		/// <summary>
		/// Gets the phonetic shape.
		/// </summary>
		/// <value>The phonetic shape.</value>
		public Shape Shape
		{
			get
			{
				return _shape;
			}
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
					morpher.TraceManager.ParseFailed(morpher.Language, word, FailureReason.BoundRoot, this, null);
				return false;
			}

			if (StemName != null && !StemName.IsRequiredMatch(word.SyntacticFeatureStruct))
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.ParseFailed(morpher.Language, word, FailureReason.RequiredStemName, this, StemName);
				return false;
			}

			foreach (RootAllomorph otherAllo in ((LexEntry) Morpheme).Allomorphs.Where(a => a != this && a.StemName != null))
			{
				if (!otherAllo.StemName.IsExcludedMatch(word.SyntacticFeatureStruct, StemName))
				{
					if (morpher.TraceManager.IsTracing)
						morpher.TraceManager.ParseFailed(morpher.Language, word, FailureReason.ExcludedStemName, this, otherAllo.StemName);
					return false;
				}
			}

			return true;
		}
	}
}
