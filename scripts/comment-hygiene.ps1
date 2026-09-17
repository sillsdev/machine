#!/usr/bin/env pwsh
<#
.SYNOPSIS
	Checks the comment hygiene of the lines this branch adds.

.DESCRIPTION
	Diffs the working tree against the merge base with the base ref, collects the
	added lines in C#, C/C++, PowerShell, shell, and Python files, and applies the
	rules in CommentHygiene.psm1. Untracked in-scope files are treated as entirely
	new.

	Scoping to added lines is what makes the check usable: existing comment debt
	stays out of the way, and a branch is answerable only for what it writes.

.PARAMETER BaseRef
	The ref to diff against. Defaults to the pull request base in CI, then
	origin/HEAD, then origin/master.

.PARAMETER Full
	Scans every tracked in-scope file instead of the added lines. Use it to size
	existing debt; it is not the pull request gate.

.PARAMETER Advisory
	Reports violations and still exits 0. CI uses this so existing debt and any
	false positive cannot block a contributor.

.PARAMETER ReportPath
	Optional path for a JSON report.

.PARAMETER SelfTest
	Runs the built-in rule cases and exits. It needs no test framework, so the
	rules stay verifiable on any machine that can run this script.

.EXAMPLE
	./scripts/comment-hygiene.ps1
	Checks the lines this branch adds and fails on a violation.

.EXAMPLE
	./scripts/comment-hygiene.ps1 -Full -Advisory
	Reports all existing comment debt without failing.
#>
[CmdletBinding()]
param(
	[string] $BaseRef,
	[switch] $Full,
	[switch] $Advisory,
	[string] $ReportPath,
	[switch] $SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'CommentHygiene.psm1') -Force

$scopedPathSpecs = @(
	'src/*.cs', 'tests/*.cs',
	'src/sentencepiece4c/*.cpp', 'src/sentencepiece4c/*.cxx', 'src/sentencepiece4c/*.cc',
	'src/sentencepiece4c/*.c', 'src/sentencepiece4c/*.h', 'src/sentencepiece4c/*.hpp',
	'scripts/*.ps1', 'scripts/*.psm1', 'scripts/*.sh', 'scripts/*.py',
	'local_check.sh'
)

function Get-RepoRoot {
	<#
	.SYNOPSIS
		Returns the top level of the working tree this script lives in.
	#>
	$root = & git rev-parse --show-toplevel 2>$null
	if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
		throw 'comment-hygiene must run inside a git working tree.'
	}
	return $root.Trim()
}

function Resolve-BaseRef {
	<#
	.SYNOPSIS
		Resolves the ref to diff against, preferring an explicit value.

	.DESCRIPTION
		The candidates are tried in order and the first one git can resolve wins.
		git remote show is deliberately not used: resolving a base must not need
		network access on a developer machine.
	#>
	param([string] $Explicit)

	$candidates = @()
	if (-not [string]::IsNullOrWhiteSpace($Explicit)) { $candidates += $Explicit }
	if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_BASE_REF)) { $candidates += "origin/$($env:GITHUB_BASE_REF)" }
	$candidates += 'origin/HEAD'
	$candidates += 'origin/master'

	foreach ($candidate in $candidates) {
		& git rev-parse --verify --quiet "$candidate^{commit}" > $null 2>&1
		if ($LASTEXITCODE -eq 0) { return $candidate }
	}
	throw "No usable base ref. Tried: $($candidates -join ', '). Fetch the base branch, or pass -BaseRef."
}

