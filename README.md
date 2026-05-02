# Photo Organiser

Copies photos and videos from a source folder into a date-based folder structure:

```
Destination\
  2024\
    01 January\
      IMG_001.jpg
  Undated\
      IMG_no_date.jpg
```

Date is read from EXIF metadata, falling back to file creation time. Copy only — source files are never modified.

## Download

Get the latest `PhotoOrganiser.exe` from the [Releases](../../releases/latest) page. No installation required.

## Verify download (recommended)

Each release includes a `PhotoOrganiser.exe.sha256` checksum file. To verify the download is intact and untampered:

1. Download both `PhotoOrganiser.exe` and `PhotoOrganiser.exe.sha256`
2. Open PowerShell in the download folder and run:

```powershell
$expected = (Get-Content PhotoOrganiser.exe.sha256).Split()[0]
$actual   = (Get-FileHash PhotoOrganiser.exe -Algorithm SHA256).Hash
if ($expected -eq $actual) { "OK" } else { "MISMATCH — do not run this file" }
```

## Usage

1. Run `PhotoOrganiser.exe`
2. Select source folder (your camera card or photo dump)
3. Select destination folder
4. Click **Analyse** to preview what will be copied
5. Click **Start Copy**

## Requirements

- Windows 10 or later (x64)
- No .NET installation required — the app is self-contained
