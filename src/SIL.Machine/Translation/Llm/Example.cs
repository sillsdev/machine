using System;

namespace SIL.Machine.Translation.Llm
{
    public sealed class Example : IEquatable<Example>
    {
        public Example(string source, string target)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public string Source { get; }
        public string Target { get; }

        public bool Equals(Example other) =>
            other != null
            && string.Equals(Source, other.Source, StringComparison.Ordinal)
            && string.Equals(Target, other.Target, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as Example);

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(Source) * 397) ^ StringComparer.Ordinal.GetHashCode(Target);
            }
        }

        public override string ToString() => $"Example(Source={Source}, Target={Target})";
    }
}
