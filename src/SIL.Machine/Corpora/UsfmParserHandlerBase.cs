using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public class UsfmParserHandlerBase : IUsfmParserHandler
    {
        public virtual void StartUsfm(UsfmParserState state) { }

        public virtual void EndUsfm(UsfmParserState state) { }

        public virtual void GotMarker(UsfmParserState state, string marker) { }

        public virtual void StartBook(UsfmParserState state, string marker, string code) { }

        public virtual void EndBook(UsfmParserState state) { }

        public virtual void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        ) { }

        public virtual void Verse(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        ) { }

        public virtual void StartPara(
            UsfmParserState state,
            string marker,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        ) { }

        public virtual void EndPara(UsfmParserState state, string marker) { }

        public virtual void StartChar(
            UsfmParserState state,
            string markerWithoutPlus,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        ) { }

        public virtual void EndChar(
            UsfmParserState state,
            string marker,
            IReadOnlyList<UsfmAttribute> attributes,
            bool closed
        ) { }

        public virtual void StartNote(UsfmParserState state, string marker, string caller, string category) { }

        public virtual void EndNote(UsfmParserState state, string marker, bool closed) { }

        public virtual void StartTable(UsfmParserState state) { }

        public virtual void EndTable(UsfmParserState state) { }

        public virtual void StartRow(UsfmParserState state, string marker) { }

        public virtual void EndRow(UsfmParserState state, string marker) { }

        public virtual void StartCell(UsfmParserState state, string marker, string align, int colspan) { }

        public virtual void EndCell(UsfmParserState state, string marker) { }

        public virtual void Text(UsfmParserState state, string text) { }

        public virtual void Unmatched(UsfmParserState state, string marker) { }

        public virtual void Ref(UsfmParserState state, string marker, string display, string target) { }

        public virtual void StartSidebar(UsfmParserState state, string marker, string category) { }

        public virtual void EndSidebar(UsfmParserState state, string marker, bool closed) { }

        public virtual void OptBreak(UsfmParserState state) { }

        public virtual void Milestone(
            UsfmParserState state,
            string marker,
            bool startMilestone,
            IReadOnlyList<UsfmAttribute> attributes
        ) { }
    }
}
