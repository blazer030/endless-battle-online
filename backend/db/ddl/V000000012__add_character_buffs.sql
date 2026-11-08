CREATE TABLE character_buffs (
    character_id BIGINT       NOT NULL,
    kind         VARCHAR(16)  NOT NULL,         -- BuffKind
    multiplier   DECIMAL(4,2) NOT NULL,
    starts_at    DATETIME(6)  NOT NULL,         -- 開打時刻在 [starts_at, ends_at) 的場次才生效
    ends_at      DATETIME(6)  NOT NULL,
    PRIMARY KEY (character_id, kind),
    CONSTRAINT fk_character_buffs_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
