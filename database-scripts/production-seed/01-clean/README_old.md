# 01 — clean and identity reset

`001-reset-all-data.sql` is the destructive first stage. It verifies the exact target database name,
takes an advisory transaction lock, explicitly truncates the audited tables with `CASCADE`, and
uses `RESTART IDENTITY`. Run it only after the backup documented in the parent README exists.
