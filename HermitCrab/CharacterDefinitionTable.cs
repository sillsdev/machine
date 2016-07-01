using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class CharacterDefinitionTable : ICollection<CharacterDefinition>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Dictionary<string, CharacterDefinition> _charDefLookup;
		private readonly HashSet<CharacterDefinition> _charDefs;

		public CharacterDefinitionTable(SpanFactory<ShapeNode> spanFactory)
		{
			_spanFactory = spanFactory;
			_charDefLookup = new Dictionary<string, CharacterDefinition>();
			_charDefs = new HashSet<CharacterDefinition>();
		}

		public string Name { get; set; }

		public SpanFactory<ShapeNode> SpanFactory
		{
			get { return _spanFactory; }
		}

		public CharacterDefinition Add(string strRep, FeatureStruct fs)
		{
			return Add(strRep.ToEnumerable(), fs);
		}

		/// <summary>
		/// Adds the character definition.
		/// </summary>
		/// <param name="strRep"></param>
		/// <param name="fs"></param>
		public CharacterDefinition Add(IEnumerable<string> strRep, FeatureStruct fs)
		{
			if (!fs.IsFrozen)
				throw new ArgumentException("The feature structure must be immutable.", "fs");
			var cd = new CharacterDefinition(strRep, fs);
			Add(cd);
			return cd;
		}

		/// <summary>
		/// Gets the segment definition for the specified string representation.
		/// </summary>
		/// <param name="strRep">The string representation.</param>
		/// <param name="fs">The symbol definition.</param>
		/// <returns></returns>
		public bool TryGetSymbolFeatureStruct(string strRep, out FeatureStruct fs)
		{
			CharacterDefinition charDef;
			if (TryGetValue(strRep, out charDef))
			{
				fs = charDef.FeatureStruct;
				return true;
			}

			fs = null;
			return false;
		}

		public FeatureStruct GetSymbolFeatureStruct(string strRep)
		{
			return this[strRep].FeatureStruct;
		}

		/// <summary>
		/// Gets all of the string representations that match the specified segment.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <returns>The string representations.</returns>
		public IEnumerable<string> GetMatchingStrReps(ShapeNode node)
		{
			foreach (CharacterDefinition cd in this)
			{
				if (cd.FeatureStruct.IsUnifiable(node.Annotation.FeatureStruct))
				{
					foreach (string representation in cd.Representations)
						yield return representation;
				}
			}
		}

		private bool GetShapeNodes(string str, out IEnumerable<ShapeNode> nodes, out int errorPos)
		{
			var nodesList = new List<ShapeNode>();
			int i = 0;
			while (i < str.Length)
			{
				bool match = false;
				for (int j = str.Length - i; j > 0; j--)
				{
					string s = str.Substring(i, j);
					FeatureStruct fs;
					if (TryGetSymbolFeatureStruct(s, out fs))
					{
						var node = new ShapeNode(SpanFactory, fs.DeepClone());
						node.Annotation.Optional = node.Annotation.Type() == HCFeatureSystem.Boundary;
						nodesList.Add(node);
						i += j;
						match = true;
						break;
					}
				}

				if (!match)
				{
					nodes = null;
					errorPos = i;
					return false;
				}
			}
			nodes = nodesList;
			errorPos = -1;
			return true;
		}

		public Shape Segment(string str)
		{
			IEnumerable<ShapeNode> nodes;
			int errorPos;
			if (GetShapeNodes(str, out nodes, out errorPos))
			{
				var shape = new Shape(_spanFactory, begin => new ShapeNode(_spanFactory, begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor));
				shape.AddRange(nodes);
				return shape;
			}

			throw new InvalidShapeException(str, errorPos);
		}

		/// <summary>
		/// Converts the specified string to a phonetic shape. It matches the longest possible segment
		/// first. If the string segmented successfully, this will return -1, otherwise it will return
		/// the position where the error occurred.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <param name="shape">The shape.</param>
		public int TrySegment(string str, out Shape shape)
		{
			IEnumerable<ShapeNode> nodes;
			int errorPos;
			if (GetShapeNodes(str, out nodes, out errorPos))
			{
				shape = new Shape(_spanFactory, begin => new ShapeNode(_spanFactory, begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor));
				shape.AddRange(nodes);
				return errorPos;
			}

			shape = null;
			return errorPos;
		}

		/// <summary>
		/// Determines whether the specified word matches the specified phonetic shape.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="shape">The phonetic shape.</param>
		/// <returns>
		/// 	<c>true</c> if the word matches the shape, otherwise <c>false</c>.
		/// </returns>
		public bool IsMatch(string word, Shape shape)
		{
			string pattern = shape.ToRegexString(this, false);
			return Regex.IsMatch(word, pattern, RegexOptions.CultureInvariant);
		}

		public IEnumerator<CharacterDefinition> GetEnumerator()
		{
			return _charDefs.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(CharacterDefinition item)
		{
			_charDefs.Add(item);
			foreach (string rep in item.Representations)
				_charDefLookup[rep] = item;
			item.CharacterDefinitionTable = this;
		}

		public void Clear()
		{
			foreach (CharacterDefinition cd in _charDefs)
				cd.CharacterDefinitionTable = null;
			_charDefs.Clear();
			_charDefLookup.Clear();
		}

		public bool Contains(CharacterDefinition item)
		{
			return _charDefs.Contains(item);
		}

		public void CopyTo(CharacterDefinition[] array, int arrayIndex)
		{
			_charDefs.CopyTo(array, arrayIndex);
		}

		public bool Remove(CharacterDefinition item)
		{
			if (_charDefs.Remove(item))
			{
				foreach (string rep in item.Representations)
					_charDefLookup.Remove(rep);
				item.CharacterDefinitionTable = null;
				return true;
			}
			return false;
		}

		public int Count
		{
			get { return _charDefs.Count; }
		}

		bool ICollection<CharacterDefinition>.IsReadOnly
		{
			get { return false; }
		}

		public bool TryGetValue(string key, out CharacterDefinition value)
		{
			return _charDefLookup.TryGetValue(key, out value);
		}

		public bool Contains(string key)
		{
			return _charDefLookup.ContainsKey(key);
		}

		public CharacterDefinition this[string key]
		{
			get { return _charDefLookup[key]; }
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}
