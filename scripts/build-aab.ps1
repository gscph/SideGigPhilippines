<#
.SYNOPSIS
    Builds the Google Play bundle (.aab) on this PC, the same way .github/workflows/build.yml does with channel prod.

.DESCRIPTION
    - Builds a clean copy of a commit (default origin/main) in a git worktree outside the repo folder, so Visual Studio
      can stay open and local edits never reach the store build.
    - Signs with the upload key. The passwords are asked for (or read from SIDEGIGPH_KEYSTORE_PASS /
      SIDEGIGPH_KEY_PASS), kept only in this process and never written to disk or the screen.
    - Refuses a bundle that isn't signed with the upload key (docs/android-signing.md).
    - Leaves the .aab and R8's mapping.txt in artifacts\android\ (git-ignored), named like the CI artifact.

    The build number is the Android versionCode: Play refuses one it has seen. It defaults to one more than the last
    number this script used on this PC (artifacts\android\last-build-number.txt), or 46 the first time (GitHub's last
    Build Android run was 45).

    Needs: .NET 10 SDK with the MAUI Android workload (Visual Studio installs both), Git for Windows (git + bash),
    a JDK's keytool (Visual Studio's Microsoft OpenJDK is found by itself), the upload keystore, and
    SideGigPh.Maui\Platforms\Android\google-services.json in this repo (git-ignored).

.EXAMPLE
    .\scripts\build-aab.ps1 -Keystore D:\keys\sidegigph-release.keystore

.EXAMPLE
    .\scripts\build-aab.ps1 -Keystore D:\keys\sidegigph-release.keystore -BuildNumber 50 -Ref HEAD