function Get-AddedLineFilter {
	<#
	.SYNOPSIS
		Returns a map from an in-scope path to the set of line numbers this branch adds.

	.DESCRIPTION
		Diffs the working tree against the merge base so local edits are checked
		before they are committed. A deleted line is never scanned, and a tracked
		line that still matches the merge base is not new. An untracked in-scope
		file counts entirely as added.
	#>
	param([Parameter(Mandatory)][string] $Base, [Parameter(Mandatory)][string] $RepoRoot)

	$mergeBase = & git merge-base $Base HEAD 2>$null
	if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($mergeBase)) {
		throw "No merge base between $Base and HEAD. Fetch enough history (fetch-depth: 0 in CI)."
	}
	$mergeBase = $mergeBase.Trim()

	$filter = @{}
	$currentFile = $null
	$hunkStart = 0
	$hunkOffset = 0
	$diff = & git diff --unified=0 $mergeBase -- @scopedPathSpecs
	foreach ($line in $diff) {
		if ($line.StartsWith('+++ ')) {
			$path = $line.Substring(4).Trim()
			if ($path -eq '/dev/null') { $currentFile = $null; continue }
			if ($path.StartsWith('b/')) { $path = $path.Substring(2) }
			$currentFile = (Join-Path $RepoRoot $path)
			if (-not $filter.ContainsKey($currentFile)) {
				$filter[$currentFile] = New-Object 'System.Collections.Generic.HashSet[int]'
			}
			continue
		}
		if ($line -match '^@@ -\S+ \+(\d+)(?:,(\d+))? @@') {
			$hunkStart = [int]$Matches[1]
			$hunkOffset = 0
			continue
		}
		if ($null -ne $currentFile -and $line.StartsWith('+')) {
			[void]$filter[$currentFile].Add($hunkStart + $hunkOffset)
			$hunkOffset++
		}
	}

	$untracked = & git ls-files --others --exclude-standard -- @scopedPathSpecs
	foreach ($path in $untracked) {
		if ([string]::IsNullOrWhiteSpace($path)) { continue }
		$full = Join-Path $RepoRoot $path.Trim()
		if (-not (Test-Path -LiteralPath $full)) { continue }
		$set = New-Object 'System.Collections.Generic.HashSet[int]'
		$count = [System.IO.File]::ReadAllLines($full, [System.Text.Encoding]::UTF8).Count
		for ($i = 1; $i -le $count; $i++) { [void]$set.Add($i) }
		$filter[$full] = $set
	}

	return @{ MergeBase = $mergeBase; Filter = $filter }
}

function Write-Violation {
	<#
	.SYNOPSIS
		Prints one violation, adding a GitHub annotation when running advisory in Actions.
	#>
	param([Parameter(Mandatory)][psobject] $Violation, [Parameter(Mandatory)][string] $RepoRoot)

	# Join-Path normalises to the platform separator while git reports forward
	# slashes, so both sides are normalised before the prefix is removed.
	$relative = $Violation.File -replace '\\', '/'
	$root = $RepoRoot -replace '\\', '/'
	if ($relative.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
		$relative = $relative.Substring($root.Length).TrimStart('/')
	}
	Write-Host ("{0}:{1}: {2}: {3}" -f $relative, $Violation.Line, $Violation.Category, $Violation.Text)
	if ($Advisory -and $env:GITHUB_ACTIONS -eq 'true') {
		Write-Host ("::warning file={0},line={1}::{2}: {3}" -f
			$relative, $Violation.Line, $Violation.Category, $Violation.Text)
	}
}

