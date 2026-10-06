$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { $compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (Get-Command node -ErrorAction SilentlyContinue) {
    & node tests\GenerateRopeReference.mjs --check
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
& $compiler /nologo /target:exe /out:RopeReferenceSmoke.exe VerletRopeSolver.cs tests\RopeReferenceSmoke.cs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
try { & .\RopeReferenceSmoke.exe; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE } }
finally { Remove-Item -LiteralPath .\RopeReferenceSmoke.exe -ErrorAction SilentlyContinue }
& $compiler /nologo /target:exe /out:RopePhysicsSmoke.exe /reference:System.Drawing.dll RopePhysics.cs VerletRopeSolver.cs tests\RopePhysicsSmoke.cs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
try { & .\RopePhysicsSmoke.exe; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE } }
finally { Remove-Item -LiteralPath .\RopePhysicsSmoke.exe -ErrorAction SilentlyContinue }
& $compiler /nologo /target:exe /out:PetBodyPhysicsSmoke.exe /reference:System.Drawing.dll PetBodyPhysics.cs tests\PetBodyPhysicsSmoke.cs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
try { & .\PetBodyPhysicsSmoke.exe; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE } }
finally { Remove-Item -LiteralPath .\PetBodyPhysicsSmoke.exe -ErrorAction SilentlyContinue }
& $compiler /nologo /target:exe /main:OverlaySmoke /out:OverlaySmoke.exe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /resource:assets/amiya_test1/character.png,WindowPet.amiya_test1.png /resource:THIRD_PARTY_NOTICES.txt,WindowPet.NOTICES.txt AssemblyInfo.cs PetAssets.cs WindowPet.cs RopePhysics.cs VerletRopeSolver.cs PetBodyPhysics.cs LayeredSurface.cs tests\OverlaySmoke.cs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
try { & .\OverlaySmoke.exe; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE } }
finally { Remove-Item -LiteralPath .\OverlaySmoke.exe -ErrorAction SilentlyContinue }
