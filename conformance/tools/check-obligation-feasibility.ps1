#!/usr/bin/env pwsh
# Answers, before anyone spends an authoring budget, whether a coverage obligation is an authoring
# task at all. Every check here corresponds to a way an obligation has already consumed most of an
# agent's budget before proving itself impossible. See conformance/docs/severance-mechanics.md.
#
# Exits 0 if the obligation looks authorable, 1 if a check rules it out. A rule-out is a finding: it
# belongs in obligation-triage.tsv, not in a work queue.

[CmdletBinding(PositionalBinding = $false)]
param(
    # An attribute as Element.attribute, e.g. LexicalEntry.partOfSpeech. Repeat for writer and reader.
    [Parameter(Mandatory = $true)][string] $Writer,
    [Parameter(Mandatory = $true)][string] $Reader,
    # Element name whose Control arm must be attributed, if this is a gate arm rather than a cell.
    [string] $ControlElement,
    [string] $ConformanceRoot = (Join-Path (Split-Path $PSScriptRoot -Parent) '')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path $ConformanceRoot).Path
$dtd = Join-Path $root 'HermitCrabInput.dtd'
if (-not (Test-Path $dtd)) { throw "DTD not found at $dtd" }

# GrammarRuleIndex.cs's switch on el.Name.LocalName -- the ONLY element names a fired rule can be
# mapped back to. An arm whose construct sits elsewhere cannot have a Control attributed.
$ruleIndexable = @(
    'MorphologicalRule', 'RealizationalRule', 'CompoundingRule', 'PhonologicalRule', 'MetathesisRule',
    'MorphemeCoOccurrenceRule', 'AllomorphCoOccurrenceRule'
)

$dtdText = Get-Content $dtd -Raw
$verdicts = [System.Collections.Generic.List[string]]::new()
$blocked = $false

function Test-Severable {
    param([string] $Spec)
    $parts = $Spec -split '\.', 2
    if ($parts.Count -ne 2) { throw "expected Element.attribute, got '$Spec'" }
    $attr = $parts[1]
    # A #REQUIRED attribute cannot be removed: the mutant throws and the row records RequiredByDtd.
    $pattern = [regex]::Escape($attr) + '\s+\S+\s+#REQUIRED'
    if ($dtdText -match $pattern) { return $false }
    return $true
}

foreach ($spec in @(@{n = 'writer'; v = $Writer }, @{n = 'reader'; v = $Reader })) {
    if (Test-Severable -Spec $spec.v) {
        $verdicts.Add("  OK       $($spec.n) $($spec.v) is severable")
    }
    else {
        $verdicts.Add("  BLOCKED  $($spec.n) $($spec.v) is DTD #REQUIRED -- severing throws, so the row can only ever be RequiredByDtd")
        $blocked = $true
    }
}

# Writer and reader must set and read the same payload inside ONE fixture, or there is no chain to
# witness -- a word cannot join constructs that live in different grammars.
$writerEl = ($Writer -split '\.')[0]; $writerAt = ($Writer -split '\.', 2)[1]
$readerEl = ($Reader -split '\.')[0]; $readerAt = ($Reader -split '\.', 2)[1]
$hosts = @()
foreach ($dir in @('languages', 'edge-cases')) {
    $base = Join-Path $root $dir
    if (-not (Test-Path $base)) { continue }
    foreach ($fx in Get-ChildItem $base -Directory) {
        $g = Join-Path $fx.FullName 'grammar.xml'
        if (-not (Test-Path $g)) { continue }
        $x = Get-Content $g -Raw
        $hasW = $x -match "<$writerEl\b[^>]*\b$writerAt="
        $hasR = $x -match "<$readerEl\b[^>]*\b$readerAt="
        if ($hasW -and $hasR) { $hosts += "$dir/$($fx.Name)" }
    }
}
if ($hosts.Count -gt 0) {
    $verdicts.Add("  OK       writer and reader co-occur in: $($hosts -join ', ')")
}
else {
    $verdicts.Add("  BLOCKED  no single fixture declares both $Writer and $Reader -- needs a grammar change, not a word")
    $blocked = $true
}

if ($ControlElement) {
    if ($ruleIndexable -contains $ControlElement) {
        $verdicts.Add("  OK       Control arm on $ControlElement is rule-indexable")
    }
    else {
        $verdicts.Add("  BLOCKED  GrammarRuleIndex cannot resolve $ControlElement to a fired-rule id -- the Control arm cannot be attributed even with a correct word present")
        $blocked = $true
    }
}

Write-Output "obligation: $Writer -> $Reader$(if ($ControlElement) { " (control on $ControlElement)" })"
$verdicts | ForEach-Object { Write-Output $_ }
Write-Output ''
if ($blocked) {
    Write-Output 'VERDICT: not an authoring task. Record it in obligation-triage.tsv with the blocker above.'
    exit 1
}
Write-Output 'VERDICT: looks authorable. Nothing here guarantees a witness exists -- only that no known mechanism forbids one.'
exit 0
