CREATE TABLE character_missions (
    id           BIGINT      NOT NULL AUTO_INCREMENT,
    character_id BIGINT      NOT NULL,
    mission_id   VARCHAR(32) NOT NULL,
    state        VARCHAR(16) NOT NULL,             -- MissionState 名稱：Active、Completed
    progress     INT         NOT NULL DEFAULT 0,   -- 擊殺目標已計入的數量；只有交付的任務為 0
    daily_period DATE        NULL,                 -- 每日任務接取時的週期（台北 09:00 起算的日期）；一般任務 NULL
    accepted_at  DATETIME(6) NOT NULL,
    completed_at DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_character_missions_character_mission (character_id, mission_id),
    CONSTRAINT fk_character_missions_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
