# HermitCrab conformance runner

`hc-conformance` runs the grammars of [sillsdev/fieldworks-parser-conformance](https://github.com/sillsdev/fieldworks-parser-conformance)
against machine's HermitCrab engine, in process. The suite is a clone in `conformance/` at the root of this repository,
at the one commit named in `conformance.commit`. Commands run from the repository root. `<hc>` below means
`dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -c Release --`.

## What runs where

| Check | Where it runs |
|---|---|
| `<hc> --fixtures conformance --include-pathological`, `--fixtures conformance/stress --include-pathological`, `--check-manifest`, `--coverage-report`, and that `conformance.commit` is on the suite's `main` | `.github/workflows/conformance.yml` and `./local_check.sh` |
| every fixture, the manifest, the DTD and schemas, the FieldWorks-producible shape rules | `dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests` (`ConformanceFixtureGateTests`, `ConformanceManifestTests`, `FieldWorksProducibleShapeTests`) |
| the 16 stress fixtures under NUnit | `dotnet test ... --filter "TestCategory=Stress"`, which is `[Explicit]` and never runs by default |

## Getting the suite

Nothing to do: building this project (which `dotnet build` and `dotnet test` of the HermitCrab tests do) runs
`EnsureConformanceSuite.targets`, which makes `conformance/` a clone of the suite at the commit in `conformance.commit`.

- Absent: it is cloned (`git clone --filter=blob:none`, so the suite's history is available) into a temporary directory
  beside `conformance/` and renamed, so an interrupted or parallel build never leaves a half-populated suite.
- At the pinned commit: nothing is fetched and nothing is written, so an offline build works.
- At another commit, for example after `git pull` changed the pin: moved to the pinned commit. If `conformance/` has
  uncommitted changes the build stops and names the file; it never touches them.
- At the pinned commit with local changes (edited files, extra fixtures): the build warns and names them, and goes on,
  so a suite author can try a change. The runner prints the same warning, and also warns when `conformance/` is not at
  the pin, which `dotnet run --no-build` would otherwise read silently.
- No git, no network, a connection that stalls (git gives up after 20 seconds without data, the build after three
  minutes in all), or any other failure: the build warns and never fails. The tests then fail with the two commands to
  run by hand, `git clone --filter=blob:none https://github.com/sillsdev/fieldworks-parser-conformance.git conformance`
  and `git -C conformance checkout --detach <the commit in conformance.commit>`.
- `SIL.Machine`, the HermitCrab library and the HermitCrab tool do not reference this project, so building or packing
  them never touches `conformance/`.

To run against a copy of the suite you are changing, set `HC_CONFORMANCE_SUITE=<path to that clone>`. The tests then use
that directory, and the build neither clones nor checks the pin. The runner takes the same path with `--fixtures`. This
is how a change to the suite is tried here before it is merged there.

Never edit, commit from or write into `conformance/` from machine. A change to a grammar, a `words.yaml`, the manifest,
`coverage.csv` or `rules.csv` is a pull request to the suite repository (see its `AGENTS.md`). No runner flag writes into
the suite.

## Bumping the pin

Do this when the suite has a change machine should run, and for no other reason.

1. The change is merged on the suite's `main` and its CI is green. A pin must be on `main`: CI fails a pin that is not an
   ancestor of the suite's `origin/main`.
2. Put the full 40-character commit SHA, nothing else, on the one line of `conformance.commit`.
3. Build once (`dotnet build tests/SIL.Machine.Morphology.HermitCrab.Tests`) so `conformance/` moves to it, then run the
   checks under "Before you push".
4. Commit `conformance.commit` with a message that says what is now true, for example "Run the conformance suite at
   `<short sha>`, which adds the `<name>` fixture".
5. If `--check-manifest` or `--coverage-report` reports one of the suite's own files as stale, that is a defect in the
   suite. Fix it there and bump to that commit. Do not patch it in `conformance/`.

## Regenerating what the suite owns

The suite checks in files that are computed from its fixtures. The runner only checks them.

- The manifest: in a clone of the suite, `python3 scripts/generate_manifest.py`. Machine's `--check-manifest` must agree
  with it byte for byte.
- `coverage.csv` and `rules.csv`: `<hc> --fixtures conformance --coverage-report --coverage-output <clone of the suite>`
  writes fresh copies into the clone. The output directory may not be inside `--fixtures`.
- The expected rows for a new or changed fixture: `<hc> --fixtures <suite clone> --include-pathological --propose` prints
  the `words.yaml` patch for each mismatch and writes nothing. Read every row against the rules before accepting it. The
  `batch` command of `src/SIL.Machine.Morphology.HermitCrab.Tool` records the same signatures for a word list.

## Running another engine

`--adapter "<command template>"` runs the same fixtures through an external engine instead of the C# engine. The
template placeholders are `{grammar}`, `{words}`, `{output}` and `{morpher}`; the engine writes one row per word to
`{output}` in the format of `batch` in `src/SIL.Machine.Morphology.HermitCrab.Tool`, as the suite's `PROTOCOL.md`
describes. Pass `--capabilities phonology` (the default in adapter mode is none) so fixtures the engine can run are not
skipped. The template is split at spaces into the program and its arguments, the same way as a line of an
`hc --script-file`; a quote only groups words and is not passed on, so write `--grammar {grammar}`, not
`--grammar="{grammar}"`. Each placeholder is filled in after the split, so a path with spaces still reaches the engine
as one argument. For example, with a built PanGloss:

```
<hc> --fixtures conformance --capabilities phonology --adapter "/path/to/pangloss batch {grammar} {words} {output}"
```

The exit code is 0 when every run passes, 1 on a failure and 2 on a harness error; each failure is a `[FAIL] <id>` line.

## Before you push

Run each command from the repository root and read the output. Do not push on a partial run.

```
cat conformance.commit
git -C conformance rev-parse HEAD
pwsh ./scripts/comment-hygiene.ps1
dotnet csharpier check .
dotnet build tests/SIL.Machine.Morphology.HermitCrab.Tests -c Release
<hc> --fixtures conformance --include-pathological
<hc> --fixtures conformance/stress --include-pathological
<hc> --fixtures conformance --check-manifest
<hc> --fixtures conformance --coverage-report
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests -c Release
```

The two commits on the first line must match. `./local_check.sh --agent-strict` runs the repository-wide version of the
same checks, including that the pin is on the suite's `main`.

In `conformance/`, `edge-cases/deep-optional-affix-nesting` carries a `budget_ms` of 15 s, and each stress fixture carries
60 to 120 s. Run Release builds; Debug is markedly slower. A loaded machine can still exceed one; rerun on a quiet
machine before concluding the engine regressed, and do not raise a budget here. Budgets are the suite's.
