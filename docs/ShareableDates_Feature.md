# Shareable Dates Feature

## Goal

Allow users to export/import a single JSON file containing both special dates and date ranges. App remembers the linked file path, auto-loads it on startup, and auto-saves changes back to it.

## Data

Single JSON file containing both collections:

```json
{
  "specialDates": [
    { "name": "Christmas", "month": 12, "day": 25 }
  ],
  "dateRanges": [
    { "name": "Summer Holiday", "startDate": "2024-07-20", "endDate": "2024-08-10" }
  ]
}
```

## Linked File Behaviour

- **Remember path** — linked file path persisted to `%LOCALAPPDATA%\PhotoOrganiser\settings.json` under key `linkedDatesFile`.
- **Auto-load on startup** — if `linkedDatesFile` is set and file exists, load it. Replaces all current special dates and date ranges.
- **Auto-save on change** — whenever user adds or deletes a special date or date range, and a linked file is active, save the full combined file immediately.
- If linked file path is set but file is missing on startup, clear the path and start with empty collections (do not error).

## UI Changes

Single set of controls placed on the main form, below the **Destination** field and above the tab control:

| Control | Detail |
|---------|--------|
| **Link File…** button | Open file dialog (filter: `Dates files (*.dates.json)|*.dates.json|All files (*.*)|*.*`). If user picks existing file: load it (replace current data). If user picks new path: save current data there. Sets linked file. |
| **Save File…** button | Open save-file dialog. Writes current special dates and date ranges to the chosen path. Does **not** change the linked file. Always enabled. |
| **Unlink** button | Clears linked file path. Does not delete file or alter in-memory data. Disabled when no file linked. |
| **Open Folder** button | Opens Windows Explorer to folder containing linked file. Disabled when no file linked. |
| Status label | Read-only. Shows linked file path, or `"No file linked"` when unlinked. |

All five controls sit in a single row/panel, visible regardless of which tab is active.

## Load / Replace Semantics

- Loading a file (via auto-load or Link File…) **replaces** all special dates and all date ranges.
- No merge. No prompt.

## Auto-Save Semantics

- Triggered after every add or delete in either tab, only when a linked file is set.
- Writes both collections together to the linked file.
- Failures (disk full, permission denied) shown as a non-blocking status label message: `"Auto-save failed: <reason>"`. Do not throw or crash.

## Settings Persistence

`%LOCALAPPDATA%\PhotoOrganiser\settings.json`:

```json
{
  "linkedDatesFile": "C:\\Users\\Andy\\OneDrive\\photo-dates.dates.json"
}
```

Existing `special_dates.json` and `date_ranges.json` remain as internal working copies and continue to be written as today. The linked file is an additional export target, not a replacement for internal storage.

## Acceptance Criteria

- Linking an existing file replaces in-memory data and saves path to settings.
- Linking a new path saves current data to that file and saves path to settings.
- On next app launch with linked file set, data loaded from linked file automatically.
- Every add/delete auto-saves to linked file when one is set.
- Save File… writes current data to chosen path without changing linked file.
- Unlink clears path from settings; data in memory unchanged.
- Open Folder opens Explorer to correct directory.
- Missing linked file on startup clears path silently, no crash.
- Auto-save failure shown in label, does not crash app.
- Both tabs show same linked file state (same file for both).
