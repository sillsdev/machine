# Template category sharing

This synthetic grammar prevents mixing one template's prefix with another template's suffix.
It also preserves two distinct lexical identities with the same surface.

Template A supplies `pa-` and `-sa`; template B supplies `ta-` and `-la`. Both have the same
required part of speech. Their rules occur only in their own slots, not in the stratum's ordinary
rule list. Thus `pakolosa` and `takolola` have the listed parses, while `pakolola` and `takolosa`
have no complete derivation. Optional slots permit bare `kolo`. Two separate lexical entries
justify exactly the two listed root identities for `mbili`; this is homophony, not proof of
the engine's same-morpheme free-fluctuation mechanism.

## Verification

Expected rows are derived above and verified against C# at
`8bad193454a14422a3be59c3156513691ae3da3b` on 2026-09-15. The original staged fixture records
earlier C# verification at `caa4ddde`. Both new fixtures pass self-check with memoization on and
off: 2 passed, 0 failed, 0 skipped, 16 rows total.

A scratch mutation adds all four affix rules to the stratum's ordinary rule list. The same expected
rows then fail 4/6: both mixed forms gain parses, and both unmixed forms gain duplicate signatures.
This confirms why slot-only reachability is necessary and why set-only comparisons would hide
multiplicity changes. The original grammar remains green.

This is NOT a same-key syntactic-feature collision test, a merge-widening necessity test, or
evidence of a memo hit. Those remain open in
[issue #505](https://github.com/sillsdev/machine/issues/505).
