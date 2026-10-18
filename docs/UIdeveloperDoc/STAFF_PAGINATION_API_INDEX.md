# Staff structure and curriculum: consolidated paginated API list

All entries are **GET**, under `/api/Staff`. These are **17 concrete list URL
variants across 10 route patterns**; `masters/{masterType}` serves multiple tabs.
Default: `PageNumber=1`, `PageSize=10`. Each uses the existing central
`QueryPagination.ToPageAsync` helper and `PaginationDefaults`.

| Area / tab | Path after `/api/Staff` |
| --- | --- |
| Professional — Job Families | `/professional-structure/masters/JobFamily` |
| Professional — Staff Categories | `/professional-structure/categories` |
| Professional — Capabilities | `/professional-structure/masters/Capability` |
| Professional — Duties | `/professional-structure/masters/DutyDefinition` |
| Professional — Positions | `/professional-structure/positions` |
| Academic — Session | `/academic-structure/masters/Session` |
| Academic — Programme | `/academic-structure/masters/Programme` |
| Academic — Group | `/academic-structure/masters/Group` |
| Academic — Section | `/academic-structure/masters/Section` |
| Academic — Subject | `/academic-structure/masters/Subject` |
| Academic — Course Offering | `/academic-structure/masters/CourseOffering` |
| Curriculum — Frameworks | `/curriculum-delivery/frameworks` |
| Curriculum — Units | `/curriculum-delivery/frameworks/{frameworkId}/units` |
| Curriculum — independent Topics | `/curriculum-delivery/units/{unitId}/topics` |
| Curriculum — Lesson Plans | `/curriculum-delivery/lesson-plans` |
| Curriculum — Delivery Logs | `/curriculum-delivery/lesson-deliveries` |
| Curriculum — Syllabus Progress | `/curriculum-delivery/syllabus-progress` |

Every request requires the existing authenticated institution bearer context and
current `STAFF_DIRECTORY` View module-operation IDs discovered from the existing
authenticated menu/permission flow. Do not hardcode permission IDs.

The root `data` remains an array with `pageNumber`, `pageSize`, `totalRecords`,
`totalPages`. There are no added staff/profile percentage or upload flags.
Existing topic coverage / syllabus progress percentages remain business data.
Positive custom page sizes work; nonpositive page/size values normalize to 1/10.
Beyond-last pages return an empty array with actual totals. Existing filters
apply before count and page. Each list has a deterministic unique Id tiebreaker.

Details, filters, examples and response/error contracts:

- [Professional Structure](STAFF_PROFESSIONAL_PAGINATION.md)
- [Academic Structure](ACADEMIC_STRUCTURE_AND_TEACHING_ASSIGNMENTS.md#academic-master-list-pagination--2026-10-18)
- [Curriculum and Lesson Delivery](CURRICULUM_PAGINATION.md)

Lookups such as academic teaching-assignment types, curriculum workflow statuses
and curriculum teaching-assignment options remain unpaged. This index covers
the structure/curriculum pagination work; it is not a list of every historically
paged API elsewhere in the project.

On 2026-10-18 all 17 variants returned authenticated local page 1/page 2 with
correct totals and no overlapping IDs. Affected backend/rollback suite: **41/41**;
Debug and Release builds: **zero errors**. [Current scenario and HTTP evidence](../testing/staff/academic-pagination/2026-10-18.md).
No Angular changes or browser pagination integration were performed. UI consumers
must implement page navigation and page-aware selectors; Render deployment and
deployed acceptance remain pending.
