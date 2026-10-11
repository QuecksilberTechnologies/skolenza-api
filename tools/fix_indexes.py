#!/usr/bin/env python3
import re
from pathlib import Path
p = Path('Skolenza.application') / 'DTOS' / 'SkolenzaPostgresScript.txt'
text = p.read_text(encoding='utf-8')
# Pattern to match CREATE (UNIQUE )?NONCLUSTERED INDEX index_name ON Skolenza.Table ( cols ) ... ;
pattern = re.compile(r"CREATE\s+(UNIQUE\s+)?NONCLUSTERED\s+INDEX\s+([A-Za-z0-9_]+)\s+ON\s+Skolenza\.([A-Za-z0-9_]+)\s*\(([^\)]+)\)[\s\S]*?;", re.IGNORECASE)

def repl(m):
    unique = bool(m.group(1))
    idx = m.group(2)
    table = m.group(3)
    cols = m.group(4)
    # split cols by comma, remove ASC/DESC and bracket
    parts = [c.strip() for c in cols.split(',')]
    cols_clean = []
    for part in parts:
        # remove ASC/DESC and table prefix
        part = re.sub(r"\bASC\b|\bDESC\b", "", part, flags=re.IGNORECASE).strip()
        if '.' in part:
            part = part.split('.')[-1]
        # remove surrounding brackets and quotes
        part = part.strip(' []"')
        cols_clean.append(f'"{part}"')
    cols_sql = ', '.join(cols_clean)
    tbl = f'Skolenza."{table}"'
    uq = 'UNIQUE ' if unique else ''
    return f'CREATE {uq}INDEX IF NOT EXISTS "{idx}" ON {tbl} ({cols_sql});'

new = pattern.sub(repl, text)
# Also handle CREATE UNIQUE NONCLUSTERED INDEX with multiple columns (already handled)
# Handle CREATE NONCLUSTERED INDEX IX_District... earlier may have different case - pattern is case-insensitive
# Additionally replace standalone 'CREATE UNIQUE NONCLUSTERED INDEX' variations (with different spacing)

# Replace occurrences of CREATE NONCLUSTERED INDEX where ON Skolenza.Table may be missing quotes
# Also fix 'ON [PRIMARY ]' leftovers
new = re.sub(r'ON\s+\[PRIMARY\s*\]\s*;',';', new)
new = re.sub(r'ON\s+\[PRIMARY\s*\]','', new)
# Some index lines like 'CREATE UNIQUE NONCLUSTERED INDEX UQ_Tenant_Module_Operation ON Skolenza.TenantEnabledOperation (  TenantId ASC  , ModuleId ASC  , OperationId ASC  )  \n\t WITH ...' already replaced.
# Write back
p.write_text(new, encoding='utf-8')
print('Fixed indexes in', p)
