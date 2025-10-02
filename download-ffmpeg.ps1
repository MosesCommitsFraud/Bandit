# PowerShell script to download ffmpeg for Windows

$ffmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
$tempZip = Join-Path $env:TEMP "ffmpeg.zip"
$tempExtract = Join-Path $env:TEMP "ffmpeg-extract"
$outputDir = Join-Path $PSScriptRoot "GnR.App\bin\Debug\net9.0-windows"

Write-Host "Downloading ffmpeg..." -ForegroundColor Cyan

try {
    # Download ffmpeg
    Invoke-WebRequest -Uri $ffmpegUrl -OutFile $tempZip -UseBasicParsing
    
    Write-Host "Extracting ffmpeg..." -ForegroundColor Cyan
    
    # Extract
    Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force
    
    # Find the bin directory
    $binDir = Get-ChildItem -Path $tempExtract -Filter "bin" -Recurse -Directory | Select-Object -First 1
    
    if ($binDir) {
        # Copy executables to output directory
        $exeFiles = Get-ChildItem -Path $binDir.FullName -Filter "*.exe"
        
        foreach ($exe in $exeFiles) {
            $destPath = Join-Path $outputDir $exe.Name
            Copy-Item -Path $exe.FullName -Destination $destPath -Force
            Write-Host "Copied $($exe.Name)" -ForegroundColor Green
        }
        
        Write-Host ""
        Write-Host "Successfully installed ffmpeg, ffprobe, and ffplay!" -ForegroundColor Green
        Write-Host "Location: $outputDir" -ForegroundColor Gray
        
        # Verify
        $ffmpegPath = Join-Path $outputDir "ffmpeg.exe"
        if (Test-Path $ffmpegPath) {
            Write-Host ""
            Write-Host "Verifying installation..." -ForegroundColor Cyan
            & $ffmpegPath -version | Select-Object -First 1
        }
    }
    else {
        Write-Host "Could not find bin directory in extracted files" -ForegroundColor Red
        exit 1
    }
    
    # Cleanup
    Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host ""
    Write-Host "ffmpeg is ready to use!" -ForegroundColor Green
}
catch {
    Write-Host "Failed to download/install ffmpeg" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
