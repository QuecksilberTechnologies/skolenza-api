#!/usr/bin/env python3
import re
from pathlib import Path
p = Path('Skolenza.application') / 'DTOS' / 'SkolenzaPostgresScript.txt'
text = p.read_text(encoding='utf-8')
# Replace [Name] -> "Name"
text = re.sub(r"\[([A-Za-z0-9_]+)\]", lambda m: '"'+m.group(1)+'"', text)
# Replace Skolenza.Table -> Skolenza."Table" when not already quoted
text = re.sub(r'Skolenza\."?([A-Za-z0-9_]+)"?', lambda m: f'Skolenza."{m.group(1)}"', text)

# Function to quote identifiers in a comma list
def quote_list(s):
    parts = [c.strip() for c in s.split(',')]
    out = []
    for part in parts:
        # remove ASC/DESC
        part = re.sub(r"\bASC\b|\bDESC\b", '', part, flags=re.IGNORECASE).strip()
        # skip if already quoted or is numeric or contains space or is function or comparison
        if re.match(r'^".*"$', part) or re.match(r'^\d+$', part) or '(' in part or '=' in part or "'" in part:
            out.append(part)
            continue
        # remove schema prefix
        if '.' in part:
            part = part.split('.')[-1]
        part = part.strip(' []"')
        if part:
            out.append(f'"{part}"')
        else:
            out.append(part)
    return ', '.join(out)

# Quote columns inside FOREIGN KEY (...)
text = re.sub(r'FOREIGN KEY\s*\(([^)]+)\)', lambda m: 'FOREIGN KEY ('+quote_list(m.group(1))+')', text)
# Quote columns inside REFERENCES table(col1, col2)
text = re.sub(r'REFERENCES\s+([A-Za-z0-9_\"\.]+)\s*\(([^)]+)\)', lambda m: f'REFERENCES {m.group(1)}(' + quote_list(m.group(2)) + ')', text)
# Quote columns in UNIQUE(...)
text = re.sub(r'UNIQUE\s*\(([^)]+)\)', lambda m: 'UNIQUE ('+quote_list(m.group(1))+')', text)
# Quote columns in PRIMARY KEY(...)
text = re.sub(r'PRIMARY KEY\s*\(([^)]+)\)', lambda m: 'PRIMARY KEY ('+quote_list(m.group(1))+')', text)
# Quote columns in INDEX definitions (already handled but extra)
text = re.sub(r'ON\s+Skolenza\."?([A-Za-z0-9_]+)"?\s*\(([^)]+)\)', lambda m: f'ON Skolenza."{m.group(1)}"('+quote_list(m.group(2))+')', text)
# Fix CHECK clauses more safely: leave as-is but replace bracketed names
text = re.sub(r'CHECK\s*\(([^)]+)\)', lambda m: 'CHECK (' + re.sub(r'\b([A-Za-z_][A-Za-z0-9_]*)\b', lambda mm: '"'+mm.group(1)+'"' if not mm.group(1).upper() in ('AND','OR','NOT') and not re.match(r'^\d+$', mm.group(1)) and not mm.group(1).startswith("'") else mm.group(1), m.group(1)) + ')', text)

# Final cleanup: replace double double-quotes
text = text.replace('""', '"')

p.write_text(text, encoding='utf-8')
print('Applied final quoting fixes to', p)
