<#
.SYNOPSIS
    PR #494 measurement harness ("Change Add to PriorityUnion" in HermitCrab analysis).

.DESCRIPTION
    Runs the [Explicit] FsMergeBench test once per (grammar x mode) combination, then aggregates every
    resulting jsonl file into a Markdown report: per grammar x mode word/timeout/error/ms/counter totals,
    plus a parity section listing which word indices produced a different analysis-set signature across
    modes. The report never contains word forms, grammar content, morpheme names, or glosses -- only word
    indices and counts -- so it is safe to commit; the jsonl files it reads are not (they carry glosses and
    surface forms) and must stay under an uncommitted scratch directory.

.PARAMETER GrammarsDir
    Directory containing "<grammar>-hc.xml" and "<grammar>-words.txt" for each name in -Grammars.

.PARAMETER OutDir
    Where jsonl/log/report output is written. Created if missing. Never point this at the repo.

.EXAMPLE
    pwsh scripts/fs-merge-bench.ps1 -GrammarsDir C:\scratch\grammars -OutDir C:\scratch\bench -MaxWords 30
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GrammarsDir,

    [Parameter(Mandatory = $true)]
    [string]$OutDir,

    [string[]]$Grammars = @('sena', 'amharic', 'mbugwe', 'indonesian'),

    [string[]]$Modes = @('Add', 'PriorityUnion', 'Exact'),

    [int]$MaxWords = 30,

    [int]$TimeoutMs = 180000,

    # Skip the initial `dotnet build`; use when the test assembly is already built (e.g. re-aggregating).
    [switch]$SkipBuild,

    # Skip running the tests entirely and just re-aggregate whatever jsonl files already exist in $OutDir.
    [switch]$AggregateOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProj = Join-Path $repoRoot 'tests\SIL.Machine.Morphology.HermitCrab.Tests\SIL.Machine.Morphology.HermitCrab.Tests.csproj'

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$counterNames = @(
    'checkCalls',
    'checkRejects',
    'merges',
    'exactUnifyFailures',
    'analysisAffixApplyCalls',
    'analysisUnapplied',
    'lexicalLookupCandidates',
    'synthesisAffixApplyCalls'
)

