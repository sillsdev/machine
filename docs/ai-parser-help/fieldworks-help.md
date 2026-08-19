---
title: FieldWorks' own built-in Help — pointing an LLM at it too
grounded_in: DistFiles/Helps/FieldWorks_Language_Explorer_Help.chm (sillsdev/FwHelps, checked out as a submodule of sillsdev/FieldWorks at DistFiles/Helps), decompiled locally with hh.exe to inspect topic structure
category: reference
when_to_use: your question is about how to *do* something in the FLEx UI (insert an affix template slot, build an environment, set up a compound rule), not just why the engine behaves a certain way
---

## Why this exists

Everything else in this reference (`broken/`, `speed/`, `workflow/`, `texts/`) documents HermitCrab's
engine internals and failure modes — grounded in the open-source parser code itself. FieldWorks
also ships its own extensive **end-user Help** ("Language Explorer Help"): task-oriented, UI-level
documentation for every tool and dialog, including the Grammar tools that build the very rules
HermitCrab executes. That Help is a genuinely large, useful resource an LLM could draw on too — it
just isn't fetchable by URL the way this repo's markdown is, so it needs a different workflow.

## What it is and where it lives

FieldWorks' installed Help is a **compiled HTML Help file**
(`FieldWorks_Language_Explorer_Help.chm`), authored in Adobe RoboHelp, sitting next to
`FieldWorks.exe` in a `Helps` folder. The source repo that produces it,
[`sillsdev/FwHelps`](https://github.com/sillsdev/FwHelps) — checked out in a FieldWorks clone as
the `DistFiles/Helps` submodule — is public, but it tracks only the **compiled** `.chm`/`.chw`
output (plus a few PDFs), not the individual RoboHelp topic sources.

Decompiling that `.chm` locally (see below) extracts **1,554 individual HTML topic files**,
organized into `Overview/`, `Beginning_Tasks/`, `Basic_Tasks/`, `Advanced_Tasks/`,
`Morphology_and_Parsing_Tasks/`, `Lexicography_Tasks/`, `Using_Tools/`, and
`User_Interface/Field_Descriptions/` + `User_Interface/Menus/` trees — a genuinely large
task-oriented reference, not a handful of tooltips.

## The catch: it's compiled, not plain text

A `.chm` file is a compressed, indexed binary format (ITSF/LZX) — pasting a
`raw.githubusercontent.com` URL to it into ChatGPT or Claude would fetch compressed bytes, not
readable markup, unlike every other URL this reference recommends. To hand its content to an LLM,
extract the topics you need as plain HTML first.

## Extracting topics

Windows ships a CHM decompiler, `hh.exe`, already on every Windows machine that can run
FieldWorks — no extra install:

```powershell
& "C:\Windows\hh.exe" -decompile "C:\path\to\extracted-help" `
    "C:\path\to\FieldWorks\Helps\FieldWorks_Language_Explorer_Help.chm"
```

(find the installed `.chm`'s actual path the same way `getting-started.md` finds
`GenerateHCConfig.exe` — it ships next to `FieldWorks.exe`.) This runs silently and populates the
destination folder with all 1,554 `.htm` topic files plus their table of contents (`.hhc`) and
index (`.hhk`) — open the topic(s) relevant to your question in a text editor and paste/upload
their contents into the chat, the same way you'd paste your exported HermitCrab grammar XML.

For a single topic, it's often faster to just open FLEx itself, press **F1** (or use the
**Help** menu) on the dialog you have a question about, and copy the visible text directly —
no decompiling needed.

## Topic map, by the kind of question you have

These paths are relative to wherever you pointed `-decompile`. They're the topics most relevant to
the same questions this reference's `broken/`, `speed/`, and `workflow/` sections answer from the
engine side — use both together.

**Parser mechanics and settings** (complements the whole reference, especially `speed/`):
- `User_Interface/Menus/Parser/Parsing_words_(HermitCrab).htm` — HermitCrab specifically, as
  opposed to the alternate XAmple/rule-based parsers FieldWorks can also use
- `User_Interface/Menus/Parser/Parser_menu_overview.htm`, `Try_a_word.htm`,
  `Parser_Test_Reports.htm`
- `User_Interface/Menus/Parser/Strata_as_a_String_in_the_Hermit_Crab_properties.htm`
- `User_Interface/Menus/Parser/MaxApps_dialog_box.htm` — the UI for the setting behind
  [`broken/compounding-max-application-count-default.md`](broken/compounding-max-application-count-default.md)
- `User_Interface/Menus/Parser/About_the_Novel_Root_Guesser.htm`
- `User_Interface/Menus/Parser/About_parser_parameters.htm`

**Grammar authoring, tool by tool** (complements `workflow/`):
- `Using_Tools/Grammar_tools/Category_Edit/` — affix templates and slots, including
  `Change_the_optionality_of_a_slot.htm` and `affix_template_table_example.htm`, directly relevant
  to [`workflow/optional-slots-null-affixes-multiple-templates.md`](workflow/optional-slots-null-affixes-multiple-templates.md)
- `Using_Tools/Grammar_tools/Environments/` — building environment strings
- `Using_Tools/Grammar_tools/Phonological_Rules/`, including `Build_a_metathesis_rule.htm`
- `Using_Tools/Grammar_tools/Compound_Rules/`
- `Using_Tools/Grammar_tools/Ad_hoc_Rules/` — co-occurrence rules, relevant to
  [`broken/coocurrence-rule-requires-all-not-any.md`](broken/coocurrence-rule-requires-all-not-any.md)
- `Using_Tools/Grammar_tools/Natural_Classes/`, `Inflection_Features/`, `Exception_Features/`
- `Using_Tools/Grammar_tools/Grammar_Sketch/` — FLEx can auto-generate a prose description of your
  own grammar; often a faster sanity check than describing it to an LLM from scratch

**Conceptual "why would I model it this way" topics** (complements `workflow/`):
- `Morphology_and_Parsing_Tasks/null_allomorphs.htm` — independently corroborates
  [`speed/affix-template-optional-slots.md`](speed/affix-template-optional-slots.md) and
  [`broken/null-affix-cannot-express-default.md`](broken/null-affix-cannot-express-default.md):
  *"Unconstrained nulls can make the parser run a lot longer"* and null allomorphs should be
  constrained with environments as tightly as possible
- `Morphology_and_Parsing_Tasks/reduplication_examples.htm`,
  `Morphology_and_Parsing_Tasks/circumfix_example.htm`,
  `Morphology_and_Parsing_Tasks/Infix_Example.htm` — complements
  [`workflow/circumfixes-and-discontinuous-morphemes.md`](workflow/circumfixes-and-discontinuous-morphemes.md)

**Interlinear texts and word analyses** (complements `texts/`):
- `Using_Tools/Texts_&_Words_tools/Interlinear_Texts/` — including `Analyzing_a_phrase.htm`,
  `Select_New_Analysis.htm`, `Select_existing_analysis.htm`
- `Using_Tools/Texts_&_Words_tools/Word_Analyses/` — including `Specify_status_of_analysis.htm`,
  directly relevant to [`texts/analysis-status-and-ground-truth.md`](texts/analysis-status-and-ground-truth.md)

## How this relates to the rest of this reference

Treat the two as complementary, not overlapping. FieldWorks' own Help is **task-oriented**: it
tells you which dialog to open and which button to click. This reference is **mechanism-oriented**:
it tells you what the engine actually does with what you entered, including behavior FieldWorks'
Help doesn't cover (default-`Any` MPR group matching, the 256-shape-node epenthesis cap, disjunctive
allomorph deferred rechecking, and the rest of `broken/` and `speed/`). When a question is "how do
I set this up," point the LLM at the relevant FieldWorks Help topic above; when it's "why is my
setup producing this result," point it at this reference instead — often both together give a
complete answer.
