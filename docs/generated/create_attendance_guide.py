from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.section import WD_SECTION
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.style import WD_STYLE_TYPE
from docx.enum.text import WD_BREAK
from docx.enum.section import WD_ORIENT
from pathlib import Path

OUT=Path(r'C:\SkolenzaCodeBase\QuecksilberTechnologies\docs\generated\Employee_Attendance_Complete_Flow_Guide_Hinglish.docx')
doc=Document()
sec=doc.sections[0]
sec.top_margin=Inches(.58); sec.bottom_margin=Inches(.58); sec.left_margin=Inches(.75); sec.right_margin=Inches(.75)

styles=doc.styles
styles['Normal'].font.name='Aptos'; styles['Normal'].font.size=Pt(9.8); styles['Normal'].font.color.rgb=RGBColor(30,30,30)
styles['Normal'].paragraph_format.space_after=Pt(4.5); styles['Normal'].paragraph_format.line_spacing=1.06
for name,size in [('Title',28),('Heading 1',18),('Heading 2',13),('Heading 3',11)]:
    st=styles[name]; st.font.name='Aptos Display'; st.font.size=Pt(size); st.font.bold=True; st.font.color.rgb=RGBColor(0,0,0)
    st.paragraph_format.space_before=Pt(11 if name!='Title' else 0); st.paragraph_format.space_after=Pt(5)

if 'Flow Step' not in styles:
    fs=styles.add_style('Flow Step',WD_STYLE_TYPE.PARAGRAPH); fs.font.name='Aptos'; fs.font.size=Pt(11); fs.font.bold=True; fs.font.color.rgb=RGBColor(24,73,130); fs.paragraph_format.space_after=Pt(3)
if 'Small Note' not in styles:
    sn=styles.add_style('Small Note',WD_STYLE_TYPE.PARAGRAPH); sn.font.name='Aptos'; sn.font.size=Pt(9); sn.font.italic=True; sn.font.color.rgb=RGBColor(80,80,80); sn.paragraph_format.space_after=Pt(6)

# Remove built-in decorative paragraph borders around the title block.\nfor style_name in ('Title','Subtitle'):\n    pPr=styles[style_name]._element.get_or_add_pPr()\n    pBdr=pPr.find(qn('w:pBdr'))\n    if pBdr is not None: pPr.remove(pBdr)\n\n# Header/footer
header=sec.header.paragraphs[0]; header.text='Skolenza  Employee Attendance Flow'; header.alignment=WD_ALIGN_PARAGRAPH.RIGHT
for r in header.runs: r.font.name='Aptos'; r.font.size=Pt(8); r.font.color.rgb=RGBColor(100,100,100)
footer=sec.footer.paragraphs[0]; footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
run=footer.add_run('Internal implementation and UI handoff guide   |   ')
run.font.size=Pt(8); run.font.color.rgb=RGBColor(100,100,100)
fld=OxmlElement('w:fldSimple'); fld.set(qn('w:instr'),'PAGE'); footer._p.append(fld)

def keep(p):
    pPr=p._p.get_or_add_pPr(); el=OxmlElement('w:keepNext'); pPr.append(el)

def shade(cell,fill):
    tcPr=cell._tc.get_or_add_tcPr(); shd=tcPr.find(qn('w:shd'))
    if shd is None: shd=OxmlElement('w:shd'); tcPr.append(shd)
    shd.set(qn('w:fill'),fill)

def margins(cell,top=100,start=120,bottom=100,end=120):
    tc=cell._tc.get_or_add_tcPr(); tcMar=tc.first_child_found_in('w:tcMar')
    if tcMar is None: tcMar=OxmlElement('w:tcMar'); tc.append(tcMar)
    for tag,val in [('top',top),('start',start),('bottom',bottom),('end',end)]:
        node=tcMar.find(qn('w:'+tag))
        if node is None: node=OxmlElement('w:'+tag); tcMar.append(node)
        node.set(qn('w:w'),str(val)); node.set(qn('w:type'),'dxa')

