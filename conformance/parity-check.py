#!/usr/bin/env python3
"""
Conformance v1->v2 migration parity proof.

Mechanically verifies the migration floors that guard the ONE irreversible step -- deletion of the v1
fixture tree -- plus an absolute v2 construct-coverage check that outlives the deletion (gate G4).
Exits NONZERO on any violation, so it can gate CI and the delete commit.

Checks (ledger "Floor summary (G4 parity check keys)"):
  A. Provenance    -- every v1 manifest.json 'provenance' string appears VERBATIM (exact value, not
                      substring) in some v2 words.yaml 'provenance:' field. One-way v1 -> v2.
  B. Per-construct -- every construct named in any v1 manifest is exercised by >=1 v2 word (coverage.csv).
  C. XAmple floor  -- every construct covered by a requires:[] v1 fixture is exercised by >=1 word in a
                      requires:[] v2 grammar (quechua/latin/athabaskan or a requires:[] edge-case).
  D. Absolute      -- every constructs.txt construct except 'Tracing (TraceType)' is exercised by >=1
                      v2 word. Independent of v1 tooling, so it still proves G4 after the delete.

Once the v1 tree is deleted, A/B/C find zero v1 fixtures and report "migration complete"; only D runs.

Usage:  python conformance/parity-check.py [conformance_root]   (default: this script's directory)
"""
import csv
import json
import os
import re
import sys

OUT_OF_SCOPE = "Tracing (TraceType)"
EXPECTED_V1_COUNT = 41  # the frozen v1 fixture set, established during the v1->v2 migration


def die(msg):
    print(f"parity-check: FATAL: {msg}", file=sys.stderr)
    sys.exit(2)


def load_constructs_txt(path):
    out = []
    with open(path, encoding="utf-8") as f:
        for raw in f:
            line = raw.strip()
            if line and not line.startswith("#"):
                out.append(line)
    return out


def discover_v1(root):
    """v1 fixture = a directory with manifest.json that is NOT under languages/ or edge-cases/ (those
    are v2, keyed by words.yaml). Returns list of parsed manifests."""
    manifests = []
    for dirpath, _dirs, files in os.walk(root):
        norm = dirpath.replace("\\", "/")
        if "/languages/" in norm + "/" or "/edge-cases/" in norm + "/":
            continue
        if "manifest.json" in files:
            with open(os.path.join(dirpath, "manifest.json"), encoding="utf-8") as f:
                manifests.append(json.load(f))
    return manifests


def load_coverage(path):
    """coverage.csv -> (set of all covered constructs, dict language -> set of constructs)."""
    covered = set()
    by_lang = {}
    with open(path, encoding="utf-8", newline="") as f:
        reader = csv.DictReader(f)
        rows = 0
        for row in reader:
            rows += 1
            construct = row["construct"]
            lang = row["language"]
            if construct:
                covered.add(construct)
                by_lang.setdefault(lang, set()).add(construct)
    if rows == 0:
        die(f"{path} has no data rows -- refusing to prove parity against an empty coverage table")
    return covered, by_lang


PROV_RE = re.compile(r'^\s*provenance:\s*(.*\S)\s*$')
LANG_RE = re.compile(r'^language:\s*(.*\S)\s*$')
REQ_RE = re.compile(r'^requires:\s*\[(.*)\]\s*$')


def _strip_quotes(s):
    if len(s) >= 2 and s[0] in "\"'" and s[-1] == s[0]:
        return s[1:-1]
    return s


