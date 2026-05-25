# OpenKey WinUI (.NET 10)

This folder contains a WinUI 3 rewrite scaffold for the Windows OpenKey control panel.

## Scope

- Targets `net10.0-windows10.0.19041.0`.
- Uses Windows App SDK and an unpackaged WinUI desktop app.
- Reads and writes the same `HKCU\SOFTWARE\TuyenMai\OpenKey` settings as the existing Win32 project.
- Uses the original Win32 `OpenKey64.exe` engine built from this source tree for actual Vietnamese input.
- The WinUI app is the settings/control panel; settings changes are written to the original registry keys and the native engine receives a reload message without restarting.
- The original tray icon opens this WinUI control panel instead of the legacy Win32 dialog.

## Build

```powershell
dotnet build .\OpenKey.WinUI.sln -c Debug -p:Platform=x64
```
