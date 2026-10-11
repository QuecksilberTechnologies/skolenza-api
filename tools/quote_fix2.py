#!/usr/bin/env python3
from pathlib import Path
p = Path('Skolenza.application')/ 'DTOS' / 'SkolenzaPostgresScript.txt'
text = p.read_text(encoding='utf-8')
# Fix double double-quotes
text = text.replace('""', '"')
p.write_text(text, encoding='utf-8')
print('Fixed double quotes in', p)
