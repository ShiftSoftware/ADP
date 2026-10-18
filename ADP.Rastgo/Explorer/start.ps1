param(
    [ValidateRange(1, 65535)][int]$Port = 4179,
    [switch]$Background
)
$ErrorActionPreference = 'Stop'
$previousPort = $env:RASTGO_EXPLORER_PORT
$env:RASTGO_EXPLORER_PORT = $Port.ToString()
try {
    $serverPath = Join-Path $PSScriptRoot 'server.mjs'
    if ($Background) {
        $logPrefix = Join-Path ([System.IO.Path]::GetTempPath()) "rastgo-explorer-$Port"
        $serverProcess = Start-Process -FilePath (Get-Command node -ErrorAction Stop).Source `
            -ArgumentList ('"' + $serverPath + '"') -WorkingDirectory $PSScriptRoot `
            -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput ($logPrefix + '.log') -RedirectStandardError ($logPrefix + '.error.log')
        [pscustomobject]@{ ProcessId = $serverProcess.Id; Url = "http://127.0.0.1:$Port/"; Log = $logPrefix + '.log' }
    } else {
        & node $serverPath
        if ($LASTEXITCODE -ne 0) { throw "Explorer exited with code $LASTEXITCODE" }
    }
} finally {
    if ($null -eq $previousPort) { Remove-Item Env:RASTGO_EXPLORER_PORT -ErrorAction SilentlyContinue }
    else { $env:RASTGO_EXPLORER_PORT = $previousPort }
}