def borders(table,color='D9D9D9'):
    tblPr=table._tbl.tblPr; tb=tblPr.first_child_found_in('w:tblBorders')
    if tb is None: tb=OxmlElement('w:tblBorders'); tblPr.append(tb)
    for edge in ('top','left','bottom','right','insideH','insideV'):
        el=OxmlElement('w:'+edge); el.set(qn('w:val'),'single'); el.set(qn('w:sz'),'6'); el.set(qn('w:color'),color); tb.append(el)

def table(headers,rows,widths=None):
    t=doc.add_table(rows=1,cols=len(headers)); t.alignment=WD_TABLE_ALIGNMENT.CENTER; t.autofit=False
    borders(t)
    for i,h in enumerate(headers):
        c=t.rows[0].cells[i]; c.text=str(h); shade(c,'1F4E78'); margins(c); c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
        for p in c.paragraphs:
            p.alignment=WD_ALIGN_PARAGRAPH.CENTER
            for r in p.runs: r.font.bold=True; r.font.color.rgb=RGBColor(255,255,255); r.font.size=Pt(8.5)
    for ri,row in enumerate(rows):
        cells=t.add_row().cells
        for i,v in enumerate(row):
            cells[i].text=str(v); margins(cells[i]); cells[i].vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            if ri%2: shade(cells[i],'F2F6FA')
            for p in cells[i].paragraphs:
                for r in p.runs: r.font.size=Pt(8.5)
                if len(str(v))<18: p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    if widths:
        for row in t.rows:
            for i,w in enumerate(widths): row.cells[i].width=Inches(w)
    doc.add_paragraph().paragraph_format.space_after=Pt(1)
    return t

def bullet(text,level=0):
    p=doc.add_paragraph(style='List Bullet' if level==0 else 'List Bullet 2'); p.add_run(text); return p

def number(text):
    p=doc.add_paragraph(style='List Number'); p.add_run(text); return p

def heading(text,level=1):
    p=doc.add_heading(text,level=level); keep(p); return p

def flow(lines):
    for i,line in enumerate(lines):
        p=doc.add_paragraph(style='Flow Step'); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run(line)
        if i<len(lines)-1:
            a=doc.add_paragraph('↓'); a.alignment=WD_ALIGN_PARAGRAPH.CENTER; a.paragraph_format.space_after=Pt(1); a.paragraph_format.space_before=Pt(0)

def page(): doc.add_page_break()

# Cover
p=doc.add_paragraph(); p.paragraph_format.space_before=Pt(55)
t=doc.add_paragraph('Employee Attendance Complete Flow Guide',style='Title'); t.alignment=WD_ALIGN_PARAGRAPH.CENTER
s=doc.add_paragraph('Admin setup se Mobile Web aur Biometric attendance tak',style='Subtitle'); s.alignment=WD_ALIGN_PARAGRAPH.CENTER
for r in s.runs: r.font.color.rgb=RGBColor(55,95,145); r.font.size=Pt(15)
p=doc.add_paragraph(); p.paragraph_format.space_before=Pt(30)
intro=doc.add_paragraph('यह guide Tenant Admin, UI developer और backend reviewer को एक ही flow में समझाती है कि employee create होने के बाद location, work arrangement, attendance policy, work pattern, override और punch आपस में कैसे जुड़ते हैं।')
intro.alignment=WD_ALIGN_PARAGRAPH.CENTER
scope=doc.add_paragraph('Main conclusion: Mobile और Web attendance biometric device के बिना चल सकती है। Client-site attendance के लिए active location, employee assignment, effective work arrangement और Mobile-enabled Published Attendance Policy आवश्यक हैं।')
scope.alignment=WD_ALIGN_PARAGRAPH.CENTER
p=doc.add_paragraph(); p.paragraph_format.space_before=Pt(55)
meta=table(['Document','Value'],[
('System','Skolenza Workforce Management'),('Subject','Employee attendance configuration and runtime flow'),('Audience','Tenant Admin  HR  UI Developer  API Developer'),('Version date','24 September 2026'),('Language','Simple Hinglish with technical field names')],[2.1,4.6])
page()

