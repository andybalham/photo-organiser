# Photo Organiser — Implementation Status

## Phases

| Phase | Description | Status |
|---|---|---|
| 1 | Project scaffold & solution structure | ✅ Done |
| 2 | Main form UI layout | ⬜ Not started |
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
