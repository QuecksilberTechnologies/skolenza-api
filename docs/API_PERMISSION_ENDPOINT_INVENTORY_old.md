# Complete endpoint permission inventory

Generated: 2026-09-11T20:09:24.157Z

Source: https://skolenza-api.onrender.com/swagger/v1/swagger.json

All 442 deployed method/path combinations inspected. 295 statically resolved UI service calls indexed. No production write endpoints invoked.

An ID field in Swagger does not by itself prove it is runtime-required: some are resource IDs. "STATIC_MAPPING_PRESENT" only means route/field placement and global catalogue agree; it does not prove a role grant, a fresh menu, caller override or successful API execution. "NO_AUTOMATIC_UI_MAPPING" is a review item, not a confirmed missing request parameter. Dynamic UI expressions and non-service callers can remain unmatched.

## Counts

- NO_ID_FIELDS_IN_SWAGGER: 199
- NO_AUTOMATIC_UI_MAPPING: 42
- STATIC_MAPPING_PRESENT: 187
- DYNAMIC_MODULE_REQUIRES_PAYLOAD: 2
- TARGET_IDS_OR_SPECIAL_CONTRACT: 9
- UI_ACTION_ABSENT_FROM_CATALOGUE: 3

## Every deployed endpoint

| Method | Endpoint | ID fields (carrier:path) | UI mapping | Finding | Matching service callers |
|---|---|---|---|---|---|
| GET | /api/Asset/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/assets-api.ts:35 |
| POST | /api/Asset/add | form:ModuleId; form:OperationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/assets-api.ts:28 |
| PUT | /api/Asset/update | form:ModuleId; form:OperationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/assets-api.ts:41 |
| DELETE | /api/Asset/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/assets-api.ts:48 |
| POST | /api/Attendance/mark-attendance | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Attendance/timmy-test | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/AttendancePolicy/create | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/attendance-policy-api.ts:40 |
| GET | /api/AttendancePolicy/get-by-id/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/attendance-policy-api.ts:34 |
| GET | /api/AttendancePolicy/get-all | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/attendance-policy-api.ts:28 |
| POST | /api/AttendancePolicy/update | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/attendance-policy-api.ts:47 |
| POST | /api/AttendancePolicy/update-status | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/attendance-policy-api.ts:56 |
| DELETE | /api/AttendancePolicy/delete/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/attendance-policy-api.ts:62 |
| POST | /api/Auth/login | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Auth/refresh-token | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/auth-api.ts:178 |
| POST | /api/Auth/update-login-password | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/auth-api.ts:206 |
| POST | /api/Auth/resend-credential | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/auth-api.ts:219 |
| POST | /api/Auth/create-new-password | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/auth-api.ts:213 |
| POST | /api/Auth/forgot-password | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/auth-api.ts:185 |
| POST | /api/Auth/validate-forgot-password-otp | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/auth-api.ts:192 |
| POST | /api/Employee/Bank/create | form:ModuleId; form:OperationId | EMP_BANK / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-banks-api.ts:88 |
| GET | /api/Employee/Bank/get | query:ModuleId; query:OperationId | EMP_BANK / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-banks-api.ts:95 |
| DELETE | /api/Employee/Bank/delete | query:ModuleId; query:OperationId | EMP_BANK / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-banks-api.ts:108 |
| POST | /api/Employee/Bank/update | form:ModuleId; form:OperationId | EMP_BANK / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-banks-api.ts:101 |
| POST | /api/Category/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Category/getallmainchildcategory | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Asset/Category/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/asset-categories-api.ts:36 |
| POST | /api/Asset/Category/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/asset-categories-api.ts:29 |
| PUT | /api/Asset/Category/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| DELETE | /api/Asset/Category/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/asset-categories-api.ts:50 |
| GET | /api/Client/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Client/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Client/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/ClientInfo/detect-device | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/auth-api.ts:169 |
| GET | /api/CommonMenu | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/common-menu-api.ts:19 |
| POST | /api/CommonModule/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Company/{firstname}/{lastname} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/ComplianceRule/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Employee/Contact/create | body:moduleId; body:operationId | EMP_CONTACT / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-contacts-api.ts:78 |
| GET | /api/Employee/Contact/get | query:ModuleId; query:OperationId | EMP_CONTACT / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-contacts-api.ts:85 |
| POST | /api/Employee/Contact/update | body:moduleId; body:operationId | EMP_CONTACT / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-contacts-api.ts:91 |
| DELETE | /api/Employee/Contact/delete | query:ModuleId; query:OperationId | EMP_CONTACT / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-contacts-api.ts:98 |
| POST | /api/DefaultEmailConfig/create | body:permissionRequest.moduleId; body:permissionRequest.operationId | HOST_DEFAULT_EMAIL_CONFIG / Add / permissionRequest | STATIC_MAPPING_PRESENT | src/app/core/services/default-email-config-api.ts:39 |
| GET | /api/DefaultEmailConfig/get-all | query:ModuleId; query:OperationId | HOST_DEFAULT_EMAIL_CONFIG / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/default-email-config-api.ts:25 |
| GET | /api/DefaultEmailConfig/get-by-id/{id} | query:ModuleId; query:OperationId | HOST_DEFAULT_EMAIL_CONFIG / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/default-email-config-api.ts:31 |
| POST | /api/DefaultEmailConfig/update | body:permissionRequest.moduleId; body:permissionRequest.operationId | HOST_DEFAULT_EMAIL_CONFIG / Update / permissionRequest | STATIC_MAPPING_PRESENT | src/app/core/services/default-email-config-api.ts:49 |
| DELETE | /api/DefaultEmailConfig/delete/{id} | query:ModuleId; query:OperationId | HOST_DEFAULT_EMAIL_CONFIG / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/default-email-config-api.ts:58 |
| POST | /api/Department/bulk/preview | form:ModuleId; form:OperationId | DEPARTMENT / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Department/bulk/confirm | body:moduleId; body:operationId | DEPARTMENT / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Department/bulk/jobs/{jobId} | query:ModuleId; query:OperationId | DEPARTMENT / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Department/bulk/jobs | query:ModuleId; query:OperationId | DEPARTMENT / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Department/bulk/retry | body:moduleId; body:operationId | DEPARTMENT / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Department/bulk/cancel | body:moduleId; body:operationId | DEPARTMENT / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Department/bulk/template | query:ModuleId; query:OperationId | DEPARTMENT / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Department/bulk/jobs/{jobId}/report | query:ModuleId; query:OperationId | DEPARTMENT / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Department/get | query:ModuleId; query:OperationId | DEPARTMENT / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/departments-api.ts:33 |
| POST | /api/Department/add | body:moduleId; body:operationId | DEPARTMENT / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/departments-api.ts:26 |
| PUT | /api/Department/update | body:moduleId; body:operationId | DEPARTMENT / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/departments-api.ts:46 |
| GET | /api/Department/option | query:ModuleId; query:OperationId | DEPARTMENT / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/departments-api.ts:40 |
| DELETE | /api/Department/delete | query:ModuleId; query:OperationId | DEPARTMENT / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/departments-api.ts:53 |
| POST | /api/Employee/Dependent/create | form:ModuleId; form:OperationId | EMP_DEPENDENTS / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-dependent-api.ts:71 |
| GET | /api/Employee/Dependent/get | query:ModuleId; query:OperationId | EMP_DEPENDENTS / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-dependent-api.ts:78 |
| GET | /api/Employee/Dependent/get-in-detail | query:ModuleId; query:OperationId | EMP_DEPENDENTS / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-dependent-api.ts:87 |
| DELETE | /api/Employee/Dependent/delete | query:ModuleId; query:OperationId | EMP_DEPENDENTS / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-dependent-api.ts:102 |
| POST | /api/Employee/Dependent/update | form:ModuleId; form:OperationId | EMP_DEPENDENTS / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-dependent-api.ts:95 |
| POST | /api/Designation/bulk/preview | form:ModuleId; form:OperationId | DESIGNATION / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Designation/bulk/confirm | body:moduleId; body:operationId | DESIGNATION / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Designation/bulk/jobs/{jobId} | query:ModuleId; query:OperationId | DESIGNATION / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Designation/bulk/jobs | query:ModuleId; query:OperationId | DESIGNATION / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Designation/bulk/retry | body:moduleId; body:operationId | DESIGNATION / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Designation/bulk/cancel | body:moduleId; body:operationId | DESIGNATION / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Designation/bulk/template | query:ModuleId; query:OperationId | DESIGNATION / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Designation/bulk/jobs/{jobId}/report | query:ModuleId; query:OperationId | DESIGNATION / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Designation/get | query:ModuleId; query:OperationId | DESIGNATION / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/designations-api.ts:64 |
| POST | /api/Designation/Department/Group/get | body:moduleId; body:operationId | DESIGNATION / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Designation/option | query:ModuleId; query:OperationId | DESIGNATION / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/designations-api.ts:71 |
| POST | /api/Designation/add | body:moduleId; body:operationId | DESIGNATION / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/designations-api.ts:57 |
| DELETE | /api/Designation/delete | query:ModuleId; query:OperationId | DESIGNATION / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/designations-api.ts:84 |
| PUT | /api/Designation/update | body:moduleId; body:operationId | DESIGNATION / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/designations-api.ts:77 |
| POST | /api/device-commands/submit | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/device-ddl-options/time | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/bell | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/device-setup | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/advanced | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/lock | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/serial | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/ethernet | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/wifi | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/app-notification | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/employee-device-credentials | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/employee-device-access-windows | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/device-ddl-options/tenant-card-inventory | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/DeviceMaster/create | None declared | HOST_DEVICE_SETUP / Add / body | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/device-master-api.ts:39 |
| GET | /api/DeviceMaster/get-by-id/{id} | None declared | HOST_DEVICE_SETUP / View / query | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/device-master-api.ts:33 |
| GET | /api/DeviceMaster/get-info-by-sno/{sNo} | None declared | HOST_DEVICE_SETUP / View / query | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/DeviceMaster/get-all | None declared | HOST_DEVICE_SETUP / View / query | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/device-master-api.ts:27 |
| POST | /api/DeviceMaster/update | None declared | HOST_DEVICE_SETUP / Update / body | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/device-master-api.ts:46 |
| POST | /api/DeviceMaster/update-status | None declared | HOST_DEVICE_SETUP / Update / body | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/device-master-api.ts:52 |
| DELETE | /api/DeviceMaster/delete/{id} | None declared | HOST_DEVICE_SETUP / Delete / query | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/device-master-api.ts:58 |
| POST | /api/Employee/Education/create | form:ModuleId; form:OperationId | EMP_EDUCATION / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-education-api.ts:80 |
| GET | /api/Employee/Education/get | query:ModuleId; query:OperationId | EMP_EDUCATION / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-education-api.ts:87 |
| DELETE | /api/Employee/Education/delete | query:ModuleId; query:OperationId | EMP_EDUCATION / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-education-api.ts:100 |
| POST | /api/Employee/Education/update-education | query:ModuleId; query:OperationId | EMP_EDUCATION / Update / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-education-api.ts:93 |
| GET | /api/EmailTemplate/get-template-by-code | None declared | HOST_EMAIL_TEMPLATE / View / query | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/EmailTemplate/send-template | None declared | HOST_EMAIL_TEMPLATE / Update / body | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/EmailTemplate/create | body:permissionRequest.moduleId; body:permissionRequest.operationId | HOST_EMAIL_TEMPLATE / Add / permissionRequest | STATIC_MAPPING_PRESENT | src/app/core/services/email-template-api.ts:45 |
| GET | /api/EmailTemplate/get-all | query:ModuleId; query:OperationId | HOST_EMAIL_TEMPLATE / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/email-template-api.ts:33 |
| GET | /api/EmailTemplate/get-by-id/{id} | query:ModuleId; query:OperationId | HOST_EMAIL_TEMPLATE / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/email-template-api.ts:39 |
| POST | /api/EmailTemplate/update | body:permissionRequest.moduleId; body:permissionRequest.operationId | HOST_EMAIL_TEMPLATE / Update / permissionRequest | STATIC_MAPPING_PRESENT | src/app/core/services/email-template-api.ts:52 |
| POST | /api/EmailTemplate/update-status | body:permissionRequest.moduleId; body:permissionRequest.operationId | HOST_EMAIL_TEMPLATE / Update / permissionRequest | STATIC_MAPPING_PRESENT | src/app/core/services/email-template-api.ts:58 |
| DELETE | /api/EmailTemplate/delete/{id} | query:ModuleId; query:OperationId | HOST_EMAIL_TEMPLATE / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/email-template-api.ts:65 |
| POST | /api/Employee/bulk/send-invitations | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/Employee/bulk/preview | form:ModuleId; form:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/Employee/bulk/confirm | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/Employee/bulk/jobs/{jobId} | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/Employee/bulk/jobs | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/Employee/bulk/retry | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/Employee/bulk/cancel | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/Employee/bulk/template | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/Employee/bulk/jobs/{jobId}/report | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/Employee/create | body:moduleId; body:operationId | EMP_LIST / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:96 |
| POST | /api/Employee/profile/pic/update | form:ModuleId; form:OperationId | EMP_OVERVIEW / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-basic-api.ts:179 |
| GET | /api/Employee/Image/get | query:ModuleId; query:OperationId | EMP_OVERVIEW / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-basic-api.ts:173 |
| POST | /api/Employee/update-edit-status | body:moduleId; body:operationId | DYNAMIC_BODY_MODULE / Allow Editing / body | DYNAMIC_MODULE_REQUIRES_PAYLOAD | src/app/core/services/profile-access-api.ts:49 |
| POST | /api/Employee/update-verification-status | body:moduleId; body:operationId | DYNAMIC_BODY_MODULE / Verify / body | DYNAMIC_MODULE_REQUIRES_PAYLOAD | src/app/core/services/profile-access-api.ts:43 |
| POST | /api/Employee/update-bulk | body:moduleId; body:operationId | EMP_LIST / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:129 |
| POST | /api/Employee/reset-password | body:moduleId; body:operationId | EMP_LIST / Reset Password / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:172 |
| GET | /api/Employee/get-all-percentage | query:ModuleId; query:OperationId | EMP_OVERVIEW / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:123 |
| GET | /api/Employee/get | query:ModuleId; query:OperationId | EMP_OVERVIEW / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-basic-api.ts:135 |
| GET | /api/Employee/get-summary | query:ModuleId; query:OperationId | EMP_LIST / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:143 |
| GET | /api/Employee/get-profile-summary | query:ModuleId; query:OperationId | EMP_OVERVIEW / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-basic-api.ts:157; src/app/core/services/employee-basic-api.ts:186 |
| GET | /api/Employee/get-all | query:ModuleId; query:OperationId | EMP_LIST / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:103 |
| DELETE | /api/Employee/delete-all | query:ModuleId; query:OperationId | EMP_LIST / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:116 |
| PUT | /api/Employee/update-status | query:ModuleId; query:OperationId | EMP_LIST / Update / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:136 |
| POST | /api/Employee/update | body:moduleId; body:operationId | EMP_OVERVIEW / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-api.ts:109; src/app/core/services/employee-basic-api.ts:141 |
| POST | /api/Employee/official/update | body:moduleId; body:operationId | EMP_OVERVIEW / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-basic-api.ts:149 |
| POST | /api/EmployeeDeviceEnrollment/create | body:moduleId; body:operationId | EMP_DEVICES / Assign / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-device-enrollment-api.ts:55 |
| GET | /api/EmployeeDeviceEnrollment/get-by-id/{id} | query:ModuleId; query:OperationId | EMP_DEVICES / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-device-enrollment-api.ts:45 |
| GET | /api/EmployeeDeviceEnrollment/get-all | query:ModuleId; query:OperationId | EMP_DEVICES / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-device-enrollment-api.ts:35 |
| POST | /api/EmployeeDeviceEnrollment/update | body:moduleId; body:operationId | EMP_DEVICES / Assign / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-device-enrollment-api.ts:66 |
| POST | /api/EmployeeDeviceEnrollment/update-status | body:moduleId; body:operationId | EMP_DEVICES / Assign / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-device-enrollment-api.ts:76 |
| POST | /api/EmployeeDeviceEnrollment/face/upsert | form:ModuleId; form:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/EmployeeDeviceEnrollment/pin/upsert | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/EmployeeDeviceEnrollment/card/bind | body:moduleId; body:operationId | EMP_DEVICES / Assign / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-device-enrollment-api.ts:90 |
| POST | /api/EmployeeDeviceEnrollment/credential/remove | body:moduleId; body:operationId | EMP_DEVICES / Remove / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-device-enrollment-api.ts:99 |
| DELETE | /api/EmployeeDeviceEnrollment/delete/{id} | query:ModuleId; query:OperationId | EMP_DEVICES / Remove / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-device-enrollment-api.ts:107 |
| POST | /api/EmployeeLeavePolicy/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/EmployeeLeavePolicy/LeaveBalance/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/EmployeeLeavePolicy/map | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/EmployeeLeavePolicy/Mapped/Leave/Policy/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/EmployeeLeavePolicy/EmployeeLeavePolicy/Mapped/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/EmployeeLeavePolicy/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/EmployeeLocationAssignment/create | body:moduleId; body:operationId | EMP_WORK_LOCATIONS / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-location-assignment-api.ts:52 |
| GET | /api/EmployeeLocationAssignment/get-by-id/{id} | query:ModuleId; query:OperationId | EMP_WORK_LOCATIONS / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-location-assignment-api.ts:40 |
| GET | /api/EmployeeLocationAssignment/get-all | query:ModuleId; query:OperationId | EMP_WORK_LOCATIONS / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-location-assignment-api.ts:30 |
| POST | /api/EmployeeLocationAssignment/update | body:moduleId; body:operationId | EMP_WORK_LOCATIONS / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-location-assignment-api.ts:63 |
| POST | /api/EmployeeLocationAssignment/update-status | body:moduleId; body:operationId | EMP_WORK_LOCATIONS / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-location-assignment-api.ts:75 |
| DELETE | /api/EmployeeLocationAssignment/delete/{id} | query:ModuleId; query:OperationId | EMP_WORK_LOCATIONS / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-location-assignment-api.ts:85 |
| POST | /api/EmployeeType/bulk/preview | form:ModuleId; form:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/EmployeeType/bulk/confirm | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/EmployeeType/bulk/jobs/{jobId} | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/EmployeeType/bulk/jobs | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/EmployeeType/bulk/retry | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/EmployeeType/bulk/cancel | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/EmployeeType/bulk/template | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/EmployeeType/bulk/jobs/{jobId}/report | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/EmployeeType/add | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/EmployeeType/get | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/employee-types-api.ts:51 |
| GET | /api/EmployeeType/option | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/employee-types-api.ts:58 |
| PUT | /api/EmployeeType/update | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| DELETE | /api/EmployeeType/delete | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/EmployeeWorkArrangement/create | body:moduleId; body:operationId | EMP_WORK_ARRANGEMENT / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-arrangement-api.ts:53 |
| GET | /api/EmployeeWorkArrangement/get-by-id/{id} | query:ModuleId; query:OperationId | EMP_WORK_ARRANGEMENT / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-arrangement-api.ts:43 |
| GET | /api/EmployeeWorkArrangement/get-all | query:ModuleId; query:OperationId | EMP_WORK_ARRANGEMENT / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-arrangement-api.ts:35 |
| POST | /api/EmployeeWorkArrangement/update | body:moduleId; body:operationId | EMP_WORK_ARRANGEMENT / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-arrangement-api.ts:64 |
| POST | /api/EmployeeWorkArrangement/update-status | body:moduleId; body:operationId | EMP_WORK_ARRANGEMENT / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-arrangement-api.ts:75 |
| DELETE | /api/EmployeeWorkArrangement/delete/{id} | query:ModuleId; query:OperationId | EMP_WORK_ARRANGEMENT / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-arrangement-api.ts:85 |
| POST | /api/EmployeeWorkModeOverride/create | body:moduleId; body:operationId | EMP_OVERRIDES / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-mode-override-api.ts:58 |
| GET | /api/EmployeeWorkModeOverride/get-by-id/{id} | query:ModuleId; query:OperationId | EMP_OVERRIDES / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-mode-override-api.ts:48 |
| GET | /api/EmployeeWorkModeOverride/get-all | query:ModuleId; query:OperationId | EMP_OVERRIDES / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-mode-override-api.ts:38 |
| POST | /api/EmployeeWorkModeOverride/update | body:moduleId; body:operationId | EMP_OVERRIDES / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-mode-override-api.ts:69 |
| POST | /api/EmployeeWorkModeOverride/update-status | body:moduleId; body:operationId | EMP_OVERRIDES / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-mode-override-api.ts:79 |
| DELETE | /api/EmployeeWorkModeOverride/delete/{id} | query:ModuleId; query:OperationId | EMP_OVERRIDES / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-mode-override-api.ts:89 |
| POST | /api/EmployeeWorkPattern/create | body:moduleId; body:operationId | EMP_WORK_PATTERN / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-pattern-api.ts:42 |
| GET | /api/EmployeeWorkPattern/get-by-id/{id} | query:ModuleId; query:OperationId | EMP_WORK_PATTERN / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-pattern-api.ts:34 |
| GET | /api/EmployeeWorkPattern/get-all | query:ModuleId; query:OperationId | EMP_WORK_PATTERN / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-pattern-api.ts:28 |
| POST | /api/EmployeeWorkPattern/update | body:moduleId; body:operationId | EMP_WORK_PATTERN / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-pattern-api.ts:51 |
| POST | /api/EmployeeWorkPattern/update-status | body:moduleId; body:operationId | EMP_WORK_PATTERN / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-pattern-api.ts:59 |
| DELETE | /api/EmployeeWorkPattern/delete/{id} | query:ModuleId; query:OperationId | EMP_WORK_PATTERN / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-work-pattern-api.ts:67 |
| GET | /api/Entity/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Enum/get-all-currencies | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/enum-api.ts:29 |
| POST | /api/Employee/Experience/create | form:ModuleId; form:OperationId | EMP_EXPERIENCE / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-experience-api.ts:103 |
| GET | /api/Employee/Experience/get | query:ModuleId; query:OperationId | EMP_EXPERIENCE / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-experience-api.ts:97 |
| POST | /api/Employee/Experience/update | form:ModuleId; form:OperationId | EMP_EXPERIENCE / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-experience-api.ts:109 |
| DELETE | /api/Employee/Experience/delete | query:ModuleId; query:OperationId | EMP_EXPERIENCE / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-experience-api.ts:116 |
| DELETE | /api/Employee/Experience/delete-doc | query:ModuleId; query:OperationId | EMP_EXPERIENCE / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-experience-api.ts:122 |
| POST | /api/FileUpload/UploadAsset/upload | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Gender/option | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/users-api.ts:46 |
| GET | /api/Gender/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Holiday/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Host/create-host-user | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:71 |
| POST | /api/Host/create-host-role | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:101 |
| GET | /api/Host/get-host-user-by-id/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:45 |
| GET | /api/Host/get-all-host-users | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:37 |
| POST | /api/Host/update-host-user | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:77 |
| POST | /api/Host/delete-host-user | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:83 |
| POST | /api/Host/change-host-user-password | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:95 |
| POST | /api/Host/reset-host-user-password | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:89 |
| GET | /api/Host/get-host-role-by-id/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:57 |
| GET | /api/Host/get-all-host-roles | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:51 |
| POST | /api/Host/update-host-role | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:107 |
| POST | /api/Host/delete-host-role | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:113 |
| GET | /api/Host/get-host-modules | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:65 |
| GET | /api/Host/get-host-module-by-id/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/HostAccess/bootstrap | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/access-bootstrap-api.ts:27 |
| GET | /api/HostRolePermission/get-role-module-permissions/{hostRoleId} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/host-api.ts:124 |
| POST | /api/HostRolePermission/save-role-module-permissions | body:permissions[].moduleId; body:permissions[].operationId | None | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/host-api.ts:139 |
| POST | /api/Insurance/create | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policies-insurance-api.ts:68 |
| GET | /api/Insurance/get-ddl | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policies-insurance-api.ts:82 |
| GET | /api/Insurance/get-detail-ddl | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policies-insurance-api.ts:102 |
| GET | /api/Insurance/get-all | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policies-insurance-api.ts:75 |
| DELETE | /api/Insurance/delete | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/policies-insurance-api.ts:95 |
| PUT | /api/Insurance/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policies-insurance-api.ts:88 |
| POST | /api/Employee/Insurance/employee-insurance-enroll | body:moduleId; body:operationId | EMP_INSURANCE / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-insurance-api.ts:69 |
| DELETE | /api/Employee/Insurance/delete | body:moduleId; body:operationId | EMP_INSURANCE / Delete / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-insurance-api.ts:82 |
| GET | /api/Employee/Insurance/get-all-enroll | query:ModuleId; query:OperationId | EMP_INSURANCE / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-insurance-api.ts:76 |
| POST | /api/Leave/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Leave/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Leave/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Leave/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/LeaveRule/create | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/leave-rule.ts:42 |
| GET | /api/LeaveRule/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/leave-rule.ts:36 |
| GET | /api/LeaveRule/LeaveRule/Sandwich/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/LeaveRule/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/leave-rule.ts:48 |
| POST | /api/LeaveRule/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/leave-rule.ts:54 |
| GET | /api/Location/country/option | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/locations-api.ts:62 |
| GET | /api/Location/State/option | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/locations-api.ts:69 |
| GET | /api/Location/District/option | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/locations-api.ts:76 |
| POST | /api/MenuStructure/get-menus-structure | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/ModuleOperation/create | body:moduleId [Swagger required]; body:operationId [Swagger required] | None | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/module-operation-api.ts:37 |
| POST | /api/ModuleOperation/update | body:moduleId [Swagger required]; body:operationId [Swagger required] | None | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/module-operation-api.ts:44 |
| DELETE | /api/ModuleOperation/delete/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/module-operation-api.ts:50 |
| GET | /api/ModuleOperation/get-by-id/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/module-operation-api.ts:31 |
| GET | /api/ModuleOperation/get-all | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/module-operation-api.ts:25 |
| GET | /api/Navigation/my-menu | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/feature-page-api.ts:23 |
| POST | /api/NewLogin/login | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/auth-api.ts:161 |
| POST | /api/OperationsMaster/create-operation | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/operations-master-api.ts:37 |
| POST | /api/OperationsMaster/update-operation | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/operations-master-api.ts:44 |
| DELETE | /api/OperationsMaster/delete-operation/{operationId} | path:operationId [Swagger required] | None | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/operations-master-api.ts:50 |
| GET | /api/OperationsMaster/get-operation/{operationId} | path:operationId [Swagger required] | None | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/operations-master-api.ts:31 |
| GET | /api/OperationsMaster/get-all-operations | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/operations-master-api.ts:25 |
| GET | /api/Option/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/option-api.ts:30 |
| POST | /api/Option/create | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/option-api.ts:36 |
| POST | /api/Option/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/option-api.ts:42 |
| GET | /api/Option/has-access | query:OperationId | None | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/option-api.ts:49 |
| POST | /api/ParentModule/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/parent-module-api.ts:44 |
| PUT | /api/ParentModule/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/parent-module-api.ts:50 |
| GET | /api/ParentModule/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/parent-module-api.ts:38 |
| PATCH | /api/ParentModule/{id}/status | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/parent-module-api.ts:56 |
| GET | /api/ParentModule/get-module-headers | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/parent-module-api.ts:31 |
| GET | /api/PlanModuleMapping/options/{subscriptionPlanId} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/PlanModuleMapping/save | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/PolicyMappingLeaveType/map | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-mapping-leave-type-api.ts:82 |
| GET | /api/PolicyMappingLeaveType/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-mapping-leave-type-api.ts:49 |
| GET | /api/PolicyMappingLeaveType/LeavePolicy/EmployeeType/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/PolicyMappingLeaveType/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-mapping-leave-type-api.ts:94 |
| POST | /api/PolicyMappingLeaveType/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-mapping-leave-type-api.ts:106 |
| GET | /api/PolicyType/get-all | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-api.ts:80 |
| GET | /api/PolicyType/get-ddl | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-api.ts:87 |
| GET | /api/PolicyType/get-all-unstruct | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-api.ts:108 |
| POST | /api/PolicyType/create | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-api.ts:93 |
| POST | /api/PolicyType/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-api.ts:99 |
| DELETE | /api/PolicyType/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-api.ts:115 |
| DELETE | /api/PolicyType/delete-doc | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| POST | /api/PolicyTypeInsuranceMap/map | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-insurance-map-api.ts:87 |
| GET | /api/PolicyTypeInsuranceMap/get-all-map-insurance | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/PolicyTypeInsuranceMap/get-all | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-insurance-map-api.ts:62 |
| GET | /api/PolicyTypeInsuranceMap/get-details | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-insurance-map-api.ts:72 |
| DELETE | /api/PolicyTypeInsuranceMap/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-insurance-map-api.ts:106 |
| PUT | /api/PolicyTypeInsuranceMap/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/policy-type-insurance-map-api.ts:95 |
| GET | /api/ProjectDetail/get-all | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Registration/candidate | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Registration/AccessDetails | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/ReportingType/create | body:moduleId; body:operationId | TENANT_REPORTING_TYPES / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/report-type-api.ts:31 |
| GET | /api/ReportingType/get-all | None declared | TENANT_REPORTING_TYPES / View / query | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/report-type-api.ts:25 |
| GET | /api/ReportingType/get-by-id | None declared | TENANT_REPORTING_TYPES / View / query | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| PUT | /api/ReportingType/update | None declared | TENANT_REPORTING_TYPES / Update / body | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/report-type-api.ts:37 |
| DELETE | /api/ReportingType/delete | None declared | TENANT_REPORTING_TYPES / Delete / query | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/report-type-api.ts:44 |
| POST | /api/Role/bulk/preview | form:ModuleId; form:OperationId | ROLE / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Role/bulk/confirm | body:moduleId; body:operationId | ROLE / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Role/bulk/jobs/{jobId} | query:ModuleId; query:OperationId | ROLE / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Role/bulk/jobs | query:ModuleId; query:OperationId | ROLE / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Role/bulk/retry | body:moduleId; body:operationId | ROLE / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Role/bulk/cancel | body:moduleId; body:operationId | ROLE / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Role/bulk/template | query:ModuleId; query:OperationId | ROLE / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Role/bulk/jobs/{jobId}/report | query:ModuleId; query:OperationId | ROLE / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| PUT | /api/Role/update | body:moduleId; body:operationId | ROLE / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/roles-api.ts:123 |
| GET | /api/Role/option | query:ModuleId; query:OperationId | ROLE / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/roles-api.ts:117 |
| POST | /api/Role/add | body:moduleId; body:operationId | ROLE / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/roles-api.ts:103 |
| GET | /api/Role/get | query:ModuleId; query:OperationId | ROLE / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/roles-api.ts:110 |
| DELETE | /api/Role/delete | query:ModuleId; query:OperationId | ROLE / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/roles-api.ts:130 |
| POST | /Sandwich/DayCombination/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /Sandwich/DayCombination/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /Sandwich/DayCombination/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /Sandwich/DayCombination/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /Sandwich/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /Sandwich/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /Sandwich/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| DELETE | /Sandwich/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Employee/Sensitive/Create | form:ModuleId; form:OperationId | EMP_IDENTITY / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/employee-identity-api.ts:74 |
| GET | /api/Employee/Sensitive/get | query:ModuleId; query:OperationId | EMP_IDENTITY / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/employee-identity-api.ts:68 |
| GET | /api/StatData/Dashboard/Employees/Statistics | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/StatData/Manager/Statistics/Dashboard/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/dashboard-api.ts:58 |
| GET | /api/StatData/Manager/Statistic/Asset | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/dashboard-api.ts:65 |
| GET | /api/Asset/Status/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/asset-status-api.ts:34 |
| POST | /api/Asset/Status/add | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/asset-status-api.ts:27 |
| PUT | /api/Asset/Status/update | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/asset-status-api.ts:40 |
| DELETE | /api/Asset/Status/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/asset-status-api.ts:47 |
| POST | /api/SubModule/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/sub-module-api.ts:40 |
| PUT | /api/SubModule/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/sub-module-api.ts:46 |
| GET | /api/SubModule/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/sub-module-api.ts:34 |
| GET | /api/SubModule/list | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/sub-module-api.ts:27 |
| GET | /api/SubModule/parent/{parentModuleId} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| PATCH | /api/SubModule/{id}/status | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/sub-module-api.ts:52 |
| GET | /api/Subscription/get-all-subscription-plan | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Subscription/get-all-host-subscription-plans | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Subscription/get-tenant-subscription-plan-info | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Subscription/get-all-tenant-accessible-modules | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Subscription/add | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| PUT | /api/Subscription/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Subscription/delete-subscription-plan | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Tenant/add-employee-code-pattern | body:moduleId; body:operationId | HOST_TENANT_LIST / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| PUT | /api/Tenant/update-employee-code-pattern | body:moduleId; body:operationId | HOST_TENANT_LIST / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Tenant/create-tenant | body:moduleId; body:operationId | HOST_TENANT_LIST / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:104 |
| PUT | /api/Tenant/new-tenant-update-by-host/{encryptedTenantId} | query:ModuleId; query:OperationId | HOST_TENANT_LIST / Update / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:168 |
| POST | /api/Tenant/sync-active-plan-entitlements | body:moduleId; body:operationId | HOST_TENANT_LIST / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:215 |
| GET | /api/Tenant/{id}/delete-dependencies | query:ModuleId; query:OperationId | HOST_TENANT_LIST / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Tenant/new-tentant-creation-by-host | body:moduleId; body:operationId | HOST_TENANT_CREATE / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:116 |
| GET | /api/Tenant/get-all-tenants | query:ModuleId; query:OperationId | HOST_TENANT_LIST / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| GET | /api/Tenant/get-tenant-by-id | query:ModuleId; query:OperationId | HOST_TENANT_LIST / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:138 |
| PUT | /api/Tenant/{id} | query:ModuleId; query:OperationId; body:moduleId; body:operationId | HOST_TENANT_LIST / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| DELETE | /api/Tenant/{id} | query:ModuleId; query:OperationId | HOST_TENANT_LIST / Delete / query | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/Tenant/{id}/resend-verification-by-host | query:ModuleId; query:OperationId | HOST_TENANT_LIST / Update / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:207 |
| POST | /api/Tenant/update-tenant | body:moduleId; body:operationId | HOST_TENANT_LIST / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:174 |
| POST | /api/Tenant/activate-tenant | body:moduleId; body:operationId | HOST_TENANT_LIST / Active / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:186 |
| POST | /api/Tenant/deactivate-tenant | body:moduleId; body:operationId | HOST_TENANT_LIST / Inactive / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:192 |
| POST | /api/Tenant/delete-tenant | body:moduleId; body:operationId | HOST_TENANT_LIST / Delete / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:198 |
| POST | /api/Tenant/create-host-user | None declared | HOST_TENANT_LIST / Update / body | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Tenant/get-all-tenant-by-subscription-plan-Id | None declared | HOST_TENANT_LIST / View / query | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Tenant/get-employee-code-pattern | query:ModuleId; query:OperationId | HOST_TENANT_LIST / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenants-api.ts:153 |
| POST | /api/Tenant/get | None declared | HOST_TENANT_LIST / Update / body | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Tenant/get-all-tenant-operations | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/tenants-api.ts:145 |
| POST | /api/Tenant/update-modules-and-operations | body:modules[].moduleId; body:modules[].operations[].operationId | HOST_TENANT_LIST / Update / body | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/tenants-api.ts:180 |
| POST | /api/Tenant/verify | None declared | HOST_TENANT_LIST / Update / body | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/tenants-api.ts:123 |
| POST | /api/TenantCardMaster/create | body:moduleId; body:operationId | HOST_TENANT_CARD_INVENTORY / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-card-master-api.ts:43 |
| GET | /api/TenantCardMaster/get-by-id/{id} | query:ModuleId; query:OperationId | HOST_TENANT_CARD_INVENTORY / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-card-master-api.ts:37 |
| GET | /api/TenantCardMaster/get-all | query:ModuleId; query:OperationId | HOST_TENANT_CARD_INVENTORY / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-card-master-api.ts:27 |
| POST | /api/TenantCardMaster/update | body:moduleId; body:operationId | HOST_TENANT_CARD_INVENTORY / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-card-master-api.ts:50 |
| POST | /api/TenantCardMaster/update-status | body:moduleId; body:operationId | HOST_TENANT_CARD_INVENTORY / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-card-master-api.ts:58 |
| DELETE | /api/TenantCardMaster/delete/{id} | query:ModuleId; query:OperationId | HOST_TENANT_CARD_INVENTORY / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-card-master-api.ts:68 |
| POST | /api/TenantDevice/create | body:moduleId; body:operationId | TENANT_DEVICES / Add / body | UI_ACTION_ABSENT_FROM_CATALOGUE | src/app/core/services/tenant-device-api.ts:42 |
| GET | /api/TenantDevice/get-by-id/{id} | query:ModuleId; query:OperationId | TENANT_DEVICES / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-api.ts:36 |
| GET | /api/TenantDevice/get-all | query:ModuleId; query:OperationId | TENANT_DEVICES / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-api.ts:27 |
| POST | /api/TenantDevice/update | body:moduleId; body:operationId | TENANT_DEVICES / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-api.ts:49 |
| POST | /api/TenantDevice/update-status | body:moduleId; body:operationId | TENANT_DEVICES / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-api.ts:55 |
| POST | /api/TenantDevice/update-location | body:moduleId; body:operationId | TENANT_DEVICES / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| DELETE | /api/TenantDevice/delete/{id} | query:ModuleId; query:OperationId | TENANT_DEVICES / Delete / query | UI_ACTION_ABSENT_FROM_CATALOGUE | src/app/core/services/tenant-device-api.ts:63 |
| POST | /api/TenantDeviceConfiguration/issue-bootstrap-url | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:94 |
| POST | /api/TenantDeviceConfiguration/apply-runtime-configuration | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:215 |
| POST | /api/TenantDeviceConfiguration/reboot | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:221 |
| POST | /api/TenantDeviceConfiguration/settings/time | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:119 |
| POST | /api/TenantDeviceConfiguration/settings/time/sync | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:125 |
| POST | /api/TenantDeviceConfiguration/settings/bell | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:133 |
| POST | /api/TenantDeviceConfiguration/settings/device-setup | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:141 |
| POST | /api/TenantDeviceConfiguration/settings/advanced | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:149 |
| POST | /api/TenantDeviceConfiguration/settings/lock | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:157 |
| POST | /api/TenantDeviceConfiguration/settings/serial | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:165 |
| POST | /api/TenantDeviceConfiguration/settings/ethernet | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:173 |
| POST | /api/TenantDeviceConfiguration/settings/wifi | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:181 |
| POST | /api/TenantDeviceConfiguration/settings/app-notification | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:189 |
| POST | /api/TenantDeviceConfiguration/settings/web-access | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:199 |
| POST | /api/TenantDeviceConfiguration/settings/screen-menu-pin | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:207 |
| GET | /api/TenantDeviceConfiguration/gateway-address/{tenantDeviceId} | query:ModuleId; query:OperationId | TENANT_DEVICE_CONFIG / View / query | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/TenantDeviceConfiguration/replace-https-gateway-url | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/TenantDeviceConfiguration/dispatch-mqtts-now | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| POST | /api/TenantDeviceConfiguration/create | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:73 |
| GET | /api/TenantDeviceConfiguration/get-by-id/{id} | query:ModuleId; query:OperationId | TENANT_DEVICE_CONFIG / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:61 |
| GET | /api/TenantDeviceConfiguration/get-all | query:ModuleId; query:OperationId | TENANT_DEVICE_CONFIG / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:45 |
| POST | /api/TenantDeviceConfiguration/update | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-device-configuration-api.ts:82 |
| POST | /api/TenantDeviceConfiguration/rotate-https-ingress-token | body:moduleId; body:operationId | TENANT_DEVICE_CONFIG / Update / body | STATIC_MAPPING_PRESENT | Not statically matched |
| DELETE | /api/TenantDeviceConfiguration/delete/{id} | query:ModuleId; query:OperationId | TENANT_DEVICE_CONFIG / Delete / query | UI_ACTION_ABSENT_FROM_CATALOGUE | src/app/core/services/tenant-device-configuration-api.ts:105 |
| POST | /api/TenantEmailConfig/create | body:moduleId; body:operationId | TENANT_EMAIL_CONFIG / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-email-config-api.ts:42 |
| GET | /api/TenantEmailConfig/get-all | query:ModuleId; query:OperationId | TENANT_EMAIL_CONFIG / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-email-config-api.ts:30 |
| GET | /api/TenantEmailConfig/get-by-id/{id} | query:ModuleId; query:OperationId | TENANT_EMAIL_CONFIG / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-email-config-api.ts:36 |
| POST | /api/TenantEmailConfig/update | body:moduleId; body:operationId | TENANT_EMAIL_CONFIG / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-email-config-api.ts:52 |
| DELETE | /api/TenantEmailConfig/delete/{id} | query:ModuleId; query:OperationId | TENANT_EMAIL_CONFIG / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-email-config-api.ts:58 |
| GET | /api/TenantIndustry/get-industries | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/industries-api.ts:44 |
| GET | /api/TenantIndustry/get-tenant-subscription-plan | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/industries-api.ts:58 |
| POST | /api/TenantLocation/create | body:moduleId; body:operationId | TENANT_LOCATIONS / Add / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-location-api.ts:46 |
| GET | /api/TenantLocation/get-by-id/{id} | query:ModuleId; query:OperationId | TENANT_LOCATIONS / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-location-api.ts:40 |
| GET | /api/TenantLocation/get-all | query:ModuleId; query:OperationId | TENANT_LOCATIONS / View / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-location-api.ts:34 |
| POST | /api/TenantLocation/update | body:moduleId; body:operationId | TENANT_LOCATIONS / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-location-api.ts:53 |
| POST | /api/TenantLocation/update-status | body:moduleId; body:operationId | TENANT_LOCATIONS / Update / body | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-location-api.ts:62 |
| DELETE | /api/TenantLocation/delete/{id} | query:ModuleId; query:OperationId | TENANT_LOCATIONS / Delete / query | STATIC_MAPPING_PRESENT | src/app/core/services/tenant-location-api.ts:68 |
| GET | /api/TenantParentModule/get-module-headers | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/TenantParentModule/list | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/TenantParentModule/{id} | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| PATCH | /api/TenantParentModule/{id}/status | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/TenantUserAccess/bootstrap | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/access-bootstrap-api.ts:33 |
| POST | /api/TicketClassification/create | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| GET | /api/TicketClassification/all | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/TicketClassification/ddl-list | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/TicketClassification/get | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| PUT | /api/TicketClassification/update | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | Not statically matched |
| DELETE | /api/TicketClassification/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/TicketCreation/open | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Ticket/TicketHeader/create | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Ticket/TicketHeader/get-by-classification-id | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| PUT | /api/Ticket/TicketHeader/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| DELETE | /api/Ticket/TicketHeader/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Ticket/TicketType/create | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Ticket/TicketType/get-all | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Ticket/TicketType/ddl-list | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Ticket/TicketType/get-by-id | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| PUT | /api/Ticket/TicketType/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| DELETE | /api/Ticket/TicketType/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Ticket/TicketType/get-by-header-id | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Travel/getalltravelmodetype | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Travel/addtravelmode | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| POST | /api/Travel/updatetravelmodetype | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/Asset/Type/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/asset-types-api.ts:38 |
| POST | /api/Asset/Type/add | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/asset-types-api.ts:31 |
| PUT | /api/Asset/Type/update | body:moduleId; body:operationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/asset-types-api.ts:44 |
| DELETE | /api/Asset/Type/delete | query:ModuleId; query:OperationId | None | NO_AUTOMATIC_UI_MAPPING | src/app/core/services/asset-types-api.ts:51 |
| POST | /api/UserModuleRolePermission/assign-role-permissions | body:moduleId; body:operationId; body:moduleOperations[].moduleId; body:moduleOperations[].operations[].operationId | ROLE / Update / body | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/roles-api.ts:153 |
| GET | /api/UserModuleRolePermission/get-role-based-permissions | query:ModuleId; query:OperationId | ROLE / View / query | TARGET_IDS_OR_SPECIAL_CONTRACT | src/app/core/services/roles-api.ts:144 |
| POST | /api/UserRole/assign-roles-to-user | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/roles-api.ts:170 |
| GET | /api/UserRole/get-all-user-roles | None declared | None | NO_ID_FIELDS_IN_SWAGGER | src/app/core/services/roles-api.ts:164 |
| POST | /api/WorkflowStage/create | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/WorkflowStage/get-all | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| GET | /api/WorkflowStage/get | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| PUT | /api/WorkflowStage/update | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |
| DELETE | /api/WorkflowStage/delete | None declared | None | NO_ID_FIELDS_IN_SWAGGER | Not statically matched |

