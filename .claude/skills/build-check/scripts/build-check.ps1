<#
.SYNOPSIS
  Compile-check SideGigPh.Maui (and the MobileAppTheme skeleton it references) without touching
  the folders Visual Studio builds in.

.DESCRIPTION
  Copies SideGigPh.Maui and MobileAppTheme (without bin/obj) into -WorkDir, maps that folder to a free drive
  letter with `subst` (Android's aapt2 fails on long Windows paths, error APT2098/APT2261),
  runs `dotnet build`, prints only errors and the warnings that matter, then unmaps and
  deletes the copy. Exit code 0 = build succeeded.

.EXAMPLE
  pwsh .claude/skills/build-check/scripts/build-check.ps1 -WorkDir "$env:TEMP\sgph-check"
#>
param(
    # Where the throwaway copy goes. Pass the session scratchpad. Never the repo itself.
    [string]$WorkDir = (Join-Path ([IO.Path]::GetTempPath()) 'sgph-buildcheck'),
    [string]$Framework = 'net10.0-android',
    [string]$Configuration = 'Debug',
    # Keep the copy afterwards (faster incremental re-checks).
    [switch]$Keep
)

$ErrorActionPreference = 'Stop'
$repo = (git rev-parse --show-toplevel).Trim()
if ($WorkDir.TrimEnd('\','/') -like "$repo*") { throw "WorkDir must be outside the repo, so the copy can't collide with Visual Studio's bin/obj." }

New-Item -ItemType Directory -Force $WorkDir | Out-Null
# The app plus every project it references (the MobileAppTheme skeleton and its Firebase adapter).
foreach ($project in 'SideGigPh.Maui', 'MobileAppTheme', 'MobileAppTheme.Firebase') {
    robocopy (Join-Path $repo $project) (Join-Path $WorkDir $project) /MIR /XD bin obj .vs /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy $project failed ($LASTEXITCODE)" }
}
foreach ($f in 'SideGigPh.slnx', 'Directory.Build.props', 'Directory.Packages.props', 'global.json', 'nuget.config', 'NuGet.config') {
    $p = Join-Path $repo $f
    if (Test-Path $p) { Copy-Item $p $WorkDir -Force }
}

$used = (Get-PSDrive -PSProvider FileSystem).Name
$letter = [char[]]('QRSTUVWXYZ') | Where-Object { $used -notcontains [string]$_ } | Select-Object -First 1
if (-not $letter) { throw 'No free drive letter for subst.' }
$drive = "$($letter):"

subst $drive $WorkDir
try {
    Push-Location "$drive\SideGigPh.Maui"
    $output = dotnet build -f $Framework -c $Configuration -nologo -v:q 2>&1
    $code = $LASTEXITCODE
    Pop-Location

    $interesting = $output | ForEach-Object { "$_" } |
        Where-Object { $_ -match ': error |: warning (RZ|BL|MUD|CS8|CS0618|CS0168|CS0219)' } |
        ForEach-Object { ($_ -replace '\s*\[[^\]]*\.csproj[^\]]*\]\s*$', '') -replace [regex]::Escape("$drive\SideGigPh.Maui\"), '' -replace [regex]::Escape("$drive\"), '' } |
        Sort-Object -Unique
    $interesting | Select-Object -First 60 | ForEach-Object { Write-Output $_ }
    if (@($interesting).Count -gt 60) { Write-Output "... $(@($interesting).Count - 60) more" }

    if ($code -eq 0) { Write-Output "BUILD OK ($Framework, $Configuration)" }
    else { Write-Output "BUILD FAILED ($Framework, exit $code)"; $output | Select-Object -Last 5 | ForEach-Object { Write-Output "$_" } }
}
finally {
    subst $drive /D
    if (-not $Keep) { Remove-Item -Recurse -Force $WorkDir -ErrorAction SilentlyContinue }
}
exit $code
