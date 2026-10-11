import pypdfium2 as pdfium
from pathlib import Path
p=Path(r'C:\SkolenzaCodeBase\QuecksilberTechnologies\docs\generated\attendance-flow-render-v1\Employee_Attendance_Complete_Flow_Guide_Hinglish.pdf')
out=p.parent
pdf=pdfium.PdfDocument(str(p))
print('PAGES',len(pdf))
for i,page in enumerate(pdf):
    img=page.render(scale=1.5).to_pil()
    img.save(out/f'page-{i+1}.png')
