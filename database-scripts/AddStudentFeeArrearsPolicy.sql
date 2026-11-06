BEGIN;
ALTER TABLE skolenza."StudentFeeStructure"
    ADD COLUMN IF NOT EXISTS "ArrearsPolicy" smallint NOT NULL DEFAULT 1;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_StudentFeeStructure_ArrearsPolicy'
                   AND conrelid = 'skolenza."StudentFeeStructure"'::regclass) THEN
        ALTER TABLE skolenza."StudentFeeStructure"
            ADD CONSTRAINT "CK_StudentFeeStructure_ArrearsPolicy" CHECK ("ArrearsPolicy" IN (1, 2));
    END IF;
END $$;
COMMIT;
