# Special Dates Feature Specification

## Overview

Two types of special date routing:

1. **Special Dates** — named recurring or one-off single days (e.g. Xmas = 25 Dec). Files matching are routed to `DD Name\` inside the month folder.
2. **Date Ranges** — named one-off spans across one or more months (e.g. Holiday = 2024-08-01 to 2024-08-14). Each day in the range gets its own `DD Name\` subfolder inside the appropriate month folder.

Both types are checked during scan. Special Dates checked first; Date Ranges checked second. First match wins.

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
    08 August\
      01 Holiday\
        day1.jpg         ← matched range day 1
      02 Holiday\
        day2.jpg         ← matched range day 2
  2024\
    12 December\
      28 Xmas Trip\
        dec28.jpg        ← cross-month range, December portion
    01 January\
      01 Xmas Trip\
        jan1.jpg         ← cross-month range, January portion
```

- Special-date/range folder sits **inside** the month folder.
- File goes to special folder **only** — not duplicated in the month folder.
- Folder name format: `DD Name` (e.g. `25 Xmas`, `01 Holiday`).
- Cross-month ranges: files land in whichever month folder their date belongs to.

## Special Date Definition

| Field      | Type    | Required | Notes                                    |
|------------|---------|----------|------------------------------------------|
| Name       | string  | yes      | Used as folder name label, e.g. `Xmas`  |
| Month      | int     | yes      | 1–12                                     |
| Day        | int     | yes      | 1–31                                     |
| Year       | int?    | no       | If set, matches that year only (one-off) |

Annual special date: Year is null — matches every year.  
One-off special date: Year is set — matches only that year.

## Date Range Definition

| Field     | Type     | Required | Notes                                        |
|-----------|----------|----------|----------------------------------------------|
| Name      | string   | yes      | Used as folder name label, e.g. `Holiday`   |
| StartDate | DateOnly | yes      | First day of range (inclusive)               |
| EndDate   | DateOnly | yes      | Last day of range (inclusive)                |

Date ranges are always one-off (no annual recurrence). `EndDate` must be >= `StartDate`.

## UI

New **"Special Dates"** tab on `MainForm` with two sub-sections separated by a label or `GroupBox`.

### Special Dates section

- `DataGridView` with columns: Name, Month, Day, Year (optional)
- **Add** button — appends blank row for inline editing
- **Delete** button — removes selected row(s)

### Date Ranges section (separate tab panel or GroupBox below Special Dates)

- `DataGridView` with columns: Name, Start Date, End Date
  - Start Date / End Date display as `yyyy-MM-dd`; edited as text, parsed on `CellEndEdit`
- **Add** button — appends blank row
- **Delete** button — removes selected row(s)

Changes save immediately to JSON on every edit (`CellEndEdit` / `RowsRemoved`).

## Storage

Special dates and date ranges persisted as separate JSON files:

```
%LOCALAPPDATA%\PhotoOrganiser\special_dates.json
%LOCALAPPDATA%\PhotoOrganiser\date_ranges.json
```

### special_dates.json example
```json
[
  { "name": "Xmas",     "month": 12, "day": 25 },
  { "name": "Birthday", "month": 6,  "day": 15 },
  { "name": "Wedding",  "month": 8,  "day": 20, "year": 2019 }
]
```

### date_ranges.json example
```json
[
  { "name": "Holiday",   "startDate": "2024-08-01", "endDate": "2024-08-14" },
  { "name": "Xmas Trip", "startDate": "2024-12-28", "endDate": "2025-01-03" }
]
```

## Matching Logic

During `FileScanner.ScanAsync`, after date resolution:

1. If `DateSource == Undated` → no special-date check, goes to `Undated\` as normal.
2. Check all loaded **Special Dates** for a match:
   - Annual match: `date.Month == sd.Month && date.Day == sd.Day`
   - One-off match: above AND `date.Year == sd.Year`
   - First match wins.
3. If no Special Date matched, check all **Date Ranges**:
   - Match: `fileDate >= dr.StartDate && fileDate <= dr.EndDate`
   - First match wins.
4. If matched (either type): `DestinationFolder = $"{year}\{month:D2} {monthName}\{day:D2} {name}"`
5. No match: normal path `{year}\{month:D2} {monthName}\`.

## New / Modified Files

| File | Change |
|------|--------|
| `Models/SpecialDate.cs` | New record: `Name`, `Month`, `Day`, `Year?` |
| `Models/DateRange.cs` | New record: `Name`, `StartDate`, `EndDate` |
| `Services/ISpecialDateService.cs` | New interface — includes both special dates and ranges |
| `Services/SpecialDateService.cs` | New: load/save JSON for both types, match logic |
| `PhotoOrganiser/Services/FileScanner.cs` | Inject `ISpecialDateService`, use in path resolution |
| `Forms/MainForm.cs` | Add Special Dates tab with two DataGridViews + Add/Delete wiring |
| `Forms/MainForm.Designer.cs` | Designer updates for new tab |
| `PhotoOrganiser.Tests/SpecialDateServiceTests.cs` | New: match logic unit tests for both types |
| `PhotoOrganiser.Tests/FileScannerTests.cs` | Extend: special-date and range path routing tests |

## Acceptance Criteria

### Special Dates
- [x] Annual date matches same day/month across any year.
- [x] One-off date matches only the specified year.
- [x] Matched file routed to `DD Name\` subfolder inside month folder.
- [x] Matched file NOT also copied to bare month folder.
- [x] No match → normal month folder path unchanged.
- [x] `Undated` files unaffected.
- [x] Multiple special dates on same day: first defined wins.
- [x] Special dates survive app restart (JSON persisted).
- [x] Add/Delete in UI updates JSON immediately.
- [x] Invalid grid input (bad month/day) shows validation error, not crash.

### Date Ranges
- [x] File on first day of range routed to `DD Name\` inside correct month folder.
- [x] File on last day of range routed to `DD Name\` inside correct month folder.
- [x] File one day before range start → not matched.
- [x] File one day after range end → not matched.
- [x] Cross-month range: files in each month land under correct month folder with `DD Name\`.
- [x] Cross-year range (e.g. 28 Dec – 3 Jan): files land under correct year and month.
- [x] Special Date takes priority over overlapping Date Range.
- [x] Multiple ranges overlapping same day: first defined wins.
- [x] Date ranges survive app restart (JSON persisted).
- [x] Add/Delete in UI updates JSON immediately.
- [x] `EndDate` < `StartDate` shows validation error, not crash.
- [x] Invalid date string in grid shows validation error, not crash.