heading('1  Sabse pehle simple meaning',1)
doc.add_paragraph('पूरे flow को school example की तरह समझें। हर table या screen की एक अलग responsibility है। एक screen दूसरी screen का duplicate नहीं है।')
table(['Part','Simple meaning','Actual responsibility'],[
('Employee','Kaun kaam karega','Tenant ka active employee record'),
('Work Location','Kahan attendance lag sakti hai','Employee ko allowed physical locations'),
('Work Arrangement','Normal duty setup kya hai','Work mode primary location aur policy version'),
('Work Pattern','Kaunse din kaise kaam hoga','Weekly schedule office WFH aur weekly off'),
('Mode Override','Special date ka exception','Temporary approved mode/location change'),
('Attendance Policy','Rules kya hain','Mobile Web biometric GPS geofence scope'),
('Attendance Punch','Actual entry','Check In ya Check Out immutable event')],[1.3,2.0,3.4])
heading('Overall order',2)
flow(['Tenant locations and Published Attendance Policy','Employee creation','Employee Work Location Assignment','Employee Work Pattern','Employee Work Arrangement','Optional approved Work Mode Override','Employee Check In and Check Out','EmployeeAttendancePunch ledger'])

heading('2  Tenant ka one time setup',1)
heading('2.1 Locations create karna',2)
doc.add_paragraph('Tenant Admin पहले वे सभी places बनाएगा जहाँ employees काम या attendance कर सकते हैं। Head Office, Branch और Client Site सभी TenantLocation records हैं।')
table(['Field','Example','Why required'],[
('Location name','PANIPAT REFIN','UI और reports में पहचान'),('Location type','Client Site','Work mode compatibility'),('Time zone','India Standard Time','सही local work date'),('Latitude Longitude','Client site coordinates','GPS distance calculation'),('Geofence radius','200 metres','Allowed boundary'),('Attendance allowed','Yes','Punch permission'),('Active and not deleted','Yes','Runtime eligibility')],[1.6,2.0,3.1])
heading('2.2 Attendance Policy create aur publish karna',2)
doc.add_paragraph('Policy category ATTENDANCE होगी। Tenant Policy Type और Policy बनाएगा, version configure करेगा और उसे Published करेगा। Work Arrangement हमेशा exact PolicyVersionId से जुड़ेगा।')
table(['Configuration','Client site mobile recommendation'],[
('Attendance location scope','Assigned Locations'),('Allow Mobile','Yes'),('Allow Web','Tenant decision'),('Allow Biometric','No if device not purchased'),('Allow Manual','Only when admin correction flow is approved'),('Allow Work From Home','According to company rule'),('Office geofence required','Yes'),('Remote GPS required','According to company rule'),('Outside location with approval','According to approval process')],[2.8,3.9])
doc.add_paragraph('नई client site के लिए हमेशा नई policy बनाना जरूरी नहीं है। Existing Published policy में Mobile, Assigned Locations और required geofence rules सही हैं तो वही version use हो सकता है। Rules बदलें तो existing policy का नया version publish करें।',style='Small Note')
page()

heading('3  Employee add hone ke baad admin ka exact flow',1)
heading('Step 1  Employee create karein',2)
doc.add_paragraph('Employees screen से employee बनता है। Attendance API request में employee ID नहीं लेती; login token से authenticated employee पहचानती है।')
table(['Required state','Expected value'],[('Tenant ownership','Same authenticated Tenant'),('Employee status','Active'),('Soft deleted','False'),('Login','Employee self service के लिए active credentials')],[2.6,4.1])

heading('Step 2  Employee Work Location Assignment',2)
doc.add_paragraph('यह screen बताती है employee किन locations पर attendance कर सकता है। एक employee की multiple allowed locations हो सकती हैं, लेकिन effective date windows और primary location rules valid होने चाहिए।')
table(['Example assignment','Primary','Attendance','Effective period'],[
('Jabalpur Head Office','Yes','Allowed','12 Sep 2026 onward'),('PANIPAT REFIN Client Site','No','Allowed','30 Sep 2026 onward')],[2.6,1.0,1.2,2.0])
heading('Assignment validation',3)
for x in ['Employee और location same Tenant के हों।','Location active और non deleted हो।','IsAttendanceAllowed true हो।','Effective From और Effective To valid हों।','Same employee location की overlapping active assignment duplicate न हो।','Conflicting primary location windows न हों।']:
    bullet(x)

