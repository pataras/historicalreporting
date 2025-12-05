# Historical Reporting - Development Runner
# Starts both the backend API (as admin) and frontend dev server

$ErrorActionPreference = "Stop"

Write-Host "Starting Historical Reporting Development Environment..." -ForegroundColor Cyan
Write-Host ""

# Store the root directory
$rootDir = $PSScriptRoot
$backendDir = "$rootDir\src\HistoricalReporting.Api"
$frontendDir = "$rootDir\frontend"

# Start the backend API as Administrator in a new window
Write-Host "Starting Backend API (.NET) as Administrator..." -ForegroundColor Yellow
$backendProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$backendDir'; Write-Host 'Backend API - Running as Administrator' -ForegroundColor Cyan; dotnet run" -Verb RunAs -PassThru

# Give the backend a moment to start
Start-Sleep -Seconds 2

# Start the frontend in current session
Write-Host "Starting Frontend (Vite)..." -ForegroundColor Yellow
$frontendJob = Start-Job -ScriptBlock {
    param($dir)
    Set-Location $dir
    npm run dev
} -ArgumentList $frontendDir

Write-Host ""
Write-Host "Both services are starting..." -ForegroundColor Green
Write-Host "  Backend API:  Running in elevated PowerShell window (http://localhost:5000)" -ForegroundColor White
Write-Host "  Frontend:     http://localhost:5173 (default Vite port)" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop frontend (close the admin window to stop backend)" -ForegroundColor Magenta
Write-Host ""

# Function to clean up
function Stop-AllServices {
    Write-Host ""
    Write-Host "Stopping services..." -ForegroundColor Yellow

    if ($frontendJob) {
        Stop-Job -Job $frontendJob -ErrorAction SilentlyContinue
        Remove-Job -Job $frontendJob -Force -ErrorAction SilentlyContinue
    }

    # Try to stop the backend process
    if ($backendProcess -and !$backendProcess.HasExited) {
        Stop-Process -Id $backendProcess.Id -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Services stopped." -ForegroundColor Green
}

# Register cleanup on script exit
$null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-AllServices }

try {
    # Monitor and display output from frontend job
    while ($true) {
        $frontendState = $frontendJob.State

        # Receive and display frontend output
        $frontendOutput = Receive-Job -Job $frontendJob -ErrorAction SilentlyContinue
        if ($frontendOutput) {
            $frontendOutput | ForEach-Object { Write-Host "[UI]  $_" -ForegroundColor Green }
        }

        # Check for job failure
        if ($frontendState -eq "Failed") {
            Write-Host "Frontend job failed!" -ForegroundColor Red
            Receive-Job -Job $frontendJob -ErrorAction SilentlyContinue
            break
        }

        # Check if frontend has stopped
        if ($frontendState -ne "Running") {
            Write-Host "Frontend has stopped." -ForegroundColor Yellow
            break
        }

        # Check if backend window was closed
        if ($backendProcess.HasExited) {
            Write-Host "Backend process has stopped." -ForegroundColor Yellow
        }

        Start-Sleep -Milliseconds 500
    }
}
finally {
    Stop-AllServices
}
