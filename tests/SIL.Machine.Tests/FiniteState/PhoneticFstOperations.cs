using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class PhoneticFstOperations : IFstOperations<AnnotatedStringData, int>
	{
		private readonly Dictionary<char, FeatureStruct> _characters; 

		public PhoneticFstOperations(Dictionary<char, FeatureStruct> characters)
		{
			_characters = characters;
		}

		public void Replace(AnnotatedStringData data, Annotation<int> ann)
		{
			KeyValuePair<char, FeatureStruct> character = _characters
				.Single(kvp => kvp.Value.ValueEquals(ann.FeatureStruct));
			data.Replace(ann.Range.Start, ann.Range.Length, character.Key.ToString());
		}

		public Range<int> Insert(AnnotatedStringData data, Annotation<int> ann, FeatureStruct fs)
		{
			KeyValuePair<char, FeatureStruct> character = _characters.Single(kvp => kvp.Value.ValueEquals(fs));
			data.Insert(ann.Range.End, character.Key.ToString());
			return Range<int>.Create(ann.Range.End, ann.Range.End + 1);
		}

		public void Remove(AnnotatedStringData data, Range<int> range)
		{
			data.Remove(range.Start, range.Length);
		}
	}
}
