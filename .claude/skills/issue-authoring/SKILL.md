---
name: issue-authoring
description: How to write a GitHub issue in sillsdev/machine - one symptom, short body, real evidence.
argument-hint: Optional issue type, title, symptoms, acceptance criteria, or source PR
user-invocable: true
---

# Writing a machine issue

A style guide for the issue text. GitHub issues are the native tracker here;
this repository has no Jira workflow. An `LT-` reference is an external link
only, and only when someone supplied it.

Search open and recently closed issues first. Say what you searched.

## The title

One symptom, in the reader's words, under about 70 characters. No "investigate",
no "improve", no component prefix the labels already carry.

Bad: *Tokenizer improvements*
Good: *USFM attribute is dropped when the locale is tr-TR*

## The lede

Three short sentences, each doing a different job. Nothing else before them.

1. **The symptom.** What goes wrong, in the reader's terms.
2. **The trigger.** The smallest condition that produces it - input, locale,
   platform, version.
3. **The cost.** Who is blocked, what is lost, or what the caller sees instead.

Good: *A USFM attribute is dropped when the tokenizer runs under tr-TR. Any
marker containing an ASCII `i` splits at the wrong index on a Turkish locale.
Round-tripping a Turkish project silently loses the attribute.*

For a feature, the same three: what is missing, when it bites, what it costs.

Keep each sentence under about 25 words. Everything longer goes in the body.

## The body

Short paragraphs or bullets, never a wall. Everything is a labelled line someone
can scan. Write `Unknown` where you do not know, and say how to find out.

**Bug** - affected package or API; version or commit, OS, runtime; the smallest
input that shows it; expected vs actual; sanitized log or exception; when it
started, or `Unknown`; the test that would catch it.

**Feature** - who is blocked and by what; the behavior proposed, with its
compatibility cost; acceptance criteria an outsider could check; non-goals.

**Porting** - the source PR or commit URL, what behavior matters here, what does
not, and the target projects if known.

Sanitize before posting: no secrets, tokens, customer text, or private project
data.

## Ready

An issue is ready when another maintainer can reproduce the bug, judge the
acceptance criteria, or find the exact change to port - without asking you a
question first.

`create-porting-issue.yml` already files the porting issue after a merge, marked
`AUTO-GENERATED-ISSUE`. Do not write a second one by hand.

Hand back the title, labels, and body. The author decides whether to publish.
