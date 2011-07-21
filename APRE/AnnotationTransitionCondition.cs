using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	public class AnnotationTransitionCondition<TOffset> : ITransitionCondition<TOffset, FeatureStructure> 
	{
		private readonly HashSet<string> _annotationTypes;
		private readonly bool _notAnnotationTypes;

		public AnnotationTransitionCondition(string annotationType, bool notAnnotationTypes)
		{
			_annotationTypes = new HashSet<string> { annotationType };
			_notAnnotationTypes = notAnnotationTypes;
		}

		public AnnotationTransitionCondition(IEnumerable<string> annotationTypes, bool notAnnotationTypes)
		{
			_annotationTypes = new HashSet<string>(annotationTypes);
			_notAnnotationTypes = notAnnotationTypes;
		}

		public bool IsMatch(Annotation<TOffset> ann, ModeType mode, ref FeatureStructure data)
		{
			if (ann == null)
				return false;

			return _notAnnotationTypes ? !_annotationTypes.Contains(ann.Type) : _annotationTypes.Contains(ann.Type);
		}

		public ITransitionCondition<TOffset, FeatureStructure> Negation()
		{
			return new AnnotationTransitionCondition<TOffset>(_annotationTypes, !_notAnnotationTypes);
		}

		public ITransitionCondition<TOffset, FeatureStructure> Conjunction(ITransitionCondition<TOffset, FeatureStructure> cond)
		{
			var annCond = (AnnotationTransitionCondition<TOffset>) cond;
			if (!_notAnnotationTypes && !annCond._notAnnotationTypes)
				return new AnnotationTransitionCondition<TOffset>(_annotationTypes.Intersect(annCond._annotationTypes), false);
			if (!_notAnnotationTypes && annCond._notAnnotationTypes)
				return new AnnotationTransitionCondition<TOffset>(_annotationTypes.Except(annCond._annotationTypes), false);
			if (_notAnnotationTypes && !annCond._notAnnotationTypes)
				return new AnnotationTransitionCondition<TOffset>(annCond._annotationTypes.Except(_annotationTypes), false);

			return new AnnotationTransitionCondition<TOffset>(_annotationTypes.Union(annCond._annotationTypes), true);
		}

		public bool IsSatisfiable
		{
			get { return _notAnnotationTypes || _annotationTypes.Any(); }
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			if (_notAnnotationTypes)
				sb.Append("!");
			sb.Append("{");
			bool first = true;
			foreach (string type in _annotationTypes)
			{
				if (!first)
					sb.Append(", ");
				sb.Append(type);
				first = false;
			}
			sb.Append("}");
			return sb.ToString();
		}
	}
}
