#Requires -Version 7.0
<#
.SYNOPSIS
    Endpoint-parity harness driver. TEMPORARY - deleted in Step 08.

.DESCRIPTION
    The operator interface for the endpoint-parity harness built in Step 00 of the ShiftEntity
    framework upgrade. All the logic lives in .NET (ADP.EndpointParity.Harness); this script is a
    driver, not the harness.

    Group selection is the PROJECT, not a test filter. That is the whole point of the six-project
    split: while any group is mid-migration - its `: Profile` classes deleted before their
    replacements are wired - only that group's project fails to build, and every other group stays
    runnable and re-verifiable.

.PARAMETER Verb
    capture  Write the goldens. Only legal on a tree whose behaviour is the reference.
    verify   Replay and diff against the committed goldens. Never writes a golden.
    summary  Print the gates for the last run and exit non-zero if any fails.
    accept   Record an intended change explicitly, with a reason.

.PARAMETER Group
    Menus | Darlastic | Surveys | ClaimableItems | WarrantyClaims

.PARAMETER Grant
    FullAccess (default) | Restricted

    Capture each group under BOTH. A restricted baseline captured after the code changes is not
    a baseline.

.EXAMPLE
    .\tools\parity.ps1 capture -Group Surveys -Grant FullAccess
    .\tools\parity.ps1 capture -Group Surveys -Grant Restricted
    .\tools\parity.ps1 summary -Group Surveys
    # ... do the upgrade ...
    .\tools\parity.ps1 verify  -Group Surveys
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet('capture', 'verify', 'summary', 'accept')]
    [string] $Verb,

    [Parameter(Mandatory)]
    [ValidateSet('Menus', 'Darlastic', 'Surveys', 'ClaimableItems', 'WarrantyClaims')]
    [string] $Group,

    [ValidateSet('FullAccess', 'Restricted')]
    [string] $Grant = 'FullAccess',

    # accept only
    [string] $Case,
    [string] $Reason,

    [switch] $SkipPrerequisiteCheck
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$parityRoot = Join-Path $repoRoot 'ADP.EndpointParity'
$project = Join-Path $parityRoot "ADP.EndpointParity.$Group"

function Write-Section([string] $text) {
    Write-Host ''
    Write-Host "== $text" -ForegroundColor Cyan
}

# ---------------------------------------------------------------------------------------------
# Prerequisites - CHECKED AND REPORTED, not assumed. A baseline captured against a database the
# harness did not control proves nothing, and the naive version of that failure (an empty DB,
# every case an empty list, every diff passing) is silent.
# ---------------------------------------------------------------------------------------------
function Test-Prerequisites {
    Write-Section 'Prerequisites'

    $sdk = (dotnet --version)
    Write-Host "  dotnet SDK           : $sdk"
    if ($sdk -notlike '10.*') {
        Write-Warning "  Expected a 10.x SDK. The baselines were captured on 10.0.400."
    }

    # SQL is required by EVERY group. Cosmos and the blob emulator are deliberately NOT:
    # the samples gate all Cosmos work on the connection string being configured, and the
    # parity host sets it empty so the whole replication + provisioning block is skipped.
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection `
            'Server=localhost\sqlexpress;Integrated Security=SSPI;TrustServerCertificate=True;Connect Timeout=5'
        $conn.Open()
        $conn.Close()
        Write-Host '  SQL Server / Express : reachable' -ForegroundColor Green
    }
    catch {
        Write-Host '  SQL Server / Express : NOT REACHABLE' -ForegroundColor Red
        throw "Every group needs SQL with integrated security. $($_.Exception.Message)"
    }

    Write-Host '  Cosmos               : not required (parity host runs with it unconfigured)'
    Write-Host '  Blob emulator        : not required (no blob endpoint is exercised)'
}

function Invoke-ParityRun([string] $mode) {
    Write-Section "$mode - $Group ($Grant)"

    $env:PARITY_MODE = $mode
    $env:PARITY_GROUP = $Group
    $env:PARITY_GRANT = $Grant
    $env:PARITY_ROOT = $parityRoot

    try {
        dotnet test $project --logger 'console;verbosity=normal'
        $exit = $LASTEXITCODE
    }
    finally {
        Remove-Item Env:PARITY_MODE, Env:PARITY_GROUP, Env:PARITY_GRANT, Env:PARITY_ROOT -ErrorAction SilentlyContinue
    }

    return $exit
}

switch ($Verb) {

    'capture' {
        if (-not $SkipPrerequisiteCheck) { Test-Prerequisites }

        $exit = Invoke-ParityRun 'capture'

        Write-Section 'Captured'
        Write-Host "  baselines: $(Join-Path $parityRoot "baselines/$($Group.ToLower())/$Grant")"
        Write-Host ''
        Write-Host '  REVIEW THE SUMMARY BEFORE COMMITTING.' -ForegroundColor Yellow
        Write-Host '  A near-empty or all-error baseline is the single most common way this whole'
        Write-Host '  exercise silently fails. In particular check:'
        Write-Host '    - CREATE 2xx is n/n            (a body that 4xxs never reaches the mapper)'
        Write-Host '    - catalogue routes covered n/n (an uncovered route is a gap, not a default)'
        Write-Host '    - hostile seed rows present    (a seed that cannot fail proves nothing)'
        Write-Host ''
        Write-Host '  Then commit the goldens in a commit that contains NO harness source changes.'
        exit $exit
    }

    'verify' {
        if (-not $SkipPrerequisiteCheck) { Test-Prerequisites }

        $exit = Invoke-ParityRun 'verify'

        $report = Join-Path $parityRoot "reports/$($Group.ToLower())/diff.md"
        Write-Section 'Verified'
        if (Test-Path $report) {
            Write-Host "  report: $report"
            Write-Host ''
            Write-Host '  Read EVERY diff. Each is either a bug you just introduced, or an intended'
            Write-Host '  change you record in the commit message and accept explicitly:'
            Write-Host "    .\tools\parity.ps1 accept -Group $Group -Case <case> -Reason `"<why>`""
            Write-Host ''
            Write-Host '  NEVER make a diff go away by re-running capture. That destroys the baseline' -ForegroundColor Yellow
            Write-Host '  and converts an unknown into a falsely-known.' -ForegroundColor Yellow
        }
        exit $exit
    }

    'summary' {
        $exit = Invoke-ParityRun 'summary'
        exit $exit
    }

    'accept' {
        if (-not $Case)   { throw 'accept requires -Case.' }
        if (-not $Reason) { throw 'accept requires -Reason. An accepted change without a recorded reason is an unexplained behaviour change.' }

        $acceptLog = Join-Path $parityRoot "baselines/$($Group.ToLower())/accepted.md"
        New-Item -ItemType Directory -Force -Path (Split-Path $acceptLog) | Out-Null

        $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        Add-Content -Path $acceptLog -Value "- [$stamp] $Group / $Grant / $Case`n  reason: $Reason"

        Write-Section 'Accepted'
        Write-Host "  recorded in $acceptLog"
        Write-Host '  Re-run capture for this case only, then commit the golden WITH the reason in'
        Write-Host '  the commit message. The golden diff in the PR is the whole control.'
    }
}
