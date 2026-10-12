# Skolenza deployment bootstrap

Run these scripts against a newly created `SkolenzaDB` after the `skolenza`
schema has been restored.

1. `01-host-admin-bootstrap.sql` creates the platform super-admin role and the
   two approved platform users. Password hashes are copied from the approved
   AxionPro seed; plain-text passwords are not stored in these scripts.
2. `02-host-access-catalogue.sql` inserts the platform module, operation,
   module-operation, and role-permission catalogue using Skolenza
   Institution/Campus terminology.
3. `04-rebind-database-functions-to-skolenza.sql` updates every copied
   user-defined function body that still qualifies objects with the old
   `axionpro` schema.
4. `03-verify-host-bootstrap.sql` performs read-only catalogue reconciliation.
5. `05-verify-database-functions.sql` fails if any Skolenza function still
   references the old schema and verifies all four Host permission outcomes.

Each write script is transactional and rejects an unsafe starting state. Use
the scripts in `rollback` in reverse order when a bootstrap must be removed.

```powershell
$env:PGPASSWORD = '<database-password>'
psql -h <host> -U <user> -d SkolenzaDB -v ON_ERROR_STOP=1 -f 01-host-admin-bootstrap.sql
psql -h <host> -U <user> -d SkolenzaDB -v ON_ERROR_STOP=1 -f 02-host-access-catalogue.sql
psql -h <host> -U <user> -d SkolenzaDB -v ON_ERROR_STOP=1 -f 04-rebind-database-functions-to-skolenza.sql
psql -h <host> -U <user> -d SkolenzaDB -v ON_ERROR_STOP=1 -f 03-verify-host-bootstrap.sql
psql -h <host> -U <user> -d SkolenzaDB -v ON_ERROR_STOP=1 -f 05-verify-database-functions.sql
```

The numeric filenames are retained because `03` was already distributed as the
catalogue verifier before the routine defect was found. Follow the order above.


## Institution and Staff catalogue

Run `06-institution-staff-access-catalogue.sql` after the Host bootstrap scripts.
It creates the compatibility-first Institution/Staff menu catalogue, operation mappings and
InstitutionType lookup rows. Run `07-verify-institution-staff-access-catalogue.sql` immediately
afterward. The matching rollback is
`rollback/06-rollback-institution-staff-access-catalogue.sql`.

After the catalogue is present, apply
`../migrations/20261012_EnforceInstitutionAccessMasterIntegrity.sql`. It binds
Role, Department, Designation, StaffMember and UserRole records to one
institution boundary and enforces the centralized RoleType range (1-6).

