using System.Collections.Generic;
using SIL.APRE;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a trie data structure. This is a specialized trie that maps phonetic shape keys to
	/// values. Each node in the trie represents a set of features instead of a character.
	/// </summary>
	internal class SegmentDefinitionTrie<T>
	{
		public class Match
		{
			private readonly List<PhoneticShapeNode> _nodes;
			private readonly T _value;

			public Match(T value)
			{
				_value = value;
				_nodes = new List<PhoneticShapeNode>();
			}

			public T Value
			{
				get
				{
					return _value;
				}
			}

			public IList<PhoneticShapeNode> Nodes
			{
				get
				{
					return _nodes;
				}
			}

			public void AddNode(PhoneticShapeNode node)
			{
				_nodes.Insert(0, node);
			}

			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				return Equals(obj as Match);
			}

			public bool Equals(Match other)
			{
				if (other == null)
					return false;

				if (!_value.Equals(other._value))
					return false;

				if (_nodes.Count != other._nodes.Count)
					return false;

				for (int i = 0; i < _nodes.Count; i++)
				{
					if (!_nodes[i].Equals(other._nodes[i]))
						return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int hashCode = 0;
				foreach (PhoneticShapeNode node in _nodes)
					hashCode ^= node.GetHashCode();
				return hashCode ^ _value.GetHashCode();
			}
		}

		private class TrieNode
		{
			private readonly List<T> _values;
			private readonly FeatureStructure _featureStructure;
			private readonly List<TrieNode> _children;

			public TrieNode()
				: this(null)
			{
			}

			private TrieNode(FeatureStructure featureStructure)
			{
				_featureStructure = featureStructure;
				_values = new List<T>();
				_children = new List<TrieNode>();
			}

			public void Add(PhoneticShapeNode node, T value, Direction dir)
			{
				if (node == null)
				{
					// we are at the end of the phonetic shape, so add the lexical
					// entry to this node
					_values.Add(value);
					return;
				}

				switch (node.Annotation.Type)
				{
					case "Boundary":
						// skip boundaries
						Add(node.GetNext(dir), value, dir);
						break;

					case "Segment":
						TrieNode tnode = null;
						foreach (TrieNode child in _children)
						{
							// we check for exact matches of feature sets when adding
							if (child._featureStructure.Equals(node.Annotation.FeatureStructure))
							{
								tnode = child;
								break;
							}
						}

						if (tnode == null)
						{
							// new node needs to be added
							tnode = new TrieNode((FeatureStructure) node.Annotation.FeatureStructure.Clone());
							_children.Add(tnode);
						}

						// recursive call matching child node
						tnode.Add(node.GetNext(dir), value, dir);
						break;
				}
			}

			public IList<Match> Search(PhoneticShapeNode node, Direction dir, bool partialMatch)
			{
				IList<Match> matches = null;
				if (node == null)
				{
					matches = new List<Match>();
					if (!partialMatch)
					{
						// we are at the end of the phonetic shape, so return
						// all values in this node
						foreach (T value in _values)
							matches.Add(new Match(value));
					}
				}
				else
				{
					switch (node.Annotation.Type)
					{
						case "Boundary":
							// skip boundaries
							matches = Search(node.GetNext(dir), dir, partialMatch);
							foreach (Match match in matches)
								match.AddNode(node);
							break;

						case "Segment":
							PhoneticShapeNode nextNode = node.GetNext(dir);
							var segMatches = new List<Match>();
							foreach (TrieNode child in _children)
							{
								// check for unifiability when searching
								if (node.Annotation.FeatureStructure.IsUnifiable(child._featureStructure))
									segMatches.AddRange(child.Search(nextNode, dir, partialMatch));
							}

							// if this is an optional node, we can try skipping it
							if (node.Annotation.IsOptional)
								segMatches.AddRange(Search(nextNode, dir, partialMatch));

							matches = segMatches;

							foreach (Match match in matches)
								match.AddNode(node);
							break;
					}	
				}

				if (partialMatch && matches != null)
				{
					foreach (T value in _values)
						matches.Add(new Match(value));
				}

				return matches;
			}

			public override string ToString()
			{
				return _featureStructure.ToString();
			}
		}

		private TrieNode _root;
		private int _numValues;
		private readonly Direction _dir;

		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentDefinitionTrie&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="dir">The direction.</param>
		public SegmentDefinitionTrie(Direction dir)
		{
			_dir = dir;
			_root = new TrieNode();
		}

		public Direction Direction
		{
			get
			{
				return _dir;
			}
		}

		/// <summary>
		/// Adds the specified lexical entry.
		/// </summary>
		/// <param name="shape"></param>
		/// <param name="value"></param>
		public void Add(PhoneticShape shape, T value)
		{
			_root.Add(shape.GetFirst(_dir), value, _dir);
			_numValues++;
		}

		public void Clear()
		{
			_root = new TrieNode();
			_numValues = 0;
		}

		public int Count
		{
			get
			{
				return _numValues;
			}
		}

		/// <summary>
		/// Searches for all values which match the specified phonetic 
		/// shape.
		/// </summary>
		/// <param name="shape">The phonetic shape.</param>
		/// <returns>All matching values.</returns>
		public IEnumerable<Match> Search(PhoneticShape shape)
		{
			return new HashSet<Match>(_root.Search(shape.GetFirst(_dir), _dir, false));
		}

		public IEnumerable<Match> SearchPartial(PhoneticShape shape)
		{
			return new HashSet<Match>(_root.Search(shape.GetFirst(_dir), _dir, true));
		}
	}
}
