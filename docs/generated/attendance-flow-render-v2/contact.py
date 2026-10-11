from PIL import Image,ImageDraw
from pathlib import Path
p=Path(r'C:\SkolenzaCodeBase\QuecksilberTechnologies\docs\generated\attendance-flow-render-v2')
files=sorted(p.glob('page-*.png'),key=lambda x:int(x.stem.split('-')[1])); cells=[]
for i,f in enumerate(files,1):
 im=Image.open(f).convert('RGB'); im.thumbnail((360,510)); c=Image.new('RGB',(380,550),'white'); c.paste(im,((380-im.width)//2,25)); ImageDraw.Draw(c).text((10,5),f'Page {i}',fill='black'); cells.append(c)
out=Image.new('RGB',(1140,2200),(220,220,220))
for i,c in enumerate(cells): out.paste(c,((i%3)*380,(i//3)*550))
out.save(p/'contact-sheet.png')
