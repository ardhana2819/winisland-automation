# CLAUDE.md

Instructions Claude Code loads at the start of every session in this repo.

## Project

winisland-automation holds the smoke tests for WinIsland, Diva's Dynamic Island style overlay for Windows 11. The tests drive the installed WinIsland from the outside through Windows UI Automation (the accessibility API), with FlaUI and xUnit, the way a person would: install, start, open Settings, press hotkeys, use the tray menu. They run on Diva's PC with one command, not in CI.

The app itself lives in the public repo `ardhana2819/winisland`. Its `docs/PRD.md` is the source of truth for what WinIsland should do. Every test names the PRD requirement it checks (FR-x). If a test and the PRD disagree, report it instead of changing the test to pass.

`docs/TEST_CASES.md` describes every test in plain English. Update it in the same pull request as the test.

## Engineering standards

The rules in WinIsland's `docs/ENGINEERING_STANDARDS.md` apply here too, in short:

- One responsibility per file. Every file starts with a short plain-English comment saying what it does and why it exists. Clear names, no clever tricks, comments explain why.
- Never weaken, skip or edit a test just to make it pass. A test that cannot run says why.
- Never say something passed unless it was actually run. Report failures as they are.
- Code, comments, docs, commit messages in English, without em dashes. Chat with Diva can be in Indonesian.

## Rules for the tests

- Tests only use what any user can see or do: UI Automation, the mouse and keyboard, files and registry values WinIsland writes. No reading WinIsland's memory and no hooks into it.
- Tests must leave Diva's PC as they found it: settings and the start with Windows entry are backed up before the run and restored after it, and WinIsland is started again if it was running.
- Never uninstall WinIsland in a test: uninstalling removes the instant notifications package, which a reinstall does not bring back (WinIsland `packaging/README.md`).
- Message text used in tests is made up. Never use real messages.
- The tests run one at a time (one xUnit collection), because only one WinIsland can run.

## Git workflow

Same as WinIsland: branch from an up-to-date `main` (`feat/`, `fix/`, `chore/`, `docs/`, `test/`), small Conventional Commits, build and format check before pushing, pull request into `main`, Diva reviews, squash merge only after she says so. Never push to `main`; the `.githooks/pre-push` hook blocks it (`git config core.hooksPath .githooks`).

## Commands

The repo path contains a space. Always quote it: `"D:\Claude Project\winisland-automation"`.

```powershell
dotnet format winisland-automation.slnx --verify-no-changes
dotnet build winisland-automation.slnx -c Release -warnaserror
.\run-smoke-tests.ps1 -SkipInstall        # tests the WinIsland already installed
```
