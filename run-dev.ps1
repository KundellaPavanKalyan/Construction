# Start Employee Payroll API + Angular (run from repo root)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "Stopping old API processes..."
Get-Process -Name "EmployeePayroll.Api" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Starting API on http://localhost:5119 ..."
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location '$root\src\EmployeePayroll.Api'; dotnet run --launch-profile http"
)

Start-Sleep -Seconds 4

Write-Host "Starting Angular on http://127.0.0.1:4200 ..."
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location '$root\src\EmployeePayroll.Web'; npx ng serve --host 127.0.0.1 --port 4200 --configuration=development"
)

Write-Host ""
Write-Host "Open: http://127.0.0.1:4200"
Write-Host "Login: admin / admin"
Write-Host "API:  http://localhost:5119"
