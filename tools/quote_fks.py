#!/usr/bin/env python3
import re
from pathlib import Path
p = Path('Skolenza.application') / 'DTOS' / 'SkolenzaPostgresScript.txt'
text = p.read_text(encoding='utf-8')
# Replace [Name] -> "Name"
text = re.sub(r"\[([A-Za-z0-9_]+)\]", lambda m: '"'+m.group(1)+'"', text)

# Function to quote identifiers inside parentheses: id1, id2 -> "id1", "id2"
def quote_list(inner):
    parts = [c.strip() for c in inner.split(',')]
    quoted = []
    for part in parts:
        # skip numeric literals or function calls or already quoted
        if re.match(r"^\d+$", part):
            quoted.append(part)
            continue
        if part.startswith('"') and part.endswith('"'):
            quoted.append(part)
            continue
        # remove any surrounding brackets (already handled) and surrounding schema.table if present
        # only take the column name (last part after dot)
        if '.' in part:
            part = part.split('.')[-1]
        # remove surrounding parentheses/spaces
        part = part.strip()
        # if part contains space (e.g., CONSTRAINT expressions), leave
        if re.search(r"\s", part):
            quoted.append(part)
            continue
        quoted.append('"'+part+'"')
    return ', '.join(quoted)

# Quote inside FOREIGN KEY (...)
text = re.sub(r'FOREIGN KEY\s*\(([^)]+)\)', lambda m: 'FOREIGN KEY (' + quote_list(m.group(1)) + ')', text)
# Quote inside REFERENCES (...)
text = re.sub(r'REFERENCES\s+([A-Za-z0-9_\"]+?)\s*\(([^)]+)\)', lambda m: f'REFERENCES {m.group(1)}(' + quote_list(m.group(2)) + ')', text)
# Quote inside UNIQUE (...)
text = re.sub(r'UNIQUE\s*\(([^)]+)\)', lambda m: 'UNIQUE (' + quote_list(m.group(1)) + ')', text)
# Quote inside PRIMARY KEY (...)
text = re.sub(r'PRIMARY KEY\s*\(([^)]+)\)', lambda m: 'PRIMARY KEY (' + quote_list(m.group(1)) + ')', text)
# Quote inside ALTER TABLE ... ADD CONSTRAINT ... CHECK (...)
text = re.sub(r'CHECK\s*\(([^)]+)\)', lambda m: 'CHECK (' + re.sub(r"\b([A-Za-z_][A-Za-z0-9_]*)\b", lambda mm: '"'+mm.group(1)+'"' if not mm.group(1).upper() in ('AND','OR','NOT') and not re.match(r"^\d+$", mm.group(1)) and not mm.group(1).startswith("'") else mm.group(1), m.group(1)) + ')', text)

# Also ensure constraint definitions have quoted names
text = re.sub(r'CONSTRAINT\s+"?([A-Za-z0-9_]+)"?', lambda m: 'CONSTRAINT "'+m.group(1)+'"', text)

p.write_text(text, encoding='utf-8')
print('Quoted FK/PK/UNIQUE identifiers in', p)
