## Quick summary

<!-- First sentence: what a caller can now do, or what stopped being broken.
Then the reviewer's main unknown and its answer. Under 200 words. -->

## Where to look

<!-- One line per risk: the risk, then the test or gate that pins it. -->

## Deliberately not included

<!-- Deferred work and what would unblock it. Delete if none. -->

## Validation

<!-- The exact commands you ran and what they returned. Delete a line you did
not run; do not list a command as evidence unless it produced the result shown.
CI collects coverage and local_check.sh does not, so a local run is not
CI-equivalent. -->

- `./local_check.sh` --
- `./local_check.sh --agent-strict` -- <!-- required of agents, optional for humans -->
- `git diff --check <merge-base>...HEAD` --
- <!-- SentencePiece CMake, package output, or focused test runs, if relevant -->

## Issue / porting context

<!-- Fixes #N only for a real GitHub issue. For machine.py work, link the
source PR. Delete if neither applies. -->

<!-- Longer reasoning belongs below a --- rule, in closed <details> blocks. -->
