$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$verifier = Join-Path $PSScriptRoot "verify-sourcelink-symbols.ps1"
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "Megaraz.ResultPattern.AspNetCore-sourcelink-$([guid]::NewGuid())"
$symbolDirectory = Join-Path $testRoot "symbols"
$invocationLog = Join-Path $testRoot "invocations.log"
$fakeSourceLink = Join-Path $testRoot "fake-sourcelink.ps1"
$powerShellExecutable = (Get-Process -Id $PID).Path
try {
    New-Item -ItemType Directory -Path $symbolDirectory | Out-Null
    New-Item -ItemType File -Path (Join-Path $symbolDirectory "Megaraz.ResultPattern.AspNetCore.pdb") | Out-Null
    $passingDirectory = Join-Path $symbolDirectory "passing"
    New-Item -ItemType Directory -Path $passingDirectory | Out-Null
    New-Item -ItemType File -Path (Join-Path $passingDirectory "Megaraz.ResultPattern.AspNetCore.pdb") | Out-Null
    @"
param([string] `$Command, [string] `$Path)
Add-Content -LiteralPath '$invocationLog' -Value `$Path
if (`$Path -notlike '*passing*') { exit 1 }
"@ | Set-Content -LiteralPath $fakeSourceLink
    & $powerShellExecutable -NoProfile -File $verifier -SymbolDirectory $symbolDirectory -SourceLinkExecutable $fakeSourceLink *> $null
    if ($LASTEXITCODE -eq 0) { throw "Source Link verification unexpectedly succeeded." }
    $invocations = @(Get-Content -LiteralPath $invocationLog)
    if ($invocations.Count -ne 1 -or $invocations[0] -like '*passing*') { throw "A failing PDB was not handled immediately." }
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
$global:LASTEXITCODE = 0
