#!/usr/bin/env python3
import re
from pathlib import Path
p=Path('Skolenza.application')/'DTOS'/'SPFunctions.txt'
text=p.read_text(encoding='utf-8')
# find all function headers
func_pattern=re.compile(r'(CREATE\s+OR\s+REPLACE\s+FUNCTION[\s\S]*?\)\s*\n)RETURNS',re.IGNORECASE)
# But easier: find each CREATE OR REPLACE FUNCTION ... AS $$ ... END; $$; block
block_pattern=re.compile(r'CREATE\s+OR\s+REPLACE\s+FUNCTION\s+Skolenza\."[^"]+"\s*\((.*?)\)\s*\nRETURNS[\s\S]*?AS \$\$\n(.*?)\nEND;\n\$\$;',re.IGNORECASE|re.DOTALL)
new=text
for m in block_pattern.finditer(text):
    params_block=m.group(1)
    body_block=m.group(2)
    # parse params: split by commas not inside parentheses
    params=[p.strip() for p in re.split(r',\s*(?![^()]*\))', params_block) if p.strip()]
    mapping=[]
    for param in params:
        # match quoted name
        mm=re.match(r'\s*"([A-Za-z0-9_]+)"\s+(.+)',param)
        if mm:
            orig=mm.group(1)
            # new param name
            newparam='p_'+orig.lower()
            mapping.append((orig,newparam))
    if not mapping:
        continue
    # create new params block text
    new_params_block=params_block
    for orig,newparam in mapping:
        # replace parameter declaration occurrences "Orig" with newparam (unquoted)
        new_params_block=re.sub(r'"'+orig+r'"', newparam, new_params_block)
    # now update body block: perform several replacements for RHS occurrences
    new_body=body_block
    for orig,newparam in mapping:
        # = "Orig"
        new_body=re.sub(r'=\s*"'+orig+r'"', '= '+newparam, new_body)
        # ("Orig",  -> (newparam,
        new_body=re.sub(r'\("'+orig+r'"\s*,', '(%s,'%newparam, new_body)
        # , "Orig") -> , newparam)
        new_body=re.sub(r',\s*"'+orig+r'"\s*\)', ', %s)'%newparam, new_body)
        # string_to_array("Orig",
        new_body=re.sub(r'string_to_array\(\s*"'+orig+r'"\s*,', 'string_to_array(%s,'%newparam, new_body)
        # unnest(string_to_array("Orig",
        new_body=re.sub(r'unnest\(string_to_array\(\s*"'+orig+r'"\s*,', 'unnest(string_to_array(%s,'%newparam, new_body)
        # IN ("Orig") -> IN (newparam)
        new_body=re.sub(r'IN\s*\(\s*"'+orig+r'"\s*\)', 'IN (%s)'%newparam, new_body)
        # WHERE "TenantId" = "TenantId" -> RHS replaced by param earlier via = pattern
        # COALESCE(..., "Orig") -> COALESCE(..., newparam)
        new_body=re.sub(r'COALESCE\(([^,\)]+),\s*"'+orig+r'"\)', lambda mm: 'COALESCE(%s, %s)'%(mm.group(1), newparam), new_body)
        # ON tem."TenantId" = "TenantId" -> RHS replaced by = pattern
    # now assemble replacement for the whole function block
    old_block=m.group(0)
    # construct new header: replace params_block with new_params_block inside old_block
    new_block=old_block.replace(params_block, new_params_block, 1)
    # replace body_block with new_body
    new_block=new_block.replace(body_block, new_body,1)
    new=new.replace(old_block,new_block,1)
# write back
p.write_text(new,encoding='utf-8')
print('Fixed parameter names in',p)
