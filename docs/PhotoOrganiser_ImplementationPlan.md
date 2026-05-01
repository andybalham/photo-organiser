# Photo Organiser — Claude Code Implementation Plan

## Project Overview

A WinForms (.NET) desktop application that copies image and video files from a source folder into a destination folder organised by **Year > Month** subdirectories, using EXIF/embedded date metadata where available and falling back to the file's creation date.

---

## Key Decisions & Constraints

| Topic | Decision |
|---|---|
| UI framework | WinForms (.NET 8+) |
| File scope | All image & video types (see Phase 1 for list) |
| Subfolder scanning | Recursive |
| Date priority | EXIF/embedded → File creation date |
| No embedded/creation date | Place file in `Undated` folder |
| Same filename, same content | Skip silently |
| Same filename, different content | Prompt user at runtime (Skip / Rename) |
| Operation | Copy only (source files are never modified) |
| Folder naming | `YYYY > MM MonthName` (e.g. `2021 > 08 August`) |

---

## Phase 1 — Project Scaffold & Solution Structure

**Goal:** Create a working, runnable WinForms skeleton with the correct project layout.

### Tasks

1. Create a new .NET 8 WinForms solution:
   ```
   PhotoOrganiser/
   ├── PhotoOrganiser.sln
   └── PhotoOrganiser/
       ├── PhotoOrganiser.csproj
       ├── Program.cs
       ├── Forms/
       │   └── MainForm.cs / MainForm.Designer.cs
       ├── Services/
       ├── Models/
       └── Helpers/
   ```

2. Add NuGet packages:
   - `MetadataExtractor` — EXIF and metadata reading for images and videos
   - No other third-party dependencies required for Phase 1

3. Define supported file extensions as a constant set in a `FileTypes` static class:
   - **Images:** `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.tiff`, `.tif`, `.webp`, `.heic`, `.heif`, `.raw`, `.cr2`, `.cr3`, `.nef`, `.arw`, `.orf`, `.rw2`, `.dng`, `.pef`, `.srw`, `.raf`
   - **Videos:** `.mp4`, `.mov`, `.avi`, `.mkv`, `.wmv`, `.m4v`, `.3gp`, `.flv`, `.mpg`, `.mpeg`, `.mts`, `.m2ts`

4. Create `Program.cs` with standard WinForms entry point targeting .NET 8.

### Acceptance Criteria
- Solution builds without errors
- Application launches and shows an empty form
- `FileTypes` class exists and is unit-testable

---

## Phase 2 — Main Form UI Layout

**Goal:** Build the complete user interface. No logic yet — just the controls and layout.

### UI Controls Required

| Control | Purpose |
|---|---|
| `TextBox` (read-only) + `Button` | Source folder path picker |
| `TextBox` (read-only) + `Button` | Destination folder path picker |
| `Button` ("Analyse") | Scans source and reports what will be copied |
| `Label` | Summary label (e.g. "423 files found, 12 already exist and will be skipped") |
| `Button` ("Start Copy") | Begins the copy operation (disabled until Analyse completes) |
| `ProgressBar` | Shows copy progress (file count) |
| `Label` | Progress status label (e.g. "Copying 45 of 423...") |
| `RichTextBox` or `ListBox` | Scrollable log output for per-file status messages |
| `Button` ("Cancel") | Cancels an in-progress copy |

### Layout Notes
- Use a `TableLayoutPanel` or manual anchoring for a clean, resizable layout
- Group source/destination pickers at the top
- Place the log/output area in the centre (expandable)
- Place progress bar and status label just above the bottom action buttons
- Start Copy and Cancel buttons sit side-by-side at the bottom right

### Acceptance Criteria
- Form renders correctly at a sensible default size (e.g. 700 × 550)
- Form is resizable and controls reflow correctly
- All buttons are wired to stub event handlers (no logic yet)
- Start Copy button is disabled by default

---

## Phase 3 — File Scanning Service

**Goal:** Implement the logic that scans the source folder and builds a list of candidate files with resolved dates.

### Models

```csharp
// Models/FileCandidate.cs
public class FileCandidate
{
    public string SourcePath { get; init; }
    public string FileName { get; init; }
    public DateTime OrganiseDate { get; init; }   // resolved date used for folder placement
    public DateSource DateSource { get; init; }    // Exif, FileCreation, or Undated
    public string DestinationFolder { get; init; } // e.g. "2021\08 August"
    public string DestinationPath { get; init; }   // full destination file path
}

public enum DateSource { Exif, FileCreation, Undated }
```

### Services/FileScanner.cs

Implement `IFileScanner` with method:
```csharp
Task<ScanResult> ScanAsync(string sourceFolder, string destinationFolder, CancellationToken ct);
```

