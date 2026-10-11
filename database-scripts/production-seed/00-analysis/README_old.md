# 00 — analysis and coverage

This folder does not modify the database.

- `TABLE_DEPENDENCY_CATALOG.md`: every audited table with parent and dependent tables.
- `foreign-key-dependencies.csv`: all 278 foreign keys in machine-readable form.
- `identity-sequences.csv`: identity/sequence inventory captured before reset.
- `table-row-counts.csv`: pre-reset row counts from the target backup point.
- `post-seed-row-counts.csv`: direct target row counts after the canonical run.
- `SEED_COVERAGE_MATRIX.csv`: all 164 current tables mapped to seeded, intentionally empty, or
  pending-decision status and their responsible script.

Use `SEED_COVERAGE_MATRIX.csv` to answer whether a specific table is covered.
