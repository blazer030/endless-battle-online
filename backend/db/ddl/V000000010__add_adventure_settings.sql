ALTER TABLE characters
    ADD COLUMN attack_preference TINYINT NULL,                 -- NULL 自動、1 物理、2 魔法
    ADD COLUMN potion_hp_percent TINYINT NOT NULL DEFAULT 15,  -- -1 略過
    ADD COLUMN potion_sp_percent TINYINT NOT NULL DEFAULT 100,
    ADD COLUMN rest_hp_percent   TINYINT NOT NULL DEFAULT 15,
    ADD COLUMN rest_sp_percent   TINYINT NOT NULL DEFAULT 15,
    ADD COLUMN escape_hp_percent TINYINT NOT NULL DEFAULT 0;   -- 0 不逃
