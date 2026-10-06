$ErrorActionPreference = 'Stop'
$running = Get-Process WindowPet -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $PSScriptRoot 'WindowPet.exe') }
if ($running) { throw 'Close WindowPet.exe before rebuilding it.' }
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { $compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $compiler)) { throw 'Windows .NET Framework C# compiler not found.' }
& $compiler /nologo /target:winexe /optimize+ /out:WindowPet.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll WindowPet.cs RopePhysics.cs VerletRopeSolver.cs PetBodyPhysics.cs LayeredSurface.cs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'Built WindowPet.exe'
