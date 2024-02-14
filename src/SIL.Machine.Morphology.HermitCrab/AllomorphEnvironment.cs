using System;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// This class represents a phonological environment.
    /// </summary>
    public class AllomorphEnvironment : IEquatable<AllomorphEnvironment>
    {
        private readonly ConstraintType _type;
        private readonly Pattern<Word, ShapeNode> _leftEnv;
        private readonly Matcher<Word, ShapeNode> _leftEnvMatcher;
        private readonly Pattern<Word, ShapeNode> _rightEnv;
        private readonly Matcher<Word, ShapeNode> _rightEnvMatcher;

        public AllomorphEnvironment(
            ConstraintType type,
            Pattern<Word, ShapeNode> leftEnv,
            Pattern<Word, ShapeNode> rightEnv
        )
        {
            _type = type;
            if (leftEnv != null && !leftEnv.IsLeaf)
            {
                if (!leftEnv.IsFrozen)
                    throw new ArgumentException("The pattern is not frozen.", "leftEnv");
                _leftEnv = leftEnv;
                _leftEnvMatcher = new Matcher<Word, ShapeNode>(
                    leftEnv,
                    new MatcherSettings<ShapeNode>
                    {
                        AnchoredToStart = true,
                        Direction = Direction.RightToLeft,
                        Filter = ann =>
                            ann.Type()
                                .IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor)
                            && !ann.IsDeleted()
                    }
                );
            }
            if (rightEnv != null && !rightEnv.IsLeaf)
            {
                if (!rightEnv.IsFrozen)
                    throw new ArgumentException("The pattern is not frozen.", "rightEnv");
                _rightEnv = rightEnv;
                _rightEnvMatcher = new Matcher<Word, ShapeNode>(
                    rightEnv,
                    new MatcherSettings<ShapeNode>
                    {
                        AnchoredToStart = true,
                        Filter = ann =>
                            ann.Type()
                                .IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor)
                            && !ann.IsDeleted()
                    }
                );
            }
        }

        public ConstraintType Type
        {
            get { return _type; }
        }

        public string Name { get; set; }

        public Pattern<Word, ShapeNode> LeftEnvironment
        {
            get { return _leftEnv; }
        }

        public Pattern<Word, ShapeNode> RightEnvironment
        {
            get { return _rightEnv; }
        }

        public bool IsWordValid(Word word, Annotation<ShapeNode> morph)
        {
            if (_type == ConstraintType.Exclude)
                return !IsMatch(word, morph);
            return IsMatch(word, morph);
        }

        private bool IsMatch(Word word, Annotation<ShapeNode> morph)
        {
            if (_leftEnvMatcher != null && !_leftEnvMatcher.IsMatch(word, morph.Range.Start.Prev))
                return false;

            if (_rightEnvMatcher != null && !_rightEnvMatcher.IsMatch(word, morph.Range.End.Next))
                return false;

            return true;
        }

        public bool Equals(AllomorphEnvironment other)
        {
            if (other == null)
                return false;

            if (_type != other._type)
                return false;

            if (_leftEnv == null)
                return other._leftEnv == null;
            if (_rightEnv == null)
                return other._rightEnv == null;

            return _leftEnv.ValueEquals(other._leftEnv) && _rightEnv.ValueEquals(other._rightEnv);
        }

        public override bool Equals(object other)
        {
            return other is AllomorphEnvironment otherEnv && Equals(otherEnv);
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + _type.GetHashCode();
            code = code * 31 + (_leftEnv == null ? 0 : _leftEnv.GetFrozenHashCode());
            code = code * 31 + (_rightEnv == null ? 0 : _rightEnv.GetFrozenHashCode());
            return code;
        }

        public string ToEnvString()
        {
            if (_leftEnv == null || _leftEnv.IsLeaf)
                return string.Format("/ _ {0}", _rightEnv);
            if (_rightEnv == null || _rightEnv.IsLeaf)
                return string.Format("/ {0} _", _leftEnv);
            return string.Format("/ {0} _ {1}", _leftEnv, _rightEnv);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;
            return ToEnvString();
        }
    }
}
