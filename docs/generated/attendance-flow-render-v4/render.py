import pypdfium2 as pdfium
from pathlib import Path
p=Path(r'C:\SkolenzaCodeBase\QuecksilberTechnologies\docs\generated\attendance-flow-render-v4\guide.pdf'); pdf=pdfium.PdfDocument(str(p)); print('PAGES',len(pdf))
for i,page in enumerate(pdf): page.render(scale=1.5).to_pil().save(p.parent/f'page-{i+1}.png')