def load_v2_words(langs_dir, edge_dir):
    """Scan every v2 words.yaml. Returns (set of provenance strings, dict language -> requires-list)."""
    provenance = set()
    lang_requires = {}
    for base in (langs_dir, edge_dir):
        if not os.path.isdir(base):
            continue
        for name in sorted(os.listdir(base)):
            wy = os.path.join(base, name, "words.yaml")
            if not os.path.isfile(wy):
                continue
            lang = None
            requires = None
            with open(wy, encoding="utf-8") as f:
                for raw in f:
                    # skip full-line comments so a '#'-commented example provenance never counts
                    if raw.lstrip().startswith("#"):
                        continue
                    m = PROV_RE.match(raw)
                    if m:
                        provenance.add(_strip_quotes(m.group(1)))
                        continue
                    if lang is None:
                        lm = LANG_RE.match(raw)
                        if lm:
                            lang = _strip_quotes(lm.group(1))
                    if requires is None:
                        rm = REQ_RE.match(raw)
                        if rm:
                            inner = rm.group(1).strip()
                            requires = [t.strip() for t in inner.split(",") if t.strip()]
            if lang is None:
                die(f"{wy} has no 'language:' field")
            if requires is None:
                die(f"{wy} has no 'requires:' field")
            lang_requires[lang] = requires
    return provenance, lang_requires


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
    root = os.path.abspath(root)
    coverage_csv = os.path.join(root, "coverage.csv")
    constructs_txt = os.path.join(root, "constructs.txt")
    langs_dir = os.path.join(root, "languages")
    edge_dir = os.path.join(root, "edge-cases")

    for p in (coverage_csv, constructs_txt):
        if not os.path.isfile(p):
            die(f"missing required input: {p}")

    checklist = load_constructs_txt(constructs_txt)
    v2_covered, v2_by_lang = load_coverage(coverage_csv)
    v2_provenance, v2_lang_requires = load_v2_words(langs_dir, edge_dir)
    v1 = discover_v1(root)

    failures = []  # list of (check, message)
    print(f"parity-check: root={root}")
    print(f"parity-check: discovered {len(v1)} v1 fixture(s), "
          f"{len(v2_by_lang)} v2 grammar(s) in coverage.csv, "
          f"{len(v2_provenance)} v2 provenance string(s), {len(checklist)} constructs.txt entries")

    v1_present = len(v1) > 0
    if v1_present and len(v1) != EXPECTED_V1_COUNT:
        die(f"expected exactly {EXPECTED_V1_COUNT} v1 fixtures (the frozen set) or 0 (deleted); "
            f"found {len(v1)} -- discovery bug or unexpected tree state, refusing to certify")

    # ---- A/B/C: v1 -> v2 floors (only while v1 exists) ----
    if not v1_present:
        print("parity-check: no v1 fixtures found -- migration complete; running only absolute check D.")
    else:
        v1_constructs = set()
        v1_provenance = set()
        xample_constructs = set()  # constructs of requires:[] v1 fixtures
        for m in v1:
            cons = set(m.get("constructs", []))
            v1_constructs |= cons
            if "provenance" in m and m["provenance"]:
                v1_provenance.add(m["provenance"])
            if m.get("requires", []) == []:
                xample_constructs |= cons
        if not v1_constructs:
            die("parsed 0 constructs from v1 manifests -- schema/parse bug, refusing to certify")

        # A. provenance exact-value containment
        missing_prov = sorted(p for p in v1_provenance if p not in v2_provenance)
        if missing_prov:
            for p in missing_prov:
                failures.append(("A/provenance", f"v1 provenance not found verbatim in any v2 words.yaml: {p!r}"))
        print(f"[{'PASS' if not missing_prov else 'FAIL'}] A. provenance: "
              f"{len(v1_provenance) - len(missing_prov)}/{len(v1_provenance)} v1 strings carried verbatim")

        # B. per-construct floor
        missing_cons = sorted(c for c in v1_constructs if c not in v2_covered)
        if missing_cons:
            for c in missing_cons:
                failures.append(("B/per-construct", f"v1 construct not covered by any v2 word: {c!r}"))
        print(f"[{'PASS' if not missing_cons else 'FAIL'}] B. per-construct floor: "
              f"{len(v1_constructs) - len(missing_cons)}/{len(v1_constructs)} v1 constructs covered in v2")

        # C. XAmple floor: xample constructs must be covered by a requires:[] v2 grammar
        xample_v2_langs = {lang for lang, req in v2_lang_requires.items() if req == []}
        xample_v2_covered = set()
        for lang in xample_v2_langs:
            xample_v2_covered |= v2_by_lang.get(lang, set())
        missing_xample = sorted(c for c in xample_constructs if c not in xample_v2_covered)
        if missing_xample:
            for c in missing_xample:
                failures.append(("C/xample", f"requires:[] v1 construct has no requires:[] v2 home: {c!r}"))
        print(f"[{'PASS' if not missing_xample else 'FAIL'}] C. XAmple floor: "
              f"{len(xample_constructs) - len(missing_xample)}/{len(xample_constructs)} requires:[] constructs "
              f"have a requires:[] v2 home (v2 requires:[] grammars: {sorted(xample_v2_langs)})")

    # ---- D: absolute construct coverage (always) ----
    in_scope = [c for c in checklist if c != OUT_OF_SCOPE]
    uncovered = sorted(c for c in in_scope if c not in v2_covered)
    if uncovered:
        for c in uncovered:
            failures.append(("D/absolute", f"constructs.txt construct at zero v2 coverage: {c!r}"))
    print(f"[{'PASS' if not uncovered else 'FAIL'}] D. absolute coverage: "
          f"{len(in_scope) - len(uncovered)}/{len(in_scope)} in-scope constructs covered "
          f"('{OUT_OF_SCOPE}' out of scope by design)")

    print()
    if failures:
        print(f"parity-check: FAILED with {len(failures)} violation(s):")
        for check, msg in failures:
            print(f"  [{check}] {msg}")
        sys.exit(1)
    print("parity-check: ALL CHECKS PASSED -- v1 coverage is fully preserved in v2; safe to delete the v1 tree.")
    sys.exit(0)


if __name__ == "__main__":
    main()
