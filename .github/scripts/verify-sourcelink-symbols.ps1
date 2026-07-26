param(
    [Parameter(Mandatory)] [string] $SymbolDirectory,
    [Parameter(Mandatory)] [string] $SourceLinkExecutable
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
Get-ChildItem -LiteralPath $SymbolDirectory -Filter Megaraz.ResultPattern.AspNetCore.pdb -Recurse | ForEach-Object {
    & $SourceLinkExecutable test $_.FullName
    if ($LASTEXITCODE -ne 0) { throw "Source Link verification failed for '$($_.FullName)' with exit code $LASTEXITCODE." }
}