if (-not $AggregateOnly) {
    if (-not $SkipBuild) {
        Write-Host "=== build ==="
        dotnet build $testProj -c Release
        if ($LASTEXITCODE -ne 0) { throw "build failed with exit code $LASTEXITCODE" }
    }

    foreach ($grammar in $Grammars) {
        $grammarPath = Join-Path $GrammarsDir "$grammar-hc.xml"
        $wordsPath = Join-Path $GrammarsDir "$grammar-words.txt"
        if (-not (Test-Path $grammarPath) -or -not (Test-Path $wordsPath)) {
            Write-Warning "skipping $grammar : grammar or words file not found under $GrammarsDir"
            continue
        }

        foreach ($mode in $Modes) {
            $outPath = Join-Path $OutDir "$grammar-$mode.jsonl"
            $logPath = Join-Path $OutDir "$grammar-$mode.log"
            Write-Host "=== $grammar / $mode (max $MaxWords word(s), ${TimeoutMs}ms/word timeout) ==="

            $env:HC_ANALYSIS_FS_MERGE = $mode
            $env:HC_FSM_GRAMMAR = $grammarPath
            $env:HC_FSM_WORDS = $wordsPath
            $env:HC_FSM_MAX_WORDS = "$MaxWords"
            $env:HC_FSM_TIMEOUT_MS = "$TimeoutMs"
            $env:HC_FSM_OUT = $outPath

            dotnet test $testProj -c Release --no-build --filter "FullyQualifiedName~FsMergeBench" 2>&1 |
                Tee-Object -FilePath $logPath
        }
    }

    Remove-Item Env:\HC_ANALYSIS_FS_MERGE, Env:\HC_FSM_GRAMMAR, Env:\HC_FSM_WORDS, `
        Env:\HC_FSM_MAX_WORDS, Env:\HC_FSM_TIMEOUT_MS, Env:\HC_FSM_OUT -ErrorAction SilentlyContinue
}

# --- Aggregation -----------------------------------------------------------------------------------------

$summaryRows = @()
$recordsByGrammarMode = @{}

foreach ($grammar in $Grammars) {
    foreach ($mode in $Modes) {
        $path = Join-Path $OutDir "$grammar-$mode.jsonl"
        if (-not (Test-Path $path)) { continue }

        $records = @(Get-Content $path | Where-Object { $_.Trim().Length -gt 0 } | ForEach-Object { $_ | ConvertFrom-Json })
        $recordsByGrammarMode["$grammar|$mode"] = $records

        $completed = @($records | Where-Object { -not $_.timedOut -and -not $_.error })
        $timeouts = @($records | Where-Object { $_.timedOut })
        $errors = @($records | Where-Object { $_.error })
        $totalMsSum = ($records | Measure-Object -Property ms -Sum).Sum
        if (-not $totalMsSum) { $totalMsSum = 0 }

        $row = [ordered]@{
            Grammar   = $grammar
            Mode      = $mode
            Words     = $records.Count
            Completed = $completed.Count
            Timeouts  = $timeouts.Count
            Errors    = $errors.Count
            TotalMs   = [math]::Round($totalMsSum, 1)
        }
        foreach ($c in $counterNames) {
            $sum = ($records | Measure-Object -Property $c -Sum).Sum
            if (-not $sum) { $sum = 0 }
            $row[$c] = $sum
        }
        $summaryRows += [pscustomobject]$row
    }
}

$report = New-Object System.Collections.Generic.List[string]
$report.Add("# PR #494 fs-merge bench report")
$report.Add("")
$report.Add("Generated $(Get-Date -Format o). Word indices only; no word forms, glosses, or morpheme names.")
$report.Add("")
$report.Add("## Per grammar x mode")
$report.Add("")
$headerCells = @('Grammar', 'Mode', 'Words', 'Completed', 'Timeouts', 'Errors', 'TotalMs') + $counterNames
$report.Add("| " + ($headerCells -join ' | ') + " |")
$report.Add("|" + (("---|") * $headerCells.Count))
foreach ($row in $summaryRows) {
    $cells = @($row.Grammar, $row.Mode, $row.Words, $row.Completed, $row.Timeouts, $row.Errors, $row.TotalMs)
    foreach ($c in $counterNames) { $cells += $row[$c] }
    $report.Add("| " + ($cells -join ' | ') + " |")
}

$report.Add("")
$report.Add("## Parity (analysis-set signature differs across modes)")

foreach ($grammar in $Grammars) {
    $modesPresent = @($Modes | Where-Object { $recordsByGrammarMode.ContainsKey("$grammar|$_") })
    if ($modesPresent.Count -lt 2) { continue }

    $report.Add("")
    $report.Add("### $grammar")
    $report.Add("")

    $byIndex = @{}
    foreach ($mode in $modesPresent) {
        foreach ($rec in $recordsByGrammarMode["$grammar|$mode"]) {
            $idx = [int]$rec.index
            if (-not $byIndex.ContainsKey($idx)) { $byIndex[$idx] = @{} }
            $sigArray = @($rec.signature)
            $sigKey = ($sigArray | ConvertTo-Json -Compress -Depth 5)
            $byIndex[$idx][$mode] = [pscustomobject]@{
                SigKey   = $sigKey
                Count    = [int]$rec.analysisCount
                TimedOut = [bool]$rec.timedOut
                HasError = -not [string]::IsNullOrEmpty($rec.error)
            }
        }
    }

    $diffLines = New-Object System.Collections.Generic.List[string]
    foreach ($idx in ($byIndex.Keys | Sort-Object)) {
        $entry = $byIndex[$idx]
        $sigKeys = $modesPresent | ForEach-Object { if ($entry.ContainsKey($_)) { $entry[$_].SigKey } else { '<missing>' } }
        $uniqueSigKeys = @($sigKeys | Select-Object -Unique)
        if ($uniqueSigKeys.Count -gt 1) {
            $cells = $modesPresent | ForEach-Object {
                if ($entry.ContainsKey($_)) {
                    $e = $entry[$_]
                    $flag = if ($e.TimedOut) { 'timeout' } elseif ($e.HasError) { 'error' } else { "$($e.Count) analyses" }
                    "$($_)=$flag"
                }
                else {
                    "$($_)=missing"
                }
            }
            $diffLines.Add("- index $idx : " + ($cells -join ', '))
        }
    }

    if ($diffLines.Count -eq 0) {
        $report.Add("No divergences among $($modesPresent -join ', ') for the $($byIndex.Keys.Count) word(s) compared.")
    }
    else {
        $report.Add("$($diffLines.Count) of $($byIndex.Keys.Count) word(s) diverge among $($modesPresent -join ', '):")
        $report.Add("")
        foreach ($line in $diffLines) { $report.Add($line) }
    }
}

$reportPath = Join-Path $OutDir "report.md"
$report | Set-Content -Path $reportPath -Encoding utf8
Write-Host "=== report written to $reportPath ==="
