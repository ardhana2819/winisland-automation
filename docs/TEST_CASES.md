# WinIsland test cases

This file lists every check for WinIsland in plain words: what is checked, why, the steps, and what should happen. It is written so anyone can follow it, also without reading code. It doubles as the checklist before a release.

- **Automatic** tests run with `run-smoke-tests.ps1` (see the README). The computer does the steps itself.
- **Manual** tests are done by Diva on her PC, because a computer cannot fake them (a real WhatsApp message, a game).
- The **PRD** column names the requirement in WinIsland's `docs/PRD.md` the test checks.

Last updated 10 Nov 2026.

## Before every automatic run

The script does this by itself; it is listed so you know what happens to your PC.

1. WinIsland's settings (`%USERPROFILE%\.winisland\settings.json`) and the "start with Windows" entry are copied aside.
2. Each test starts its own fresh WinIsland with the default settings (Medium size, Solid look, the default hotkeys).
3. After the last test, the settings and the entry are put back exactly as they were, and WinIsland is started again if it was running.

If a run is cut off (power cut, window closed), the copy stays as `settings.json.smoke-test-backup` next to the settings. The next run puts it back first.

## Status

| Batch | Tests | Status |
|---|---|---|
| 1 | ST-01 to ST-13: install, start, Settings, hotkeys, tray | Built |
| 2 | ST-14 to ST-18: music, with a fake music player | Planned |
| 3 | ST-19 to ST-24: notifications, with a test notification | Planned |

## Install and start

### ST-01 Installer puts WinIsland in place
- **Why:** the setup must install WinIsland for your account, with its shortcuts and its entry in Installed apps. PRD FR-44.
- **Steps:** the script runs the setup silently. The test then looks at the WinIsland folder, the Start menu and the list of installed apps.
- **Expected:** the setup finishes without an error. `WinIsland.App.exe` is in `%LOCALAPPDATA%\Programs\WinIsland`. The Start menu has "WinIsland" and "Update WinIsland". Installed apps lists WinIsland with the publisher "Diva" and the same version as the program file.
- **Note:** with `-SkipInstall` no setup runs, so the version in Installed apps is not compared. A test build copied into the folder by hand leaves the last setup's version there.
- **How:** automatic.

### ST-02 Island appears at the top center with the time
- **Why:** the island must sit at the top middle of the main screen, above every window, and show the clock. PRD FR-1, FR-2.
- **Steps:** start WinIsland.
- **Expected:** the island window is centered at the top edge, is "always on top", and shows the current time as hours:minutes (24 hours).
- **How:** automatic.

### ST-03 Clicking the island does not take focus
- **Why:** in a game, clicking the island must not take the keyboard away from the game. PRD FR-3.
- **Steps:** open a plain test window (it stands in for the game) and put it in front. Click the island.
- **Expected:** the test window is still the window in front after the click.
- **How:** automatic. The real check in a game is manual (M-03).

### ST-04 Starting WinIsland twice gives one island
- **Why:** a second copy would make a second island and fight over the hotkeys. PRD FR-6b.
- **Steps:** with WinIsland running, start it again.
- **Expected:** the second copy closes by itself; one WinIsland and one island stay.
- **How:** automatic.

## Settings

### ST-05 Settings opens from the tray and shows the version
- **Why:** Settings is opened from the tray, and shows which version is installed. PRD FR-6, FR-46.
- **Steps:** right-click the WinIsland icon in the tray (also when it is hidden behind the ^), choose Settings.
- **Expected:** the "WinIsland settings" window opens and shows "Version" with the installed version, for example "Version 1.8.0-beta.1".
- **How:** automatic.

### ST-06 Island size scales the island and is kept
- **Why:** the island size choice must make the island smaller or bigger at once, and stay after a restart. PRD FR-32.
- **Steps:** in Settings choose Small, Medium, Large and Extra large one after the other. Then restart WinIsland.
- **Expected:** compared with Medium, the island is about 85, 100, 115 and 130 percent of its size (5 percent margin for whole pixels). `settings.json` saves each choice. After the restart the island is still Extra large.
- **How:** automatic.

### ST-07 Look switches between Solid and Glass
- **Why:** the look must switch between Solid and Glass without an error. PRD FR-37.
- **Steps:** in Settings check the Look list, choose "Glass (beta)", then "Solid" again.
- **Expected:** the list offers exactly "Solid" and "Glass (beta)", Solid is the default, each choice is saved, WinIsland keeps running and Settings shows no error.
- **Note:** this does not check how Glass looks; that needs your eyes (M-06).
- **How:** automatic.