`ScanResult` contains:
- `List<FileCandidate> ToCopy` — files that need to be copied
- `List<FileCandidate> ToSkip` — files where destination already exists with same name AND same size (treat as duplicate)
- `List<FileCandidate> Undated` — files placed in the `Undated` folder

**Scanning logic:**
1. Enumerate all files recursively from source using `Directory.EnumerateFiles(..., SearchOption.AllDirectories)`
2. Filter to supported extensions only (case-insensitive)
3. For each file, resolve the organise date:
   - Try EXIF `DateTimeOriginal` via `MetadataExtractor` (works for JPEG, PNG, HEIC, many RAW formats)
   - Try EXIF `DateTime` (media creation date) as second EXIF fallback
   - For video files, try `QuickTime Movie Header` atom date via MetadataExtractor
   - Fallback: `File.GetCreationTime()`
   - If all fail or produce a date before 1900: mark as `Undated`, place in `Undated\` folder
4. Build destination path:
   - Dated: `<destRoot>\<YYYY>\<MM> <MonthName>\<filename>`
   - Undated: `<destRoot>\Undated\<filename>`
5. Check if destination file already exists:
   - If yes AND `FileInfo.Length` matches: add to `ToSkip`
   - If yes AND `FileInfo.Length` differs: flag as `ConflictExists = true` on the candidate (to be handled in Phase 5)
   - If no: add to `ToCopy`

### Acceptance Criteria
- Correctly identifies all supported file types
- EXIF date is preferred over file creation date
- Files with no resolvable date land in `Undated\`
- Existing same-size files are excluded from `ToSkip`
- Conflicting files (same name, different size) are flagged

---

## Phase 4 — Pre-Copy Analysis & Confirmation

**Goal:** Wire the Analyse button to the scanner and display a human-readable summary before any files are touched.

### Behaviour

1. User clicks **Analyse**
2. Disable all controls; show "Scanning…" in the status label
3. Run `FileScanner.ScanAsync` on a background thread
4. On completion, populate the summary label with:
   ```
   Found 423 files.  →  391 to copy  |  12 already exist (will be skipped)  |  8 undated  |  12 conflicts need review
   ```
5. Enable the **Start Copy** button only if `ToSkip.Count > 0` or there are files to copy
6. Populate the log area with a breakdown:
   - List all `ToSkip` files as `[SKIP] <filename> — already exists`
   - List `Undated` files as `[UNDATED] <filename> — will copy to Undated\`
   - List conflicts as `[CONFLICT] <filename> — same name, different size`
7. If zero files found to copy and zero conflicts: show a message "Nothing to copy." and leave Start Copy disabled

### Acceptance Criteria
- Scanning runs without freezing the UI
- Summary counts are accurate
- Start Copy is only enabled when there is actionable work
- Log is populated before any copy begins

---

## Phase 5 — Conflict Resolution Dialog

**Goal:** When conflicts exist (same filename, different file size/content), prompt the user to decide before copying starts.

### ConflictResolutionForm

A modal dialog that shows a `DataGridView` with one row per conflict:

| Column | Content |
|---|---|
| File Name | filename only |
| Source Path | full source path |
| Destination Path | full destination path |
| Source Size | human-readable (e.g. 3.2 MB) |
| Dest Size | human-readable |
| Action | `ComboBox` per row: **Skip** / **Rename copy** |

**Bottom of dialog:**
- "Apply same action to all" dropdown (Skip All / Rename All)
- OK / Cancel buttons

**Rename behaviour:** if the user chooses Rename, append a numeric suffix before the extension:
- `photo.jpg` → `photo_1.jpg` → `photo_2.jpg` (increment until no collision)

This dialog is shown automatically after Analyse if any conflicts exist, and can be re-opened via a button ("Review Conflicts") before starting the copy.

### Acceptance Criteria
- Dialog is skipped entirely when there are no conflicts
- User can set per-file or bulk actions
- Rename suffix logic correctly avoids further collisions
- Cancelling the dialog returns to pre-copy state without starting the copy

---

## Phase 6 — Copy Engine

**Goal:** Implement the background copy operation with progress reporting and cancellation.

### Services/CopyEngine.cs

```csharp
Task<CopyResult> CopyAsync(
    IReadOnlyList<FileCandidate> files,
    IProgress<CopyProgress> progress,
    CancellationToken ct);
