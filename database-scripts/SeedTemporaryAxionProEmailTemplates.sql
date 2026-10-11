-- Temporary compatibility seed copied from the approved AxionPro EmailTemplate catalogue.
-- Replace branding/content later when the Skolenza email templates are approved.
-- Contains template content only. SMTP credentials are intentionally excluded.
BEGIN;

INSERT INTO skolenza."EmailTemplate" ("Id","TemplateName","TemplateCode","Subject","Body","FromEmail","FromName","CcEmail","BccEmail","Category","LanguageCode","IsActive","AddedById","AddedDateTime","UpdatedById","UpdatedDateTime","AddedFromIP","UpdatedFromIP")
VALUES (1,'Welcome Email','WELCOME_EMAIL','Welcome to the AxionPro System!','<p></p><p style="text-align: center;"><img src="http://localhost:4200/images/logo-small.svg" alt="" width="67" height="67"></p><p style="text-align: center;"></p><p style="text-align: center;"><strong>{{TenantName}}</strong></p><p>Dear <strong>{{UserName}}</strong>,</p><p><span style="color: rgb(153, 27, 27);">Welcome </span>to <strong>{{TenantName}}</strong>!</p><p></p><p>Your account has been <span style="background-color: rgb(250, 204, 21);">successfully </span>created by your organization''s <span style="color: rgb(245, 158, 11);">HR team</span>.</p><p><span style="color: rgb(21, 128, 61);">Please click the button below to set your password and activate your account.</span></p><p style="text-align: center;"><a target="_blank" rel="noopener" href="{{VerificationUrl}}"><strong>Set Your Password</strong></a></p><p></p><p>⏳ <strong>Link expiry:</strong> This link will expire in <strong>{{LinkExpiryMinutes}} minutes</strong>.</p><p>For security reasons, please do not share this link with anyone.</p><p></p><p>Regards,<br><strong>{{TenantName}} Team</strong></p><p style="text-align: center;">Need help? Contact us at <a target="_blank" rel="noopener" href="mailto:{{SupportEmail}}"><strong>{{SupportEmail}}</strong></a></p><p style="text-align: center;">© {{Year}} {{TenantName}}. All rights reserved.</p>','hr@quecksilber.in','AxionPro Notifications','sujeetjaiswara2012@gmail.com;mca.deepesh@gmail.com',NULL,'System','en','t',1,'2025-06-03 05:05:11.41+00',1,'2026-09-20 06:24:49.173122+00','192.168.1.100',NULL)
ON CONFLICT ("Id") DO UPDATE SET "TemplateName"=EXCLUDED."TemplateName","TemplateCode"=EXCLUDED."TemplateCode","Subject"=EXCLUDED."Subject","Body"=EXCLUDED."Body","FromEmail"=EXCLUDED."FromEmail","FromName"=EXCLUDED."FromName","CcEmail"=EXCLUDED."CcEmail","BccEmail"=EXCLUDED."BccEmail","Category"=EXCLUDED."Category","LanguageCode"=EXCLUDED."LanguageCode","IsActive"=EXCLUDED."IsActive","AddedById"=EXCLUDED."AddedById","AddedDateTime"=EXCLUDED."AddedDateTime","UpdatedById"=EXCLUDED."UpdatedById","UpdatedDateTime"=EXCLUDED."UpdatedDateTime","AddedFromIP"=EXCLUDED."AddedFromIP","UpdatedFromIP"=EXCLUDED."UpdatedFromIP";


