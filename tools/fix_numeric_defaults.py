#!/usr/bin/env python3
import re
from pathlib import Path
p=Path('Skolenza.application')/'DTOS'/'SkolenzaPostgresScript.txt'
text=p.read_text(encoding='utf-8')
lines=text.splitlines()
out=[]
for line in lines:
    if 'DEFAULT false' in line:
        # find column type
        m=re.match(r"(\s*)\"([^\"]+)\"\s+([^\s]+)(.*)$", line)
        if m:
            indent, col, coltype, rest = m.groups()
            t=coltype.lower()
            if t.startswith('decimal') or t.startswith('numeric'):
                line = line.replace('DEFAULT false.00','DEFAULT 0.00').replace('DEFAULT false','DEFAULT 0.00')
            elif t in ('int','bigint','smallint','integer'):
                line = line.replace('DEFAULT false.00','DEFAULT 0').replace('DEFAULT false','DEFAULT 0')
            elif t in ('float','real','double','doubleprecision','double_precision','double-precision','double precision') or 'double' in t:
                line = line.replace('DEFAULT false.00','DEFAULT 0.0').replace('DEFAULT false','DEFAULT 0.0')
            elif t.startswith('timestamp') or t.startswith('timestamptz'):
                # leave booleans only
                pass
            else:
                # if type boolean, skip
                if 'boolean' in t:
                    pass
                else:
                    # fallback: replace DEFAULT false.00->0.00 and DEFAULT false->0
                    line = line.replace('DEFAULT false.00','DEFAULT 0.00').replace('DEFAULT false','DEFAULT 0')
    out.append(line)
new='\n'.join(out)
p.write_text(new,encoding='utf-8')
print('Fixed numeric defaults in',p)
