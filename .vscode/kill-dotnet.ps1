param([string]$WorkspacePath)

if (-not $WorkspacePath -or $WorkspacePath -eq '') {
    $WorkspacePath = (Get-Location).Path
}

Write-Host "[kill-dotnet] workspacePath=$WorkspacePath"
$escaped = [regex]::Escape($WorkspacePath)

$procs = Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -and $_.CommandLine -match $escaped -and $_.Name -match 'dotnet' }
if ($procs) {
    foreach ($p in $procs) {
        try {
            Write-Host "[kill-dotnet] stopping PID $($p.ProcessId) ($($p.Name))"
            Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
        } catch {
            Write-Host "[kill-dotnet] failed to stop PID $($p.ProcessId): $_" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "[kill-dotnet] no matching dotnet processes found"
}

exit 0
