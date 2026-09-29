# 실제 게임 플레이 트레일러(MP4)와 스팀 스크린샷(PNG)을 배치 모드로 찍는다 (tools/trailer/GameplayCapture.cs)
# 에디터가 켜져 있어도 되도록 release.ps1 · steam-upload.ps1 과 같은 프로젝트 복사본에서 실행한다.
# 복사본은 제품 이름을 바꿔서, 스킨 장착 같은 촬영용 저장값이 원래 에디터 저장 데이터와 섞이지 않게 한다.
#
# 사용법: pwsh tools/trailer/record.ps1                       (30fps · Medium 화질)
#         pwsh tools/trailer/record.ps1 -Fps 60 -Quality High
# 결과:   %LOCALAPPDATA%\SoulSaverBuild\Capture\SoulSaver_Trailer.mp4, screenshots\*.png
param(
    [int]$Fps = 30,
    [ValidateSet("Low", "Medium", "High")][string]$Quality = "Medium",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8

$Project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work "Project"
$Out = Join-Path $Work "Capture"

New-Item -ItemType Directory -Force $CopyDir | Out-Null
foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
$capDir = Join-Path $CopyDir "Assets\Capture"
New-Item -ItemType Directory -Force $capDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "GameplayCapture.cs"), (Join-Path $PSScriptRoot "GameplayCapture.asmdef") $capDir
$settings = Join-Path $CopyDir "ProjectSettings\ProjectSettings.asset"
(Get-Content $settings -Raw) -replace "(?m)^  productName: .*$", "  productName: Soul Saver Capture" | Set-Content $settings -NoNewline

if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
New-Item -ItemType Directory -Force $Out | Out-Null
$env:CAPTURE_OUT = $Out
$env:CAPTURE_FPS = "$Fps"
$env:CAPTURE_QUALITY = $Quality
$log = Join-Path $Work "capture.log"
Write-Host "촬영 중... ($Fps fps · $Quality, 로그: $log)"
$p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    "-batchmode", "-projectPath", "`"$CopyDir`"",
    "-runTests", "-testPlatform", "PlayMode", "-testFilter", "GameplayCapture",
    "-testResults", "`"$(Join-Path $Work 'capture-results.xml')`"",
    "-logFile", "`"$log`"")

# 복사본을 원래대로 (다음 빌드에 촬영 코드가 섞이지 않게)
Remove-Item -Recurse -Force $capDir, "$capDir.meta" -ErrorAction SilentlyContinue
robocopy (Join-Path $Project "ProjectSettings") (Join-Path $CopyDir "ProjectSettings") /MIR /NFL /NDL /NJH /NJS /NP | Out-Null

Select-String -Path $log -Pattern "\[CAP\]" | ForEach-Object { $_.Line }
$mp4 = Join-Path $Out "SoulSaver_Trailer.mp4"
if (-not (Test-Path $mp4)) { Get-Content $log -Tail 40; throw "영상이 만들어지지 않았습니다 (Unity 종료 코드 $($p.ExitCode))" }
Write-Host ("완료: {0} ({1:N1} MB)" -f $mp4, ((Get-Item $mp4).Length / 1MB))
Get-ChildItem (Join-Path $Out "screenshots") | ForEach-Object { Write-Host "  $($_.Name)" }
