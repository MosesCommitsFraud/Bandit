# PowerShell script to download yt-dlp.exe

$ytDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
$outputDir = Join-Path $PSScriptRoot "Bandit.App\bin\Debug\net9.0-windows"
$outputPath = Join-Path $outputDir "yt-dlp.exe"

Write-Host "Downloading yt-dlp.exe..." -ForegroundColor Cyan

# Create directory if it doesn't exist
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

try {
    # Download yt-dlp
    Invoke-WebRequest -Uri $ytDlpUrl -OutFile $outputPath -UseBasicParsing
    
    Write-Host "✓ Successfully downloaded yt-dlp.exe to:" -ForegroundColor Green
    Write-Host "  $outputPath" -ForegroundColor Gray
    
    # Verify it works
    Write-Host "`nVerifying installation..." -ForegroundColor Cyan
    & $outputPath --version
    
    Write-Host "`n✓ yt-dlp is ready to use!" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to download yt-dlp.exe" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

