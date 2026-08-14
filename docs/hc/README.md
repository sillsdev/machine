# HermitCrab-for-LLMs reference

This is a living reference for asking an LLM (ChatGPT, Claude, or otherwise) questions
about **HermitCrab**, the rule-based morphological parser/generator implemented in this
repository (`sillsdev/machine`, namespace `SIL.Machine.Morphology.HermitCrab`). Each file
covers one topic in enough depth to answer questions about that topic without needing
local access to the repo — code excerpts, mechanisms, and worked examples are inlined.

## How to use this with an LLM

Paste the **raw** URL of the relevant topic file into your chat, e.g.:

```
https://raw.githubusercontent.com/sillsdev/machine/master/docs/hc/affix-templates-and-optionality.md
```

Then ask your question. Use the raw URL (`raw.githubusercontent.com`), not the normal
`github.com/.../blob/...` page — the raw URL returns plain markdown text with no site
chrome, which fetches cleanly for both ChatGPT (web browsing) and Claude (WebFetch) without
JS rendering or auth. If you're not sure which topic file is relevant, paste this README's
raw URL first — an LLM that can follow links will use it as an index; otherwise, browse the
list below yourself.

Do not use these guides as a source of real grammar data — see "What belongs here" below.

## Topics

- [`affix-templates-and-optionality.md`](affix-templates-and-optionality.md) — how HermitCrab's
  `AffixTemplate`/slot mechanism works, why independently-optional affix slots cause
  exponential (`O(2^n)`) parse blowup, and the linear-cost alternative (mutually-exclusive
  rules in one slot).

_(Add new topic files here as they're written, with a one-line description each.)_

## What belongs here

- General HermitCrab engine mechanics: how rules, strata, templates, features, and the
  analysis/synthesis engines work. This is documentation of the open-source parser itself.
- Synthetic/toy grammar snippets used purely to illustrate a mechanism (e.g. `p1`..`p12`,
  `sg`/`pl` × invented cases) are fine.
- **Not** real grammar data for any specific language (e.g. Sena, Amharic, Indonesian, Aweti).
  Those grammars are private and must never be committed to this repo — see the project's
  existing grammar-privacy constraints. If a question requires reasoning about a real
  grammar, describe the relevant structure abstractly instead of pasting the real rules.

## Source grounding

Claims in these guides are grounded in the actual source under
`src/SIL.Machine.Morphology.HermitCrab/` and `src/SIL.Machine/Rules/` as of the commit each
file was last updated, plus the conformance fixtures under `conformance/`. File/line
references may drift as the code evolves; if something looks stale, check the live source
at the paths cited.