#>
[CmdletBinding()]
param(
    # The upload keystore. Default: the SIDEGIGPH_KEYSTORE environment variable.
    [string]$Keystore = $env:SIDEGIGPH_KEYSTORE,

    # The key's alias. Default: SIDEGIGPH_KEY_ALIAS, else "sidegigph".
    [string]$KeyAlias = $(if ($env:SIDEGIGPH_KEY_ALIAS) { $env:SIDEGIGPH_KEY_ALIAS } else { 'sidegigph' }),

    # The Android versionCode. Default: the last one this script used + 1.
    [int]$BuildNumber = 0,

    # The commit to build. Default: origin/main, fetched first.
    [string]$Ref = 'origin/main',

    # major.minor.patch. Default: worked out by .github/scripts/app-version.sh, as in CI.
    [string]$Version = '',

    # Where the temporary worktree goes. Kept short: Android builds fail on long Windows paths.
    [string]$WorkRoot = "$env:SystemDrive\tbuild"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# The upload key's SHA-1 (docs/android-signing.md). A bundle signed with any other key is refused.
$UploadKeySha1 = '55:D2:EF:BE:2A:0C:79:7C:88:4E:11:08:47:E2:44:F2:2C:F1:E0:DC'
$FirstBuildNumber = 46

function Step([string]$text) { Write-Host "`n== $text" -ForegroundColor Cyan }
# Throws, so the clean-up in finally (worktree, passwords) always runs.
function Fail([string]$text) { throw "ERROR: $text" }

function Invoke-Native([string]$what, [scriptblock]$command) {
    & $command
    if ($LASTEXITCODE -ne 0) { Fail "$what failed (exit code $LASTEXITCODE)." }
}

function Find-Keytool {
    $candidates = @()
    if ($env:JAVA_HOME) { $candidates += Join-Path $env:JAVA_HOME 'bin\keytool.exe' }
    $onPath = Get-Command keytool -ErrorAction SilentlyContinue
    if ($onPath) { $candidates += $onPath.Source }
    foreach ($root in @("$env:ProgramFiles\Microsoft", "$env:ProgramFiles\Android\openjdk", "$env:ProgramFiles\Java")) {
        if (Test-Path $root) {
            $candidates += Get-ChildItem $root -Directory | Sort-Object Name -Descending |
                ForEach-Object { Join-Path $_.FullName 'bin\keytool.exe' }
        }
    }
    foreach ($path in $candidates) { if ($path -and (Test-Path $path)) { return $path } }
    Fail 'keytool not found. Install a JDK (Visual Studio installs Microsoft OpenJDK) or set JAVA_HOME.'
}

# Git for Windows' bash, not WSL's (C:\Windows\System32\bash.exe runs Linux and can't see this checkout the same way).
function Find-Bash {
    foreach ($path in @("$env:ProgramFiles\Git\bin\bash.exe", "${env:ProgramFiles(x86)}\Git\bin\bash.exe")) {
        if (Test-Path $path) { return $path }
    }
    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($git) {
        $path = Join-Path (Split-Path (Split-Path $git.Source)) 'bin\bash.exe'
        if (Test-Path $path) { return $path }
    }
    return $null
}

function Read-Secret([string]$prompt) {
    $secure = Read-Host -Prompt $prompt -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

# --- Checks before anything is built -------------------------------------------------------------------------------
$repo = (& git rev-parse --show-toplevel 2>$null)
if (-not $repo) { Fail 'Run this from inside the SideGigPhilippines repository.' }
$repo = $repo.Trim()
$outDir = Join-Path $repo 'artifacts\android'
$lastNumberFile = Join-Path $outDir 'last-build-number.txt'
$googleServices = Join-Path $repo 'SideGigPh.Maui\Platforms\Android\google-services.json'

Step 'Checking what the build needs'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Fail '.NET SDK not found (dotnet).' }
$bash = Find-Bash
if (-not $bash -and -not $Version) { Fail "Git for Windows' bash not found. Install it, or pass -Version major.minor.patch." }
if (-not (Test-Path $googleServices)) {
    Fail "Missing $googleServices. Download it from Firebase (Project settings > the Android app com.tarbaho.app)."
}
if (-not $Keystore) { Fail 'Pass -Keystore <path to the upload keystore> (or set SIDEGIGPH_KEYSTORE).' }
if (-not (Test-Path $Keystore)) { Fail "Keystore not found: $Keystore" }
$Keystore = (Resolve-Path $Keystore).Path
$keytool = Find-Keytool

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
if ($BuildNumber -le 0) {
    $last = $FirstBuildNumber - 1
    if (Test-Path $lastNumberFile) { $last = [int]((Get-Content $lastNumberFile -Raw).Trim()) }
    $BuildNumber = $last + 1
}
Write-Host "versionCode (build number): $BuildNumber"

# --- The commit to build -----------------------------------------------------------------------------------------
Step "Getting $Ref"
if ($Ref -like 'origin/*') {
    Invoke-Native 'git fetch' { git -C $repo fetch --quiet origin ($Ref.Substring(7)) }
}
$commit = & git -C $repo rev-parse --verify --quiet "$Ref^{commit}"
if ($LASTEXITCODE -ne 0 -or -not $commit) { Fail "Unknown commit: $Ref" }
$commit = $commit.Trim()
Write-Host "Commit: $($commit.Substring(0, 7)) $(& git -C $repo log -1 --format=%s $commit)"
$pending = & git -C $repo status --porcelain
if ($pending) { Write-Host 'Note: your uncommitted changes are NOT in this build (it builds the commit only).' -ForegroundColor Yellow }

# --- Passwords (memory only) ---------------------------------------------------------------------------------------
$storePass = $env:SIDEGIGPH_KEYSTORE_PASS
$keyPass = $env:SIDEGIGPH_KEY_PASS
if (-not $storePass) { $storePass = Read-Secret 'Keystore password' }
if (-not $keyPass) {
    $keyPass = Read-Secret 'Key password (Enter if the same as the keystore password)'
    if (-not $keyPass) { $keyPass = $storePass }
}

$work = Join-Path $WorkRoot "aab-$BuildNumber-$([Guid]::NewGuid().ToString('N').Substring(0, 6))"
$hadStorePass = [bool]$env:SIDEGIGPH_KEYSTORE_PASS
$hadKeyPass = [bool]$env:SIDEGIGPH_KEY_PASS
$hadGhToken = [bool]$env:GH_TOKEN
try {
    Step "Making a clean copy in $work"
    New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null
    Invoke-Native 'git worktree add' { git -C $repo worktree add --detach --quiet $work $commit }
    Copy-Item $googleServices (Join-Path $work 'SideGigPh.Maui\Platforms\Android\google-services.json')

    Step 'Working out the version'
    if (-not $Version) {
        # app-version.sh counts the PRs squashed into main; their "minor" / "major" labels need GitHub (gh CLI).
        $gh = Get-Command gh -ErrorAction SilentlyContinue
        if ($gh -and -not $env:GH_TOKEN) {
            $env:GH_TOKEN = (& gh auth token 2>$null)
        }
        if ($env:GH_TOKEN) {
            $remote = (& git -C $repo remote get-url origin)
            if ($remote -match 'github\.com[:/](.+?)(\.git)?$') { $env:GITHUB_REPOSITORY = $Matches[1] }
        }
        if (-not $env:GH_TOKEN) {
            Write-Host 'Note: gh CLI not signed in, so PR labels are not read: a PR labelled "minor" counts as a patch. Pass -Version to set it.' -ForegroundColor Yellow
        }
        Push-Location $work
        try { $Version = (& $bash .github/scripts/app-version.sh | Out-String).Trim() } finally { Pop-Location }
        if ($LASTEXITCODE -ne 0 -or -not $Version) { Fail 'app-version.sh failed. Pass -Version major.minor.patch.' }
    }
    $label = & dotnet msbuild (Join-Path $work 'SideGigPh.Contracts\SideGigPh.Contracts.csproj') -getProperty:AppVersionLabel "-p:BuildNumber=$BuildNumber" "-p:AppVersionNumber=$Version" |
        Where-Object { $_ -match '^\s*\d+\.\d+\.\d+' } | Select-Object -Last 1
    if ($label) { $label = $label.Trim() }
    if (-not $label) { Fail 'No AppVersionLabel (Directory.Build.props).' }
    Write-Host "Version: $label (versionCode $BuildNumber)"

    Step 'Building the signed bundle (takes several minutes)'
    $env:SIDEGIGPH_KEYSTORE_PASS = $storePass
    $env:SIDEGIGPH_KEY_PASS = $keyPass
    $output = Join-Path $work 'output\android'
    Invoke-Native 'dotnet publish' {
        dotnet publish (Join-Path $work 'SideGigPh.Maui\SideGigPh.Maui.csproj') -f net10.0-android -c Release `
            -p:AndroidPackageFormat=aab "-p:BuildNumber=$BuildNumber" "-p:AppVersionNumber=$Version" -p:AppChannel=prod `
            "-p:ReleaseKeystore=$Keystore" "-p:ReleaseKeyAlias=$KeyAlias" `
            -p:FirebaseAnalytics=false -p:FirebaseExperiments=false -o $output
    }

    Step 'Checking the signing key'
    $signed = Get-ChildItem $output -Filter '*-Signed.aab' | Select-Object -First 1
    if (-not $signed) { Fail "No signed .aab in $output" }
    $sha1 = (& $keytool -printcert -jarfile $signed.FullName | Select-String 'SHA1:' | Select-Object -First 1).Line.Trim().Split(' ', [StringSplitOptions]::RemoveEmptyEntries)[-1]
    if ($sha1 -ne $UploadKeySha1) { Fail "The bundle is signed with $sha1, not the upload key $UploadKeySha1. Wrong keystore or alias?" }
    Write-Host "Signed with the upload key ($sha1)" -ForegroundColor Green

    $mapping = Get-ChildItem (Join-Path $work 'SideGigPh.Maui\bin\Release') -Recurse -Filter 'mapping.txt' |
        Where-Object { $_.FullName -match 'android' } | Select-Object -First 1
    if (-not $mapping) { Fail "No R8 mapping.txt: R8 didn't run (AndroidLinkTool in the csproj)." }

    $name = "tarbaho-$label-build$BuildNumber-prod"
    $aab = Join-Path $outDir "$name.aab"
    $map = Join-Path $outDir "$name-r8-mapping.txt"
    Copy-Item $signed.FullName $aab -Force
    Copy-Item $mapping.FullName $map -Force
    Set-Content -Path $lastNumberFile -Value $BuildNumber

    Step 'Done'
    Write-Host "Bundle:  $aab" -ForegroundColor Green
    Write-Host "Mapping: $map"
    Write-Host "Commit $($commit.Substring(0, 7)), version $label, versionCode $BuildNumber."
    Write-Host 'Upload both in the Play Console (docs/play-store.md > Each new release).'
}
finally {
    if (-not $hadStorePass) { Remove-Item Env:SIDEGIGPH_KEYSTORE_PASS -ErrorAction SilentlyContinue }
    if (-not $hadKeyPass) { Remove-Item Env:SIDEGIGPH_KEY_PASS -ErrorAction SilentlyContinue }
    $storePass = $null; $keyPass = $null
    if (-not $hadGhToken) { Remove-Item Env:GH_TOKEN -ErrorAction SilentlyContinue }
    if (Test-Path $work) {
        & git -C $repo worktree remove --force $work 2>$null
        if (Test-Path $work) { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue }
        & git -C $repo worktree prune
    }
}
