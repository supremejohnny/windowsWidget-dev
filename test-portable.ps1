param([Parameter(Mandatory=$true)][string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
$ExecutablePath = (Resolve-Path -LiteralPath $ExecutablePath).Path
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { $compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$testDirectory = Join-Path $PSScriptRoot ('.test-output\portable 空目录 ' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$portableExe = Join-Path $testDirectory 'WindowPet.exe'
Copy-Item -LiteralPath $ExecutablePath -Destination $portableExe
$sourceImage = Join-Path $PSScriptRoot 'assets\amiya_test1\character.png'
foreach ($architecture in @('x64','x86')) {
    if ($architecture -eq 'x64' -and -not [Environment]::Is64BitOperatingSystem) { continue }
    $tester = Join-Path $PSScriptRoot ".test-output\PortableSmoke-$architecture.exe"
    & $compiler /nologo /target:exe "/platform:$architecture" "/out:$tester" /reference:System.Drawing.dll (Join-Path $PSScriptRoot 'tests\PortableSmoke.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Portable test compilation failed.' }
    Push-Location $testDirectory
    try {
        & $tester $portableExe $sourceImage
        if ($LASTEXITCODE -ne 0) { throw "Portable smoke failed ($architecture)." }
    } finally { Pop-Location }
}
Write-Host "Tested single EXE in: $testDirectory"
