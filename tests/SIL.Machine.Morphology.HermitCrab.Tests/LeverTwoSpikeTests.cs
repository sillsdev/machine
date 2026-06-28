using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// LEVER_2.md spike (algorithm-level, symbol alphabet): prove that LAZY composition of an inverse-
/// phonology transducer (Pinv: surface→underlying) with a morphotactic acceptor (Lex: underlying, tokens
/// on states) recovers a <b>deleted</b> segment AND that the lexicon <b>constrains</b> the restoration —
/// the property that the runtime inverse lacked (it restored deleted segments everywhere → garbage).
///
/// Toy: root "sat" + suffix "-d", rule t→∅ / _d, so sat+d = "satd" → surface "sad". Analyzing "sad" must
/// restore the deleted "t" to recover [sat, -d] — but a t-restoration must only survive where Lex has a
/// "t" arc. Deletion (not substitution) is the case every prior approach died on, so the spike targets it.
/// </summary>
public class LeverTwoSpikeTests
{
    // A tiny transition graph. Arc: (input symbol or "" for ε, output symbol, target state).
    private sealed class Graph
    {
        public readonly Dictionary<int, List<(string In, string Out, int To)>> Arcs = new();
        public readonly Dictionary<int, string> TokenOnEntry = new();
        public readonly HashSet<int> Accepting = new();
        public int Start;

        public void Add(int from, string inSym, string outSym, int to)
        {
            if (!Arcs.TryGetValue(from, out var list))
                Arcs[from] = list = new List<(string, string, int)>();
            list.Add((inSym, outSym, to));
        }

        public IEnumerable<(string In, string Out, int To)> From(int s) =>
            Arcs.TryGetValue(s, out var list) ? list : Enumerable.Empty<(string, string, int)>();
    }

    // Pinv: surface → underlying. Identity on s/a/d, plus an ε:t arc (restore a deleted t) that must be
    // followed by consuming a "d" — i.e. "a t was deleted before this d".
    private static Graph BuildPinv()
    {
        var g = new Graph { Start = 0 };
        g.Accepting.Add(0);
        g.Add(0, "s", "s", 0);
        g.Add(0, "a", "a", 0);
        g.Add(0, "d", "d", 0);
        g.Add(0, "", "t", 1); // ε-input: restore an underlying t (consumes no surface)
        g.Add(1, "d", "d", 0); // the restored t must be immediately before a surface d
        return g;
    }

    // Lex: underlying acceptor. Path s-a-t-(root "sat")-d-(suffix "-d"). Optionally a second root "sad"
    // (no t) to show the t-restoration is NOT taken where the lexicon lacks a t.
    private static Graph BuildLex(bool includeSadRoot)
    {
        var g = new Graph { Start = 0 };
        // root "sat" then suffix "d": 0-s->1-a->2-t->3 (root) -d->4 (suffix)
        g.Add(0, "s", "s", 1);
        g.Add(1, "a", "a", 2);
        g.Add(2, "t", "t", 3);
        g.TokenOnEntry[3] = "sat";
        g.Add(3, "d", "d", 4);
        g.TokenOnEntry[4] = "-d";
        g.Accepting.Add(4);
        if (includeSadRoot)
        {
            // a distinct bare root "sad": 0-s->10-a->11-d->12 (root "sad", accepting). No t arc.
            g.Add(0, "s", "s", 10);
            g.Add(10, "a", "a", 11);
            g.Add(11, "d", "d", 12);
            g.TokenOnEntry[12] = "sad";
            g.Accepting.Add(12);
        }
        return g;
    }

