using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// How serious a <see cref="GrammarHealthFinding"/> is. <c>Error</c> means the engine will
    /// behave incorrectly (or refuse the word outright) whenever the offending construct is
    /// exercised, with no further information needed to know that. <c>Warning</c> means the
    /// construct is a genuine risk to the grammar's reliability, but whether it actually causes a
    /// problem for a given word depends on how the grammar's rules use it.
    /// </summary>
    public enum GrammarHealthSeverity
    {
        Warning,
        Error,
    }

    /// <summary>
    /// The stable finding codes <see cref="GrammarHealthChecker"/> reports. Treat these strings,
    /// not <see cref="GrammarHealthFinding.Message"/>, as the identifier a host uses to filter,
    /// suppress, or test for a particular kind of finding -- the message text is free to change.
    /// </summary>
    public static class GrammarHealthCodes
    {
        public const string DuplicateFeatureBundle = "hc-duplicate-feature-bundle";
        public const string UndeclaredSegment = "hc-undeclared-segment";
    }

    /// <summary>
    /// One admissibility problem found in a <see cref="Language"/> by <see cref="GrammarHealthChecker"/>.
    /// This is diagnostic only: producing a finding never changes how the grammar parses.
    /// </summary>
    public class GrammarHealthFinding
    {
        private readonly ReadOnlyCollection<object> _subjects;

        public GrammarHealthFinding(
            GrammarHealthSeverity severity,
            string code,
            string message,
            IEnumerable<object> subjects
        )
        {
            if (code == null)
                throw new ArgumentNullException("code");
            if (message == null)
                throw new ArgumentNullException("message");
            if (subjects == null)
                throw new ArgumentNullException("subjects");

            Severity = severity;
            Code = code;
            Message = message;
            _subjects = new ReadOnlyCollection<object>(subjects.ToList());
        }

        public GrammarHealthSeverity Severity { get; private set; }

        /// <summary>
        /// A stable identifier for the kind of problem found. See <see cref="GrammarHealthCodes"/>.
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// A human-readable description naming the offending declaration(s).
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// The model objects the finding is about (e.g. a <see cref="CharacterDefinitionTable"/>,
        /// the <see cref="CharacterDefinition"/>s that collide, a <see cref="LexEntry"/>, or a
        /// <see cref="ShapeNode"/>), in the order most useful for a host to navigate to them. This
        /// is the object model itself, not a copy or a serialized form, so a host that already
        /// holds the same <see cref="Language"/> can use reference equality to find its own
        /// project-specific wrapper around each subject.
        /// </summary>
        public ReadOnlyCollection<object> Subjects
        {
            get { return _subjects; }
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}: {2}", Severity, Code, Message);
        }
    }
}
