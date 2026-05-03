# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Status

Phases 1–4 complete + Special Dates + Date Ranges feature. Solution scaffolded, UI built, FileScanner service implemented (99 unit tests passing). Phase 4 wired: Analyse button runs ScanAsync async, guards source==dest and dest-inside-source, populates summary label and colour-coded log, enables Start Copy only when actionable work exists. Special Dates: SpecialDate model, ISpecialDateService/SpecialDateService (JSON persistence in %LOCALAPPDATA%\PhotoOrganiser\special_dates.json), injected into FileScanner, Special Dates tab in MainForm with DataGridView + Add/Delete. Date Ranges: DateRange model (Name, StartDate, EndDate as DateOnly), persisted to %LOCALAPPDATA%\PhotoOrganiser\date_ranges.json, MatchRange checks file date falls within range (inclusive, supports cross-month/year), checked after Special Dates (Special Date wins on overlap), separate Date Ranges tab in MainForm. Phase 5+ not yet implemented.

**Runtime note:** Only .NET 10 runtime is installed on this machine. Both projects target `net10.0-windows` (plan says net8.0 but that runtime is absent).

## Project

WinForms (.NET 8) desktop app. Copies images/videos from source folder to `<dest>\YYYY\MM MonthName\` (or `Undated\`) using EXIF/embedded date, falling back to `File.GetCreationTime()`. Copy only — never modify source.

## Architecture (target)

```
PhotoOrganiser.sln
└── PhotoOrganiser/
    ├── Forms/      MainForm + ConflictResolutionForm (thin coordinators only)
    ├── Services/   FileScanner, CopyEngine (all business logic here)
    ├── Models/     FileCandidate, ScanResult, CopyProgress, CopyResult, DateSource
    └── Helpers/    FileTypes (supported extension constants)
```

Pipeline: `MainForm` → `IFileScanner.ScanAsync` → optional `ConflictResolutionForm` → `CopyEngine.CopyAsync(IProgress<CopyProgress>, CancellationToken)` → summary.

Date resolution order per file: EXIF `DateTimeOriginal` → EXIF `DateTime` → QuickTime/MP4 atom (videos) → `File.GetCreationTime()` → `Undated`. Dates < 1900 treated as invalid.

Conflict rule: same filename + same size = silent skip. Same filename + different size = user prompt (Skip / Rename with `_N` suffix incrementing until free).

## Commands

Once scaffolded:

```
dotnet build PhotoOrganiser.slnx
dotnet run --project PhotoOrganiser
dotnet test PhotoOrganiser.Tests
dotnet test --filter "FullyQualifiedName~FileScannerTests"
dotnet test --filter "FullyQualifiedName~CopyEngineTests"
dotnet test --filter "FullyQualifiedName~FileScannerTests.ScanAsync_PrefersExifOverCreation"
```

## Hard rules (from plan, §"Notes for Claude Code")

- Target .NET 8, `<UseWindowsForms>true</UseWindowsForms>`.
- Use `MetadataExtractor` NuGet for all metadata (EXIF/IPTC/XMP/QuickTime/MP4). Do **not** use `System.Drawing` EXIF APIs.
- `async`/`await` only. No `Thread`, no `BackgroundWorker`.
- Cross-thread UI updates via `IProgress<T>` only. No direct `Control.Invoke`.
- `File.Copy(..., overwrite: false)` only — conflicts must be resolved before copy starts.
- Cancellation honored between files, never mid-file.
- Preserve source timestamps on destination (`SetCreationTime` + `SetLastWriteTime`).
- Guard: source == destination, and destination inside source — block before scan.

## Reference

Full phased spec, models, acceptance criteria, and test scenarios: `docs/PhotoOrganiser_ImplementationPlan.md`. Build order suggested there: 1 → 2 → 3 → 4 → 6 → 7 → 5 → 8 → 9 (conflict dialog deferred until happy path runs).
