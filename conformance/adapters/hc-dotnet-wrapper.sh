#!/usr/bin/env bash
# Adapter-protocol wrapper for SIL.Machine.Morphology.HermitCrab.Tool (hc.dll), per
# conformance/PROTOCOL.md section 7: hc.dll's own CLI shape ("-i grammar -s script.txt", where
# script.txt contains a "batch words.txt output.tsv" line) predates and doesn't match section 1's
# single 3-positional-arg contract, so this one-line-idea wrapper bridges the two: it writes a
# temporary script.txt and invokes hc.dll the normal way.
#
# Usage (matches the protocol's canonical shape, plus one extra arg for the tool path):
#   hc-dotnet-wrapper.sh batch <grammar.xml> <words.txt> <output.tsv> [path/to/hc.dll]
#
# Exists so --adapter mode (SIL.Machine.Morphology.HermitCrab.Conformance's external-engine path)
# can be exercised end-to-end against a real conforming engine even when no second engine (e.g.
# hc-rs, the Rust port) is present in this checkout -- this repo's own C# oracle, run as a
# subprocess through the documented adapter contract instead of self-check mode's in-process call,
# is a legitimate (if not very interesting) conformance-testable engine in its own right.
set -euo pipefail

if [ "${1:-}" != "batch" ]; then
    echo "usage: hc-dotnet-wrapper.sh batch <grammar.xml> <words.txt> <output.tsv> [path/to/hc.dll]" >&2
    exit 2
fi
grammar="$2"
words="$3"
output="$4"

# Resolve the default hc.dll path relative to this script's own location (repo root), not the
# caller's cwd, so the wrapper works when invoked from anywhere. An explicit 5th arg always wins,
# used exactly as given (relative to the caller's cwd, same as before).
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"

if [ -n "${5:-}" ]; then
    hcdll="$5"
else
    hcdll=""
    for candidate in \
        "$repo_root/src/SIL.Machine.Morphology.HermitCrab.Tool/bin/Debug/net10.0/hc.dll" \
        "$repo_root/src/SIL.Machine.Morphology.HermitCrab.Tool/bin/Release/net10.0/hc.dll"
    do
        if [ -f "$candidate" ]; then
            hcdll="$candidate"
            break
        fi
    done
    # Neither build exists: fall through with the Debug path so the error message below points at
    # the expected default location instead of silently trying an empty path.
    if [ -z "$hcdll" ]; then
        hcdll="$repo_root/src/SIL.Machine.Morphology.HermitCrab.Tool/bin/Debug/net10.0/hc.dll"
    fi
fi

tmpscript="$(mktemp)"
trap 'rm -f "$tmpscript"' EXIT

printf 'batch "%s" "%s"\n' "$words" "$output" > "$tmpscript"

# hc.dll's stdout is load-progress/prompt chatter, not adapter-contract output -- but it also
# carries load errors, which AdapterEngine needs to see in its own captured stderr on a nonzero
# exit. Redirect (not discard) it, so it's still available for diagnostics without polluting the
# adapter protocol's stdout. Now that Program.Main's script mode actually returns a nonzero exit
# code on failure (rather than always 0), `set -e` above genuinely propagates a hc.dll failure.
dotnet "$hcdll" -i "$grammar" -s "$tmpscript" >&2
