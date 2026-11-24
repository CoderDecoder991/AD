<#
.SYNOPSIS
  Builds the Android development APK, retrying automatically when the build
  fails because a download (Gradle/Maven dependency) was interrupted.

.DESCRIPTION
  This PC's connection intermittently corrupts long TLS downloads
  ("bad_record_mac"), which makes Gradle fail part-way through fetching
  dependencies. Every file that finishes downloading is cached by Gradle, so
  each retry only fetches what is still missing and eventually succeeds.

  - Inside one build, Gradle's own per-request retries are raised (default is 3).
  - If the build still fails on a download error, the whole build is rerun.
  - Any other kind of failure (compile error, etc.) stops immediately.

.EXAMPLE
  .\tools\Build-Android.ps1
  .\tools\Build-Android.ps1 -Install          # also install and launch on the connected device
  .\tools\Build-Android.ps1 -MaxAttempts 50
#>
[CmdletBinding()]
param(
    [int]$MaxAttempts = 30,
    [int]$DelaySeconds = 5,
    [switch]$Install,
    [switch]$Review,          # non-development review build: no watermark, no dev overlay/buttons (VS-1)
    [string]$UnityVersion = "6000.6.10f1",
    [string]$PackageName = "com.placeholder.airdefense"
)

$ErrorActionPreference = "Stop"

$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$editorRoot  = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor"
$unityExe    = Join-Path $editorRoot "Unity.exe"
$adbExe      = Join-Path $editorRoot "Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$apkName     = if ($Review) { "AirDefense-review.apk" } else { "AirDefense-dev.apk" }
$apkPath     = Join-Path $projectPath "Builds\Android\$apkName"
$buildMethod = if ($Review) { "AirDefense.EditorTools.BuildScript.BuildAndroidReview" } else { "AirDefense.EditorTools.BuildScript.BuildAndroidDev" }
$logDir      = Join-Path $projectPath "Logs\Build"

if (-not (Test-Path $unityExe)) { throw "Unity not found: $unityExe" }
New-Item -ItemType Directory -Force $logDir | Out-Null

# Patterns that mean "a download broke" -> safe to retry.
$retryable = @(
    "Could not download", "Could not get resource", "Could not GET", "Could not HEAD",
    "bad_record_mac", "Tag mismatch", "Connection reset", "Read timed out",
    "Connect timed out", "Remote host terminated the handshake", "SSLException",
    "Premature end of Content-Length", "could not be added to the local file system"
)

# Applies to every JVM Unity starts, including the Gradle daemon:
#  - more Gradle network retries with backoff, longer HTTP timeouts
#  - JVM AES/GHASH hardware intrinsics off (a known cause of bad_record_mac on some CPUs)
$env:JAVA_TOOL_OPTIONS = @(
    "-Dorg.gradle.internal.repository.max.retries=10",
    "-Dorg.gradle.internal.repository.initial.backoff=1000",
    "-Dorg.gradle.internal.http.connectionTimeout=120000",
    "-Dorg.gradle.internal.http.socketTimeout=120000",
    "-XX:+UnlockDiagnosticVMOptions",
    "-XX:-UseAESIntrinsics", "-XX:-UseAESCTRIntrinsics", "-XX:-UseGHASHIntrinsics"
) -join " "

# --- Local seed repository -------------------------------------------------
# Assets/Plugins/Android/settingsTemplate.gradle makes Gradle check
# <project>/.gradle-seed before the internet. When Gradle fails to download a
# file, we fetch that exact file here with curl, which RESUMES partial
# downloads (-C -), and verify it against the repository's .sha1 checksum.
$seedRoot = Join-Path $projectPath ".gradle-seed"
$curlExe  = "$env:SystemRoot\System32\curl.exe"
$repoPrefixes = @(
    "https://dl.google.com/dl/android/maven2/",
    "https://maven.google.com/",
    "https://plugins.gradle.org/m2/",
    "https://repo.maven.apache.org/maven2/",
    "https://repo1.maven.org/maven2/"
)

