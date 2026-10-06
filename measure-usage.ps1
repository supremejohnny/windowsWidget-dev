param(
    [int]$ProcessId = 0,
    [ValidateRange(0.05,1440)][double]$DurationMinutes = 60,
    [ValidateRange(1,300)][int]$IntervalSeconds = 10,
    [string]$OutputPath = ''
)
$ErrorActionPreference = 'Stop'
if (-not $ProcessId) {
    $candidates = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like 'WindowPet*' })
    if ($candidates.Count -ne 1) { throw 'Pass -ProcessId to identify exactly one running WindowPet.' }
    $ProcessId = $candidates[0].Id
}
$observedProcess = Get-Process -Id $ProcessId
$expectedStart = $observedProcess.StartTime.ToUniversalTime()
if (-not $OutputPath) { $OutputPath = Join-Path $PSScriptRoot ('.test-output\usage-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + "-$ProcessId.csv") }
$OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if (Test-Path -LiteralPath $OutputPath) { throw 'Choose a new output file; existing samples are preserved.' }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
if (-not ('WindowPetUsage.Native' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
namespace WindowPetUsage {
  public static class Native {
    [DllImport("user32.dll", SetLastError=true)]
    public static extern uint GetGuiResources(IntPtr process, uint flags);
  }
}
'@
}
$watch = [Diagnostics.Stopwatch]::StartNew()
$priorTime = $null
$priorCpu = $null
$samples = 0
try {
    while ($watch.Elapsed.TotalMinutes -le $DurationMinutes) {
        $observedProcess.Refresh()
        if ($observedProcess.HasExited) { Write-Host 'Observed process exited.'; break }
        if ($observedProcess.StartTime.ToUniversalTime() -ne $expectedStart) { throw 'Process identity changed.' }
        $elapsed = $watch.Elapsed.TotalSeconds
        $cpu = $observedProcess.TotalProcessorTime.TotalSeconds
        $cpuOneCore = $null
        if ($null -ne $priorTime) { $cpuOneCore = 100 * ($cpu - $priorCpu) / ($elapsed - $priorTime) }
        $privateWorkingSet = $null
        try {
            $perf = Get-CimInstance Win32_PerfFormattedData_PerfProc_Process -Filter "IDProcess = $ProcessId" -ErrorAction Stop
            if ($null -ne $perf) { $privateWorkingSet = [math]::Round($perf.WorkingSetPrivate / 1MB,3) }
        } catch { Write-Warning 'Private working set unavailable for this sample; other metrics still recorded.' }
        $row = [pscustomobject]@{
            TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
            ProcessId = $ProcessId
            ElapsedSeconds = [math]::Round($elapsed,3)
            PrivateWorkingSetMiB = $privateWorkingSet
            WorkingSetMiB = [math]::Round($observedProcess.WorkingSet64 / 1MB,3)
            PrivateBytesMiB = [math]::Round($observedProcess.PrivateMemorySize64 / 1MB,3)
            CpuOneCorePercent = $(if ($null -ne $cpuOneCore) { [math]::Round($cpuOneCore,3) } else { $null })
            CpuMachinePercent = $(if ($null -ne $cpuOneCore) { [math]::Round($cpuOneCore / [Environment]::ProcessorCount,3) } else { $null })
            TotalCpuSeconds = [math]::Round($cpu,4)
            GdiHandles = [WindowPetUsage.Native]::GetGuiResources($observedProcess.Handle,0)
            UserHandles = [WindowPetUsage.Native]::GetGuiResources($observedProcess.Handle,1)
            Handles = $observedProcess.HandleCount
            Threads = $observedProcess.Threads.Count
        }
        $row | Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding UTF8 -Append
        $samples++
        $priorTime = $elapsed
        $priorCpu = $cpu
        $remaining = ($DurationMinutes * 60) - $watch.Elapsed.TotalSeconds
        if ($remaining -le 0) { break }
        Start-Sleep -Milliseconds ([int]([math]::Min($IntervalSeconds,$remaining) * 1000))
    }
} finally { $observedProcess.Dispose() }
Write-Host "Recorded $samples samples: $OutputPath"
Write-Host 'MiB = 1,048,576 bytes. CpuOneCorePercent uses one logical CPU as 100%; CpuMachinePercent uses the whole machine.'