INSERT INTO skolenza."EmailTemplate" ("Id","TemplateName","TemplateCode","Subject","Body","FromEmail","FromName","CcEmail","BccEmail","Category","LanguageCode","IsActive","AddedById","AddedDateTime","UpdatedById","UpdatedDateTime","AddedFromIP","UpdatedFromIP")
VALUES (2,'Forgot Password','FORGOT_PASSWORD','Reset Your AxionPro Password','<html>

  <body style=''font-family: Arial, sans-serif; padding: 20px;''>

    <h2 style=''color: #2E86C1;''>Dear {{UserName}},</h2>

    <p style=''font-size: 16px;''>

        This is a <strong>verification OTP</strong> sent by <em>Axion-Pro</em>.

        The OTP will expire in <strong>5 minutes</strong>.

    </p>

    <p style=''font-size: 20px; font-weight: bold; color: #e74c3c;''>OTP: {{Otp}}</p>

    <p style=''font-size: 14px; color: gray;''>

        Regards,<br/>

        <b>Axion-Pro Team</b>

    </p>

  </body>

</html>

','hr@quecksilber.in','AxionPro Support',NULL,NULL,'System','en','t',1,'2025-06-03 05:05:11.41+00',NULL,NULL,'192.168.1.100',NULL)
ON CONFLICT ("Id") DO UPDATE SET "TemplateName"=EXCLUDED."TemplateName","TemplateCode"=EXCLUDED."TemplateCode","Subject"=EXCLUDED."Subject","Body"=EXCLUDED."Body","FromEmail"=EXCLUDED."FromEmail","FromName"=EXCLUDED."FromName","CcEmail"=EXCLUDED."CcEmail","BccEmail"=EXCLUDED."BccEmail","Category"=EXCLUDED."Category","LanguageCode"=EXCLUDED."LanguageCode","IsActive"=EXCLUDED."IsActive","AddedById"=EXCLUDED."AddedById","AddedDateTime"=EXCLUDED."AddedDateTime","UpdatedById"=EXCLUDED."UpdatedById","UpdatedDateTime"=EXCLUDED."UpdatedDateTime","AddedFromIP"=EXCLUDED."AddedFromIP","UpdatedFromIP"=EXCLUDED."UpdatedFromIP";


INSERT INTO skolenza."EmailTemplate" ("Id","TemplateName","TemplateCode","Subject","Body","FromEmail","FromName","CcEmail","BccEmail","Category","LanguageCode","IsActive","AddedById","AddedDateTime","UpdatedById","UpdatedDateTime","AddedFromIP","UpdatedFromIP")
VALUES (3,'Birthday Wish','BIRTHDAY_WISH','Happy Birthday {{UserName}}!','Dear {{UserName}},<br/><br/>Wishing you a fantastic birthday and a wonderful year ahead!<br/><br/>Best wishes,<br/>HR Team','hr@quecksilber.in','HR Department',NULL,NULL,'HR','en','t',1,'2025-06-03 05:05:11.41+00',1,'2026-09-09 09:46:04.578002+00','192.168.1.100',NULL)
ON CONFLICT ("Id") DO UPDATE SET "TemplateName"=EXCLUDED."TemplateName","TemplateCode"=EXCLUDED."TemplateCode","Subject"=EXCLUDED."Subject","Body"=EXCLUDED."Body","FromEmail"=EXCLUDED."FromEmail","FromName"=EXCLUDED."FromName","CcEmail"=EXCLUDED."CcEmail","BccEmail"=EXCLUDED."BccEmail","Category"=EXCLUDED."Category","LanguageCode"=EXCLUDED."LanguageCode","IsActive"=EXCLUDED."IsActive","AddedById"=EXCLUDED."AddedById","AddedDateTime"=EXCLUDED."AddedDateTime","UpdatedById"=EXCLUDED."UpdatedById","UpdatedDateTime"=EXCLUDED."UpdatedDateTime","AddedFromIP"=EXCLUDED."AddedFromIP","UpdatedFromIP"=EXCLUDED."UpdatedFromIP";


INSERT INTO skolenza."EmailTemplate" ("Id","TemplateName","TemplateCode","Subject","Body","FromEmail","FromName","CcEmail","BccEmail","Category","LanguageCode","IsActive","AddedById","AddedDateTime","UpdatedById","UpdatedDateTime","AddedFromIP","UpdatedFromIP")
VALUES (4,'Leave Approved','LEAVE_APPROVAL','Your Leave Request has been Approved','Hello {{UserName}},<br/><br/>Your leave request from {{LeaveStartDate}} to {{LeaveEndDate}} has been approved.<br/><br/>Regards,<br/>HR Department','hr@quecksilber.in','HR Department',NULL,NULL,'HR','en','t',1,'2025-06-03 05:05:11.41+00',NULL,NULL,'192.168.1.100',NULL)
ON CONFLICT ("Id") DO UPDATE SET "TemplateName"=EXCLUDED."TemplateName","TemplateCode"=EXCLUDED."TemplateCode","Subject"=EXCLUDED."Subject","Body"=EXCLUDED."Body","FromEmail"=EXCLUDED."FromEmail","FromName"=EXCLUDED."FromName","CcEmail"=EXCLUDED."CcEmail","BccEmail"=EXCLUDED."BccEmail","Category"=EXCLUDED."Category","LanguageCode"=EXCLUDED."LanguageCode","IsActive"=EXCLUDED."IsActive","AddedById"=EXCLUDED."AddedById","AddedDateTime"=EXCLUDED."AddedDateTime","UpdatedById"=EXCLUDED."UpdatedById","UpdatedDateTime"=EXCLUDED."UpdatedDateTime","AddedFromIP"=EXCLUDED."AddedFromIP","UpdatedFromIP"=EXCLUDED."UpdatedFromIP";


INSERT INTO skolenza."EmailTemplate" ("Id","TemplateName","TemplateCode","Subject","Body","FromEmail","FromName","CcEmail","BccEmail","Category","LanguageCode","IsActive","AddedById","AddedDateTime","UpdatedById","UpdatedDateTime","AddedFromIP","UpdatedFromIP")
VALUES (5,'Account Verification','ACCOUNT_VERIFICATION','Verify Your AxionProAccount','<p></p><p style="text-align: center;"><img src="https://brandlogos.net/wp-content/uploads/2023/10/vi-vodafone-idea-logo.png" alt="" width="63" height="62"></p><p></p><p></p><p>Dear {{UserName}},<br><br>Thank you for registering with EMS.<br>Please <a target="_blank" rel="noopener" href="{{VerificationLink}}">click here</a> to verify your account.<br><br>If you did not sign up, please ignore this email.<br><br>Regards,<br>EMS Support Team</p>','hr@quecksilber.in','AxionPro Support',NULL,NULL,'System','en','t',1,'2025-06-03 05:05:11.41+00',1,'2026-09-22 20:36:53.663557+00','192.168.1.100',NULL)
ON CONFLICT ("Id") DO UPDATE SET "TemplateName"=EXCLUDED."TemplateName","TemplateCode"=EXCLUDED."TemplateCode","Subject"=EXCLUDED."Subject","Body"=EXCLUDED."Body","FromEmail"=EXCLUDED."FromEmail","FromName"=EXCLUDED."FromName","CcEmail"=EXCLUDED."CcEmail","BccEmail"=EXCLUDED."BccEmail","Category"=EXCLUDED."Category","LanguageCode"=EXCLUDED."LanguageCode","IsActive"=EXCLUDED."IsActive","AddedById"=EXCLUDED."AddedById","AddedDateTime"=EXCLUDED."AddedDateTime","UpdatedById"=EXCLUDED."UpdatedById","UpdatedDateTime"=EXCLUDED."UpdatedDateTime","AddedFromIP"=EXCLUDED."AddedFromIP","UpdatedFromIP"=EXCLUDED."UpdatedFromIP";

SELECT setval(pg_get_serial_sequence('skolenza."EmailTemplate"','Id'), GREATEST((SELECT MAX("Id") FROM skolenza."EmailTemplate"),1), true);
COMMIT;
