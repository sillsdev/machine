using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
    public class StringFeatureValue : SimpleFeatureValue, ICloneable<StringFeatureValue>
    {
        public static implicit operator StringFeatureValue(string str)
        {
            return new StringFeatureValue(str);
        }

        public static explicit operator string(StringFeatureValue sfv)
        {
            if (sfv.Not)
                throw new InvalidCastException("A negated string feature value cannot be converted to a string.");
            return sfv._values.First();
        }

        private HashSet<string> _values;

        public StringFeatureValue()
            : this(Enumerable.Empty<string>()) { }

        public StringFeatureValue(IEnumerable<string> values)
            : this(values, false) { }

        public StringFeatureValue(IEnumerable<string> values, bool not)
        {
            _values = new HashSet<string>(values);
            Not = not;
        }

        public StringFeatureValue(string value)
        {
            _values = new HashSet<string> { value };
        }

        public StringFeatureValue(string varName, bool agree)
            : base(varName, agree)
        {
            _values = new HashSet<string>();
        }

        protected StringFeatureValue(StringFeatureValue sfv)
            : base(sfv)
        {
            _values = new HashSet<string>(sfv._values);
            Not = sfv.Not;
        }

        public IEnumerable<string> Values
        {
            get { return _values; }
        }

        public bool Not { get; private set; }

        protected override bool IsSatisfiable
        {
            get { return base.IsSatisfiable || (Not || _values.Count > 0); }
        }

        protected override bool IsUninstantiated
        {
            get { return base.IsUninstantiated && (Not && _values.Count == 0); }
        }

        public bool Contains(string str)
        {
            return Not ? !_values.Contains(str) : _values.Contains(str);
        }

        public bool Overlaps(IEnumerable<string> strings)
        {
            return Not ? !_values.Overlaps(strings) : _values.Overlaps(strings);
        }

        protected override bool IsSupersetOf(bool not, SimpleFeatureValue other, bool notOther)
        {
            var otherSfv = other as StringFeatureValue;
            if (otherSfv == null)
                return false;

            not = not ? !Not : Not;
            notOther = notOther ? !otherSfv.Not : otherSfv.Not;

            if (!not && !notOther)
                return _values.IsSupersetOf(otherSfv._values);
            if (!not)
                return false;
            if (!notOther)
                return !_values.Overlaps(otherSfv._values);

            return _values.IsSubsetOf(otherSfv._values);
        }

        protected override bool Overlaps(bool not, SimpleFeatureValue other, bool notOther)
        {
            var otherSfv = other as StringFeatureValue;
            if (otherSfv == null)
                return false;

            not = not ? !Not : Not;
            notOther = notOther ? !otherSfv.Not : otherSfv.Not;

            if (!not && !notOther)
                return _values.Overlaps(otherSfv._values);
            if (!not)
                return !_values.IsSubsetOf(otherSfv._values);
            if (!notOther)
                return !_values.IsSupersetOf(otherSfv._values);

            return true;
        }

        protected override void IntersectWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            var otherSfv = other as StringFeatureValue;
            if (otherSfv == null)
                return;

            not = not ? !Not : Not;
            notOther = notOther ? !otherSfv.Not : otherSfv.Not;

            if (!not && !notOther)
            {
                Not = false;
                _values.IntersectWith(otherSfv._values);
            }
            else if (!not)
            {
                Not = false;
                _values.ExceptWith(otherSfv._values);
            }
            else if (!notOther)
            {
                Not = false;
                _values = new HashSet<string>(otherSfv._values.Except(_values));
            }
            else
            {
                Not = true;
                _values.UnionWith(otherSfv._values);
            }
        }

        protected override void UnionWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            var otherSfv = other as StringFeatureValue;
            if (otherSfv == null)
                return;

            not = not ? !Not : Not;
            notOther = notOther ? !otherSfv.Not : otherSfv.Not;

            if (!not && !notOther)
            {
                Not = false;
                _values.UnionWith(otherSfv._values);
            }
            else if (!not)
            {
                Not = true;
                _values.ExceptWith(otherSfv._values);
            }
            else if (!notOther)
            {
                Not = true;
                _values = new HashSet<string>(otherSfv._values.Except(_values));
            }
            else
            {
                Not = true;
                _values.IntersectWith(otherSfv.Values);
            }
        }

        protected override void ExceptWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            var otherSfv = other as StringFeatureValue;
            if (otherSfv == null)
                return;

            not = not ? !Not : Not;
            notOther = notOther ? !otherSfv.Not : otherSfv.Not;

            if (!not && !notOther)
            {
                Not = false;
                _values.ExceptWith(otherSfv._values);
            }
            else if (!not)
            {
                Not = false;
                _values.IntersectWith(otherSfv._values);
            }
            else if (!notOther)
            {
                Not = true;
                _values.UnionWith(otherSfv._values);
            }
            else
            {
                Not = false;
                _values = new HashSet<string>(otherSfv._values.Except(_values));
            }
        }

        protected override SimpleFeatureValue CloneImpl()
        {
            return Clone();
        }

        public override SimpleFeatureValue Negation()
        {
            return IsVariable ? new StringFeatureValue(VariableName, !Agree) : new StringFeatureValue(_values, !Not);
        }

        public bool ValueEquals(StringFeatureValue other)
        {
            if (other == null)
                return false;

            return base.ValueEquals(other) && _values.SetEquals(other._values) && Not == other.Not;
        }

        public override bool ValueEquals(SimpleFeatureValue other)
        {
            var otherSfv = other as StringFeatureValue;
            return other != null && ValueEquals(otherSfv);
        }

        protected override int GetValuesHashCode()
        {
            int code = base.GetValuesHashCode();
            code = code * 31 + Not.GetHashCode();
            code = code * 31 + _values.OrderBy(str => str).GetSequenceHashCode();
            return code;
        }

        public new StringFeatureValue Clone()
        {
            return new StringFeatureValue(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder(base.ToString());
            if (_values.Count > 0)
            {
                bool firstValue = true;
                if (Not)
                    sb.Append('!');
                if (_values.Count == 1)
                {
                    sb.Append('"');
                    sb.Append(_values.First());
                    sb.Append('"');
                }
                else
                {
                    sb.Append('{');
                    foreach (string value in _values.OrderBy(v => v))
                    {
                        if (!firstValue)
                            sb.Append(", ");
                        sb.Append('"');
                        sb.Append(value);
                        sb.Append('"');
                        firstValue = false;
                    }
                    sb.Append('}');
                }
            }
            return sb.ToString();
        }
    }
}
