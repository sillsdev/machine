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

dotnet tool restore || failed+=(tool-restore)
dotnet restore || failed+=(restore)
dotnet csharpier check . || failed+=(csharpier)
dotnet build --no-restore -c Release || failed+=(build)
dotnet test --verbosity normal || failed+=(test)
dotnet run --no-build -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance --   --semantic-coverage --repository-root . || failed+=(semantic-coverage)
dotnet run --no-build -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance --   --fixtures conformance || failed+=(conformance)
python conformance/parity-check.py || failed+=(parity-check)

if [ "${#failed[@]}" -ne 0 ]; then
  echo "local_check: FAILED (${failed[*]})"
  exit 1
fi
echo "local_check: PASSED"
