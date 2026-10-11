#!/usr/bin/env python3
import re
from pathlib import Path
ROOT = Path('.')
INPUT = ROOT / 'Skolenza.application' / 'DTOS' / 'SkolenzaPostgresScript.txt'
if not INPUT.exists():
    print('Input not found:', INPUT)
    raise SystemExit(1)
text = INPUT.read_text(encoding='utf-8')
# Replace CREATE TABLE Skolenza.Name -> CREATE TABLE Skolenza."Name"
text = re.sub(r'CREATE TABLE\s+Skolenza\.([A-Za-z0-9_]+)\s*\(', lambda m: f'CREATE TABLE Skolenza."{m.group(1)}" (', text)
# DROP TABLE
text = re.sub(r'DROP TABLE\s+Skolenza\.([A-Za-z0-9_]+);', lambda m: f'DROP TABLE Skolenza."{m.group(1)}";', text)
# ALTER TABLE Skolenza.Name -> quoted
text = re.sub(r'ALTER TABLE\s+Skolenza\.([A-Za-z0-9_]+)', lambda m: f'ALTER TABLE Skolenza."{m.group(1)}"', text)
# REFERENCES Skolenza.Name(...) -> quoted and quote columns inside parens
text = re.sub(r'REFERENCES\s+Skolenza\.([A-Za-z0-9_]+)\s*\(\s*([^\)]+)\s*\)', lambda m: 'REFERENCES Skolenza."%s"(%s)'%(m.group(1), ', '.join([f'"{c.strip()}"' for c in m.group(2).split(',')])), text)
# Inside CREATE TABLE, quote column names at line start (indent then identifier)
def quote_columns(match):
    body = match.group(0)
    # process each line
    def repl_col(line):
        m = re.match(r'(\s*)([A-Za-z0-9_]+)(\s+)(.+)', line)
        if m:
            col = m.group(2)
            rest = m.group(4)
            return f'{m.group(1)}"{col}"{m.group(3)}{rest}'
        return line
    lines = body.split('\n')
    out = '\n'.join(repl_col(l) for l in lines)
    return out
# Apply to each CREATE TABLE block
text = re.sub(r'CREATE TABLE Skolenza\."[A-Za-z0-9_]+" \([^\)]+\)\s*;', lambda m: m.group(0), text, flags=re.DOTALL)
# Better: find all CREATE TABLE blocks and process
pattern = re.compile(r'(CREATE TABLE Skolenza\."[A-Za-z0-9_]+" \([^\)]*\))', re.DOTALL)
new = ''
last = 0
for mo in pattern.finditer(text):
    new += text[last:mo.start()]
    block = mo.group(1)
    # quote column names inside block
    block_body = re.sub(r'\n', '\n', block)
    block_quoted = re.sub(r'(^\s*[A-Za-z0-9_]+\s+[^\n]+)', lambda m: '"' + m.group(0).lstrip().split()[0] + '"' + m.group(0)[len(m.group(0).lstrip().split()[0]):], block, flags=re.M)
    # The above is simplistic; instead call quote_columns on inner lines
    inner = block
    # find the content between parentheses
    start = inner.find('(')
    end = inner.rfind(')')
    header = inner[:start+1]
    content = inner[start+1:end]
    footer = inner[end:]
    # process content lines
    lines = content.split('\n')
    out_lines = []
    for line in lines:
        stripped = line.lstrip()
        if not stripped or stripped.startswith('--') or stripped.strip().upper().startswith('CONSTRAINT') or stripped.strip().upper().startswith('UNIQUE') or stripped.strip().upper().startswith('PRIMARY KEY'):
            out_lines.append(line)
            continue
        mcol = re.match(r'(\s*)([A-Za-z0-9_]+)(\s+)(.+)', line)
        if mcol:
            col = mcol.group(2)
            rest = mcol.group(4)
            out_lines.append(f'{mcol.group(1)}"{col}" {rest}')
        else:
            out_lines.append(line)
    new_block = header + '\n'.join(out_lines) + footer
    new += new_block
    last = mo.end()
new += text[last:]
text = new
# Quote identifiers in constraints and indexes: CONSTRAINT name -> CONSTRAINT "name"
text = re.sub(r'CONSTRAINT\s+([A-Za-z0-9_]+)', lambda m: f'CONSTRAINT "{m.group(1)}"', text)
# Quote columns in UNIQUE, PRIMARY KEY, CHECK lists
text = re.sub(r'UNIQUE\s*\(([^\)]+)\)', lambda m: 'UNIQUE (' + ', '.join([f'"{c.strip()}"' for c in m.group(1).split(',')]) + ')', text)
text = re.sub(r'PRIMARY KEY\s*\(([^\)]+)\)', lambda m: 'PRIMARY KEY (' + ', '.join([f'"{c.strip()}"' for c in m.group(1).split(',')]) + ')', text)
# CHECK (ExpectedSalary >= 0) -> quote identifier
text = re.sub(r'CHECK\s*\((\s*([A-Za-z0-9_]+)\s*[^\)]+)\)', lambda m: 'CHECK (' + re.sub(r'\b([A-Za-z0-9_]+)\b', lambda mm: f'"{mm.group(1)}"' if not mm.group(1).isdigit() and mm.group(1).upper() not in ('AND','OR','NOT') else mm.group(1), m.group(1)) + ')', text)
# References to columns elsewhere: replace (Id) -> ("Id") in REFERENCES or FK definitions already handled partly
text = re.sub(r'\((\s*Id\s*)\)', '("Id")', text)
# Write back
OUT = INPUT
OUT.write_text(text, encoding='utf-8')
print('Wrote quoted schema to', OUT)
