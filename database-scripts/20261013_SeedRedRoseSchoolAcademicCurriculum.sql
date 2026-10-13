BEGIN;

DO $$
BEGIN
    IF (SELECT COUNT(*) FROM skolenza."Institution" WHERE "InstitutionCode" = 'RRSMS' AND NOT COALESCE("IsSoftDeleted", false)) <> 1 THEN
        RAISE EXCEPTION 'Expected exactly one active institution with code RRSMS.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM skolenza."StaffMember" staff
        JOIN skolenza."Institution" institution ON institution."Id" = staff."InstitutionId"
        WHERE institution."InstitutionCode" = 'RRSMS'
          AND NOT COALESCE(staff."IsSoftDeleted", false)
    ) THEN
        RAISE EXCEPTION 'RRSMS requires an active staff member for audit ownership.';
    END IF;
END $$;

CREATE TEMP TABLE seed_context ON COMMIT DROP AS
SELECT
    institution."Id" AS institution_id,
    (
        SELECT staff."Id"
        FROM skolenza."StaffMember" staff
        WHERE staff."InstitutionId" = institution."Id"
          AND NOT COALESCE(staff."IsSoftDeleted", false)
        ORDER BY staff."Id"
        LIMIT 1
    ) AS actor_id
FROM skolenza."Institution" institution
WHERE institution."InstitutionCode" = 'RRSMS'
  AND NOT COALESCE(institution."IsSoftDeleted", false);