function Invoke-SelfTest {
	<#
	.SYNOPSIS
		Exercises the rules against fixtures in a temporary directory.

	.OUTPUTS
		0 when every case holds, 1 otherwise.
	#>
	$root = Join-Path ([System.IO.Path]::GetTempPath()) ("comment-hygiene-selftest-" + [guid]::NewGuid())
	New-Item -ItemType Directory -Path $root | Out-Null
	$editorConfigContent = "[*]`nmax_line_length = 120`ntab_width = 4`n"
	Set-Content -LiteralPath (Join-Path $root '.editorconfig') -Value $editorConfigContent -NoNewline
	$failures = New-Object System.Collections.ArrayList

	function Test-Case {
		param([string] $Name, [string] $FileName, [string[]] $ContentLines, [string[]] $ExpectedCategories)

		$content = ($ContentLines -join "`n") + "`n"
		$path = Join-Path $root $FileName
		Set-Content -LiteralPath $path -Value $content -NoNewline
		$result = Get-CommentHygieneViolations -Files @($path) -RepoRoot $root
		$found = @($result | ForEach-Object { $_.Category } | Sort-Object)
		$expected = @($ExpectedCategories | Sort-Object)
		if (($found -join ',') -ne ($expected -join ',')) {
			[void]$failures.Add(
				("{0}: expected [{1}] but found [{2}]" -f $Name, ($expected -join ','), ($found -join ','))
			)
		}
	}

	$long = 'x' * 130
	$sixty = 'y' * 60

	Test-Case 'clean C# is clean' 'clean.cs' @('// Guards against a null stratum.', 'int a = 1;') @()
	Test-Case 'doc comment is budget exempt' 'doc.cs' @(
		"/// <summary>$sixty</summary>",
		"/// <remarks>$sixty</remarks>",
		"/// <value>$sixty</value>",
		"/// <returns>$sixty</returns>",
		'int a = 1;'
	) @()
	Test-Case 'block budget aggregates lines' 'block.cs' @(
		"// $sixty", "// $sixty", "// $sixty", "// $sixty", 'int a = 1;'
	) @('comment-too-long')
	$fifty = 'z' * 50
	Test-Case 'exactly at budget is clean' 'exact.cs' @(
		"// $fifty", "// $fifty", "// $fifty", "// $fifty", 'int a = 1;'
	) @()
	Test-Case 'one over budget is reported' 'over.cs' @(
		"// $fifty", "// $fifty", "// $fifty", "// ${fifty}a", 'int a = 1;'
	) @('comment-too-long')
	Test-Case 'blank line ends a block' 'split.cs' @(
		"// $sixty", "// $sixty", '', "// $sixty", "// $sixty", 'int a = 1;'
	) @()
	Test-Case 'width counts the marker and indent' 'wide.cs' @("`t`t// $long", 'int a = 1;') @('comment-line-too-long')
	Test-Case 'em dash is reported' 'dash.cs' @(
		("// Uses a dash " + [char]0x2014 + " here."), 'int a = 1;'
	) @('non-ascii-punctuation')
	Test-Case 'absence narration is reported' 'absence.cs' @(
		'// This field is no longer used by the loader.', 'int a = 1;'
	) @('absence-narration')
	Test-Case 'markdown pointer is reported' 'pointer.cs' @(
		'// See design-notes.md for the rationale.', 'int a = 1;'
	) @('doc-pointer')
	Test-Case 'provenance is reported' 'prov.cs' @(
		'// Extracted from the old tokenizer.', 'int a = 1;'
	) @('provenance')
	Test-Case 'preprocessor directive is not a comment' 'pre.cs' @(
		'#nullable enable', '#region Parsing', 'int a = 1;'
	) @()
	Test-Case 'shell shebang is exempt' 'run.sh' @('#!/bin/bash', 'echo hi') @()
	Test-Case 'shell comment uses the same budget' 'budget.sh' @(
		"# $sixty", "# $sixty", "# $sixty", "# $sixty", 'echo hi'
	) @('comment-too-long')
	Test-Case 'unscoped extension is skipped' 'notes.md' @('// no longer relevant') @()

	Remove-Item -LiteralPath $root -Recurse -Force
	if ($failures.Count -gt 0) {
		Write-Host "comment-hygiene self-test FAILED"
		$failures | ForEach-Object { Write-Host "  $_" }
		return 1
	}
	Write-Host "comment-hygiene self-test passed"
	return 0
}

if ($SelfTest) { exit (Invoke-SelfTest) }

$repoRoot = Get-RepoRoot
$lineFilter = $null
$scanFiles = @()
$scopeDescription = ''

if ($Full) {
	$tracked = & git ls-files -- @scopedPathSpecs
	$scanFiles = @($tracked | Where-Object { $_ } | ForEach-Object { Join-Path $repoRoot $_.Trim() })
	$scopeDescription = "all $($scanFiles.Count) tracked in-scope files"
}
else {
	$base = Resolve-BaseRef -Explicit $BaseRef
	$added = Get-AddedLineFilter -Base $base -RepoRoot $repoRoot
	$lineFilter = $added.Filter
	$scanFiles = @($lineFilter.Keys)
	$scopeDescription = "lines added since $base (merge base $($added.MergeBase.Substring(0, 8)))"
}

if ($scanFiles.Count -eq 0) {
	Write-Host "comment-hygiene: no in-scope files to check ($scopeDescription)."
	exit 0
}

$violations = Get-CommentHygieneViolations -Files $scanFiles -LineFilter $lineFilter -RepoRoot $repoRoot

if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
	$reportDir = Split-Path -Parent $ReportPath
	if ($reportDir -and -not (Test-Path -LiteralPath $reportDir)) {
		New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
	}
	$payload = [PSCustomObject]@{
		scope          = $scopeDescription
		advisory       = [bool]$Advisory
		violationCount = $violations.Count
		violations     = $violations
	}
	$payload | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding utf8
}

if ($violations.Count -eq 0) {
	Write-Host "comment-hygiene: clean ($scopeDescription)."
	exit 0
}

Write-Host "comment-hygiene: $($violations.Count) violation(s) in $scopeDescription."
Write-Host ''
foreach ($violation in $violations) { Write-Violation -Violation $violation -RepoRoot $repoRoot }
Write-Host ''
Write-Host 'The standard is .claude/skills/code-comments/SKILL.md.'
Write-Host 'Reproduce locally with: pwsh ./scripts/comment-hygiene.ps1'

if ($Advisory) {
	Write-Host 'Advisory only; not failing the run.'
	exit 0
}
exit 1
