$ErrorActionPreference = 'Stop'
$releaseDirectory = Join-Path $PSScriptRoot 'dist\v0.0.1'
$executable = Join-Path $releaseDirectory 'WindowPet-0.0.1.exe'
& (Join-Path $PSScriptRoot 'build.ps1') -OutputPath $executable
& (Join-Path $PSScriptRoot 'test-portable.ps1') -ExecutablePath $executable
$hash = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
($hash + '  ' + (Split-Path -Leaf $executable)) | Set-Content -LiteralPath (Join-Path $releaseDirectory 'SHA256SUMS.txt') -Encoding ASCII
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\RELEASE_0.0.1.md') -Destination (Join-Path $releaseDirectory 'RELEASE_NOTES.md')
Write-Host "Release candidate prepared: $releaseDirectory"
Write-Host 'No GitHub upload or tag was created.'
