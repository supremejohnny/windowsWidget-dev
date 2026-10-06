$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { $compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
& $compiler /nologo /target:exe /out:RopePhysicsSmoke.exe /reference:System.Drawing.dll RopePhysics.cs tests\RopePhysicsSmoke.cs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
try { & .\RopePhysicsSmoke.exe; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE } }
finally { Remove-Item -LiteralPath .\RopePhysicsSmoke.exe -ErrorAction SilentlyContinue }
& $compiler /nologo /target:exe /main:OverlaySmoke /out:OverlaySmoke.exe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll WindowPet.cs RopePhysics.cs LayeredSurface.cs tests\OverlaySmoke.cs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
try { & .\OverlaySmoke.exe; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE } }
finally { Remove-Item -LiteralPath .\OverlaySmoke.exe -ErrorAction SilentlyContinue }