heading('Step 3  Employee Work Pattern',2)
doc.add_paragraph('Pattern normal weekly calendar है। इससे expected Office day, WFH day और weekly off पता चलता है।')
table(['Day','Example plan'],[('Monday','Client Site'),('Tuesday','Client Site'),('Wednesday','Client Site'),('Thursday','Client Site'),('Friday','Client Site'),('Saturday','Weekly Off'),('Sunday','Weekly Off')],[2.3,4.4])
doc.add_paragraph('Current boundary: Pattern CRUD/configuration मौजूद है, लेकिन वर्तमान mark attendance engine अभी weekly off, planned Office/WFH day, reporting time या late status enforce नहीं करता।',style='Small Note')

heading('Step 4  Employee Work Arrangement',2)
doc.add_paragraph('Arrangement employee का long running normal duty setup है। यह Work Mode, Primary Location और Published Attendance Policy Version को जोड़ता है।')
table(['Field','Client deployment example'],[('Employee','Rahul Sharma'),('Work mode','Client Site'),('Primary location','PANIPAT REFIN'),('Attendance policy','Published Mobile Client Site version'),('Effective From','01 Oct 2026'),('Effective To','31 Dec 2026'),('Active','Yes')],[2.3,4.4])
heading('Arrangement validation',3)
for x in ['Client Site mode के साथ Client Site location ही compatible हो।','Primary location employee की active attendance enabled assignment हो।','Location assignment पूरे arrangement period को cover करे।','Policy version Published और effective हो।','Policy physical location और selected source allow करे।','एक दिन पर exactly one active effective arrangement हो।']:
    bullet(x)
page()

heading('4  Work Mode Override kab use hoga',1)
doc.add_paragraph('Override temporary exception है। यह permanent Work Arrangement को edit नहीं करता। उदाहरण: employee normally Client Site पर है लेकिन एक दिन WFH approved है।')
table(['Normal setup','Temporary event','Override'],[
('Client Site','Site closed on 15 Oct','15 Oct Work From Home'),('Work From Home','Meeting at Head Office','20 Oct Office at Head Office'),('Head Office','Short client visit','Specified date Client Site')],[2.1,2.4,2.2])
heading('Expected runtime priority',2)
flow(['Approved active override for the date','Effective Work Pattern day','Effective Work Arrangement','Published Attendance Policy configuration'])
doc.add_paragraph('Current boundary: mark attendance repository अभी Work Mode Override को read नहीं करती। इसलिए saved override अभी punch validation में base arrangement को replace नहीं करता। यह runtime integration अलग से complete करना होगा।',style='Small Note')

heading('5  Biometric device nahi liya to kya hoga',1)
doc.add_paragraph('Mobile और Web attendance के लिए biometric device खरीदना या Employee Device Enrollment करना जरूरी नहीं है। Attendance Policy में Mobile या Web allowed होना चाहिए।')
table(['Situation','Result'],[
('No biometric device but Mobile allowed','Employee mobile से punch कर सकता है'),('No biometric device but Web allowed','Employee web से punch कर सकता है'),('Policy only allows Biometric','Mobile Web punch reject होगा'),('Biometric configured and employee enrolled','Trusted biometric ingestion flow use होगा'),('Biometric configured but employee not enrolled','उस device से employee match नहीं होगा')],[3.0,3.7])
page()
heading('Attendance device type master',2)
table(['Code','Meaning','Physical registration'],[('MOBILE','Authenticated mobile app','No'),('WEB','Authenticated web client','No'),('BIOMETRIC','Registered physical device','Yes'),('MANUAL','Authorized admin entry','No')],[1.4,3.5,1.8])
doc.add_paragraph('UI GET /api/Attendance/device-types से IDs लेगी। Numeric IDs hard code नहीं किए जाएंगे।',style='Small Note')
page()

