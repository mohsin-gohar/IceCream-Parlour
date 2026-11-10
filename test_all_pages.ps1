$ErrorActionPreference = "Continue"

Write-Output "=== Ice Cream Parlour - Test Script ==="
Write-Output "Stopping any existing dotnet processes..."
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Output "Starting the project..."
$path = "F:\.net\Ice_Cream_Parlour_Eproject\Ice_Cream_Parlour_Eproject"
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "run", "--urls", "http://localhost:5000"
$psi.WorkingDirectory = $path
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$p = New-Object System.Diagnostics.Process
$p.StartInfo = $psi
$p.Start() | Out-Null
Write-Output "PID: $($p.Id)"

Start-Sleep -Seconds 15

$urls = @("/", "/Home/About", "/Home/Product", "/Home/Recipe", "/Book", "/Cart", "/health", "/Account/Login")
foreach ($u in $urls) {
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:5000$u" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        Write-Output "Page $u => $($resp.StatusCode)"
    } catch {
        Write-Output "Page $u => Failed"
    }
}

Write-Output "=== Test Complete ==="
$p.Kill()
$ErrorActionPreference = "Stop"
cd "F:\.net\Ice_Cream_Parlour_Eproject\Ice_Cream_Parlour_Eproject"

# Kill existing dotnet processes
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force

Start-Sleep -Seconds 2

# Start dotnet run in background
Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", "http://localhost:5000" -WorkingDirectory "F:\.net\Ice_Cream_Parlour_Eproject\Ice_Cream_Parlour_Eproject" -PassThru

# Wait for server to start
Start-Sleep -Seconds 10

# Test pages
$urls = @("/", "/Home/About", "/Home/Product", "/Recipe", "/Book", "/Cart", "/Cart/Checkout", "/Account/Login", "/Account/Register", "/health", "/Admin/Dashboard")

foreach ($url in $urls) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000$url" -UseBasicParsing -TimeoutSec 10
        Write-Output "$url => $($response.StatusCode)"
    } catch {
        Write-Output "$url => 0 (Error)"
    }
}

Write-Output "ALL_DONE"
