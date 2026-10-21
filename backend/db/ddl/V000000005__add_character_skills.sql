CREATE TABLE character_skills (
    character_id BIGINT      NOT NULL,
    skill_id     VARCHAR(32) NOT NULL,
    level        INT         NOT NULL,   -- 已習得的技能等級
    PRIMARY KEY (character_id, skill_id),
    CONSTRAINT fk_character_skills_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE character_skill_slots (
    character_id BIGINT      NOT NULL,
    slot         TINYINT     NOT NULL,   -- 技能欄 1..5
    skill_id     VARCHAR(32) NOT NULL,
    chance       INT         NOT NULL,   -- 發動機率；被動欄存技能自己的值
    PRIMARY KEY (character_id, slot),
    CONSTRAINT fk_character_skill_slots_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
