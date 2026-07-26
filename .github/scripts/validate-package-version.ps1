param(
    [Parameter(Mandatory)] [string] $Tag,
    [Parameter(Mandatory)] [string] $ProjectPath,
    [Parameter(Mandatory)] [string] $PackageDirectory
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$tagMatch = [regex]::Match($Tag, '^v(?<version>[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?)$', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
if (-not $tagMatch.Success) { throw "Release tag '$Tag' is malformed. Expected v<major>.<minor>.<patch> with an optional prerelease suffix." }
$tagVersion = $tagMatch.Groups["version"].Value

[xml] $project = Get-Content -LiteralPath $ProjectPath -Raw
$projectVersion = [string] $project.Project.PropertyGroup.Version | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($projectVersion)) { throw "No Version property was found in '$ProjectPath'." }
if (-not $tagVersion.Equals($projectVersion.Trim(), [System.StringComparison]::OrdinalIgnoreCase)) { throw "Release tag version '$tagVersion' does not match project version '$projectVersion'." }

$packages = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter "*.nupkg" -File | Where-Object { $_.Name -notlike "*.snupkg" })
if ($packages.Count -ne 1) { throw "Expected exactly one NuGet package in '$PackageDirectory', but found $($packages.Count)." }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($packages[0].FullName)
try {
    $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName -like "*.nuspec" })
    if ($nuspecEntries.Count -ne 1) { throw "Expected exactly one .nuspec in '$($packages[0].Name)', but found $($nuspecEntries.Count)." }
    $reader = [System.IO.StreamReader]::new($nuspecEntries[0].Open())
    try { [xml] $nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
} finally { $archive.Dispose() }
$packageVersion = [string] $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']").InnerText
if ([string]::IsNullOrWhiteSpace($packageVersion)) { throw "No package version was found in '$($packages[0].Name)'." }
if (-not $tagVersion.Equals($packageVersion.Trim(), [System.StringComparison]::OrdinalIgnoreCase)) { throw "Release tag version '$tagVersion' does not match packed package version '$packageVersion'." }
Write-Host "Validated release version $tagVersion against the project and packed package."
