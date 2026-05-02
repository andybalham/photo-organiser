# Special Dates Feature Specification

## Overview

User defines named recurring or one-off special dates (e.g. Xmas = 25 Dec). During scan, files whose resolved date matches a special date are routed to a subfolder `DD Name\` inside the normal month folder instead of directly into the month folder.

## Folder Structure

```
<dest>\
  2024\
    12 December\
      25 Xmas\
        photo.jpg        ← matched special date
      other_photo.jpg    ← normal file in December
    06 June\
      15 Birthday\
        birthday.jpg
```

- Special-date folder sits **inside** the month folder.
- File goes to special folder **only** — not duplicated in the month folder.
- Folder name format: `DD Name` (e.g. `25 Xmas`, `15 Birthday`).

## Special Date Definition

| Field      | Type    | Required | Notes                                    |
|------------|---------|----------|------------------------------------------|
| Name       | string  | yes      | Used as folder name label, e.g. `Xmas`  |
| Month      | int     | yes      | 1–12                                     |
| Day        | int     | yes      | 1–31                                     |
| Year       | int?    | no       | If set, matches that year only (one-off) |

Annual special date: Year is null — matches every year.  
One-off special date: Year is set — matches only that year.

## UI

New **"Special Dates"** tab on `MainForm` (alongside Source/Destination controls).

Tab contains:
- `DataGridView` with columns: Name, Month, Day, Year (optional)
- **Add** button — appends a blank row for inline editing
- **Delete** button — removes selected row(s)

Changes save immediately to JSON on every edit (grid `CellEndEdit` / `RowsRemoved`).

## Storage

Special dates persisted as JSON in user's AppData alongside existing settings:

```
%LOCALAPPDATA%\PhotoOrganiser\special_dates.json
```

Example:
```json
[
  { "name": "Xmas",     "month": 12, "day": 25 },
  { "name": "Birthday", "month": 6,  "day": 15 },
  { "name": "Wedding",  "month": 8,  "day": 20, "year": 2019 }
]
```

## Matching Logic

During `FileScanner.ScanAsync`, after date resolution:

1. If `DateSource == Undated` → no special-date check, goes to `Undated\` as normal.
2. Otherwise, check all loaded special dates for a match:
   - Annual match: `date.Month == sd.Month && date.Day == sd.Day`
   - One-off match: above AND `date.Year == sd.Year`
3. First match wins (list order = priority).
4. If matched: `DestinationFolder = $"{year}\{month:D2} {monthName}\{day:D2} {sd.Name}"`
5. No match: normal path `{year}\{month:D2} {monthName}\`.

## New / Modified Files

| File | Change |
|------|--------|
| `Models/SpecialDate.cs` | New record: `Name`, `Month`, `Day`, `Year?` |
| `Services/SpecialDateService.cs` | New: load/save JSON, match logic |
| `Services/ISpecialDateService.cs` | New interface |
| `PhotoOrganiser/Services/FileScanner.cs` | Inject `ISpecialDateService`, use in path resolution |
| `Forms/MainForm.cs` | Add Special Dates tab + DataGridView wiring |
| `Forms/MainForm.Designer.cs` | Designer updates for new tab |
| `PhotoOrganiser.Tests/SpecialDateServiceTests.cs` | New: match logic unit tests |
| `PhotoOrganiser.Tests/FileScannerTests.cs` | Extend: special-date path routing tests |

## Acceptance Criteria

- [ ] Annual date matches same day/month across any year.
- [ ] One-off date matches only the specified year.
- [ ] Matched file routed to `DD Name\` subfolder inside month folder.
- [ ] Matched file NOT also copied to bare month folder.
- [ ] No match → normal month folder path unchanged.
- [ ] `Undated` files unaffected.
- [ ] Multiple special dates on same day: first defined wins.
- [ ] Special dates survive app restart (JSON persisted).
- [ ] Add/Delete in UI updates JSON immediately.
- [ ] Invalid grid input (bad month/day) shows validation error, not crash.
