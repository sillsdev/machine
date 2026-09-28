<#
.SYNOPSIS
	Comment-hygiene rules for the machine repository.

.DESCRIPTION
	Classifies whole-line comments and applies the banned-content categories,
	the .editorconfig display-width limit, and the aggregate
	implementation-comment budget. The rules stand apart from any file list or
	diff scope, so they can be exercised on their own.
#>

Set-StrictMode -Version Latest

# Declared at module scope: under Set-StrictMode a script-scoped variable read
# before its first assignment throws rather than returning $null.
$script:editorConfigCache = @{}

function Get-NonAsciiPunctuationCharacters {
	<#
	.SYNOPSIS
		Returns the set of typographic characters the banned-punctuation rule reports.

	.DESCRIPTION
		Built from [char] code points rather than escape sequences so the set is
		identical under every PowerShell edition that can run this module. Only
		these characters are reported; comments may otherwise contain any script,
		IPA, or Unicode content the language data requires.
	#>
	return @(
		[char]0x2014, [char]0x2013, [char]0x2192, [char]0x2190, [char]0x2194,
		[char]0x2026, [char]0x2022, [char]0x00d7, [char]0x2018, [char]0x2019,
		[char]0x201c, [char]0x201d, [char]0x00a7
	)
}

function Get-NonAsciiPunctuationPattern {
	<#
	.SYNOPSIS
		Returns a regex matching any single character in Get-NonAsciiPunctuationCharacters.
	#>
	$escaped = Get-NonAsciiPunctuationCharacters | ForEach-Object { [regex]::Escape([string]$_) }
	return '(?:' + ($escaped -join '|') + ')'
}

function Get-CommentHygieneCategories {
	<#
	.SYNOPSIS
		Returns the ordered category-name to regex-pattern map.

	.DESCRIPTION
		Each pattern targets a specific low-signal comment shape. "Stage N" and
		"this commit" are not banned: HermitCrab strata and Commit are domain
		terms here.
	#>
	$absenceNarration = '(?i)\b(?:it|this|that|these|those|we|they|which)\s+used to\b' `
		+ '|\bused to be\b|\bpreviously (?:read|worked|did|returned|used|called)\b' `
		+ '|\b(?:was|were) removed\b|\b(?:was|were) stale\b|\brenamed from\b|\bfirst shipped\b' `
		+ '|\bno longer (?:used|needed|exists|exist|supported|present|applies|apply|valid)\b'
	$provenance = '(?i)\bshared by \w+ and \w+\b|\bthe only caller\b|\bthe sole caller\b|\bextracted from\b'

	return [ordered]@{
		'process-framing'       = '(?i)\bPhase[\s-]?\d+\b|\blater we\x27ll\b|\bwe\x27ll (?:later|eventually)\b'
		'doc-pointer'           = '\b[\w./-]+\.md\b|(?i)\bsection\s+\d+[a-z]?\b'
		# A bare "used to" or "no longer" usually reads as purpose or present
		# state here, so both require an explicitly historical phrasing.
		'absence-narration'     = $absenceNarration
		'cross-file-pointer'    = "(?i)\bsee [A-Za-z]+\x27s note\b|\bas documented (?:on|in) [A-Za-z]+\b"
		'provenance'            = $provenance
		'non-ascii-punctuation' = Get-NonAsciiPunctuationPattern
	}
}

function Get-CommentHygieneLanguage {
	<#
	.SYNOPSIS
		Classifies a path into a comment-syntax family by extension.

	.OUTPUTS
		'CLike' for C# and the SentencePiece C/C++ wrapper, 'Script' for
		PowerShell, shell, and Python, or $null for anything else.
	#>
	param([Parameter(Mandatory)][string] $Path)

	if ($Path -match '\.(ps1|psm1|sh|py)$') { return 'Script' }
	if ($Path -match '\.(cs|cpp|cxx|cc|c|h|hpp)$') { return 'CLike' }
	return $null
}

function Get-CommentHygieneEditorConfig {
	<#
	.SYNOPSIS
		Reads max_line_length and tab_width from the repository's .editorconfig [*] section.

	.DESCRIPTION
		The comment width limit is whatever .editorconfig already declares for
		every file, so the two cannot drift apart. Only the [*] section is read,
		because that is where this repository declares the value. A language
		section that later sets its own max_line_length would need section
		matching here first.

	.OUTPUTS
		A hashtable with MaxLineLength and TabWidth, defaulting to 120 and 4.
	#>
	param([Parameter(Mandatory)][string] $RepoRoot)

	if ($script:editorConfigCache.ContainsKey($RepoRoot)) { return $script:editorConfigCache[$RepoRoot] }

	$settings = @{ MaxLineLength = 120; TabWidth = 4 }
	$path = Join-Path $RepoRoot '.editorconfig'
	if (Test-Path -LiteralPath $path) {
		$inStarSection = $false
		foreach ($raw in [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)) {
			$line = $raw.Trim()
			if ($line.StartsWith('#') -or $line.Length -eq 0) { continue }
			if ($line.StartsWith('[')) { $inStarSection = ($line -eq '[*]'); continue }
			if (-not $inStarSection) { continue }
			if ($line -match '^max_line_length\s*=\s*(\d+)$') { $settings.MaxLineLength = [int]$Matches[1] }
			if ($line -match '^tab_width\s*=\s*(\d+)$') { $settings.TabWidth = [int]$Matches[1] }
		}
	}

	$script:editorConfigCache[$RepoRoot] = $settings
	return $settings
}

function Get-CommentDisplayWidth {
	<#
	.SYNOPSIS
		Returns a line's width in display columns, expanding tabs to the next tab stop.

	.DESCRIPTION
		String length counts a tab as one character while max_line_length counts
		display columns. The two disagree by enough to decide a violation either
		way in tab-indented files.
	#>
	param(
		[Parameter(Mandatory)][AllowEmptyString()][string] $Line,
		[Parameter(Mandatory)][int] $TabWidth
	)

	$width = 0
	foreach ($char in $Line.ToCharArray()) {
		if ($char -eq "`t") { $width += $TabWidth - ($width % $TabWidth) }
		else { $width++ }
	}
	return $width
}

