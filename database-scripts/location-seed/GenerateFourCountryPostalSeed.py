"""Generate a PostgreSQL/pgAdmin-compatible four-country postal locality seed.

Input files are the official GeoNames postal-code exports named IN-postal.zip,
CN-postal.zip, DE-postal.zip, and US-postal.zip. The generated SQL is idempotent
and preserves existing identifiers and tenant references.
"""

from __future__ import annotations

import argparse
import hashlib
import os
import zipfile
from collections import Counter, defaultdict


COUNTRIES = {
    "IN": ("India", "+91"),
    "CN": ("China", "+86"),
    "DE": ("Germany", "+49"),
    "US": ("United States", "+1"),
}


def sql(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def stable_code(*parts: str) -> str:
    value = "|".join(part.casefold().strip() for part in parts)
    return hashlib.sha256(value.encode("utf-8")).hexdigest()[:20].upper()


def read_rows(source_directory: str):
    rows = {}
    state_names: dict[tuple[str, str], Counter[str]] = defaultdict(Counter)
    district_names: dict[tuple[str, str, str], Counter[str]] = defaultdict(Counter)

    for country_code in COUNTRIES:
        archive_path = os.path.join(source_directory, f"{country_code}-postal.zip")
        with zipfile.ZipFile(archive_path) as archive:
            content = archive.read(f"{country_code}.txt").decode("utf-8")

        for line in content.splitlines():
            fields = line.split("\t")
            postal_code = fields[1].strip()
            locality_name = fields[2].strip()
            state_name = fields[3].strip() or "Unassigned State"
            state_code = fields[4].strip() or "UNASSIGNED"

            # Germany publishes Kreis/Landkreis in admin3; the other exports use
            # admin2 as their district/county/prefecture level.
            district_name = (fields[7].strip() if country_code == "DE" else fields[5].strip())
            district_code = (fields[8].strip() if country_code == "DE" else fields[6].strip())
            if not district_name:
                district_name = f"{state_name} - Unassigned District"
            if not district_code:
                district_code = "UNASSIGNED"

            state_names[(country_code, state_code)][state_name] += 1
            district_names[(country_code, state_code, district_code)][district_name] += 1
            locality_code = stable_code(country_code, state_code, district_code, postal_code, locality_name)
            rows[(country_code, state_code, district_code, locality_code)] = (
                country_code,
                state_code,
                district_code,
                locality_code,
                locality_name,
                postal_code,
            )

    resolved_states = {key: names.most_common(1)[0][0] for key, names in state_names.items()}
    state_groups: dict[tuple[str, str], list[tuple[str, int]]] = defaultdict(list)
    for key, name in resolved_states.items():
        state_groups[(key[0], name.casefold())].append((key[1], sum(state_names[key].values())))
    state_alias = {
        (country_code, state_code): max(candidates, key=lambda item: item[1])[0]
        for (country_code, _), candidates in state_groups.items()
        for state_code, _ in candidates
    }

    resolved_districts = {key: names.most_common(1)[0][0] for key, names in district_names.items()}
    district_groups: dict[tuple[str, str, str], list[tuple[str, int]]] = defaultdict(list)
    for key, name in resolved_districts.items():
        canonical_state = state_alias[(key[0], key[1])]
        district_groups[(key[0], canonical_state, name.casefold())].append(
            (key[2], sum(district_names[key].values()))
        )
    district_alias = {
        (country_code, state_code, district_code): max(candidates, key=lambda item: item[1])[0]
        for (country_code, state_code, _), candidates in district_groups.items()
        for district_code, _ in candidates
    }

    canonical_states = {}
    for (country_code, state_code), state_name in resolved_states.items():
        canonical_states[(country_code, state_alias[(country_code, state_code)])] = state_name
    canonical_districts = {}
    for (country_code, state_code, district_code), district_name in resolved_districts.items():
        canonical_state = state_alias[(country_code, state_code)]
        canonical_district = district_alias[(country_code, canonical_state, district_code)]
        canonical_districts[(country_code, canonical_state, canonical_district)] = district_name

    canonical_rows = {}
    for country_code, state_code, district_code, _, locality_name, postal_code in rows.values():
        canonical_state = state_alias[(country_code, state_code)]
        canonical_district = district_alias[(country_code, canonical_state, district_code)]
        locality_code = stable_code(country_code, canonical_state, canonical_district, postal_code, locality_name)
        canonical_rows[(country_code, canonical_state, canonical_district, locality_code)] = (
            country_code, canonical_state, canonical_district, locality_code, locality_name, postal_code
        )

    return canonical_rows.values(), canonical_states, canonical_districts


def write_values(output, table: str, rows, columns: str, chunk_size: int = 1000):
    rows = list(rows)
    for offset in range(0, len(rows), chunk_size):
        chunk = rows[offset : offset + chunk_size]
        output.write(f"INSERT INTO {table} {columns} VALUES\n")
        output.write(",\n".join("    (" + ", ".join(sql(str(value)) for value in row) + ")" for row in chunk))
        output.write(";\n")


def generate(source_directory: str, output_path: str):
    localities, states, districts = read_rows(source_directory)
    with open(output_path, "w", encoding="utf-8", newline="\n") as output:
        output.write("""/*
  India, China, Germany, and United States postal locality seed.
  Source: GeoNames postal-code exports (https://download.geonames.org/export/zip/)
  License: Creative Commons Attribution 4.0. Generated by GenerateFourCountryPostalSeed.py.
  Existing IDs/references are preserved; rows are matched by their stable codes.
*/
BEGIN;
SET LOCAL lock_timeout = '30s';
SET LOCAL statement_timeout = '20min';
INSERT INTO Skolenza."LocalityType" ("Id", "TypeName", "IsActive") VALUES
    (1, 'City', TRUE), (2, 'Town', TRUE), (3, 'Village', TRUE),
    (4, 'Other / Unclassified', TRUE)
ON CONFLICT ("Id") DO UPDATE SET "TypeName"=EXCLUDED."TypeName", "IsActive"=TRUE;
UPDATE Skolenza."Country" SET "CountryName"='India', "STDCode"='+91', "IsActive"=TRUE WHERE "CountryCode"='IN';
UPDATE Skolenza."Country" SET "CountryName"='China', "STDCode"='+86', "IsActive"=TRUE WHERE "CountryCode"='CN';
UPDATE Skolenza."Country" SET "CountryName"='Germany', "STDCode"='+49', "IsActive"=TRUE WHERE "CountryCode"='DE';
UPDATE Skolenza."Country" SET "CountryName"='United States', "STDCode"='+1', "IsActive"=TRUE WHERE "CountryCode"='US';
INSERT INTO Skolenza."Country" ("CountryName", "CountryCode", "STDCode", "IsActive")
SELECT seed.* FROM (VALUES ('India','IN','+91',TRUE), ('China','CN','+86',TRUE),
    ('Germany','DE','+49',TRUE), ('United States','US','+1',TRUE))
    AS seed("CountryName","CountryCode","STDCode","IsActive")
WHERE NOT EXISTS (SELECT 1 FROM Skolenza."Country" country WHERE country."CountryCode"=seed."CountryCode");
CREATE TEMP TABLE postal_seed (
    "CountryCode" varchar(10), "StateCode" varchar(30), "DistrictCode" varchar(50),
    "LocalityCode" varchar(100), "LocalityName" varchar(100), "PostalCode" varchar(20)
) ON COMMIT DROP;
""")
        normalized_rows = []
        for country_code, state_code, district_code, locality_code, locality_name, postal_code in localities:
            normalized_rows.append((country_code, state_code, district_code, locality_code, locality_name[:100], postal_code[:20]))
        write_values(output, "postal_seed", normalized_rows, '("CountryCode","StateCode","DistrictCode","LocalityCode","LocalityName","PostalCode")')

        output.write("\nCREATE TEMP TABLE state_seed (\"CountryCode\" varchar(10), \"StateCode\" varchar(30), \"StateName\" varchar(100)) ON COMMIT DROP;\n")
        write_values(output, "state_seed", ((cc, sc, name[:100]) for (cc, sc), name in states.items()), '("CountryCode","StateCode","StateName")')
        output.write("\nCREATE TEMP TABLE district_seed (\"CountryCode\" varchar(10), \"StateCode\" varchar(30), \"DistrictCode\" varchar(50), \"DistrictName\" varchar(100)) ON COMMIT DROP;\n")
        write_values(output, "district_seed", ((cc, sc, dc, name[:100]) for (cc, sc, dc), name in districts.items()), '("CountryCode","StateCode","DistrictCode","DistrictName")')

        output.write("""
INSERT INTO Skolenza."State" ("CountryId","StateCode","StateName","IsActive")
SELECT country."Id", seed."StateCode", seed."StateName", TRUE
FROM state_seed seed JOIN Skolenza."Country" country ON country."CountryCode"=seed."CountryCode"
WHERE NOT EXISTS (
    SELECT 1 FROM Skolenza."State" state WHERE state."CountryId"=country."Id"
      AND (state."StateCode"=seed."StateCode" OR lower(state."StateName")=lower(seed."StateName"))
);

CREATE TEMP TABLE state_map AS
SELECT DISTINCT ON (seed."CountryCode", seed."StateCode")
       seed."CountryCode", seed."StateCode", state."Id" AS "StateId"
FROM state_seed seed
JOIN Skolenza."Country" country ON country."CountryCode"=seed."CountryCode"
JOIN Skolenza."State" state ON state."CountryId"=country."Id"
 AND (state."StateCode"=seed."StateCode" OR lower(state."StateName")=lower(seed."StateName"))
ORDER BY seed."CountryCode", seed."StateCode",
         CASE WHEN state."StateCode"=seed."StateCode" THEN 0 ELSE 1 END, state."Id";

INSERT INTO Skolenza."District" ("StateId","DistrictCode","DistrictName","IsActive")
SELECT state_map."StateId", seed."DistrictCode", seed."DistrictName", TRUE
FROM district_seed seed
JOIN state_map ON state_map."CountryCode"=seed."CountryCode" AND state_map."StateCode"=seed."StateCode"
WHERE NOT EXISTS (
    SELECT 1 FROM Skolenza."District" district WHERE district."StateId"=state_map."StateId"
      AND (district."DistrictCode"=seed."DistrictCode" OR lower(district."DistrictName")=lower(seed."DistrictName"))
);

CREATE TEMP TABLE district_map AS
SELECT DISTINCT ON (seed."CountryCode", seed."StateCode", seed."DistrictCode")
       seed."CountryCode", seed."StateCode", seed."DistrictCode", district."Id" AS "DistrictId",
       state_map."StateId"
FROM district_seed seed
JOIN state_map ON state_map."CountryCode"=seed."CountryCode" AND state_map."StateCode"=seed."StateCode"
JOIN Skolenza."District" district ON district."StateId"=state_map."StateId"
 AND (district."DistrictCode"=seed."DistrictCode" OR lower(district."DistrictName")=lower(seed."DistrictName"))
ORDER BY seed."CountryCode", seed."StateCode", seed."DistrictCode",
         CASE WHEN district."DistrictCode"=seed."DistrictCode" THEN 0 ELSE 1 END, district."Id";

INSERT INTO Skolenza."Locality" ("LocalityName","LocalityCode","PostalCode","StateId","DistrictId","LocalityTypeId","IsActive")
SELECT seed."LocalityName", seed."LocalityCode", seed."PostalCode", district_map."StateId",
       district_map."DistrictId", 4, TRUE
FROM postal_seed seed
JOIN district_map ON district_map."CountryCode"=seed."CountryCode"
 AND district_map."StateCode"=seed."StateCode" AND district_map."DistrictCode"=seed."DistrictCode"
ON CONFLICT ("DistrictId","LocalityCode") DO UPDATE SET
    "LocalityName"=EXCLUDED."LocalityName", "PostalCode"=EXCLUDED."PostalCode", "IsActive"=TRUE;

SELECT country."CountryCode", count(DISTINCT state."Id") AS "StateCount",
       count(DISTINCT district."Id") AS "DistrictCount", count(locality."Id") AS "PostalLocalityCount"
FROM Skolenza."Country" country
JOIN Skolenza."State" state ON state."CountryId"=country."Id"
JOIN Skolenza."District" district ON district."StateId"=state."Id"
JOIN Skolenza."Locality" locality ON locality."DistrictId"=district."Id" AND locality."PostalCode" IS NOT NULL
WHERE country."CountryCode" IN ('IN','CN','DE','US')
GROUP BY country."CountryCode" ORDER BY country."CountryCode";
COMMIT;
""")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("source_directory")
    parser.add_argument("output_path")
    arguments = parser.parse_args()
    generate(arguments.source_directory, arguments.output_path)
