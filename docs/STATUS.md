# Photo Organiser — Implementation Status

## Phases

| Phase | Description | Status |
|---|---|---|
| 1 | Project scaffold & solution structure | ✅ Done |
| 2 | Main form UI layout | ✅ Done |
| 3 | File scanning service | ✅ Done |
| 4 | Pre-copy analysis & confirmation | ✅ Done |
| 5 | Conflict resolution dialog | ✅ Done |
| 6 | Copy engine | ✅ Done |
| 7 | Post-copy summary | ✅ Done |
| 8 | Polish & edge cases | ✅ Done |
| 9 | Testing checklist | ⬜ Not started |

## Phase 1 — Done

- `PhotoOrganiser.slnx` + `PhotoOrganiser/PhotoOrganiser.csproj` (net10.0-windows, UseWindowsForms)
- `MetadataExtractor` 2.9.3 added
- `Helpers/FileTypes.cs` — all supported image + video extensions
- `Forms/MainForm.cs` + `MainForm.Designer.cs`
- `Services/`, `Models/` folders ready
- `PhotoOrganiser.Tests/PhotoOrganiser.Tests.csproj` (xUnit, net10.0-windows) added to solution
- `FileTypesTests.cs` — 46 tests: all image/video extensions, case-insensitivity, unsupported types
- `FileScannerTests.cs` + `CopyEngineTests.cs` — stubs (filled in Phases 3 & 6)
- Builds clean: 0 warnings, 0 errors
- `dotnet test` — 46 passed, 0 failed
- **Note:** targets net10.0-windows (only runtime installed on dev machine; plan specifies net8.0)

## Phase 2 — Done

- `TableLayoutPanel` layout: source/dest pickers → summary label → log → progress → buttons
- Source + dest rows: `Label` + read-only `TextBox` + "Browse…" `Button` (uses `FolderBrowserDialog`)
- Analyse button (left-aligned); Start Copy + Cancel buttons (right-aligned, both disabled by default)
- `ProgressBar` + progress status `Label` in dedicated row
- `RichTextBox` log area (Consolas 9pt, read-only, expands to fill)
- Form 700×550 default, 540×400 minimum, fully resizable
- All buttons wired to stub handlers; Start Copy disabled until Phase 4 enables it
- Builds clean: 0 warnings, 0 errors

## Phase 3 — Done

- `Models/FileCandidate.cs` — `SourcePath`, `FileName`, `OrganiseDate`, `DateSource`, `DestinationFolder`, `DestinationPath`, `ConflictExists`
- `Models/DateSource.cs` — enum: `Exif`, `FileCreation`, `Undated`
- `Models/ScanResult.cs` — `ToCopy`, `ToSkip`, `Undated` lists
- `Services/IFileScanner.cs` + `Services/FileScanner.cs`
- Date resolution order: EXIF `DateTimeOriginal` → EXIF `DateTime` → QuickTime/MP4 atom → `File.GetCreationTime()` → `Undated` (dates < 1900 treated as invalid)
- Destination path: `<dest>\YYYY\MM MonthName\<file>` or `<dest>\Undated\<file>`
- Conflict detection: same filename + same size → `ToSkip`; same filename + different size → `ConflictExists = true`
- `FileScannerTests.cs` — 8 tests all passing
- `dotnet test` — 54 passed, 0 failed

## Phase 5 — Done

- `Models/ConflictAction.cs` — enum: `Skip`, `Rename`
- `Models/ConflictResolution.cs` — record: `(FileCandidate Candidate, ConflictAction Action)`
- `Models/FileCandidate.cs` — changed `class` → `record` to support `with` expressions
- `Helpers/FileNameHelper.cs` — `GetUniqueDestinationPath(destPath, exists)`: appends `_1`, `_2`, … until no collision
- `Forms/ConflictResolutionForm.cs` — modal dialog with `DataGridView` (File Name, Source/Dest paths, sizes, Action combobox); "Apply to all" dropdown; OK/Cancel
- `MainForm.cs` — auto-shows dialog after Analyse when conflicts exist; stores `_conflictResolutions`; "Review Conflicts…" button re-opens dialog; Start Copy enabled only when all conflicts resolved
- `MainForm.Designer.cs` — added `_btnReviewConflicts` (left-aligned, disabled until conflicts present)
- `FileNameHelperTests.cs` — 6 tests: no collision, single collision, multiple collisions, extension preservation, no-extension, nested path
- `dotnet test` — 60 passed, 0 failed