heading('6  Client site par Mobile attendance ka complete example',1)
heading('Configuration checklist',2)
for x in ['PANIPAT REFIN को Client Site TenantLocation बनाएं।','Location पर attendance allowed, timezone, latitude, longitude और geofence radius configure करें।','Published Attendance Policy में Mobile और Assigned Locations allow करें।','Employee को PANIPAT REFIN location assign करें।','Employee का Client Site Work Arrangement बनाकर PANIPAT REFIN primary रखें।','Effective dates assignment, arrangement और policy में compatible रखें।','Work Pattern में applicable working days रखें।']:
    bullet(x)
heading('Employee app flow',2)
flow(['Employee logs in','GET Attendance device types','GET today attendance','App obtains GPS when required','Employee taps Check In','POST mark attendance','Server validates and saves punch','UI reloads today status','Employee later taps Check Out'])
heading('Sample request',2)
p=doc.add_paragraph(); r=p.add_run('POST /api/Attendance/mark-attendance'); r.bold=True
code='''{
  "action": 1,
  "attendanceDeviceTypeId": 1,
  "tenantLocationId": 25,
  "latitude": 29.3909,
  "longitude": 76.9635,
  "accuracyMeters": 15,
  "clientOccurredAt": "2026-10-01T09:05:00+05:30",
  "idempotencyKey": "661a418b-4294-4f87-bd82-d3f08712ba27"
}'''
p=doc.add_paragraph(); p.paragraph_format.left_indent=Inches(.25); p.paragraph_format.space_before=Pt(4); p.paragraph_format.space_after=Pt(8)
rr=p.add_run(code); rr.font.name='Consolas'; rr.font.size=Pt(8.5); rr.font.color.rgb=RGBColor(30,30,30)
doc.add_paragraph('action 1 = Check In और action 2 = Check Out। IDs केवल examples हैं; UI lookup APIs से actual IDs लेगी।',style='Small Note')

page()
heading('7  Punch ke waqt backend ki validation',1)
checks=[
('1','Bearer token','Request authenticated हो'),('2','Employee identity','Token का active non deleted employee मिले'),('3','Idempotency','Same button retry duplicate row न बनाए'),('4','Arrangement','आज exactly one effective arrangement हो'),('5','Policy configuration','Arrangement policy version की typed attendance config मिले'),('6','Device type','Active MOBILE या WEB source हो'),('7','Policy source','Selected source policy में allowed हो'),('8','Location','Physical mode में active attendance enabled location हो'),('9','Location scope','Primary या assigned location rule pass हो'),('10','Effective dates','Assignment आज valid हो'),('11','GPS','Required होने पर latitude longitude मिलें'),('12','Geofence','Employee permitted radius में हो'),('13','Work date','Location timezone से local date निकले'),('14','Punch order','Duplicate Check In और orphan Check Out reject हों'),('15','Persistence','Immutable punch transaction में save हो')]
table(['No','Validation','Pass condition'],checks,[.55,2.0,4.15])
page()

heading('8  Data kahan save hota hai',1)
heading('AttendancePolicyVersionConfiguration',2)
doc.add_paragraph('यह rules table है। इसमें employee punches नहीं आते। हर Published Attendance Policy version के Mobile, Web, Biometric, Manual, WFH, GPS, geofence और location scope rules रहते हैं।')
heading('EmployeeAttendancePunch',2)
doc.add_paragraph('हर successful Check In या Check Out की immutable entry यहाँ save होती है।')
table(['Column','Purpose'],[
('TenantId','Tenant isolation'),('EmployeeId','Authenticated employee'),('EmployeeWorkArrangementId','Applied arrangement'),('PolicyVersionId','Applied policy version'),('TenantLocationId','Physical location when applicable'),('AttendanceDeviceTypeId','Mobile Web Biometric or Manual source'),('WorkDate','Location timezone local date'),('OccurredAtUtc','Authoritative server time'),('PunchAction','Check In or Check Out'),('Latitude Longitude','Captured GPS'),('AccuracyMeters','Device reported accuracy'),('DistanceFromLocationMeters','Calculated geofence distance'),('IdempotencyKey','Safe retry key')],[2.4,4.3])
heading('Today status endpoint',2)
doc.add_paragraph('GET /api/Attendance/today आज की ordered punch timeline और isCurrentlyCheckedIn देता है। UI page load और successful punch के बाद इसे call करेगी।')