```

```csharp
public record CopyProgress(int Completed, int Total, string CurrentFile);
public record CopyResult(int Copied, int Skipped, int Failed, List<string> Errors);
```

**Copy logic per file:**
1. Ensure destination directory exists (`Directory.CreateDirectory`)
2. Call `File.Copy(source, dest, overwrite: false)`
   - `overwrite: false` as a safety net — the scanner should have already resolved conflicts
3. On success: report progress and log `[OK] <filename>`
4. On `IOException` (e.g. disk full, permissions): log `[ERROR] <filename> — <message>`, add to Errors list, continue
5. Honour `CancellationToken` between each file — do not cancel mid-file

**Progress reporting:**
- Update `ProgressBar.Value` after each file
- Update status label: "Copying 45 of 391 — photo.jpg"
- Append each result to the log `RichTextBox` (colour-coded: green = OK, orange = skip, red = error)

### Acceptance Criteria
- UI remains responsive during copy (async/await + Progress<T>)
- Cancel button stops the copy cleanly after the current file completes
- Destination folders are created automatically
- No source files are modified or deleted
- Errors are logged but do not abort the entire operation

---

## Phase 7 — Post-Copy Summary

**Goal:** Display a clear completion message once the copy finishes (or is cancelled).

### Behaviour

- Show a `MessageBox` or update the summary label:
  ```
  Copy complete.  391 copied  |  0 errors  |  12 skipped
  ```
- If cancelled:
  ```
  Cancelled after 147 of 391 files.  147 copied  |  0 errors
  ```
- If errors occurred, offer to open a log file saved to the destination root (`copy_log.txt`) containing all `[ERROR]` entries with timestamps
- Re-enable all controls and reset the Start Copy / Cancel button states
- Leave the log area populated so the user can scroll and review

### Acceptance Criteria
- Summary is always shown on completion, cancellation, or failure
- Error log file is written only when there are errors
- UI returns to a fully usable state after completion

---

## Phase 8 — Polish & Edge Cases

**Goal:** Harden the application against real-world edge cases and improve usability.

### Items

1. **Source = Destination guard:** If the user picks the same folder for both source and destination, show a validation error and block Analyse.
2. **Destination inside source guard:** Detect and warn if the destination is a subfolder of the source (would cause recursive copying of already-copied files).
3. **Long path support:** Add `<LongPathsEnabled>` manifest entry and use `\\?\` prefix handling for paths over 260 characters.
4. **Preserve file timestamps:** After copying, set `File.SetCreationTime` and `File.SetLastWriteTime` on the destination to match the source, so metadata is preserved.
5. **Re-run safety:** Clicking Analyse again after a completed copy re-scans correctly (previously copied files now appear as `ToSkip`).
6. **Empty source folder:** Show a friendly message ("No supported files found in the selected folder.") rather than enabling Start Copy with zero files.
7. **Access denied handling:** Catch `UnauthorizedAccessException` during scan and log inaccessible folders without crashing.
8. **Settings persistence:** Save last-used source and destination folder paths to `user.config` via `Properties.Settings` so they are restored on next launch.

### Acceptance Criteria
- All guards produce user-friendly messages, not unhandled exceptions
- Timestamps are preserved on copied files
- Last-used folders are remembered between sessions

---

## Phase 9 — Testing Checklist

Before marking the project complete, manually verify the following scenarios:

| Scenario | Expected Result |
|---|---|
| Mix of JPEGs (with EXIF) and PNGs (without) | JPEGs use EXIF date; PNGs use file creation date |
| File already exists at destination, same size | Silently skipped, counted in summary |
| File already exists at destination, different size | Conflict dialog shown; user chooses Skip or Rename |
| RAW files (e.g. .cr2, .nef) | Processed and dated correctly via MetadataExtractor |
| Video files (.mp4, .mov) | Processed and dated via QuickTime/MP4 metadata atom |
| File with no date resolvable | Placed in `Undated\` folder |
| Cancel mid-copy | Stops after current file; summary shows partial count |
| Disk full during copy | Error logged per file; operation continues for remaining files |
| Source = Destination | Validation error, no scan |
| 1000+ files | No UI freeze; progress updates smoothly |
| Path > 260 characters | Handled without crash |

---

## Suggested Build Order

```
Phase 1  →  Phase 2  →  Phase 3  →  Phase 4  →  Phase 6  →  Phase 7  →  Phase 5  →  Phase 8  →  Phase 9
(scaffold)  (UI)       (scanner)   (analyse)   (copy)      (summary)   (conflicts)  (polish)    (test)
```

> Phase 5 (Conflict Dialog) is intentionally deferred until the happy path (Phases 6–7) is working end-to-end, so you have a runnable app sooner.

---

## Notes for Claude Code

- Target **.NET 8** with `<UseWindowsForms>true</UseWindowsForms>` in the `.csproj`
- Use `async`/`await` throughout — never `Thread` or `BackgroundWorker`
- Use `IProgress<T>` for cross-thread UI updates — never `Control.Invoke` directly
- Keep `MainForm` as a thin coordinator; all business logic lives in `Services/`
- `MetadataExtractor` NuGet package handles EXIF, IPTC, XMP, QuickTime, and MP4 atoms in a single library — prefer it over `System.Drawing` EXIF APIs
- Do not use `File.Copy` with `overwrite: true` anywhere — conflicts must be resolved before copying begins