function Get-SeedPath([string]$url) {
    foreach ($prefix in $repoPrefixes) {
        if ($url.StartsWith($prefix)) {
            return Join-Path $seedRoot ($url.Substring($prefix.Length) -replace "/", "\")
        }
    }
    return $null
}

# Downloads $url to $dest, resuming after each interruption, until the file
# matches the published SHA-1 (or the attempt budget runs out).
function Get-VerifiedFile([string]$url, [string]$dest, [switch]$Optional) {
    # curl reports retries on stderr; under "Stop" Windows PowerShell would turn that into a fatal error.
    # Exit codes and checksums decide success here instead.
    $ErrorActionPreference = "Continue"
    New-Item -ItemType Directory -Force (Split-Path $dest) | Out-Null
    $expected = (& $curlExe -sSLf --max-time 60 --retry 10 --retry-all-errors "$url.sha1" 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $expected) {
        if ($Optional) { return $false }      # e.g. no .module file published
        Write-Host "    no checksum published for $url" -ForegroundColor Yellow
        return $false
    }
    $expected = ($expected | Out-String).Trim().Substring(0, 40).ToLowerInvariant()

    for ($i = 1; $i -le 30; $i++) {
        if (Test-Path $dest) {
            $actual = (Get-FileHash $dest -Algorithm SHA1).Hash.ToLowerInvariant()
            if ($actual -eq $expected) { return $true }
        }
        # -C - resumes from the current size of $dest.
        & $curlExe -sSL --fail -C - --retry 20 --retry-all-errors --retry-delay 2 --max-time 600 -o "$dest" "$url" 2>$null
        if (Test-Path $dest) {
            $actual = (Get-FileHash $dest -Algorithm SHA1).Hash.ToLowerInvariant()
            if ($actual -eq $expected) { return $true }
            # A complete-sized but corrupt file can't be resumed: start over.
            $remoteSize = (& $curlExe -sSIL --max-time 30 "$url" | Select-String -Pattern "^Content-Length:\s*(\d+)" | Select-Object -Last 1)
            if ($remoteSize -and ((Get-Item $dest).Length -ge [long]$remoteSize.Matches[0].Groups[1].Value)) {
                Remove-Item $dest -Force
            }
        }
    }
    if (Test-Path $dest) { Remove-Item $dest -Force }
    return $false
}

function Add-SeedArtifacts([string]$logText) {
    $urls = [regex]::Matches($logText, "Could not get resource '([^']+)'") |
            ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    foreach ($url in $urls) {
        $dest = Get-SeedPath $url
        if (-not $dest) { Write-Host "    (unknown repository, left to Gradle) $url"; continue }
        $name = Split-Path $url -Leaf
        Write-Host "    seeding $name with resumable download..." -NoNewline
        if (Get-VerifiedFile $url $dest) {
            # Gradle also needs the artifact's metadata from the same repository.
            $dirUrl   = $url.Substring(0, $url.LastIndexOf("/"))
            $version  = Split-Path $dirUrl -Leaf
            $artifact = Split-Path ($dirUrl.Substring(0, $dirUrl.LastIndexOf("/"))) -Leaf
            $base     = "$dirUrl/$artifact-$version"
            $null = Get-VerifiedFile "$base.pom"    (Get-SeedPath "$base.pom")
            $null = Get-VerifiedFile "$base.module" (Get-SeedPath "$base.module") -Optional
            Write-Host " ok (SHA-1 verified)" -ForegroundColor Green
        } else {
            Write-Host " failed; Gradle will try again" -ForegroundColor Yellow
        }
    }
}

$started = Get-Date
for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    $log = Join-Path $logDir ("build-{0:yyyyMMdd-HHmmss}-a{1}.log" -f (Get-Date), $attempt)
    Write-Host ("[{0:HH:mm:ss}] Attempt {1}/{2} ..." -f (Get-Date), $attempt, $MaxAttempts) -ForegroundColor Cyan

    if (Test-Path $apkPath) { Remove-Item $apkPath -Force }

    $unityArgs = @(
        "-batchmode", "-quit", "-nographics",
        "-projectPath", "`"$projectPath`"",
        "-buildTarget", "Android",
        "-executeMethod", $buildMethod,
        "-logFile", "`"$log`""
    )
    $p = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow

    $text = if (Test-Path $log) { Get-Content $log -Raw } else { "" }
    $summary = ($text -split "`n" | Where-Object { $_ -match "\[BuildScript\]" } | Select-Object -Last 1)

    if ($p.ExitCode -eq 0 -and (Test-Path $apkPath)) {
        $mb = [math]::Round((Get-Item $apkPath).Length / 1MB, 1)
        Write-Host ("[{0:HH:mm:ss}] SUCCESS on attempt {1} after {2:n0} min. APK {3} MB" -f (Get-Date), $attempt, ((Get-Date) - $started).TotalMinutes, $mb) -ForegroundColor Green
        break
    }

    $hit = $retryable | Where-Object { $text -match [regex]::Escape($_) } | Select-Object -First 1
    if (-not $hit) {
        Write-Host "Build failed for a reason that is NOT a download error. Not retrying." -ForegroundColor Red
        if ($summary) { Write-Host $summary }
        Write-Host "Log: $log"
        exit 1
    }

    $missing = [regex]::Matches($text, "Could not download (\S+)") | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    Write-Host ("  Download interrupted ({0}). Still missing: {1}" -f $hit, ($(if ($missing) { $missing -join ", " } else { "?" }))) -ForegroundColor Yellow
    Add-SeedArtifacts $text

    if ($attempt -eq $MaxAttempts) {
        Write-Host "Gave up after $MaxAttempts attempts. Log: $log" -ForegroundColor Red
        exit 1
    }
    Start-Sleep -Seconds $DelaySeconds
}

if ($Install) {
    if (-not (Test-Path $adbExe)) { throw "adb not found: $adbExe" }
    # adb prints "daemon not running; starting now" on stderr, which "Stop" would turn into a failure.
    # Native exit codes are checked explicitly below instead.
    $ErrorActionPreference = "Continue"
    & $adbExe start-server 2>$null | Out-Null
    $devices = & $adbExe devices | Select-String "\tdevice$"
    if (-not $devices) { Write-Host "No authorised Android device connected; skipping install." -ForegroundColor Yellow; exit 0 }
    Write-Host "Installing on device..."
    & $adbExe install -r "$apkPath"
    if ($LASTEXITCODE -ne 0) { throw "adb install failed" }
    & $adbExe shell monkey -p $PackageName -c android.intent.category.LAUNCHER 1 | Out-Null
    Write-Host "Launched $PackageName." -ForegroundColor Green
}