page()
heading('9  Common scenarios aur expected result',1)
table(['Scenario','Expected result'],[
('Mobile allowed no biometric purchased','Mobile punch accepted after other validations'),('Mobile disabled in policy','Rejected'),('Client location not assigned','Rejected for Assigned Locations scope'),('Location attendance disabled','Rejected'),('Employee outside geofence','Rejected'),('GPS missing when required','Rejected'),('Second Check In before Check Out','Rejected'),('Check Out without Check In','Rejected'),('Network retries same idempotency key','Original result reused no duplicate'),('Inactive employee','Rejected'),('No effective arrangement','Rejected'),('Two overlapping effective arrangements','Conflict rejected')],[3.2,3.5])
page()

heading('10  Admin operational checklist',1)
for x in ['Employee active और login enabled है।','Required Tenant locations active हैं।','Locations की timezone और geofence complete है।','Attendance Policy version Published है।','Required Mobile Web या Biometric source allowed है।','Employee location assignment effective है।','Primary assignment arrangement period cover करती है।','Work Arrangement में correct mode location और PolicyVersion selected है।','Work Pattern configured है।','Temporary changes approved Override में हैं।','UI device type IDs GET API से ले रही है।','Check In और Check Out explicit action भेजे जा रहे हैं।']:
    bullet(x)

heading('11  Current completion status and remaining runtime integration',1)
table(['Area','Status','Meaning'],[
('Location and arrangement consistency','Implemented','Cross record and effective date validation'),('Typed attendance policy config','Implemented','Version owned channel location and GPS rules'),('Mobile Web punch ledger','Implemented','Token identity geofence idempotency and punch order'),('Device type lookup and FK','Implemented','Seeded catalogue and GET API'),('Today status','Implemented','Current state and timeline'),('Work Pattern enforcement during punch','Pending integration','Weekly off planned mode timing and late rules'),('Override resolution during punch','Pending integration','Approved temporary mode/location must take priority'),('Biometric log ingestion','Separate flow','Requires trusted configured device and employee enrollment'),('Manual correction workflow','Separate flow','Needs authorized admin audit and approval rules')],[2.5,1.4,2.8])
doc.add_paragraph('यह distinction जरूरी है: configuration screens मौजूद होना और mark attendance runtime में उनका enforce होना अलग बातें हैं। UI को केवल implemented runtime behavior पर भरोसा करना चाहिए।',style='Small Note')

heading('12  Final simple example',1)
doc.add_paragraph('Rahul को Panipat client site भेजना है और biometric device नहीं खरीदा गया है। Admin Panipat को attendance enabled Client Site बनाता है, Mobile enabled Published Attendance Policy चुनता है, Rahul को Panipat assign करता है और Client Site arrangement बनाता है। Rahul mobile app में login करके GPS के साथ Check In करता है। API token, employee, arrangement, policy, source, assigned location और geofence check करके EmployeeAttendancePunch में entry बनाती है। शाम को explicit Check Out दूसरी entry बनाता है। Biometric enrollment इस flow में कहीं आवश्यक नहीं है।')

# table repeats and no split where possible
for t in doc.tables:
    trPr=t.rows[0]._tr.get_or_add_trPr(); rep=OxmlElement('w:tblHeader'); rep.set(qn('w:val'),'true'); trPr.append(rep)
    for row in t.rows:
        pr=row._tr.get_or_add_trPr(); cant=OxmlElement('w:cantSplit'); pr.append(cant)

# core props
doc.core_properties.title='Employee Attendance Complete Flow Guide'
doc.core_properties.subject='Skolenza employee attendance setup validation and punch flow'
doc.core_properties.author='Quecksilber Technologies'
doc.save(OUT)
print(OUT)



