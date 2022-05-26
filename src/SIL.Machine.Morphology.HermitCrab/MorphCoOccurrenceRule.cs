using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The co-occurrence adjacency type
    /// </summary>
    public enum MorphCoOccurrenceAdjacency
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

    /// <summary>
    /// This class represents a morpheme co-occurrence rule. Morpheme co-occurrence rules are used
    /// to determine if a list of morphemes co-occur with a specific morpheme.
    /// </summary>
    public abstract class MorphCoOccurrenceRule<T> : IEquatable<MorphCoOccurrenceRule<T>> where T : class
    {
        private readonly ConstraintType _type;
        private readonly List<T> _others;
        private readonly MorphCoOccurrenceAdjacency _adjacency;

        /// <summary>
        /// Initializes a new instance of the <see cref="MorphCoOccurrenceRule{T}"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="others">The others.</param>
        /// <param name="adjacency">The adjacency.</param>
        protected MorphCoOccurrenceRule(
            ConstraintType type,
            IEnumerable<T> others,
            MorphCoOccurrenceAdjacency adjacency
        )
        {
            _type = type;
            _others = others.ToList();
            _adjacency = adjacency;
        }

        public ConstraintType Type
        {
            get { return _type; }
        }

        public IEnumerable<T> Others
        {
            get { return _others; }
        }

        public MorphCoOccurrenceAdjacency Adjacency
        {
            get { return _adjacency; }
        }

        public bool IsWordValid(T key, Word word)
        {
            if (_type == ConstraintType.Exclude)
                return !CoOccurs(key, word);
            return CoOccurs(key, word);
        }

        /// <summary>
        /// Determines if all of the specified morphemes co-occur with the key morpheme.
        /// </summary>
        private bool CoOccurs(T key, Word word)
        {
            List<Allomorph> morphList = word.AllomorphsInMorphOrder.ToList();
            List<T> others = _others.ToList();

            switch (_adjacency)
            {
                case MorphCoOccurrenceAdjacency.Anywhere:
                    foreach (Allomorph morph in morphList)
                        others.Remove(GetMorphObject(morph));
                    break;

                case MorphCoOccurrenceAdjacency.SomewhereToLeft:
                case MorphCoOccurrenceAdjacency.AdjacentToLeft:
                    for (int i = 0; i < morphList.Count; i++)
                    {
                        T curMorphObj = GetMorphObject(morphList[i]);
                        if (key == curMorphObj)
                        {
                            break;
                        }
                        if (others.Count > 0 && others[0] == curMorphObj)
                        {
                            if (_adjacency == MorphCoOccurrenceAdjacency.AdjacentToLeft)
                            {
                                if (i == morphList.Count - 1)
                                    return false;

                                T nextMorphObj = GetMorphObject(morphList[i + 1]);
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

                case MorphCoOccurrenceAdjacency.SomewhereToRight:
                case MorphCoOccurrenceAdjacency.AdjacentToRight:
                    for (int i = morphList.Count - 1; i >= 0; i--)
                    {
                        T curMorphObj = GetMorphObject(morphList[i]);
                        if (key == curMorphObj)
                        {
                            break;
                        }
                        if (others.Count > 0 && others[others.Count - 1] == curMorphObj)
                        {
                            if (_adjacency == MorphCoOccurrenceAdjacency.AdjacentToRight)
                            {
                                if (i == 0)
                                    return false;

                                T prevMorphObj = GetMorphObject(morphList[i - 1]);
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

        protected abstract T GetMorphObject(Allomorph morph);

        public bool Equals(MorphCoOccurrenceRule<T> other)
        {
            if (other == null)
                return false;

            return _type == other._type && _adjacency == other._adjacency && _others.SequenceEqual(other._others);
        }

        public override bool Equals(object other)
        {
            var otherRule = other as MorphCoOccurrenceRule<T>;
            return otherRule != null && Equals(otherRule);
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + _type.GetHashCode();
            code = code * 31 + _adjacency.GetHashCode();
            code = code * 31 + _others.GetSequenceHashCode();
            return code;
        }
    }
}
