using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class PhoneticFstOperations : IFstOperations<AnnotatedStringData, int>
	{
		private readonly SpanFactory<int> _spanFactory; 
		private readonly Dictionary<char, FeatureStruct> _characters; 

		public PhoneticFstOperations(SpanFactory<int> spanFactory, Dictionary<char, FeatureStruct> characters)
		{
			_spanFactory = spanFactory;
			_characters = characters;
		}

		public void Replace(AnnotatedStringData data, Annotation<int> ann)
		{
			KeyValuePair<char, FeatureStruct> character = _characters.Single(kvp => kvp.Value.ValueEquals(ann.FeatureStruct));
			data.Replace(ann.Span.Start, ann.Span.Length, character.Key.ToString());
		}

		public Span<int> Insert(AnnotatedStringData data, Annotation<int> ann, FeatureStruct fs)
		{
			KeyValuePair<char, FeatureStruct> character = _characters.Single(kvp => kvp.Value.ValueEquals(fs));
			data.Insert(ann.Span.End, character.Key.ToString());
			return _spanFactory.Create(ann.Span.End, ann.Span.End + 1);
		}

		public void Remove(AnnotatedStringData data, Span<int> span)
		{
			data.Remove(span.Start, span.Length);
		}
	}
}
