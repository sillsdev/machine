---
name: issue-authoring
description: Use when creating, refining, or triaging a GitHub issue in sillsdev/machine; produce evidence-based bug, feature, or machine.py porting issues without inventing Jira requirements.
argument-hint: Optional issue type, title, symptoms, acceptance criteria, or source PR
user-invocable: true
---

# Issue Authoring

Use GitHub issues as the native issue system for sillsdev/machine. This skill
supports Bug, Feature, and Porting issues. It does not fetch, assign, transition,
or comment on Jira tickets. An LT- reference may be included as an external
reference only when supplied and verified.

## Common intake and evidence gate

1. Search existing open and recently closed GitHub issues for duplicates and
   related PRs before drafting.
2. Ask for the smallest concrete example distinguishing a problem from an
   enhancement request.
3. Separate observed facts, reproduction/acceptance evidence, and hypotheses.
4. Remove secrets, tokens, private data, and unsanitized customer/project data.
5. Name affected version/commit, OS, architecture, runtime, and package when
   known.
6. If a fact is unknown, write Unknown and identify how to verify it.

An issue is ready when another maintainer can reproduce the bug, evaluate the
feature acceptance criteria, or identify the exact source change to port.

## Bug issue

Require:

* concise symptom and affected package/API;
* version or commit, OS, architecture, and .NET runtime;
* minimal input, fixture, code sample, or repository state;
* exact reproduction steps and frequency;
* expected result and actual result;
* sanitized exception/log output;
* regression range or not known; and
* a minimal regression-test idea.

If automation is feasible, propose a failing test before implementation and name
the likely test project. If not, state the concrete reason--visual/manual
behavior, unavailable external service, or packaging infrastructure--and give an
alternative verification plan.

## Feature issue

Require:

* user/problem statement and affected consumers;
* use cases and non-goals;
* proposed behavior or API, including compatibility concerns;
* observable acceptance criteria;
* test strategy and representative edge cases;
* performance/resource/platform constraints; and
* deliberately excluded follow-up work.

Do not prescribe an implementation before behavior and acceptance criteria are
clear. Use Fixes #N only when closing the issue on merge is intended.

## machine.py porting issue

Include source repository and PR/commit URL, target behavior to port, what is
not relevant to machine, verified target projects/files if known,
compatibility/test implications, and source validation evidence or an explicit
gap.

The existing merged-PR workflow normally creates the opposite-repository issue
with title Port '<PR title>', label porting, and this body:

    Port any relevant changes in <PR URL> from <current repo> to <other repo>.

    <!-- AUTO-GENERATED-ISSUE -->

Do not duplicate that issue. If a closing issue reference already contains the
marker, the workflow skips another generated issue. If the port is not covered
by a merged PR, create a normal Porting issue using the fields above.

## Handoff

Return proposed title, type/labels, complete body, duplicate-search result,
evidence gaps, and links. The author decides whether to publish it. Do not
claim that a test, reproduction, or external issue was checked unless it was
actually checked.
