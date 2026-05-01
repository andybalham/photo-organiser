# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Status

Greenfield. Only `docs/PhotoOrganiser_ImplementationPlan.md` exists — no solution, project, or code yet. First implementation work should scaffold per Phase 1 of the plan.

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
dotnet test                      # if test project added
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
