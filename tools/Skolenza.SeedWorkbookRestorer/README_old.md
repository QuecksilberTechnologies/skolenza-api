# Skolenza seed workbook restore

This command restores the Render seed workbook without running individual SQL seed files. It validates the workbook marker, manifest, sheet row counts, columns and PostgreSQL types before writing.

Validate without changing the database:

```powershell
dotnet run --project .\tools\Skolenza.SeedWorkbookRestorer -- `
  --file .\outputs\render-seed-workbook\Skolenza_Render_Seed_Data_2026-10-09.xlsx -- `
  --settings .\skolenza.api\appsettings.Development.json
```

Verify a complete clean-schema restore and roll every test write back:

```powershell
dotnet run --project .\tools\Skolenza.SeedWorkbookRestorer -- `
  --file .\outputs\render-seed-workbook\Skolenza_Render_Seed_Data_2026-10-09.xlsx -- `
  --settings .\skolenza.api\appsettings.Development.json --verify-restore
```

Apply the restore:

```powershell
dotnet run --project .\tools\Skolenza.SeedWorkbookRestorer -- `
  --file .\outputs\render-seed-workbook\Skolenza_Render_Seed_Data_2026-10-09.xlsx -- `
  --settings .\skolenza.api\appsettings.Development.json --apply
```

`--apply` uses one transaction and primary-key upserts. It is intended for an empty target database or a target already carrying the same seed identities. A schema mismatch, natural-key conflict, foreign-key error or invalid value rolls the entire transaction back.

`--verify-restore` truncates the target schema only inside its test transaction, performs the complete restore, then rolls the transaction back. It must never be described as a committed restore.
