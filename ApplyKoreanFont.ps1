# Unity 에디터가 이 프로젝트를 열고 있지 않을 때만 실행하세요.
$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe"
if (-not (Test-Path $Unity)) {
    Write-Error "Unity.exe 를 찾을 수 없습니다: $Unity`nHub에서 설치한 버전 경로로 수정하세요."
}
$Log = Join-Path $ProjectRoot "Temp\korean_font_batch.log"
New-Item -ItemType Directory -Force -Path (Split-Path $Log) | Out-Null
& $Unity -batchmode -nographics -quit -projectPath $ProjectRoot -logFile $Log -executeMethod KoreanTmpFontSetup.ApplyKoreanFontBatch
Write-Host "Unity exit code: $LASTEXITCODE"
Write-Host "Log: $Log"
Get-Content $Log -Tail 40
