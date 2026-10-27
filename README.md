# Stackify

Create named stacks of Windows apps, launch them together, close their windows together, and group their taskbar buttons.

## Features

- Multiple saved stacks and desktop shortcuts that remember their stack.
- Add running apps with Ctrl-click, Shift-click, and Ctrl+A, or browse for local .exe files.
- Launch apps that are not already running; request a normal close for all windows in a stack.
- Dark mode and a tray menu.
- Generate a combined icon from up to four running apps. Windows decides how taskbar group icons are displayed; appearance can vary by Windows build, monitor, and other taskbar mods.

## Run

Windows 11 x64 with .NET Framework 4.8 is the initial target. Extract the release ZIP into a stable folder and run Stackify.exe. Keep Stackify.ico beside it. No administrator access is needed. Create a stack, then choose its apps. No personal stacks are bundled.

Stackify opens the manager on first launch. Closing the manager leaves it in the notification area. Use the tray menu to exit. Enable Windows taskbar button combining for groups to appear as one button. Other taskbar grouping mods can override the result; compatibility is not guaranteed.

Apps running as administrator may be inaccessible. Apps that minimize to the tray or show a save prompt may remain running after a close request. Stackify never force-kills them. Only normal local executable paths are supported, not URLs, scripts, command strings, Store-app identifiers, or network executables.

## Shortcuts and startup

Choose **Desktop shortcut** for a selected stack. Double-clicking that shortcut launches the saved apps. The stack ID remains stable after renaming or editing.

Startup is opt-in: create a shortcut to Stackify.exe with the argument --background and place it in the folder opened by Win+R → shell:startup. Remove that shortcut to disable startup. No scheduled task or Windows service is installed.

Command line: --manage, --background, --launch <stack-id>, --create-shortcut <stack-id>, --stop. A launch command uses an already-saved stack; it cannot supply an arbitrary command to execute.

## Build and test

Run ./build.ps1 in PowerShell on Windows. It uses the .NET Framework compiler already provided by Windows and downloads no build dependencies. Output is in dist/. Run ./test.ps1 for isolated configuration, input-validation, persistence, and multi-selection checks.

## Windhawk

Stackify is a standalone .NET desktop application, not a Windhawk `.wh.cpp` mod. It should be distributed as a Windows application rather than submitted to the Windhawk mod catalog.

## Privacy and removal

See PRIVACY.md and SECURITY.md. Exit from the tray, remove any startup/desktop shortcuts you created, and delete the extracted application folder. Saved stacks remain in %LOCALAPPDATA%\Stackify until you delete that folder yourself. Your apps and documents are never removed.

MIT licensed. By merpzzz.