### ST-08 Launch with Windows turns start with Windows on and off
- **Why:** the "Launch with Windows" switch must make WinIsland start with Windows, or stop that. PRD FR-20.
- **Steps:** in Settings press the "Launch with Windows" switch with the keyboard (Space), then press it again.
- **Expected:** it depends on how WinIsland is set up:
  - **With instant notifications** (package identity, set up with `Install-WinIslandIdentity.ps1`): switching on turns on WinIsland's Windows startup task, the one shown in Windows Settings > Apps > Startup; switching off turns it off. The registry's Run list has no WinIsland entry at any time.
  - **Without them:** switching on adds the entry `WinIsland` with the path of WinIsland.App.exe under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`; switching off removes it.
- **Before the test:** with instant notifications, the identity package must be version 1.0.3.0 or newer, because older ones have no startup task. The test says so if it is older.
- **History:** on 10 Nov 2026 this test found that switching off left the Run entry when instant notifications were set up, because Windows gives such an app a private copy of the Run list. WinIsland PR #57 moved it to the startup task.
- **How:** automatic. Windows starting WinIsland after a real sign-in stays a manual check (M-10).

### ST-09 Update button is there and enabled
- **Why:** the Update button in Settings starts the update script. PRD FR-46.
- **Steps:** open Settings.
- **Expected:** the update script is installed with WinIsland, and the Update button is there and can be pressed. The test does not press it, so nothing is updated.
- **How:** automatic.

## Hotkeys and tray

### ST-10 Ctrl+Alt+H hides and shows the island
- **Why:** the hide hotkey must hide the island and bring it back. PRD FR-5.
- **Steps:** press Ctrl+Alt+H, then press it again.
- **Expected:** the first press hides the island (it shrinks away), the second brings it back at its full size.
- **How:** automatic.

### ST-11 Ctrl+Alt+I opens the music page and closes it
- **Why:** the expand hotkey must open the island on the music page and close it again. PRD FR-2, FR-5, FR-21.
- **Steps:** press Ctrl+Alt+I, then press it again.
- **Expected:** the first press opens the island on the music page, which shows a title ("Not playing" without music, otherwise the song). The second press closes it.
- **How:** automatic. ST-18 checks the "Not playing" view exactly, with the fake music player closed.

### ST-12 Tray menu hides, pauses and resumes the island
- **Why:** the tray menu must offer its four items, and each must do what it says. PRD FR-6.
- **Steps:** right-click the tray icon and read the menu. Choose "Hide island", then "Show island". Choose "Pause for 1 hour", then "Resume".
- **Expected:** the menu reads "Pause for 1 hour", "Hide island (Ctrl+Alt+H)", "Settings", "Quit". "Hide island" hides the island and turns into "Show island (Ctrl+Alt+H)". "Pause for 1 hour" hides the island and turns into "Resume". "Resume" brings the island back, and the menu shows "Pause for 1 hour" again.
- **How:** automatic.

### ST-13 A taken hotkey shows a clear conflict message
- **Why:** when another program already uses a hotkey, WinIsland must say so instead of quietly not working. PRD FR-5.
- **Steps:** the test takes Ctrl+Alt+I first, like another program would, then starts WinIsland and opens Settings.
- **Expected:** the island shows a conflict banner that names Ctrl+Alt+I, and Settings shows the same problem in amber.
- **How:** automatic.

## Music (batch 2, planned)

These tests use a small test program, `FakeMusicPlayer`, that plays silence and tells Windows it plays "WinIsland Test Song" by "Automation", so they do not need Spotify. Diva's own music should be paused during the run.

### ST-14 The playing song shows on the island
- **Why:** WinIsland must show what any music app tells Windows. PRD FR-7, FR-41.
- **Steps:** start the fake music player.
- **Expected:** the compact island shows "WinIsland Test Song".
- **How:** automatic (planned).

### ST-15 Music buttons reach the music app
- **Why:** play/pause, next and previous on the island must control the music app. PRD FR-8.
- **Steps:** open the island, press Pause, Next, Previous.
- **Expected:** the music page shows the title and the artist, and the fake player reports each button press.
- **How:** automatic (planned).

### ST-16 Music keys work while the island is open
- **Why:** Space, Ctrl+Right and Ctrl+Left control the music while the island is open. PRD FR-8.
- **Steps:** open the island, press Space, Ctrl+Right, Ctrl+Left.
- **Expected:** the fake player reports play/pause, next and previous.
- **How:** automatic (planned).

### ST-17 The song's time moves on
- **Why:** the progress of the song must keep moving. PRD FR-9.
- **Steps:** open the island while the fake song plays and read the time twice, a few seconds apart.
- **Expected:** the elapsed time has gone up.
- **How:** automatic (planned).

### ST-18 Closing the music app shows "Not playing"
- **Why:** when the music app closes, the island must show the empty view without errors. PRD FR-10, FR-24.
- **Steps:** close the fake music player, then open the island.
- **Expected:** the music page shows "Not playing", and WinIsland keeps running.
- **How:** automatic (planned).

## Notifications (batch 3, planned)

These tests send a made-up notification from a small test program, `TestToastSender`. They need WinIsland's notification permission, which Windows asks you for once; without it they are skipped with that reason. Whether Windows lets the test program send a notification that WinIsland accepts as "another Windows app" is still to be checked at the start of batch 3.

### ST-19 A test notification shows as a banner
- **Why:** WinIsland must show notifications from allowed apps, and only those. PRD FR-13, FR-14, FR-42.
- **Steps:** with "Show other Windows apps" on, send a test notification. Turn the switch off and send another.
- **Expected:** the first shows as a banner with its sender and text; the second shows nothing.
- **How:** automatic (planned).

### ST-20 "Hide message text" hides the text
- **Why:** with this switch on, the message text must never show. PRD FR-17.
- **Steps:** turn on "Hide message text", send a test notification.
- **Expected:** the banner shows the sender, but not the message text.
- **How:** automatic (planned).

### ST-21 The banner goes away by itself
- **Why:** the banner must hide after the notification duration. PRD FR-16.
- **Steps:** send a test notification and wait.
- **Expected:** the banner is gone after the chosen duration (6 seconds by default).
- **How:** automatic (planned).

### ST-22 The notifications page lists notifications
- **Why:** the island's second page shows the last notifications, or says there are none. PRD FR-22.
- **Steps:** open the notifications page before and after a test notification.
- **Expected:** before: "No notifications". After: the test notification is listed.
- **How:** automatic (planned).

### ST-23 A notification cleared in Windows leaves the island
- **Why:** when Windows removes a notification (for example you read it), it must leave the island too. PRD FR-15.
- **Steps:** send a test notification, then clear it from Windows.
- **Expected:** it disappears from the notifications page.
- **How:** automatic (planned).

### ST-24 Message text is never written to disk
- **Why:** message text stays in memory only. PRD FR-18.
- **Steps:** after the notification tests, search WinIsland's folder `%USERPROFILE%\.winisland` (settings and log) for the unique made-up text the tests sent.
- **Expected:** the text is found nowhere.
- **How:** automatic (planned).

## Manual tests

Done by Diva on her PC, before a release or when the feature changed.

| ID | What | Steps | Expected | PRD |
|---|---|---|---|---|
| M-01 | Real WhatsApp and Discord messages | Send yourself a message in WhatsApp Web and in Discord. | A banner with the app's logo, sender and text; it shows on the notifications page and stacks per chat. | FR-13 to FR-15, FR-28 to FR-30, FR-35 |
| M-02 | WhatsApp call | Get a voice call and a video call in WhatsApp Web. | The call banner with the right symbol stays while it rings and leaves when it stops. | FR-38 |
| M-03 | In a game | Play a borderless game, use the hotkeys, click the island, get a message. | The game keeps the keyboard; the island stays on top; the music keys belong to the game when the island is closed. | FR-3, FR-8 |
| M-04 | Volume | Open the island while music plays, use the speaker button, the slider and Ctrl+Up / Ctrl+Down. | The music app gets louder or quieter; the game and Windows do not. | FR-39 |
| M-05 | Camera and microphone dots | Join a Discord voice call; use the camera if you have one. | An orange dot for the microphone, a green one for the camera. | FR-40 |
| M-06 | Look and motion | Compare the island with the prototype: Solid and Glass, over a light and a dark game, every animation. | Looks and moves like `docs/design/prototype.html`. | FR-4, FR-25, FR-37, FR-45 |
| M-07 | Color emoji | Get a message and play a song with emoji in the name. | Emoji show in color. | FR-47 |
| M-08 | Update | Press Update in Settings when a newer release exists. | WinIsland updates and starts again with the new version. | FR-46 |
| M-09 | Uninstall | Only when the installer changed: uninstall from Installed apps. Afterwards install again and run `Install-WinIslandIdentity.ps1` for instant notifications. | Files, shortcuts and the start with Windows entry are gone; `.winisland` stays. | FR-44 |
| M-10 | Start with Windows | Turn "Launch with Windows" on, sign out and sign in again. Then turn it off and do the same. | On: WinIsland starts by itself, once (one island). Off: it does not start. | FR-20 |

Uninstall is manual on purpose: it also removes the instant notifications package, and installing again does not bring it back.
