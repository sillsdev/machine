using System.Collections.Generic;
using System.Collections.ObjectModel;
using SIL.APRE;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a morpheme co-occurrence rule. Morpheme co-occurrence rules are used
	/// to determine if a list of morphemes co-occur with a specific morpheme.
	/// </summary>
	public class MorphCoOccurrence
	{
		/// <summary>
		/// The object type
		/// </summary>
		public enum ObjectType
		{
			/// <summary>
			/// Allomorph
			/// </summary>
			Allomorph,
			/// <summary>
			/// Morpheme
			/// </summary>
			Morpheme
		}

		/// <summary>
		/// The co-occurrence adjacency type
		/// </summary>
		public enum AdjacencyType
		{
			/// <summary>
			/// Anywhere in the same word
			/// </summary>
			Anywhere,
			/// <summary>
			/// Somewhere to the left
			/// </summary>
			SomewhereToLeft,
			/// <summary>
			/// Somewhere to the right
			/// </summary>
			SomewhereToRight,
			/// <summary>
			/// Adjacent to the left
			/// </summary>
			AdjacentToLeft,
			/// <summary>
			/// Adjacent to the right
			/// </summary>
			AdjacentToRight
		}

		private readonly List<IIDBearer> _others;
		private readonly AdjacencyType _adjacency;
		private readonly ObjectType _objectType;

		/// <summary>
		/// Initializes a new instance of the <see cref="MorphCoOccurrence"/> class.
		/// </summary>
		/// <param name="others">The other allomorphs or morphemes.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="adjacency">The adjacency.</param>
		public MorphCoOccurrence(IEnumerable<IIDBearer> others, ObjectType objectType, AdjacencyType adjacency)
		{
			_others = new List<IIDBearer>(others);
			_objectType = objectType;
			_adjacency = adjacency;
		}

		/// <summary>
		/// Determines if all of the specified morphemes co-occur with the key morpheme.
		/// </summary>
		/// <param name="morphs">The morphs.</param>
		/// <param name="key">The key morpheme.</param>
		/// <returns></returns>
		public bool CoOccurs(Morphs morphs, IIDBearer key)
		{
			Collection<Morph> morphList = morphs;
			var others = new List<IIDBearer>(_others);

			switch (_adjacency)
			{
				case AdjacencyType.Anywhere:
					foreach (Morph morph in morphList)
						others.Remove(GetMorphObject(morph));
					break;

				case AdjacencyType.SomewhereToLeft:
				case AdjacencyType.AdjacentToLeft:
					for (int i = 0; i < morphList.Count; i++)
					{
						IIDBearer curMorphObj = GetMorphObject(morphList[i]);
						if (key == curMorphObj)
						{
							break;
						}
						else if (others.Count > 0 && others[0] == curMorphObj)
						{
							if (_adjacency == AdjacencyType.AdjacentToLeft)
							{
								if (i == morphList.Count - 1)
									return false;

								IIDBearer nextMorphObj = GetMorphObject(morphList[i + 1]);
								if (others.Count > 1)
								{
									if (others[1] != nextMorphObj)
										return false;
								}
								else if (key != nextMorphObj)
								{
									return false;
								}
							}
							others.RemoveAt(0);
						}
					}
					break;

				case AdjacencyType.SomewhereToRight:
				case AdjacencyType.AdjacentToRight:
					for (int i = morphList.Count - 1; i >= 0; i--)
					{
						IIDBearer curMorphObj = GetMorphObject(morphList[i]);
						if (key == curMorphObj)
						{
							break;
						}
						else if (others.Count > 0 && others[others.Count - 1] == curMorphObj)
						{
							if (_adjacency == AdjacencyType.AdjacentToRight)
							{
								if (i == 0)
									return false;

								IIDBearer prevMorphObj = GetMorphObject(morphList[i - 1]);
								if (others.Count > 1)
								{
									if (others[others.Count - 2] != prevMorphObj)
										return false;
								}
								else if (key != prevMorphObj)
								{
									return false;
								}
							}
							others.RemoveAt(others.Count - 1);
						}
					}
					break;
			}

			return others.Count == 0;
		}

		private IIDBearer GetMorphObject(Morph morph)
		{
			switch (_objectType)
			{
				case ObjectType.Allomorph:
					return morph.Allomorph;

				case ObjectType.Morpheme:
					return morph.Allomorph.Morpheme;
			}
			return null;
		}
	}
}
