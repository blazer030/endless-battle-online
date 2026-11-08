ALTER TABLE characters
    ADD COLUMN current_place_id INT NOT NULL DEFAULT 1,       -- 地區 id
    ADD COLUMN current_sub_place TINYINT NULL,                -- 子地區 1..4，無則 NULL
    MODIFY current_stage_id VARCHAR(64) NULL;                 -- 計畫 6 起不再使用
UPDATE characters SET current_sub_place = 1 WHERE current_sub_place IS NULL;
