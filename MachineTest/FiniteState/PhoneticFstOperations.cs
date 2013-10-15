using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SIL.Machine.FeatureModel;
using SIL.Machine.FiniteState;

namespace SIL.Machine.Test.FiniteState
{
	public class PhoneticFstOperations : IFstOperations<StringData, int>
	{
		private readonly SpanFactory<int> _spanFactory; 
		private readonly Dictionary<char, FeatureStruct> _characters; 

		public PhoneticFstOperations(SpanFactory<int> spanFactory, Dictionary<char, FeatureStruct> characters)
		{
			_spanFactory = spanFactory;
			_characters = characters;
		}

		public void Replace(StringData data, Annotation<int> ann)
		{
			KeyValuePair<char, FeatureStruct> character = _characters.Single(kvp => kvp.Value.ValueEquals(ann.FeatureStruct));
			data.Replace(ann.Span.Start, ann.Span.Length, character.Key.ToString(CultureInfo.InvariantCulture));
		}

		public Span<int> Insert(StringData data, Annotation<int> ann, FeatureStruct fs)
		{
			KeyValuePair<char, FeatureStruct> character = _characters.Single(kvp => kvp.Value.ValueEquals(fs));
			data.Insert(ann.Span.End, character.Key.ToString(CultureInfo.InvariantCulture));
			return _spanFactory.Create(ann.Span.End, ann.Span.End + 1);
		}

		public void Remove(StringData data, Span<int> span)
		{
			data.Remove(span.Start, span.Length);
		}
	}
}
