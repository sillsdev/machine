using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    /// <summary>
    /// Interface for USFM parsing events.
    /// </summary>
    public interface IUsfmParserHandler
    {
        /// <summary>
        /// Start of the USFM
        /// </summary>
        void StartUsfm(UsfmParserState state);

        /// <summary>
        /// End of the USFM
        /// </summary>
        void EndUsfm(UsfmParserState state);

        /// <summary>
        /// Got a marker of any kind
        /// </summary>
        void GotMarker(UsfmParserState state, string marker);

        /// <summary>
        /// Start of a book element
        /// </summary>
        void StartBook(UsfmParserState state, string marker, string code);

        /// <summary>
        /// End of a book element, not the end of the entire book.
        /// Book element contains the description as text
        /// </summary>
        void EndBook(UsfmParserState state);

        /// <summary>
        /// Chapter element
        /// </summary>
        void Chapter(UsfmParserState state, string number, string marker, string altNumber, string pubNumber);

        /// <summary>
        /// Verse element
        /// </summary>
        void Verse(UsfmParserState state, string number, string marker, string altNumber, string pubNumber);

        /// <summary>
        /// Start of a paragraph
        /// </summary>
        void StartPara(UsfmParserState state, string marker, bool unknown, IReadOnlyList<UsfmAttribute> attributes);

        /// <summary>
        /// End of a paragraph
        /// </summary>
        void EndPara(UsfmParserState state, string marker);

        /// <summary>
        /// Start of a character style
        /// </summary>
        void StartChar(
            UsfmParserState state,
            string markerWithoutPlus,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        );

        /// <summary>
        /// End of a character style
        /// </summary>
        void EndChar(UsfmParserState state, string marker, IReadOnlyList<UsfmAttribute> attributes, bool closed);

        /// <summary>
        /// Start of a note
        /// </summary>
        void StartNote(UsfmParserState state, string marker, string caller, string category);

        /// <summary>
        /// End of a note
        /// </summary>
        void EndNote(UsfmParserState state, string marker, bool closed);

        /// <summary>
        /// Start of a table
        /// </summary>
        void StartTable(UsfmParserState state);

        /// <summary>
        /// End of a table
        /// </summary>
        void EndTable(UsfmParserState state);

        /// <summary>
        /// Start of a row of a table
        /// </summary>
        void StartRow(UsfmParserState state, string marker);

        /// <summary>
        /// End of a row of a table
        /// </summary>
        void EndRow(UsfmParserState state, string marker);

        /// <summary>
        /// Start of a cell within a table row
        /// </summary>
        void StartCell(UsfmParserState state, string marker, string align, int colspan);

        /// <summary>
        /// End of a cell within a table row
        /// </summary>
        void EndCell(UsfmParserState state, string marker);

        /// <summary>
        /// Text element
        /// </summary>
        void Text(UsfmParserState state, string text);

        /// <summary>
        /// Unmatched end marker
        /// </summary>
        void Unmatched(UsfmParserState state, string marker);

        /// <summary>
        /// Automatically extracted ref to a Scripture location
        /// </summary>
        void Ref(UsfmParserState state, string marker, string display, string target);

        /// <summary>
        /// Start of a study Bible sidebar
        /// </summary>
        void StartSidebar(UsfmParserState state, string marker, string category);

        /// <summary>
        /// End of a study Bible sidebar
        /// </summary>
        void EndSidebar(UsfmParserState state, string marker, bool closed);

        /// <summary>
        /// Optional break (// in usfm)
        /// </summary>
        void OptBreak(UsfmParserState state);

        /// <summary>
        /// Milestone start or end
        /// </summary>
        void Milestone(
            UsfmParserState state,
            string marker,
            bool startMilestone,
            IReadOnlyList<UsfmAttribute> attributes
        );
    }
}
