using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    // Wire protocol shared by the worker host and the client (newline-delimited JSON over
    // stdin/stdout). A morpheme is identified across the process boundary by its string Id;
    // both ends load the same compiled grammar config, so Ids resolve to the same morphemes.

    /// <summary>A batch of surface forms to analyze (one request line).</summary>
    public sealed class HermitCrabAnalyzeRequest
    {
        public List<string> Words { get; set; } = new List<string>();
    }

    /// <summary>The analyses for a whole batch (one response line), aligned to the request order.</summary>
    public sealed class HermitCrabAnalyzeResponse
    {
        public List<HermitCrabWordResult> Results { get; set; } = new List<HermitCrabWordResult>();
    }

    public sealed class HermitCrabWordResult
    {
        public string Word { get; set; } = string.Empty;

        /// <summary>Set when the surface form could not be segmented (InvalidShapeException).</summary>
        public string? Error { get; set; }

        public List<HermitCrabAnalysisDto> Analyses { get; set; } = new List<HermitCrabAnalysisDto>();
    }

    public sealed class HermitCrabAnalysisDto
    {
        /// <summary>The morphemes in surface order.</summary>
        public List<HermitCrabMorphemeDto> Morphemes { get; set; } = new List<HermitCrabMorphemeDto>();

        public int RootMorphemeIndex { get; set; } = -1;

        public string? Category { get; set; }
    }

    /// <summary>
    /// A morpheme carried across the process boundary. It implements <see cref="IMorpheme"/>, so the
    /// client can hand it straight to <see cref="WordAnalysis"/> without re-loading the grammar.
    /// </summary>
    public sealed class HermitCrabMorphemeDto : IMorpheme
    {
        public string Id { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Gloss { get; set; } = null!;
        public MorphemeType MorphemeType { get; set; }
    }
}
