# Photo Organiser — Implementation Status

## Phases

| Phase | Description | Status |
|---|---|---|
| 1 | Project scaffold & solution structure | ✅ Done |
| 2 | Main form UI layout | ✅ Done |
| 3 | File scanning service | ⬜ Not started |
| 4 | Pre-copy analysis & confirmation | ⬜ Not started |
| 5 | Conflict resolution dialog | ⬜ Not started |
| 6 | Copy engine | ⬜ Not started |
| 7 | Post-copy summary | ⬜ Not started |
| 8 | Polish & edge cases | ⬜ Not started |
| 9 | Testing checklist | ⬜ Not started |

## Phase 1 — Done

- `PhotoOrganiser.slnx` + `PhotoOrganiser/PhotoOrganiser.csproj` (net8.0-windows, UseWindowsForms)
- `MetadataExtractor` 2.9.3 added
- `Helpers/FileTypes.cs` — all supported image + video extensions
- `Forms/MainForm.cs` + `MainForm.Designer.cs`
- `Services/`, `Models/` folders ready
- Builds clean: 0 warnings, 0 errors
- **Pending:** `PhotoOrganiser.Tests/` xUnit project not yet created (required before Phase 3)

## Phase 2 — Done

- `TableLayoutPanel` layout: source/dest pickers → summary label → log → progress → buttons
- Source + dest rows: `Label` + read-only `TextBox` + "Browse…" `Button` (uses `FolderBrowserDialog`)
- Analyse button (left-aligned); Start Copy + Cancel buttons (right-aligned, both disabled by default)
- `ProgressBar` + progress status `Label` in dedicated row
- `RichTextBox` log area (Consolas 9pt, read-only, expands to fill)
- Form 700×550 default, 540×400 minimum, fully resizable
- All buttons wired to stub handlers; Start Copy disabled until Phase 4 enables it
- Builds clean: 0 warnings, 0 errors
