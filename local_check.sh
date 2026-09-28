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

dotnet tool restore
dotnet restore
dotnet csharpier check .
if [ $? -ne 0 ]; then
  exit 1
fi
dotnet build --no-restore -c Release
if [ $? -ne 0 ]; then
  exit 1
fi
dotnet test --verbosity normal
