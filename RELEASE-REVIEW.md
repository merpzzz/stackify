# Release review: 1.0.0

Initial public release, reviewed on Windows 11 25H2 x64 (build 26200).

- Built from the source in this repository using the Windows .NET Framework compiler.
- Fresh defaults contain no personal app list, paths, screenshots, account tokens, or credentials.
- Explicit asInvoker manifest: no requested administrator elevation.
- No network listener, telemetry, remote commands, automatic downloads, or updater.
- Release ZIP includes only the app, its required icon if any, documentation, and SHA-256 hashes. No debug symbols, caches, settings, backups, or development tools.
- Source and binaries scanned for the original machine's username, private path prefixes and common credential patterns.
- Tests passed: empty configuration, stable stack IDs, name validation, all-or-nothing app moves, local executable validation, atomic save/backup, corrupt settings preservation, and native multi-selection.
- Disposable-window tests passed: launch missing apps, avoid duplicate launches, close only selected executable paths, preserve other apps with the same filename, and respect cancelled close requests.
- Existing taskbar grouping code retained; generated icons depend on Windows' rendering behavior and other taskbar mods. No claim of compatibility with every Windows build or taskbar replacement.

This review is not an independent audit or a guarantee against vulnerabilities. Builds are unsigned. See SECURITY.md for reporting and limitations.
