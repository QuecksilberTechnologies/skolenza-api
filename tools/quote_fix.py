#!/usr/bin/env python3
import re
from pathlib import Path
ROOT = Path('.')
INPUT = ROOT / 'Skolenza.application' / 'DTOS' / 'SkolenzaPostgresScript.txt'
text = INPUT.read_text(encoding='utf-8')
# Find CREATE TABLE blocks
pattern = re.compile(r'CREATE TABLE Skolenza\."[A-Za-z0-9_]+" \((.*?)\)\s*;', re.DOTALL)

def quote_cols_in_block(match):
    body = match.group(1)
    lines = body.split('\n')
    out_lines = []
    for line in lines:
        stripped = line.lstrip()
        if not stripped:
            out_lines.append(line); continue
        up = stripped.upper()
        if up.startswith('CONSTRAINT') or up.startswith('UNIQUE') or up.startswith('PRIMARY KEY') or up.startswith('CHECK') or up.startswith(')'):
            # Also need to quote column names inside these lines later
            out_lines.append(line); continue
        # if already quoted column
        if re.match(r"\s*\"[A-Za-z0-9_]+\"", line):
            out_lines.append(line); continue
        # match column name
        m = re.match(r"(\s*)([A-Za-z][A-Za-z0-9_]*)(\s+)(.*)", line)
        if m:
            indent, col, sep, rest = m.groups()
            out_lines.append(f'{indent}"{col}"{sep}{rest}')
        else:
            out_lines.append(line)
    return '(' + '\n'.join(out_lines) + ')'

text = pattern.sub(lambda m: 'CREATE TABLE ' + re.search(r'CREATE TABLE (Skolenza\."[A-Za-z0-9_]+")', m.group(0)).group(1) + ' ' + quote_cols_in_block(m) + ';', text)

# Now quote remaining standalone column names in lines like UNIQUE (PhoneNumber) etc.
text = re.sub(r'UNIQUE\s*\(([^\)]+)\)', lambda m: 'UNIQUE (' + ', '.join([f'"{c.strip().strip("\"")}"' for c in m.group(1).split(',')]) + ')', text)
text = re.sub(r'PRIMARY KEY\s*\(([^\)]+)\)', lambda m: 'PRIMARY KEY (' + ', '.join([f'"{c.strip().strip("\"")}"' for c in m.group(1).split(',')]) + ')', text)
# Quote columns inside CHECK expressions (simple)
text = re.sub(r'CHECK\s*\(([^\)]+)\)', lambda m: 'CHECK (' + re.sub(r"\b([A-Za-z_][A-Za-z0-9_]*)\b", lambda mm: f'"{mm.group(1)}"' if not mm.group(1).isdigit() and mm.group(1).upper() not in ('AND','OR','NOT') and not mm.group(1).startswith("'") else mm.group(1), m.group(1)) + ')', text)

# Ensure REFERENCES have quoted table and quoted column names
text = re.sub(r'REFERENCES\s+Skolenza\."?([A-Za-z0-9_]+)"?\s*\(([^\)]+)\)', lambda m: f'REFERENCES Skolenza."{m.group(1)}"(' + ', '.join([f'"{c.strip().strip("\"")}"' for c in m.group(2).split(',')]) + ')', text)

# Quote any remaining unquoted identifiers at line starts in table blocks
# Already handled above; write back
INPUT.write_text(text, encoding='utf-8')
print('Fixed quoting in', INPUT)
