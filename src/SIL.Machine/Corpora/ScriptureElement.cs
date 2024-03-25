using System;

namespace SIL.Machine.Corpora
{
    public class ScriptureElement : IEquatable<ScriptureElement>, IComparable<ScriptureElement>
    {
        public ScriptureElement(int position, string name)
        {
            Position = position;
            Name = name;
        }

        public int Position { get; }
        public string Name { get; }

        int IComparable<ScriptureElement>.CompareTo(ScriptureElement other)
        {
            return CompareTo(other, strict: true);
        }

        public int CompareTo(ScriptureElement other, bool strict = true)
        {
            if (strict)
            {
                int res = Position.CompareTo(other.Position);
                if (res != 0)
                    return res;
            }

            return Name.CompareTo(other.Name);
        }

        public bool Equals(ScriptureElement other)
        {
            return Position == other.Position && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is ScriptureElement se && Equals(se);
        }

        public override int GetHashCode()
        {
            int hashCode = 23;
            hashCode = hashCode * 31 + Position.GetHashCode();
            hashCode = hashCode * 31 + Name.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            if (Position == 0)
                return Name;
            return $"{Position}:{Name}";
        }
    }
}
