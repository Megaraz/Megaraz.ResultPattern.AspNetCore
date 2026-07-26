$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$validator = Join-Path $PSScriptRoot "validate-package-version.ps1"
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "Megaraz.ResultPattern.AspNetCore-version-validation-$([guid]::NewGuid())"
$powerShellExecutable = (Get-Process -Id $PID).Path

function New-TestFixture([string] $Name, [string] $ProjectVersion, [string] $PackageVersion) {
    $fixture = Join-Path $testRoot $Name
    $artifacts = Join-Path $fixture "artifacts"
    $content = Join-Path $fixture "package"
    New-Item -ItemType Directory -Path $artifacts, $content | Out-Null
    "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><Version>$ProjectVersion</Version></PropertyGroup></Project>" | Set-Content -LiteralPath (Join-Path $fixture "Package.csproj")
    "<?xml version=`"1.0`"?><package><metadata><id>Megaraz.ResultPattern.AspNetCore</id><version>$PackageVersion</version></metadata></package>" | Set-Content -LiteralPath (Join-Path $content "Megaraz.ResultPattern.AspNetCore.nuspec")
    $zip = Join-Path $artifacts "package.zip"
    Compress-Archive -Path (Join-Path $content "*") -DestinationPath $zip
    Move-Item -LiteralPath $zip -Destination (Join-Path $artifacts "Megaraz.ResultPattern.AspNetCore.$PackageVersion.nupkg")
    return $fixture
}
function Invoke-ValidationCase([string] $Name, [string] $Tag, [string] $ProjectVersion, [string] $PackageVersion, [bool] $ShouldSucceed) {
    $fixture = New-TestFixture $Name $ProjectVersion $PackageVersion
    & $powerShellExecutable -NoProfile -File $validator -Tag $Tag -ProjectPath (Join-Path $fixture "Package.csproj") -PackageDirectory (Join-Path $fixture "artifacts") *> $null
    if (($LASTEXITCODE -eq 0) -ne $ShouldSucceed) { throw "Validation case '$Name' expected success '$ShouldSucceed'." }
}
try {
    Invoke-ValidationCase "matching versions" "v0.1.1" "0.1.1" "0.1.1" $true
    Invoke-ValidationCase "malformed tag" "v0.1" "0.1.1" "0.1.1" $false
    Invoke-ValidationCase "project mismatch" "v0.1.1" "0.1.0" "0.1.1" $false
    Invoke-ValidationCase "package mismatch" "v0.1.1" "0.1.1" "0.1.0" $false
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
$global:LASTEXITCODE = 0