INSERT INTO skolenza."AcademicSessionMaster"
    ("InstitutionId", "CampusId", "ParentAcademicSessionId", "Code", "Name", "Description", "SessionType",
     "StartDate", "EndDate", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT institution_id, NULL, NULL, 'AY-2026-27', 'Academic Year 2026–27',
       'Primary, middle and secondary school academic session.', 1,
       DATE '2026-04-01', DATE '2027-03-31', true, false, actor_id, CURRENT_TIMESTAMP
FROM seed_context
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "SessionType" = EXCLUDED."SessionType",
    "StartDate" = EXCLUDED."StartDate",
    "EndDate" = EXCLUDED."EndDate",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

CREATE TEMP TABLE programme_seed
    (code varchar(60), name varchar(150), description varchar(500), level varchar(80)) ON COMMIT DROP;
INSERT INTO programme_seed VALUES
    ('PRIMARY', 'Primary School', 'Foundational and preparatory learning for Grades 1 to 5.', 'Grades 1–5'),
    ('MIDDLE', 'Middle School', 'Subject-based learning for Grades 6 to 8.', 'Grades 6–8'),
    ('SECONDARY', 'Secondary School', 'Secondary-stage learning for Grades 9 and 10.', 'Grades 9–10');

INSERT INTO skolenza."AcademicProgrammeMaster"
    ("InstitutionId", "Code", "Name", "Description", "Level", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT context.institution_id, seed.code, seed.name, seed.description, seed.level,
       true, false, context.actor_id, CURRENT_TIMESTAMP
FROM seed_context context CROSS JOIN programme_seed seed
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "Level" = EXCLUDED."Level",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

CREATE TEMP TABLE grade_seed
    (grade_no smallint, code varchar(60), name varchar(150), programme_code varchar(60)) ON COMMIT DROP;
INSERT INTO grade_seed
SELECT grade_no,
       'GRADE-' || lpad(grade_no::text, 2, '0'),
       'Grade ' || grade_no,
       CASE WHEN grade_no <= 5 THEN 'PRIMARY' WHEN grade_no <= 8 THEN 'MIDDLE' ELSE 'SECONDARY' END
FROM generate_series(1, 10) grade_no;

INSERT INTO skolenza."AcademicGroupMaster"
    ("InstitutionId", "AcademicSessionId", "AcademicProgrammeId", "ParentAcademicGroupId", "Code", "Name",
     "Description", "GroupType", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT context.institution_id, session."Id", programme."Id", NULL, grade.code, grade.name,
       grade.name || ' learning group for Academic Year 2026–27.', 1,
       true, false, context.actor_id, CURRENT_TIMESTAMP
FROM seed_context context
JOIN skolenza."AcademicSessionMaster" session
  ON session."InstitutionId" = context.institution_id AND session."Code" = 'AY-2026-27'
JOIN grade_seed grade ON true
JOIN skolenza."AcademicProgrammeMaster" programme
  ON programme."InstitutionId" = context.institution_id AND programme."Code" = grade.programme_code
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET
    "AcademicSessionId" = EXCLUDED."AcademicSessionId",
    "AcademicProgrammeId" = EXCLUDED."AcademicProgrammeId",
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "GroupType" = EXCLUDED."GroupType",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

INSERT INTO skolenza."ClassSectionMaster"
    ("InstitutionId", "AcademicGroupId", "CampusId", "Code", "Name", "Description", "Capacity",
     "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT context.institution_id, academic_group."Id", NULL,
       grade.code || '-' || section.section_code,
       grade.name || ' – Section ' || section.section_code,
       'Section ' || section.section_code || ' of ' || grade.name || '.', 35,
       true, false, context.actor_id, CURRENT_TIMESTAMP
FROM seed_context context
JOIN grade_seed grade ON true
JOIN skolenza."AcademicGroupMaster" academic_group
  ON academic_group."InstitutionId" = context.institution_id AND academic_group."Code" = grade.code
CROSS JOIN (VALUES ('A'), ('B')) section(section_code)
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET
    "AcademicGroupId" = EXCLUDED."AcademicGroupId",
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "Capacity" = EXCLUDED."Capacity",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

CREATE TEMP TABLE subject_seed
    (code varchar(60), name varchar(150), description varchar(500), subject_type smallint, periods smallint) ON COMMIT DROP;
INSERT INTO subject_seed VALUES
    ('ENG', 'English', 'Language, literature, reading, writing and communication.', 1, 6),
    ('HIN', 'Hindi', 'Hindi language, literature, reading, writing and communication.', 1, 6),
    ('MATH', 'Mathematics', 'Number sense, arithmetic, algebra, geometry and problem solving.', 1, 6),
    ('EVS', 'Environmental Studies', 'Self, community, nature, health and environmental awareness.', 1, 5),
    ('SCI', 'Science', 'Physical, life and environmental science through inquiry.', 1, 5),
    ('SST', 'Social Science', 'History, civics, geography and social understanding.', 1, 5),
    ('COMP', 'Computer Science', 'Digital literacy, productivity, computational thinking and safety.', 2, 2),
    ('ART', 'Art & Craft', 'Visual expression, creative practice and art appreciation.', 3, 2),
    ('PE', 'Physical Education', 'Fitness, movement, sports skills, teamwork and wellbeing.', 3, 3),
    ('GK', 'General Knowledge', 'Current awareness, India, the world and reasoning skills.', 2, 1);

INSERT INTO skolenza."SubjectMaster"
    ("InstitutionId", "Code", "Name", "Description", "SubjectType", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT context.institution_id, seed.code, seed.name, seed.description, seed.subject_type,
       true, false, context.actor_id, CURRENT_TIMESTAMP
FROM seed_context context CROSS JOIN subject_seed seed
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "SubjectType" = EXCLUDED."SubjectType",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

CREATE TEMP TABLE grade_subject_seed (grade_no smallint, subject_code varchar(60)) ON COMMIT DROP;
INSERT INTO grade_subject_seed
SELECT grade_no, subject_code
FROM generate_series(1, 2) grade_no
CROSS JOIN unnest(ARRAY['ENG','HIN','MATH','EVS','ART','PE']) subject_code
UNION ALL
SELECT grade_no, subject_code
FROM generate_series(3, 5) grade_no
CROSS JOIN unnest(ARRAY['ENG','HIN','MATH','EVS','COMP','ART','PE','GK']) subject_code
UNION ALL
SELECT grade_no, subject_code
FROM generate_series(6, 8) grade_no
CROSS JOIN unnest(ARRAY['ENG','HIN','MATH','SCI','SST','COMP','ART','PE']) subject_code
UNION ALL
SELECT grade_no, subject_code
FROM generate_series(9, 10) grade_no
CROSS JOIN unnest(ARRAY['ENG','HIN','MATH','SCI','SST','COMP','PE']) subject_code;

INSERT INTO skolenza."CourseOfferingMaster"
    ("InstitutionId", "AcademicSessionId", "AcademicGroupId", "SubjectId", "Code", "Name", "Description",
     "CurriculumVersion", "RequiredPeriodsPerWeek", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT context.institution_id, session."Id", academic_group."Id", subject."Id",
       grade.code || '-' || subject_seed.code,
       grade.name || ' – ' || subject_seed.name,
       subject_seed.name || ' course offering for ' || grade.name || ' in Academic Year 2026–27.',
       'RRSMS-2026-27', subject_seed.periods,
       true, false, context.actor_id, CURRENT_TIMESTAMP
FROM seed_context context
JOIN skolenza."AcademicSessionMaster" session
  ON session."InstitutionId" = context.institution_id AND session."Code" = 'AY-2026-27'
JOIN grade_subject_seed mapping ON true
JOIN grade_seed grade ON grade.grade_no = mapping.grade_no
JOIN skolenza."AcademicGroupMaster" academic_group
  ON academic_group."InstitutionId" = context.institution_id AND academic_group."Code" = grade.code
JOIN subject_seed ON subject_seed.code = mapping.subject_code
JOIN skolenza."SubjectMaster" subject
  ON subject."InstitutionId" = context.institution_id AND subject."Code" = mapping.subject_code
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET
    "AcademicSessionId" = EXCLUDED."AcademicSessionId",
    "AcademicGroupId" = EXCLUDED."AcademicGroupId",
    "SubjectId" = EXCLUDED."SubjectId",
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "CurriculumVersion" = EXCLUDED."CurriculumVersion",
    "RequiredPeriodsPerWeek" = EXCLUDED."RequiredPeriodsPerWeek",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

INSERT INTO skolenza."CurriculumFrameworkMaster"
    ("InstitutionId", "Code", "Name", "Description", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime",
     "AcademicSessionId", "AuthorityName", "Version", "EffectiveFrom", "EffectiveTo", "WorkflowStatusId",
     "ReviewedById", "ReviewedDateTime", "ReviewRemark")
SELECT context.institution_id, 'RRSMS-CURRICULUM-2026-27', 'School Curriculum Framework 2026–27',
       'Institution-owned teaching framework for Grades 1 to 10.', true, false, context.actor_id, CURRENT_TIMESTAMP,
       session."Id", 'Red Rose St Marry School Academic Council', '2026.1', DATE '2026-04-01', DATE '2027-03-31', 3,
       context.actor_id, CURRENT_TIMESTAMP, 'Approved starter framework for the 2026–27 academic session.'
FROM seed_context context
JOIN skolenza."AcademicSessionMaster" session
  ON session."InstitutionId" = context.institution_id AND session."Code" = 'AY-2026-27'
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "AcademicSessionId" = EXCLUDED."AcademicSessionId",
    "AuthorityName" = EXCLUDED."AuthorityName",
    "Version" = EXCLUDED."Version",
    "EffectiveFrom" = EXCLUDED."EffectiveFrom",
    "EffectiveTo" = EXCLUDED."EffectiveTo",
    "WorkflowStatusId" = EXCLUDED."WorkflowStatusId",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

CREATE TEMP TABLE curriculum_unit_seed
    (subject_code varchar(60), sequence smallint, unit_name varchar(150), learning_outcome varchar(1000)) ON COMMIT DROP;
INSERT INTO curriculum_unit_seed VALUES
    ('ENG',1,'Language Foundations','Use age-appropriate vocabulary and grammar accurately.'),
    ('ENG',2,'Reading & Comprehension','Read literary and informational texts with understanding.'),
    ('ENG',3,'Writing & Communication','Express ideas clearly through writing, speaking and listening.'),
    ('HIN',1,'भाषा की नींव','आयु-उपयुक्त शब्दावली और व्याकरण का सही प्रयोग करना।'),
    ('HIN',2,'पठन एवं बोध','साहित्यिक और सूचनात्मक पाठों को समझकर पढ़ना।'),
    ('HIN',3,'लेखन एवं संचार','लेखन, वाचन और श्रवण द्वारा विचार स्पष्ट रूप से व्यक्त करना।'),
    ('MATH',1,'Number Sense','Represent, compare and reason with numbers at the grade level.'),
    ('MATH',2,'Operations & Patterns','Apply operations, patterns and early algebraic reasoning.'),
    ('MATH',3,'Geometry & Measurement','Use shapes, space, measurement and data to solve problems.'),
    ('EVS',1,'Self & Community','Understand self, family, neighbourhood and responsible citizenship.'),
    ('EVS',2,'Natural World','Observe plants, animals, resources and seasonal patterns.'),
    ('EVS',3,'Health & Environment','Practice healthy habits and care for the environment.'),
    ('SCI',1,'Physical Science','Investigate matter, energy, force and everyday physical phenomena.'),
    ('SCI',2,'Life Science','Explain life processes, diversity and interdependence.'),
    ('SCI',3,'Earth & Environment','Relate earth systems, resources and sustainability.'),
    ('SST',1,'History & Heritage','Interpret chronology, evidence, heritage and historical change.'),
    ('SST',2,'Civics & Society','Understand institutions, rights, duties and social responsibility.'),
    ('SST',3,'Geography & Resources','Use spatial understanding to study people, places and resources.'),
    ('COMP',1,'Digital Foundations','Use computing devices and digital information responsibly.'),
    ('COMP',2,'Productivity & Communication','Create, organise and communicate digital work effectively.'),
    ('COMP',3,'Computational Thinking & Safety','Apply logical steps, introductory coding and cyber-safety practices.'),
    ('ART',1,'Visual Elements','Use line, shape, colour, texture and composition creatively.'),
    ('ART',2,'Creative Practice','Plan and produce artwork with varied materials and techniques.'),
    ('ART',3,'Art Appreciation','Observe, discuss and respect diverse art forms and traditions.'),
    ('PE',1,'Movement & Fitness','Develop coordination, endurance, strength and safe movement.'),
    ('PE',2,'Games & Teamwork','Apply rules, skills, cooperation and fair play in games.'),
    ('PE',3,'Health & Wellbeing','Build habits supporting physical and emotional wellbeing.'),
    ('GK',1,'India & the World','Build awareness of people, places, culture and institutions.'),
    ('GK',2,'Science & Current Awareness','Connect everyday science with age-appropriate current awareness.'),
    ('GK',3,'Reasoning & Life Skills','Use observation, logic and practical judgement in daily contexts.');

INSERT INTO skolenza."CurriculumUnit"
    ("InstitutionId", "Code", "Name", "Description", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime",
     "CurriculumFrameworkId", "CourseOfferingId", "ParentCurriculumUnitId", "Sequence", "PlannedPeriods", "LearningOutcome")
SELECT context.institution_id,
       offering."Code" || '-U' || lpad(unit.sequence::text, 2, '0'),
       academic_group."Name" || ' – ' || unit.unit_name,
       'Structured ' || subject."Name" || ' curriculum unit for ' || academic_group."Name" || '.',
       true, false, context.actor_id, CURRENT_TIMESTAMP,
       framework."Id", offering."Id", NULL, unit.sequence, 24, unit.learning_outcome
FROM seed_context context
JOIN skolenza."CurriculumFrameworkMaster" framework
  ON framework."InstitutionId" = context.institution_id AND framework."Code" = 'RRSMS-CURRICULUM-2026-27'
JOIN skolenza."CourseOfferingMaster" offering
  ON offering."InstitutionId" = context.institution_id AND offering."CurriculumVersion" = 'RRSMS-2026-27'
JOIN skolenza."AcademicGroupMaster" academic_group ON academic_group."Id" = offering."AcademicGroupId"
JOIN skolenza."SubjectMaster" subject ON subject."Id" = offering."SubjectId"
JOIN curriculum_unit_seed unit ON unit.subject_code = subject."Code"
ON CONFLICT ("InstitutionId", "CurriculumFrameworkId", "Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "CourseOfferingId" = EXCLUDED."CourseOfferingId",
    "Sequence" = EXCLUDED."Sequence",
    "PlannedPeriods" = EXCLUDED."PlannedPeriods",
    "LearningOutcome" = EXCLUDED."LearningOutcome",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

CREATE TEMP TABLE curriculum_topic_seed
    (subject_code varchar(60), unit_sequence smallint, topic_sequence smallint, topic_name varchar(150), learning_outcome varchar(1000)) ON COMMIT DROP;
INSERT INTO curriculum_topic_seed VALUES
    ('ENG',1,1,'Vocabulary in Context','Infer and use new words in meaningful contexts.'),('ENG',1,2,'Grammar in Use','Apply grade-appropriate grammar in speech and writing.'),('ENG',1,3,'Language Practice','Improve accuracy through guided and independent practice.'),
    ('ENG',2,1,'Literary Reading','Interpret characters, events, themes and language in literary texts.'),('ENG',2,2,'Informational Reading','Locate, connect and evaluate information in non-fiction texts.'),('ENG',2,3,'Comprehension Strategies','Use prediction, questioning, summarising and evidence.'),
    ('ENG',3,1,'Structured Writing','Plan, draft and improve grade-appropriate written work.'),('ENG',3,2,'Creative Expression','Develop original ideas through stories, poems and descriptions.'),('ENG',3,3,'Speaking & Listening','Communicate respectfully and respond to others with attention.'),
    ('HIN',1,1,'शब्दावली','संदर्भ के अनुसार नए शब्दों को समझना और प्रयोग करना।'),('HIN',1,2,'व्याकरण','बोलने और लिखने में कक्षा-उपयुक्त व्याकरण लागू करना।'),('HIN',1,3,'भाषा अभ्यास','निर्देशित और स्वतंत्र अभ्यास से भाषा की शुद्धता बढ़ाना।'),
    ('HIN',2,1,'साहित्यिक पठन','पात्र, घटनाएँ, भाव और भाषा को समझना।'),('HIN',2,2,'सूचनात्मक पठन','गैर-साहित्यिक पाठ से सूचना ढूँढना और जोड़ना।'),('HIN',2,3,'बोध रणनीतियाँ','अनुमान, प्रश्न, सार और पाठ-प्रमाण का प्रयोग करना।'),
    ('HIN',3,1,'संरचित लेखन','आयु-उपयुक्त लेखन की योजना, प्रारूप और सुधार करना।'),('HIN',3,2,'रचनात्मक अभिव्यक्ति','कहानी, कविता और वर्णन द्वारा मौलिक विचार व्यक्त करना।'),('HIN',3,3,'वाचन एवं श्रवण','ध्यान से सुनना और सम्मानपूर्वक संवाद करना।'),
    ('MATH',1,1,'Number Representation','Represent numbers in multiple concrete, pictorial and symbolic forms.'),('MATH',1,2,'Comparison & Estimation','Compare quantities and make reasonable estimates.'),('MATH',1,3,'Number Reasoning','Explain numerical relationships and solution choices.'),
    ('MATH',2,1,'Arithmetic Operations','Select and apply operations accurately.'),('MATH',2,2,'Patterns & Relationships','Recognise, extend and explain mathematical patterns.'),('MATH',2,3,'Problem Solving','Model and solve contextual mathematical problems.'),
    ('MATH',3,1,'Shapes & Space','Describe and reason about geometric figures and position.'),('MATH',3,2,'Measurement','Choose units and measure quantities accurately.'),('MATH',3,3,'Data Handling','Collect, represent and interpret grade-appropriate data.'),
    ('EVS',1,1,'Myself & Family','Recognise personal needs, relationships and responsibilities.'),('EVS',1,2,'Neighbourhood','Identify community places, people and services.'),('EVS',1,3,'Responsible Habits','Practice safety, respect and responsible participation.'),
    ('EVS',2,1,'Plants & Animals','Observe diversity, needs and relationships among living things.'),('EVS',2,2,'Weather & Seasons','Connect weather and seasonal change with everyday life.'),('EVS',2,3,'Natural Resources','Recognise uses and responsible care of natural resources.'),
    ('EVS',3,1,'Food, Health & Hygiene','Relate nutrition, hygiene and routines to health.'),('EVS',3,2,'Waste & Conservation','Reduce waste and conserve water, energy and materials.'),('EVS',3,3,'Environmental Action','Plan a simple action that improves the local environment.'),
    ('SCI',1,1,'Matter & Materials','Classify materials and explain observable properties.'),('SCI',1,2,'Force & Motion','Investigate motion, force and simple machines.'),('SCI',1,3,'Energy','Describe common forms, transfers and uses of energy.'),
    ('SCI',2,1,'Cells & Organisms','Connect structures with functions in living organisms.'),('SCI',2,2,'Life Processes','Explain nutrition, respiration, growth and reproduction.'),('SCI',2,3,'Ecosystems','Analyse relationships among organisms and environments.'),
    ('SCI',3,1,'Earth Systems','Relate earth, atmosphere and water systems.'),('SCI',3,2,'Resources','Evaluate renewable and non-renewable resource use.'),('SCI',3,3,'Sustainability','Use evidence to propose responsible environmental choices.'),
    ('SST',1,1,'Chronology & Change','Sequence events and explain continuity and change.'),('SST',1,2,'Sources & Evidence','Use sources to make supported historical interpretations.'),('SST',1,3,'Culture & Heritage','Recognise and respect diverse cultural heritage.'),
    ('SST',2,1,'Institutions','Describe the purpose and working of civic institutions.'),('SST',2,2,'Rights & Duties','Connect rights, duties and constitutional values.'),('SST',2,3,'Participation','Evaluate responsible participation in community life.'),
    ('SST',3,1,'Maps & Location','Read maps and apply location and scale concepts.'),('SST',3,2,'People & Places','Explain relationships between people and environments.'),('SST',3,3,'Resources & Development','Analyse resource distribution, use and sustainable development.'),
    ('COMP',1,1,'Devices & Interfaces','Identify computer components and use interfaces confidently.'),('COMP',1,2,'Files & Information','Organise files and locate reliable digital information.'),('COMP',1,3,'Digital Citizenship','Use technology respectfully, legally and safely.'),
    ('COMP',2,1,'Documents & Presentations','Create clear digital documents and presentations.'),('COMP',2,2,'Data & Spreadsheets','Organise, calculate and interpret simple datasets.'),('COMP',2,3,'Digital Collaboration','Communicate and collaborate responsibly with digital tools.'),
    ('COMP',3,1,'Algorithms','Break problems into ordered, testable steps.'),('COMP',3,2,'Programming Foundations','Build and debug age-appropriate programs.'),('COMP',3,3,'Cyber Safety','Protect identity, data, devices and online wellbeing.'),
    ('ART',1,1,'Line, Shape & Form','Explore visual elements through observation and practice.'),('ART',1,2,'Colour & Texture','Use colour and texture intentionally.'),('ART',1,3,'Composition','Arrange visual elements to communicate an idea.'),
    ('ART',2,1,'Drawing & Painting','Develop control and expression with drawing and painting media.'),('ART',2,2,'Craft & Construction','Design and construct work using suitable materials.'),('ART',2,3,'Creative Project','Plan, make, review and refine an original artwork.'),
    ('ART',3,1,'Indian Art Traditions','Recognise selected Indian art forms and contexts.'),('ART',3,2,'World Art','Observe varied artistic styles and purposes.'),('ART',3,3,'Reflection & Critique','Describe artistic choices respectfully using relevant vocabulary.'),
    ('PE',1,1,'Fundamental Movement','Perform locomotor and non-locomotor movements safely.'),('PE',1,2,'Fitness Components','Develop stamina, strength, flexibility and coordination.'),('PE',1,3,'Safe Practice','Warm up, cool down and use equipment responsibly.'),
    ('PE',2,1,'Game Skills','Apply sport-specific movement and ball skills.'),('PE',2,2,'Rules & Strategy','Use rules and simple tactics during play.'),('PE',2,3,'Teamwork & Fair Play','Cooperate, communicate and demonstrate sportsmanship.'),
    ('PE',3,1,'Healthy Lifestyle','Relate movement, rest, hydration and nutrition to wellbeing.'),('PE',3,2,'Personal Goals','Set, monitor and review realistic fitness goals.'),('PE',3,3,'Emotional Wellbeing','Use physical activity to support confidence and wellbeing.'),
    ('GK',1,1,'India','Recognise key features of Indian geography, culture and institutions.'),('GK',1,2,'The World','Build awareness of countries, people, places and global connections.'),('GK',1,3,'Culture & Events','Relate significant cultural and public events to daily life.'),
    ('GK',2,1,'Everyday Science','Apply science awareness to familiar situations.'),('GK',2,2,'Environment & Technology','Recognise current environmental and technology themes.'),('GK',2,3,'Age-appropriate Current Affairs','Discuss selected current events with context and care.'),
    ('GK',3,1,'Observation','Notice, classify and recall relevant information.'),('GK',3,2,'Logical Reasoning','Use patterns, clues and evidence to reach conclusions.'),('GK',3,3,'Practical Judgement','Apply awareness and reasoning to everyday choices.');

INSERT INTO skolenza."CurriculumTopic"
    ("InstitutionId", "Code", "Name", "Description", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime",
     "CurriculumUnitId", "Sequence", "PlannedPeriods", "LearningOutcome")
SELECT context.institution_id,
       unit."Code" || '-T' || lpad(topic.topic_sequence::text, 2, '0'),
       topic.topic_name,
       topic.topic_name || ' for ' || academic_group."Name" || ' ' || subject."Name" || '.',
       true, false, context.actor_id, CURRENT_TIMESTAMP,
       unit."Id", topic.topic_sequence, 8, topic.learning_outcome
FROM seed_context context
JOIN skolenza."CurriculumUnit" unit ON unit."InstitutionId" = context.institution_id
JOIN skolenza."CourseOfferingMaster" offering ON offering."Id" = unit."CourseOfferingId"
JOIN skolenza."AcademicGroupMaster" academic_group ON academic_group."Id" = offering."AcademicGroupId"
JOIN skolenza."SubjectMaster" subject ON subject."Id" = offering."SubjectId"
JOIN curriculum_topic_seed topic
  ON topic.subject_code = subject."Code" AND topic.unit_sequence = unit."Sequence"
JOIN skolenza."CurriculumFrameworkMaster" framework
  ON framework."Id" = unit."CurriculumFrameworkId" AND framework."Code" = 'RRSMS-CURRICULUM-2026-27'
ON CONFLICT ("InstitutionId", "CurriculumUnitId", "Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "Sequence" = EXCLUDED."Sequence",
    "PlannedPeriods" = EXCLUDED."PlannedPeriods",
    "LearningOutcome" = EXCLUDED."LearningOutcome",
    "IsActive" = true,
    "IsSoftDeleted" = false,
    "UpdatedById" = EXCLUDED."AddedById",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

COMMIT;
