param([string]$OutputPath = (Join-Path $PSScriptRoot 'WindowPet.exe'))
$ErrorActionPreference = 'Stop'
$OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$running = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like 'WindowPet*' -and $_.Path -eq $OutputPath }
if ($running) { throw "Close $OutputPath before rebuilding it, or choose another -OutputPath." }
$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { $compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $compiler)) { throw 'Windows .NET Framework C# compiler not found.' }
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$OutputPath" /win32manifest:WindowPet.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /resource:assets/amiya_test1/character.png,WindowPet.amiya_test1.png /resource:THIRD_PARTY_NOTICES.txt,WindowPet.NOTICES.txt AssemblyInfo.cs PetAssets.cs WindowPet.cs RopePhysics.cs VerletRopeSolver.cs PetBodyPhysics.cs LayeredSurface.cs
    if ($LASTEXITCODE -ne 0) { throw "C# compiler failed: $LASTEXITCODE" }
} finally { Pop-Location }
Write-Host "Built $OutputPath"
