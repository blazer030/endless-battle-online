-- 任務可以有多個擊殺目標：進度改存成依目標順序的 JSON 陣列，舊資料變成只有第一個目標的進度
ALTER TABLE character_missions ADD COLUMN kill_progress JSON NULL AFTER progress;

UPDATE character_missions SET kill_progress = JSON_ARRAY(progress);

ALTER TABLE character_missions
    MODIFY COLUMN kill_progress JSON NOT NULL,
    DROP COLUMN progress;
