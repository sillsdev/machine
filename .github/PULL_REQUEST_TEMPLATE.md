## Quick summary

<!-- What changed, why it matters, and the one question reviewers should answer. Keep this short. -->

## Issue / porting context

<!-- Use Fixes #N only when this PR should close a GitHub issue. For machine.py
work, link the source PR or porting issue. -->

## What changed

<!-- Include public API, serialization, package, platform, or compatibility effects. -->

## Deliberately not included

<!-- State relevant deferred behavior or follow-up work. Delete this section if none. -->

## Validation

- [ ] dotnet tool restore
- [ ] dotnet restore
- [ ] dotnet csharpier check .
- [ ] dotnet build --no-restore -c Release
- [ ] dotnet test --verbosity normal
- [ ] ./local_check.sh --agent-strict (required for agents; optional for humans)
- [ ] Applicable SentencePiece CMake build run, or not relevant
- [ ] Applicable package output checked, or not relevant
- [ ] git diff --check and commit-range whitespace checked

<!-- Replace each checked item with the actual result in the PR body. Do not check an unrun command. -->

## Reviewer focus

<!-- Name the files/symbols and risks that deserve attention. -->
