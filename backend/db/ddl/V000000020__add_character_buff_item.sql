-- 密藥記下最後喝的那瓶；每種仍只有一列，喝同種時一起覆蓋。舊資料沒有道具，維持 NULL
ALTER TABLE character_buffs ADD COLUMN item_id VARCHAR(64) NULL AFTER kind;
