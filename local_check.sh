#!/bin/bash
# Usage: ./local_check.sh [--agent-strict]
#
# --agent-strict adds the comment-hygiene check and fails on a violation in the
# lines this branch adds. Agents must pass it; humans need not.

agent_strict=false
if [ "${1:-}" = "--agent-strict" ]; then
  agent_strict=true
  shift
fi

if [ "$#" -ne 0 ]; then
  echo "Usage: $0 [--agent-strict]" >&2
  exit 2
fi

if [ "$agent_strict" = true ]; then
  if ! command -v pwsh > /dev/null 2>&1; then
    echo "--agent-strict needs PowerShell 7 (pwsh) on PATH." >&2
    exit 1
  fi
  pwsh -NoProfile -File ./scripts/comment-hygiene.ps1
  if [ $? -ne 0 ]; then
    exit 1
  fi
fi

# Every step runs even after one fails, and the last line names the failures, so
# output read through tail cannot pass off a build failure as a test pass.
failed=()

# conformance/ is the suite clone at the commit in conformance.commit; the build below clones it or moves it to the
# pin. HC_CONFORMANCE_SUITE=<path> uses another copy.
suite="${HC_CONFORMANCE_SUITE:-conformance}"

dotnet tool restore || failed+=(tool-restore)
dotnet restore || failed+=(restore)
dotnet csharpier check . || failed+=(csharpier)
dotnet build --no-restore -c Release || failed+=(build)
dotnet test --verbosity normal || failed+=(test)

# What the suite looks like before the runner steps, so edits you made to it do not read as the runner's.
suite_state() {
  git -C "$suite" status --porcelain --untracked-files=all 2>/dev/null
  git -C "$suite" diff 2>/dev/null
}
suite_before=$(suite_state | sha1sum)

# The same checks as .github/workflows/conformance.yml.
hc_conformance=(dotnet run --no-build -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance --)
"${hc_conformance[@]}" --fixtures "$suite" --include-pathological || failed+=(conformance)
"${hc_conformance[@]}" --fixtures "$suite/stress" --include-pathological || failed+=(conformance-stress)
"${hc_conformance[@]}" --fixtures "$suite" --check-manifest || failed+=(manifest)
"${hc_conformance[@]}" --fixtures "$suite" --coverage-report || failed+=(coverage-report)
# The runner steps must not have changed the suite, tracked or untracked.
[ "$(suite_state | sha1sum)" = "$suite_before" ] || failed+=(suite-modified)
# The pin must be on the suite's main (CI checks the same). Not checked without a network or with another suite.
if [ -z "${HC_CONFORMANCE_SUITE:-}" ]; then
  if git -C conformance fetch --quiet origin main; then
    git -C conformance merge-base --is-ancestor "$(tr -d '[:space:]' < conformance.commit)" origin/main || failed+=(pin-not-on-main)
  else
    echo "local_check: could not fetch the suite's main, so the pin was not checked against it"
  fi
fi

if [ "${#failed[@]}" -ne 0 ]; then
  echo "local_check: FAILED (${failed[*]})"
  exit 1
fi
echo "local_check: PASSED"
