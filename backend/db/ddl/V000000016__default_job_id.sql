-- 正式內容改成 WOG 4.5 原版：預設職業從自訂的 warrior 改成原版戰士 job-6
ALTER TABLE characters ALTER COLUMN job_id SET DEFAULT 'job-6';
