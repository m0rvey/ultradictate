$modelsDir = Join-Path $env:APPDATA "UltraDictate\models"
if (-not (Test-Path $modelsDir)) {
    New-Item -ItemType Directory -Force -Path $modelsDir | Out-Null
}
$target = Join-Path $modelsDir "ggml-small.bin"
Write-Host "Target model path: $target"
if (Test-Path $target) {
    Write-Host "Model already exists: $((Get-Item $target).Length) bytes"
} else {
    Write-Host "Downloading ggml-small.bin (~465 MB)..."
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile("https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin", $target)
    Write-Host "Download complete: $((Get-Item $target).Length) bytes"
}
