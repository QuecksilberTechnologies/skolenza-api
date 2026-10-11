#!/usr/bin/env python3
import re
from pathlib import Path
p=Path('Skolenza.application/DTOS/SkolenzaPostgresScript.txt')
text=p.read_text(encoding='utf-8')
# replacements
# 1. replace newid() -> gen_random_uuid()::text
text=re.sub(r"newid\s*\(\s*\)", "gen_random_uuid()::text", text, flags=re.IGNORECASE)
# 2. remove square brackets
text=text.replace('[','').replace(']','')
# 3. replace MSSQL float -> double precision
text=re.sub(r"\bfloat\b","double precision",text,flags=re.IGNORECASE)
# 4. timestamp DEFAULT CURRENT_TIMESTAMP -> timestamptz DEFAULT now()
text=re.sub(r"timestamp\s+DEFAULT\s+CURRENT_TIMESTAMP","timestamptz DEFAULT now()",text,flags=re.IGNORECASE)
# 5. timestamp DEFAULT getutcdate() -> timestamptz DEFAULT timezone('utc', now())
text=re.sub(r"timestamp\s+DEFAULT\s+getutcdate\s*\(\s*\)","timestamptz DEFAULT timezone('utc', now())",text,flags=re.IGNORECASE)
# 6. timestamp DEFAULT getdate()/sysdatetime -> timestamptz DEFAULT now()
text=re.sub(r"timestamp\s+DEFAULT\s+getdate\s*\(\s*\)","timestamptz DEFAULT now()",text,flags=re.IGNORECASE)
text=re.sub(r"timestamp\s+DEFAULT\s+sysdatetime\s*\(\s*\)","timestamptz DEFAULT now()",text,flags=re.IGNORECASE)
# 7. generic: UpdatedDateTime timestamp NULL -> UpdatedDateTime timestamptz NULL
text=re.sub(r"UpdatedDateTime\s+timestamp\s+NULL","UpdatedDateTime timestamptz NULL",text)
text=re.sub(r"DeletedDateTime\s+timestamp\s+NULL","DeletedDateTime timestamptz NULL",text)
# 8. numeric defaults incorrectly set to boolean false/true
# replace patterns like "bigint DEFAULT false" -> "bigint DEFAULT 0"
text=re.sub(r"\b(bigint|int)\s+DEFAULT\s+false\b","\1 DEFAULT 0",text,flags=re.IGNORECASE)
text=re.sub(r"\b(bigint|int)\s+DEFAULT\s+true\b","\1 DEFAULT 1",text,flags=re.IGNORECASE)
# also specific fields RetryCount int DEFAULT false -> 0
text=re.sub(r"RetryCount\s+int\s+DEFAULT\s+false","RetryCount int DEFAULT 0",text,flags=re.IGNORECASE)
# ImageType int DEFAULT true -> 1
text=re.sub(r"ImageType\s+int\s+DEFAULT\s+true","ImageType int DEFAULT 1",text,flags=re.IGNORECASE)
# 9. Convert various remaining 'timestamp' column types for AddedDateTime etc.
text=re.sub(r"(AddedDateTime|CreatedDateTime|UpdatedDateTime)\s+timestamp\b", lambda m: f"{m.group(1)} timestamptz", text)
# 10. Ensure role table name without brackets
text=text.replace('Skolenza.[Role]','Skolenza.role')
text=text.replace('CREATE TABLE Skolenza.role (','CREATE TABLE Skolenza.role (')
# 11. Clean repeated spaces
text=re.sub(r" +"," ",text)
# write back
p.write_text(text,encoding='utf-8')
print('Cleanup applied')
