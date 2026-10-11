from PIL import Image,ImageOps,ImageDraw
from pathlib import Path
p=Path(r'C:\SkolenzaCodeBase\QuecksilberTechnologies\docs\generated\attendance-flow-render-v1')
files=sorted(p.glob('page-*.png'),key=lambda x:int(x.stem.split('-')[1]))
thumbs=[]
for i,f in enumerate(files,1):
 im=Image.open(f).convert('RGB'); im.thumbnail((360,510)); canvas=Image.new('RGB',(380,550),'white'); canvas.paste(im,((380-im.width)//2,25)); ImageDraw.Draw(canvas).text((10,5),f'Page {i}',fill='black'); thumbs.append(canvas)
out=Image.new('RGB',(380*3,550*5),(220,220,220))
for i,im in enumerate(thumbs): out.paste(im,((i%3)*380,(i//3)*550))
out.save(p/'contact-sheet.png')