## Phase 4 — Done

- `MainForm.BtnAnalyse_Click` wired as `async void`
- Guards: empty paths, source == destination, destination inside source (blocks scan with `MessageBox`)
- Runs `FileScanner.ScanAsync` on background thread via `Task.Run`; UI stays responsive
- Summary label: `Found N files. → X to copy | Y already exist | Z undated | W conflicts need review`
- Log populated before any copy: `[SKIP]` (gray), `[UNDATED]` (orange), `[CONFLICT]` (red)
- Start Copy enabled only when actionable files exist (to-copy or conflicts); shows "Nothing to copy." otherwise
- Cancel button wired to `CancellationTokenSource` — cancels scan mid-flight
- `IFileScanner` injected via field; `MainForm` remains a thin coordinator
- Builds clean: 0 warnings, 0 errors; 54 tests still passing

## Phase 7 — Done

- Cancelled case: summary shows `Cancelled after N of M files.  N copied` using last progress report
- Error log: `copy_log.txt` written to dest root only when `Failed > 0`; timestamped, one `[ERROR]` line per failure
- Offer to open log: `MessageBox` Yes/No; opens with `Process.Start(UseShellExecute:true)`
- UI returns to fully usable state in all exit paths (complete / cancelled / exception) via `finally`
- `dotnet test` — 67 passed, 0 failed

## Phase 8 — Done

- Items 1 & 2 (src=dest, dest-in-src guards) — already implemented in Phase 4
- Item 3 (long path support) — `app.manifest` with `<longPathAware>true</longPathAware>`; `ApplicationManifest` wired in csproj
- Item 4 (preserve timestamps) — already implemented in Phase 6
- Item 5 (re-run safety) — inherent: `BtnAnalyse_Click` resets `_lastScan` and re-scans; previously copied files return as `ToSkip`
- Item 6 (empty source folder) — shows "No supported files found in the selected folder." when total=0
- Item 7 (access denied) — `FileScanner.EnumerateFilesSafe` does per-directory recursion, catches `UnauthorizedAccessException`, adds to `ScanResult.InaccessibleFolders`; MainForm logs `[ACCESS DENIED]` per folder
- Item 8 (settings persistence) — `Properties/Settings.cs` (`ApplicationSettingsBase`) stores `SourceFolder`/`DestinationFolder` in user.config; loaded on init, saved on Browse
- `dotnet test` — 69 passed, 0 failed (added `ScanAsync_EmptySourceFolder_ReturnsEmptyResult`, `ScanAsync_InaccessibleSubfolder_LoggedAndScanContinues`)

## Phase 6 — Done

- `Models/CopyProgress.cs` — record: `(int Completed, int Total, string CurrentFile)`
- `Models/CopyResult.cs` — record: `(int Copied, int Skipped, int Failed, List<string> Errors)`
- `Services/ICopyEngine.cs` + `Services/CopyEngine.cs`
- Copy logic: `Directory.CreateDirectory` → `File.Copy(overwrite:false)` → preserve timestamps (`SetCreationTime` + `SetLastWriteTime`) → `IOException` logged and skipped, copy continues
- Cancellation honoured between files via `ct.ThrowIfCancellationRequested()`
- `MainForm.BtnStartCopy_Click` wired as `async void`; builds final file list (clean + resolved-as-rename conflicts); reports progress via `IProgress<CopyProgress>`; updates `ProgressBar`, status label, log (green OK / red ERROR / orange CANCELLED); summary label on completion
- `CopyEngineTests.cs` — 7 tests: copies file, creates dest dir, preserves timestamps, logs error and continues, honours cancellation, reports progress per file, never overwrites
- `dotnet test` — 67 passed, 0 failed
