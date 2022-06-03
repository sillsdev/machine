namespace SIL.Machine.Corpora
{
    public class UsfmAttribute
    {
        public UsfmAttribute(string name, string value, int offset = 0)
        {
            Name = name;
            Value = value;
            Offset = offset;
        }

        public string Name { get; }
        public string Value { get; }

        public int Offset { get; }

        public override string ToString()
        {
            return Name + $"=\"{Value}\"";
        }
    }
}