    /// <summary>On-the-fly product walk of Pinv ⊗ Lex over the surface. A config is
    /// (pinvState, lexState, tokens). Pinv consumes surface and emits underlying; that underlying must
    /// unify (here: equal) a Lex arc, which advances Lex and accrues its token. ε-input Pinv arcs
    /// (restorations) advance in the closure without consuming surface.</summary>
    private static HashSet<string> Analyze(Graph pinv, Graph lex, string surface)
    {
        var start = new List<(int P, int L, string Toks)> { (pinv.Start, lex.Start, "") };
        List<(int P, int L, string Toks)> frontier = Closure(pinv, lex, start);
        foreach (char c in surface)
        {
            string s = c.ToString();
            var next = new List<(int, int, string)>();
            foreach ((int p, int l, string toks) in frontier)
            {
                foreach ((string inSym, string outSym, int pTo) in pinv.From(p))
                {
                    if (inSym != s)
                        continue; // this arc consumes a different surface symbol
                    foreach ((string lin, string _, int lTo) in lex.From(l))
                    {
                        if (lin == outSym)
                            next.Add((pTo, lTo, toks + Tok(lex, lTo)));
                    }
                }
            }
            frontier = Closure(pinv, lex, next);
            if (frontier.Count == 0)
                break;
        }
        var results = new HashSet<string>();
        foreach ((int p, int l, string toks) in frontier)
        {
            if (pinv.Accepting.Contains(p) && lex.Accepting.Contains(l))
                results.Add(toks.Trim('+'));
        }
        return results;
    }

    // Apply ε-input Pinv arcs (deletion restorations) to fixpoint: each emits an underlying symbol that
    // must unify a Lex arc (the lexicon constraint that prunes spurious restorations).
    private static List<(int P, int L, string Toks)> Closure(
        Graph pinv,
        Graph lex,
        List<(int P, int L, string Toks)> configs
    )
    {
        var seen = new HashSet<(int, int, string)>(configs);
        var result = new List<(int, int, string)>(configs);
        var stack = new Stack<(int P, int L, string Toks)>(configs);
        while (stack.Count > 0)
        {
            (int p, int l, string toks) = stack.Pop();
            foreach ((string inSym, string outSym, int pTo) in pinv.From(p))
            {
                if (inSym != "")
                    continue; // only ε-input arcs in the closure
                foreach ((string lin, string _, int lTo) in lex.From(l))
                {
                    if (lin != outSym)
                        continue; // restoration only survives where the lexicon has this underlying symbol
                    var nc = (pTo, lTo, toks + Tok(lex, lTo));
                    if (seen.Add(nc))
                    {
                        result.Add(nc);
                        stack.Push(nc);
                    }
                }
            }
        }
        return result;
    }

    private static string Tok(Graph lex, int state) =>
        lex.TokenOnEntry.TryGetValue(state, out string? t) ? "+" + t : "";

    [Test]
    public void LazyComposition_RecoversDeletedSegment()
    {
        HashSet<string> got = Analyze(BuildPinv(), BuildLex(includeSadRoot: false), "sad");
        Assert.That(got, Does.Contain("sat+-d"), "must restore the deleted t and recover [sat, -d]");
        Assert.That(got.Count, Is.EqualTo(1), "no spurious analyses — restoration is lexicon-constrained");
    }

    [Test]
    public void LazyComposition_RestorationIsLexiconConstrained()
    {
        // With a bare root "sad" added, "sad" has TWO valid analyses: the bare root (no restoration) and
        // the deleted-t form. The walk finds exactly those — restoration fires only where Lex has a t.
        HashSet<string> got = Analyze(BuildPinv(), BuildLex(includeSadRoot: true), "sad");
        Assert.That(got, Does.Contain("sat+-d"), "deleted-t analysis");
        Assert.That(got, Does.Contain("sad"), "bare-root analysis (no restoration)");
        Assert.That(got.Count, Is.EqualTo(2), "exactly the two lexicon-valid analyses — no garbage from over-restoration");
    }

    [Test]
    public void LazyComposition_NonWordYieldsNothing()
    {
        // "saa": no Lex path accepts it, with or without restoration → empty (the t-restoration cannot
        // rescue it because Lex never has the needed arcs). Soundness of the mechanism.
        HashSet<string> got = Analyze(BuildPinv(), BuildLex(includeSadRoot: true), "saa");
        Assert.That(got, Is.Empty, "a non-word must yield nothing even with deletion-restoration available");
    }
}
