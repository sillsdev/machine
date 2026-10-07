---
name: issue-authoring
description: How to write a GitHub issue in sillsdev/machine - one symptom, short body, real evidence.
argument-hint: Optional issue type, title, symptoms, acceptance criteria, or source PR
user-invocable: true
---

# Writing a machine issue

Search open and recently closed issues first, and say what you searched. Then
write the title and the three-sentence lede; the form fields hold the rest.

GitHub issues are the tracker here. An `LT-` reference is an external link, and
only when someone supplied it.

## 1. Title: one symptom

Under about 70 characters, in the reader's words. No "investigate", no
"improve", no component prefix the labels already carry.

Bad: *Tokenizer improvements*
Good: *USFM attribute is dropped when the locale is tr-TR*

## 2. Lede: three sentences

1. **The symptom** - what goes wrong.
2. **The trigger** - the smallest condition that produces it.
3. **The cost** - who is blocked, or what the caller sees instead.

Under 25 words each. For a feature, the same three: what is missing, when it
bites, what it costs.

Good: *A USFM attribute is dropped when the tokenizer runs under tr-TR. Any
marker containing an ASCII `i` splits at the wrong index on a Turkish locale.
Round-tripping a Turkish project silently loses the attribute.*

## 3. Body: labelled lines, never a wall

Write `Unknown` where you do not know, and say how to find out.

- **Bug** - affected API; version, OS, runtime; the smallest input that shows
  it; expected vs actual; sanitized log; when it started; the test that catches
  it.
- **Feature** - who is blocked and by what; the proposed behavior and its
  compatibility cost; acceptance criteria an outsider could check; non-goals.
- **Porting** - the source PR URL, what behavior matters here, what does not.

Sanitize first: no secrets, tokens, customer text, or private project data.

## 4. Check it is ready

Ready means another maintainer can reproduce the bug, judge the acceptance
criteria, or find the change to port - without asking you a question.

A workflow files the porting issue after a merge, marked `AUTO-GENERATED-ISSUE`.
Do not write a second one by hand.

Hand back the title, labels, and body. The author decides whether to publish.
