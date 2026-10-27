# Privacy

Stackify operates locally and has no network functionality or telemetry.
It enumerates running processes and visible windows to identify the applications you select. It does not read window contents, record keystrokes, or capture screenshots.

Stack names, executable paths, preferences, generated icon collages, and local error/status files are stored in %LOCALAPPDATA%\Stackify. Executable paths can contain your Windows username. Generated shortcuts also contain local paths. These files are not encrypted and are never uploaded by Stackify. Do not share this folder or personal shortcuts publicly.

Stackify launches only local .exe paths selected by the user, without shell commands or extra command-line arguments. It does not verify that a selected executable is trustworthy. Closing a stack sends normal close requests; it never force-kills an app.

The public source and release archives contain no saved stacks, personal paths, access tokens, or screenshots of a real desktop. Uninstalling the executable does not delete your saved stacks. Delete the per-user data folder separately if you want to erase it.
