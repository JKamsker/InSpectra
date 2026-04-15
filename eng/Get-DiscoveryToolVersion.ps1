param(
  [string]$Ref = 'HEAD',
  [string[]]$SourcePaths = @(
    'src/InSpectra.Discovery.Tool',
    'src/InSpectra.Lib',
    'src/InSpectra.Gen.StartupHook',
    'README.md'
  ),
  [string]$VersionPrefix = '0.1.0-ci'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$commitSha = (git log -1 --format=%H $Ref -- @SourcePaths).Trim()
if ([string]::IsNullOrWhiteSpace($commitSha)) {
  throw "Unable to resolve a commit that touched the discovery tool package inputs from ref '$Ref'."
}

$commitTimestamp = (git log -1 --format=%cd --date=format:'%Y%m%d%H%M%S' $commitSha).Trim()
$shortSha = $commitSha.Substring(0, [Math]::Min(12, $commitSha.Length)).ToLowerInvariant()

return "$VersionPrefix.$commitTimestamp.$shortSha"
