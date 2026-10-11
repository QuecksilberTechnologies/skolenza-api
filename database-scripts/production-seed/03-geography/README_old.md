# 03 — geography hierarchy

`000-iso-country-catalog.sql` inserts all 249 ISO 3166-1 countries and territories with alpha-2 codes
and their primary international calling codes. Seven territories without an independently assigned
calling code inherit the code of their numbering-plan administrator.

`001-four-country-postal-catalog.sql` inserts the detailed State → District → Locality hierarchy for
India, China, Germany and the United States. Its acceptance checks require those 4 countries, at least 100 states, at least 4,000
districts and at least 200,000 postal localities before commit.

`002-country-regulatory-master.sql` runs after geography. It cleans and identity-resets
`ComplianceTypeMaster` and `StatutoryType`. Every active country receives four neutral compliance
domains. Verified statutory masters are additionally inserted for India (`IN`), China (`CN`), Germany
(`DE`) and the United States (`US`). Contribution flags are not invented for jurisdictions without a
verified statutory catalogue. Country IDs are resolved from `CountryCode`; no environment-specific
numeric country ID is embedded in the mappings.

## Additive UAE supplement

After Country and LocalityType masters, run
[`../../SeedUaeLocations.sql`](../../SeedUaeLocations.sql) separately; the existing
canonical reset/runner is unchanged. This approved GeoNames CC BY 4.0 catalogue
adds 7 emirates, 28 source districts plus 7 explicitly Unassigned District groups,
and 2,776 localities. Existing rows are preserved; missing district membership and
postal codes are not invented. Source SHA256 and attribution are in the SQL header.

```powershell
python database-scripts/location-seed/GenerateUaeLocationSeed.py <path-to-AE.zip> database-scripts/SeedUaeLocations.sql
```

Regeneration against a changed source requires data review; snapshot tests pin
these counts. [Local verification](../../../docs/testing/employee/contact-relation-location/2026-10-01.md).