function Get-CommentLineClassification {
	<#
	.SYNOPSIS
		Classifies every line as an implementation comment, an exempt doc comment, or neither.

	.PARAMETER Lines
		The file's lines.

	.PARAMETER Language
		'CLike' for the // and /// forms, or 'Script' for the number-sign form
		and the PowerShell block-comment form.

	.OUTPUTS
		A hashtable with parallel arrays Kinds ('impl', 'exempt', or $null per
		line) and Bodies (comment text per line, or $null). An exempt line is
		outside the aggregate budget but still subject to width and content
		rules. A shebang is not a comment. A bare # is never a comment in a
		CLike file, so a preprocessor directive is never misread as one.
	#>
	param(
		[Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]] $Lines,
		[Parameter(Mandatory)][ValidateSet('CLike', 'Script')][string] $Language
	)

	$kinds = New-Object 'object[]' $Lines.Count
	$bodies = New-Object 'object[]' $Lines.Count
	$inHelpBlock = $false

	for ($i = 0; $i -lt $Lines.Count; $i++) {
		$trimmed = $Lines[$i].Trim()

		if ($Language -eq 'Script') {
			if ($inHelpBlock) {
				$kinds[$i] = 'exempt'
				$bodies[$i] = $trimmed
				if ($trimmed.EndsWith('#>')) { $inHelpBlock = $false }
				continue
			}
			if ($trimmed.StartsWith('<#')) {
				$kinds[$i] = 'exempt'
				$bodies[$i] = $trimmed.Substring(2).TrimEnd('#', '>', ' ')
				if (-not $trimmed.EndsWith('#>')) { $inHelpBlock = $true }
				continue
			}
			if ($i -eq 0 -and $trimmed.StartsWith('#!')) {
				$kinds[$i] = $null
				$bodies[$i] = $null
				continue
			}
			if ($trimmed.StartsWith('#')) {
				$kinds[$i] = 'impl'
				$bodies[$i] = $trimmed.Substring(1)
				continue
			}
		}
		else {
			if ($trimmed.StartsWith('///')) {
				$kinds[$i] = 'exempt'
				$bodies[$i] = $trimmed.Substring(3)
				continue
			}
			if ($trimmed.StartsWith('//')) {
				$kinds[$i] = 'impl'
				$bodies[$i] = $trimmed.Substring(2)
				continue
			}
		}

		$kinds[$i] = $null
		$bodies[$i] = $null
	}

	return @{ Kinds = $kinds; Bodies = $bodies }
}

