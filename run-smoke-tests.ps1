<#
run-smoke-tests.ps1: runs the WinIsland smoke tests with one command.

What it does, in order:
  1. Gets the setup to test: a file on this PC (-Setup), a release from the public
     winisland repository (-Version), or nothing (-SkipInstall, tests what is installed).
  2. Notes whether WinIsland runs now, so the tests can start it again at the end.
  3. Runs the setup silently. It closes WinIsland, updates it in place and keeps the settings
     (WinIsland PRD FR-44). Uninstalling is never done: it would remove the instant
     notifications package.
  4. Runs the tests. They back up WinIsland's settings first and put them back at the end.
  5. Writes a short summary next to the full results in results\<date-time>\.

The tests use the mouse and keyboard for a few minutes. Do not touch the PC while they run,
and do not run them during a game.

Examples:
  .\run-smoke-tests.ps1 -SkipInstall
  .\run-smoke-tests.ps1 -Setup "D:\Downloads\WinIsland-v1.8.0-beta.1-setup.exe"
  .\run-smoke-tests.ps1 -Version v1.8.0-beta.1
#>

[CmdletBinding(DefaultParameterSetName = "Installed")]
param(
    [Parameter(Mandatory, ParameterSetName = "File")]
    [string] $Setup,

    [Parameter(Mandatory, ParameterSetName = "Release")]
    [string] $Version,

    [Parameter(ParameterSetName = "Installed")]
    [switch] $SkipInstall
)

$ErrorActionPreference = "Stop"

# The only download is the setup, from the public winisland releases; WinIsland itself stays offline.
$ReleasesApi = "https://api.github.com/repos/ardhana2819/winisland/releases/tags"
$RepoRoot = $PSScriptRoot
$TestProject = Join-Path $RepoRoot "tests\WinIsland.SmokeTests\WinIsland.SmokeTests.csproj"
$ResultsFolder = Join-Path $RepoRoot ("results\" + (Get-Date -Format "yyyyMMdd-HHmmss"))
New-Item -ItemType Directory -Force -Path $ResultsFolder | Out-Null

function Get-ReleaseSetup([string] $Tag) {
    Write-Host "Looking up WinIsland $Tag in ardhana2819/winisland..."
    try {
        $release = Invoke-RestMethod -Uri "$ReleasesApi/$Tag" -Headers @{ "User-Agent" = "winisland-automation" }
    }
    catch {
        throw "Release '$Tag' was not found in ardhana2819/winisland. Check the tag, for example v1.8.0-beta.1. ($($_.Exception.Message))"
    }

    $asset = $release.assets | Where-Object { $_.name -like "*-setup.exe" } | Select-Object -First 1
    if ($null -eq $asset) {
        throw "Release '$Tag' has no setup.exe."
    }

    $target = Join-Path $ResultsFolder $asset.name
    Write-Host "Downloading $($asset.name)..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $target -Headers @{ "User-Agent" = "winisland-automation" }
    return $target
}

function Install-WinIsland([string] $SetupPath) {
    if (-not (Test-Path -LiteralPath $SetupPath)) {
        throw "Setup not found: $SetupPath"
    }

    $log = Join-Path $ResultsFolder "install.log"
    Write-Host "Installing $(Split-Path -Leaf $SetupPath) silently..."
    $arguments = @("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/LOG=`"$log`"")
    $process = Start-Process -FilePath $SetupPath -ArgumentList $arguments -Wait -PassThru
    Write-Host "Setup finished with exit code $($process.ExitCode)."
    return $process.ExitCode
}

function Write-Summary([string] $TrxPath, [string] $SummaryPath) {
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        Set-Content -LiteralPath $SummaryPath -Encoding utf8 -Value "# Smoke test run`n`nNo results: the tests did not run. See the console output."
        return
    }

    [xml] $trx = Get-Content -LiteralPath $TrxPath -Raw
    $lines = @("# Smoke test run, $(Get-Date -Format 'd MMM yyyy HH:mm')", "")
    $lines += "WinIsland under test: $installedVersion", ""
    $lines += "| Test | Result | Why it failed |", "|---|---|---|"
    foreach ($result in ($trx.TestRun.Results.UnitTestResult | Sort-Object testName)) {
        $message = ""
        if ($result.outcome -ne "Passed" -and $null -ne $result.Output.ErrorInfo) {
            $message = ($result.Output.ErrorInfo.Message -replace "\r?\n", " ").Trim()
        }
        $lines += "| $($result.testName) | $($result.outcome) | $message |"
    }

    $counters = $trx.TestRun.ResultSummary.Counters
    $lines += "", "Passed $($counters.passed) of $($counters.total), failed $($counters.failed)."
    Set-Content -LiteralPath $SummaryPath -Encoding utf8 -Value $lines
}

$wasRunning = $null -ne (Get-Process -Name "WinIsland.App" -ErrorAction SilentlyContinue)
$env:WINISLAND_WAS_RUNNING = if ($wasRunning) { "1" } else { "0" }
Remove-Item Env:WINISLAND_SETUP_EXIT_CODE -ErrorAction SilentlyContinue

switch ($PSCmdlet.ParameterSetName) {
    "File" { $env:WINISLAND_SETUP_EXIT_CODE = Install-WinIsland (Resolve-Path -LiteralPath $Setup).Path }
    "Release" { $env:WINISLAND_SETUP_EXIT_CODE = Install-WinIsland (Get-ReleaseSetup $Version) }
    default { Write-Host "Skipping the install: testing the WinIsland already installed." }
}

$exe = Join-Path $env:LOCALAPPDATA "Programs\WinIsland\WinIsland.App.exe"
$installedVersion = if (Test-Path -LiteralPath $exe) { (Get-Item -LiteralPath $exe).VersionInfo.ProductVersion } else { "not installed" }

Write-Host ""
Write-Host "The tests start in 5 seconds and use the mouse and keyboard. Please do not touch the PC." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# dotnet test writes failed tests to the error stream. Windows PowerShell 5.1 turns that into a
# stopping error when the output is redirected, which would skip the summary, so it may not stop here.
$ErrorActionPreference = "Continue"
dotnet test $TestProject -c Release --results-directory $ResultsFolder --logger "trx;LogFileName=smoke-tests.trx" --logger "console;verbosity=normal"
$testExitCode = $LASTEXITCODE
$ErrorActionPreference = "Stop"

$summary = Join-Path $ResultsFolder "summary.md"
Write-Summary (Join-Path $ResultsFolder "smoke-tests.trx") $summary
Write-Host ""
Write-Host "Summary: $summary"
exit $testExitCode
