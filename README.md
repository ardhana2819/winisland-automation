# winisland-automation

Smoke tests for [WinIsland](https://github.com/ardhana2819/WinIsland), Diva's Dynamic Island style overlay for Windows 11. The tests use WinIsland the way a person does: install it, start it, open Settings, press the hotkeys, use the tray menu. They tell in a few minutes whether a new release still works, without clicking through everything by hand.

Every test is described in plain words in [docs/TEST_CASES.md](docs/TEST_CASES.md), together with the checks that stay manual.

## How it works

- The tests find WinIsland's windows and buttons through **Windows UI Automation**, the accessibility interface screen readers use, with the free library [FlaUI](https://github.com/FlaUI/FlaUI). They move the real mouse and press real keys.
- They run with **xUnit**, the same test framework as WinIsland's own unit tests.
- They run **on your PC**, not on GitHub: GitHub's machines have no desktop session to show the island in, no tray, and no notification permission.
- WinIsland itself is not changed for the tests. It already names its buttons for screen readers, which is what the tests use.

## What you need

- Windows 11 with WinIsland installed, or a WinIsland setup to install.
- The .NET 10 SDK (`winget install Microsoft.DotNet.SDK.10`).

## Run the tests

Open PowerShell in this folder:

```powershell
cd "D:\Claude Project\winisland-automation"
```

Then one of:

```powershell
.\run-smoke-tests.ps1 -SkipInstall                                              # test the WinIsland already installed
.\run-smoke-tests.ps1 -Setup "D:\Downloads\WinIsland-v1.8.0-beta.1-setup.exe"   # install this setup, then test
.\run-smoke-tests.ps1 -Version v1.8.0-beta.1                                    # download that release, install, test
```

`-Version` downloads the setup from the public [WinIsland-releases](https://github.com/ardhana2819/WinIsland-releases) repository, so it needs no sign-in. This download is done by the script, not by WinIsland: WinIsland itself stays offline.

**During the run (about 2 minutes) do not touch the mouse or keyboard, and do not run it during a game.**

Afterwards the script prints where the results are: `results\<date-time>\summary.md` (a short table: each test, passed or failed, and why) and `smoke-tests.trx` (the full detail).

## What the tests do to your PC

- **Install:** with `-Setup` or `-Version` the setup runs silently. It closes WinIsland, updates it in place and keeps your settings, like a normal update.
- **Settings:** your settings and the start with Windows entry are backed up before the first test and put back after the last. With instant notifications set up, start with Windows is a Windows startup task that follows your settings when WinIsland starts, so if WinIsland was not running, it is started for a moment at the end to put the task back. If a run is cut off, the backup stays as `%USERPROFILE%\.winisland\settings.json.smoke-test-backup`, and the next run restores it first.
- **WinIsland:** each test starts a fresh WinIsland with the default settings. At the end WinIsland is started again if it was running before.
- **Never:** the tests never uninstall WinIsland (that would remove the instant notifications package), never press Update, and never use real messages.

## Folders

| Folder | What is in it |
|---|---|
| `run-smoke-tests.ps1` | The one command that runs everything. |
| `docs/TEST_CASES.md` | Every test in plain words, plus the manual checks. |
| `tests/WinIsland.SmokeTests/Tests` | The tests, one file per area (install, start, Settings, hotkeys, tray). |
| `tests/WinIsland.SmokeTests/Pages` | One file per part of WinIsland the tests use: the island, Settings, the tray menu. |
| `tests/WinIsland.SmokeTests/Infrastructure` | Helpers: start and stop WinIsland, back up settings, press hotkeys, wait for animations. |
| `results/` | Results of each run. Not kept in git. |

## For developers

```powershell
dotnet format winisland-automation.slnx --verify-no-changes
dotnet build winisland-automation.slnx -c Release -warnaserror
dotnet test tests\WinIsland.SmokeTests -c Release --filter "FullyQualifiedName~ST06"   # one test
```

Work on a branch and open a pull request into `main`; see `CLAUDE.md`. Enable the hook that blocks pushes to `main` once per clone: `git config core.hooksPath .githooks`.
