<#
.SYNOPSIS
  Compare two PerfBench JSON outputs (baseline vs candidate) for parity and speedup.
.DESCRIPTION
  Asserts per-word signature-set equality (index + grammar name only, never the word itself),
  reports per-word min-ms timings, speedup, and spread for both arms, flags unreliable words,
  and exits 1 on any parity divergence.
#>
param(
    [Parameter(Mandatory = $true)][string]$Baseline,
    [Parameter(Mandatory = $true)][string]$Candidate
)

function Get-Runs($result) {
    $runs = @()
    if ($result.Main) { $runs += [pscustomobject]@{ Label = $result.Main.grammarFile; Run = $result.Main } }
    foreach ($f in $result.Fixtures) { $runs += [pscustomobject]@{ Label = $f.fixtureId; Run = $f.Run } }
    return $runs
}

$baseJson = Get-Content $Baseline -Raw | ConvertFrom-Json
$candJson = Get-Content $Candidate -Raw | ConvertFrom-Json

$baseRuns = Get-Runs $baseJson
$candRuns = Get-Runs $candJson
$baseByLabel = @{}
foreach ($r in $baseRuns) { $baseByLabel[$r.Label] = $r.Run }
$candByLabel = @{}
foreach ($r in $candRuns) { $candByLabel[$r.Label] = $r.Run }

$divergences = 0
$totalBaseMs = 0.0
$totalCandMs = 0.0
$unreliableWords = 0

foreach ($cr in $candRuns) {
    if (-not $baseByLabel.ContainsKey($cr.Label)) {
        Write-Host "MISSING in baseline: $($cr.Label)" -ForegroundColor Red
        $divergences++
    }
}

foreach ($br in $baseRuns) {
    $grammarName = $br.Label
    $baseRun = $br.Run
    if (-not $candByLabel.ContainsKey($grammarName)) {
        Write-Host "MISSING in candidate: $grammarName" -ForegroundColor Red
        $divergences++
        continue
    }
    $candRun = $candByLabel[$grammarName]
    Write-Host ""
    Write-Host "== $grammarName ==" -ForegroundColor Cyan

    $baseWords = @{}
    foreach ($w in $baseRun.words) { $baseWords[[int]$w.index] = $w }
    $candWords = @{}
    foreach ($w in $candRun.words) { $candWords[[int]$w.index] = $w }

    foreach ($idx in ($baseWords.Keys | Sort-Object)) {
        $bw = $baseWords[$idx]
        if (-not $candWords.ContainsKey($idx)) {
            Write-Host ("  [{0}] MISSING in candidate ({1})" -f $idx, $grammarName) -ForegroundColor Red
            $divergences++
            continue
        }
        $cw = $candWords[$idx]

        $bSig = @($bw.signatures) | Sort-Object
        $cSig = @($cw.signatures) | Sort-Object
        # An error outcome is part of parity too: throwing on one arm and parsing on the other diverges.
        $sigsEqual = (($bSig -join "`n") -eq ($cSig -join "`n")) -and ("$($bw.error)" -eq "$($cw.error)")
        if (-not $sigsEqual) {
            Write-Host ("  [{0}] SIGNATURE DIVERGENCE (grammar={1})" -f $idx, $grammarName) -ForegroundColor Red
            $divergences++
        }

        $bMin = [double]$bw.minMs
        $cMin = [double]$cw.minMs
        $bSpread = [double]$bw.spread
        $cSpread = [double]$cw.spread
        $speedup = if ($cMin -gt 0) { $bMin / $cMin } else { 0 }
        $reliable = $true
        if ($baseRun.reliable -eq $false -or $candRun.reliable -eq $false) { $reliable = $false }
        if ([Math]::Max($bSpread, $cSpread) -gt [Math]::Abs($speedup - 1)) { $reliable = $false }
        if (-not $reliable) { $unreliableWords++ }

        $flag = if (-not $reliable) { " (unreliable)" } else { "" }
        Write-Host ("  [{0}] base={1:F2}ms cand={2:F2}ms speedup={3:F2}x spread(base={4:F2},cand={5:F2}){6}" `
            -f $idx, $bMin, $cMin, $speedup, $bSpread, $cSpread, $flag)

        $totalBaseMs += $bMin
        $totalCandMs += $cMin
    }

    foreach ($idx in ($candWords.Keys | Sort-Object)) {
        if (-not $baseWords.ContainsKey($idx)) {
            Write-Host ("  [{0}] MISSING in baseline ({1})" -f $idx, $grammarName) -ForegroundColor Red
            $divergences++
        }
    }
}

Write-Host ""
$totalSpeedup = if ($totalCandMs -gt 0) { $totalBaseMs / $totalCandMs } else { 0 }
Write-Host ("total: base={0:F1}ms cand={1:F1}ms speedup={2:F2}x unreliable-words={3}" `
    -f $totalBaseMs, $totalCandMs, $totalSpeedup, $unreliableWords)

if ($divergences -gt 0) {
    Write-Host "$divergences parity divergence(s) found" -ForegroundColor Red
    exit 1
}

Write-Host "parity OK" -ForegroundColor Green
exit 0
