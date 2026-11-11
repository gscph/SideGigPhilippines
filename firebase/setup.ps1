<#
.SYNOPSIS
    Creates SideGigPh's Firestore database and deploys its rules and indexes (docs/data-model.md).

.DESCRIPTION
    Firestore is schemaless: there is no CREATE TABLE. A collection appears when the first document
    is written to it. "Creating everything" therefore means:
      1. create the database once, in a region (permanent: it can't be moved later),
      2. deploy the security rules (who can read/write what, and which fields are allowed),
      3. deploy the composite indexes the app's queries need,
      4. deploy the Storage rules (photo bytes).
    Categories, jobs and towns stay in the app's ReferenceData code, so nothing is seeded.

    Needs the Firebase CLI (npm install -g firebase-tools) and `firebase login` once.
    The project must be on the Blaze plan for Storage.

.EXAMPLE
    ./setup.ps1 -ProjectId sidegigph-dev
    ./setup.ps1 -ProjectId sidegigph-dev -SkipDatabase      # rules + indexes only (redeploy)
    ./setup.ps1 -Emulator                                    # local Firestore/Storage/Auth, no cloud
#>
[CmdletBinding()]
param(
    [string] $ProjectId,

    # Closest Firestore region to the Philippines. Permanent: it can't be changed after creation.
    [string] $Location = 'asia-southeast1',

    [switch] $SkipDatabase,

    [switch] $Emulator
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command firebase -ErrorAction SilentlyContinue)) {
    throw 'Firebase CLI not found. Install it with:  npm install -g firebase-tools   then run:  firebase login'
}

if ($Emulator) {
    # Rules are compiled on start, so this is also the quickest way to check them for errors.
    # Emulator UI: http://localhost:4000
    firebase emulators:start --only 'auth,firestore,storage' --project 'demo-sidegigph'
    return
}

if (-not $ProjectId) {
    throw 'Pass -ProjectId (the Firebase project id, e.g. sidegigph-dev), or -Emulator for a local run.'
}

if (-not $SkipDatabase) {
    Write-Host "Creating the (default) Firestore database in $Location..."
    firebase firestore:databases:create '(default)' --location $Location --project $ProjectId
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'Database not created (it may already exist). Continuing with rules and indexes.'
    }
}

Write-Host 'Deploying Firestore rules + indexes and Storage rules...'
firebase deploy --only 'firestore:rules,firestore:indexes,storage' --project $ProjectId
if ($LASTEXITCODE -ne 0) {
    throw @'
Deploy failed. Common causes:
  - Storage isn't enabled yet: Firebase console > Storage > Get started (Blaze plan, same region as Firestore).
  - A rules compile error: the CLI prints the file, line and column.
'@
}

Write-Host 'Done. Indexes can take a few minutes to build: Firebase console > Firestore > Indexes.'