function Get-CommentHygieneViolations {
	<#
	.SYNOPSIS
		Scans the given files for mechanical comment-hygiene violations.

	.PARAMETER Files
		Paths to scan. A file whose extension Get-CommentHygieneLanguage does not
		recognize is skipped.

	.PARAMETER LineFilter
		Optional map from a path to the set of 1-based line numbers to check.
		Omit it to scan every line of every file.

	.PARAMETER RepoRoot
		The root whose .editorconfig supplies the width limit.

	.OUTPUTS
		One object per violation with File, Line, Category, and Text. The
		'comment-too-long' category covers a run of consecutive implementation
		comment lines whose combined trimmed bodies exceed the budget; a block is
		reported at its first line, and a block whose untouched lines alone
		already exceed the budget is left to a separate cleanup.
	#>
	param(
		[Parameter(Mandatory)][AllowEmptyCollection()][string[]] $Files,
		[hashtable] $LineFilter,
		[Parameter(Mandatory)][string] $RepoRoot
	)

	$editorConfig = Get-CommentHygieneEditorConfig -RepoRoot $RepoRoot
	$categories = Get-CommentHygieneCategories
	$violations = New-Object System.Collections.ArrayList
	$maxImplCommentChars = 200

	foreach ($file in $Files) {
		if (-not (Test-Path -LiteralPath $file)) { continue }

		$language = Get-CommentHygieneLanguage -Path $file
		if ($null -eq $language) { continue }

		$allowedLines = $null
		if ($LineFilter -and $LineFilter.ContainsKey($file)) { $allowedLines = $LineFilter[$file] }

		# ReadAllLines rather than Get-Content: this scan runs on every pull
		# request over every changed file, and StreamReader still honours a BOM.
		$lines = [System.IO.File]::ReadAllLines($file, [System.Text.Encoding]::UTF8)
		$classification = Get-CommentLineClassification -Lines $lines -Language $language
		$kinds = $classification.Kinds
		$bodies = $classification.Bodies

		for ($i = 0; $i -lt $lines.Count; $i++) {
			if ($null -eq $kinds[$i]) { continue }
			$lineNumber = $i + 1
			if ($allowedLines -and -not $allowedLines.Contains($lineNumber)) { continue }

			foreach ($category in $categories.Keys) {
				if ($bodies[$i] -match $categories[$category]) {
					[void]$violations.Add([PSCustomObject]@{
							File     = $file
							Line     = $lineNumber
							Category = $category
							Text     = $bodies[$i].Trim()
						})
				}
			}

			# Width comes from .editorconfig and applies to every comment line
			# including doc comments: those are exempt from what they may say,
			# not from how wide they may run.
			$displayWidth = Get-CommentDisplayWidth -Line $lines[$i] -TabWidth $editorConfig.TabWidth
			if ($displayWidth -gt $editorConfig.MaxLineLength) {
				[void]$violations.Add([PSCustomObject]@{
						File     = $file
						Line     = $lineNumber
						Category = 'comment-line-too-long'
						Text     = ("{0} columns (max {1}): {2}" -f
							$displayWidth, $editorConfig.MaxLineLength, $bodies[$i].Trim())
					})
			}
		}

		$blockStart = -1
		$blockLength = 0
		for ($i = 0; $i -le $lines.Count; $i++) {
			$isImplLine = ($i -lt $lines.Count) -and ($kinds[$i] -eq 'impl')
			if ($isImplLine) {
				if ($blockStart -lt 0) { $blockStart = $i }
				$blockLength++
				continue
			}

			if ($blockLength -gt 0) {
				$blockIndexes = $blockStart..($blockStart + $blockLength - 1)
				$totalChars = [int](
					$blockIndexes | ForEach-Object { $bodies[$_].Trim().Length } | Measure-Object -Sum
				).Sum

				if ($totalChars -gt $maxImplCommentChars) {
					$touchesBlock = $true
					$untouchedChars = 0
					if ($allowedLines) {
						$touchesBlock = [bool]($blockIndexes | Where-Object { $allowedLines.Contains($_ + 1) })
						$untouchedIndexes = $blockIndexes | Where-Object { -not $allowedLines.Contains($_ + 1) }
						if ($untouchedIndexes) {
							$untouchedChars = [int](
								$untouchedIndexes | ForEach-Object { $bodies[$_].Trim().Length } | Measure-Object -Sum
							).Sum
						}
					}
					if ($touchesBlock -and $untouchedChars -le $maxImplCommentChars) {
						[void]$violations.Add([PSCustomObject]@{
								File     = $file
								Line     = $blockStart + 1
								Category = 'comment-too-long'
								Text     = ("{0} chars (budget {1}): {2}" -f
									$totalChars, $maxImplCommentChars, $bodies[$blockStart].Trim())
							})
					}
				}
			}
			$blockStart = -1
			$blockLength = 0
		}
	}

	# The unary comma stops the pipeline unrolling an empty or single-element
	# array into $null or a bare scalar.
	return , $violations.ToArray()
}

Export-ModuleMember -Function @(
	'Get-CommentHygieneViolations'
)
