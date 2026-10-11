# 05 — country-dependent identity catalogue

`001-employee-identity-catalog.sql` runs after geography because `CountryIdentityRule` references
`Country`. It seeds the government identity category/documents and supported country mappings,
creates the empty tenant employee-section default table, and finally synchronizes every `skolenza`
identity sequence to its actual maximum ID.