## UI calls absent from deployed Swagger

These may be stale routes, dynamic extraction limitations or backend paths omitted from OpenAPI. They are not automatically permission failures.

- POST /api/Asset/assign-employee — src/app/core/services/assets-api.ts:54
- POST /api/Auth/reset-login-new-password — src/app/core/services/auth-api.ts:199
- GET /api/device-ddl-options/{id} — src/app/core/services/device-ddl-options-api.ts:18
- GET /api/Role/get-permissions — src/app/core/services/employee-api.ts:158
- POST /api/Employee/assign-role — src/app/core/services/employee-api.ts:166
- POST /api/UserModuleRolePermission/assign-employee-permissions — src/app/core/services/employee-api.ts:189
- POST /api/Employee/Sensitive/update — src/app/core/services/employee-identity-api.ts:80
- POST /api/Leave/add-leave-type — src/app/core/services/leave-types.ts:44
- POST /api/Leave/update-leave-type — src/app/core/services/leave-types.ts:50
- POST /api/Leave/delete-leave-type — src/app/core/services/leave-types.ts:56
- GET /api/PolicyMappingLeaveType/get-by-employee-type-Id — src/app/core/services/policy-mapping-leave-type-api.ts:70
- GET /api/Role/get-permissions — src/app/core/services/roles-api.ts:137
- GET /api/Gender/Gender/get — src/app/core/services/users-api.ts:39
