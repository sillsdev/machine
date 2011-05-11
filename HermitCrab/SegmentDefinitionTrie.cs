using System.Collections.Generic;
using System.Linq;
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

		class TrieNode
		{
			List<T> m_values;
			SegmentDefinition m_segDef;
			List<TrieNode> m_children;

			public TrieNode()
				: this(null)
			{
			}

			public TrieNode(SegmentDefinition segDef)
			{
				m_segDef = segDef;
				m_values = new List<T>();
				m_children = new List<TrieNode>();
			}

			public void Add(PhoneticShapeNode node, T value, Direction dir)
			{
				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.MARGIN:
						if (node == node.Owner.GetLast(dir))
						{
							// we are at the end of the phonetic shape, so add the lexical
							// entry to this node
							m_values.Add(value);
							return;
						}
						else
						{
							// skip first margin
							Add(node.GetNext(dir), value, dir);
						}
						break;

					case PhoneticShapeNode.NodeType.BOUNDARY:
						// skip boundaries
						Add(node.GetNext(dir), value, dir);
						break;

					case PhoneticShapeNode.NodeType.SEGMENT:
						Segment seg = (Segment)node;
						TrieNode tnode = null;
						foreach (TrieNode child in m_children)
						{
							if (seg.FeatureValues.FeatureSystem.HasFeatures)
							{
								// we check for exact matches of feature sets when adding
								if (child.m_segDef.SynthFeatureStructure.Equals(seg.FeatureValues))
								{
									tnode = child;
									break;
								}
							}
							else if (child.m_segDef == seg.SegmentDefinition)
							{
								tnode = child;
								break;
							}
						}

						if (tnode == null)
						{
							// new node needs to be added
							tnode = new TrieNode(seg.SegmentDefinition);
							m_children.Add(tnode);
						}

						// recursive call matching child node
						tnode.Add(node.GetNext(dir), value, dir);
						break;
				}
			}

			public IList<Match> Search(PhoneticShapeNode node, Direction dir, bool partialMatch)
			{
				IList<Match> matches = null;
				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.MARGIN:
						if (node == node.Owner.GetLast(dir))
						{
							matches = new List<Match>();
							if (!partialMatch)
							{
								// we are at the end of the phonetic shape, so return
								// all values in this node
								foreach (T value in m_values)
									matches.Add(new Match(value));
							}
						}
						else
						{
							// skip the first margin
							matches = Search(node.GetNext(dir), dir, partialMatch);
						}
						break;

					case PhoneticShapeNode.NodeType.BOUNDARY:
						// skip boundaries
						matches = Search(node.GetNext(dir), dir, partialMatch);
						foreach (Match match in matches)
							match.AddNode(node);
						break;

					case PhoneticShapeNode.NodeType.SEGMENT:
						Segment seg = (Segment)node;
						PhoneticShapeNode nextNode = node.GetNext(dir);
						List<Match> segMatches = new List<Match>();
						foreach (TrieNode child in m_children)
						{
							// check for unifiability when searching
							if (seg.FeatureValues.FeatureSystem.HasFeatures)
							{
								if (seg.FeatureValues.IsUnifiable(child.m_segDef.SynthFeatureStructure))
									segMatches.AddRange(child.Search(nextNode, dir, partialMatch));
							}
							else if (seg.IsSegmentInstantiated(child.m_segDef))
							{
								segMatches.AddRange(child.Search(nextNode, dir, partialMatch));
							}
						}

						// if this is an optional node, we can try skipping it
						if (node.IsOptional)
							segMatches.AddRange(Search(nextNode, dir, partialMatch));

						matches = segMatches;

						foreach (Match match in matches)
							match.AddNode(node);
						break;
				}

				if (partialMatch)
				{
					foreach (T value in m_values)
						matches.Add(new Match(value));
				}

				return matches;
			}

			public override string ToString()
			{
				return m_segDef.ToString();
			}
		}

		TrieNode m_root;
		int m_numValues = 0;
		Direction m_dir;

		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentDefinitionTrie&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="dir">The direction.</param>
		public SegmentDefinitionTrie(Direction dir)
		{
			m_dir = dir;
			m_root = new TrieNode();
		}

		public Direction Direction
		{
			get
			{
				return m_dir;
			}
		}

		/// <summary>
		/// Adds the specified lexical entry.
		/// </summary>
		/// <param name="entry">The lexical entry.</param>
		public void Add(PhoneticShape shape, T value)
		{
			m_root.Add(shape.GetFirst(m_dir), value, m_dir);
			m_numValues++;
		}

		public void Clear()
		{
			m_root = new TrieNode();
			m_numValues = 0;
		}

		public int Count
		{
			get
			{
				return m_numValues;
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
			return new Set<Match>(m_root.Search(shape.GetFirst(m_dir), m_dir, false));
		}

		public IEnumerable<Match> SearchPartial(PhoneticShape shape)
		{
			return new Set<Match>(m_root.Search(shape.GetFirst(m_dir), m_dir, true));
		}
	}
}
