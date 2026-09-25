# 윈도우 빌드를 만들어 비공개 저장소(myFirst2DGame-builds)의 Release로 올린다.
# 에디터가 켜져 있어도 되도록 프로젝트 복사본에서 빌드한다.
#
# 사용법: pwsh tools/release.ps1            (버전 자동 증가: v1.0 -> v1.1 ...)
#         pwsh tools/release.ps1 -Version v2.0
#         pwsh tools/release.ps1 -NotesFile docs/patch-notes/v1.0.md
param(
    [string]$Version,
    [string]$NotesFile,     # 패치노트 파일 (없으면 커밋 목록으로 자동 작성)
    [string]$Repo = "suritola/myFirst2DGame-builds",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"

$Project = Split-Path $PSScriptRoot -Parent
$Work = Join-Path $env:LOCALAPPDATA "FuxkBuild"
$CopyDir = Join-Path $Work "Project"
$OutDir = Join-Path $Work "Out"
$Gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
if (-not $Gh) { $Gh = "C:\Program Files\GitHub CLI\gh.exe" }

# ---------------------------------------------------------------- 버전과 변경 내역
$commit = (git -C $Project rev-parse --short HEAD).Trim()
$prevTag = $null
$prevCommit = $null
$latest = & $Gh release list -R $Repo --limit 1 --json tagName --jq ".[0].tagName" 2>$null
if ($LASTEXITCODE -eq 0 -and $latest) {
    $prevTag = $latest.Trim()
    # 여러 줄 설명은 줄 배열로 오므로 하나로 합침
    $body = (& $Gh release view $prevTag -R $Repo --json body --jq ".body") -join "`n"
    if ($body -match "소스 커밋: ([0-9a-f]+)") { $prevCommit = $Matches[1] }
}
if (-not $Version) {
    if ($prevTag -match "^v(\d+)\.(\d+)$") { $Version = "v$($Matches[1]).$([int]$Matches[2] + 1)" }
    else { $Version = "v1.0" }
}
if ($prevCommit) { $changes = git -C $Project log --pretty="- %s" "$prevCommit..HEAD" }
else { $changes = git -C $Project log --pretty="- %s" -n 15 }
if ($NotesFile) { $notes = "소스 커밋: $commit`n`n" + (Get-Content $NotesFile -Raw -Encoding utf8) }
else { $notes = "소스 커밋: $commit`n`n## 변경 내역`n" + ($changes -join "`n") }
Write-Host "버전 $Version (커밋 $commit)"

# ---------------------------------------------------------------- 프로젝트 복사 (Library는 재사용해서 빠르게)
New-Item -ItemType Directory -Force $CopyDir | Out-Null
foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}

# ---------------------------------------------------------------- 빌드
if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }
$log = Join-Path $Work "build.log"
Write-Host "Unity 빌드 중... (로그: $log)"
$p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    "-batchmode", "-quit", "-projectPath", "`"$CopyDir`"",
    "-executeMethod", "BuildScript.BuildWindows",
    "-buildOutput", "`"$OutDir`"", "-buildVersion", $Version.TrimStart("v"),
    "-logFile", "`"$log`"")
if ($p.ExitCode -ne 0 -or -not (Get-ChildItem $OutDir -Filter *.exe -ErrorAction SilentlyContinue)) {
    Get-Content $log -Tail 40
    throw "빌드 실패 (종료 코드 $($p.ExitCode))"
}

# ---------------------------------------------------------------- 압축 (디버그 파일 제외)
Get-ChildItem $OutDir -Directory -Filter "*DoNotShip*" | Remove-Item -Recurse -Force
$zip = Join-Path $Work "GunSaver-$Version-Windows.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path (Join-Path $OutDir "*") -DestinationPath $zip
Write-Host ("압축 완료: {0:N1} MB" -f ((Get-Item $zip).Length / 1MB))

# ---------------------------------------------------------------- 업로드
& $Gh release create $Version $zip -R $Repo --title "Soul Saver $Version" --notes $notes
if ($LASTEXITCODE -ne 0) { throw "Release 업로드 실패" }
Write-Host "완료: https://github.com/$Repo/releases/tag/$Version"
