-- 旅館：每個角色一列目前的入住（整筆取代）
CREATE TABLE character_lodgings (
    character_id BIGINT      NOT NULL,
    room_id      VARCHAR(64) NOT NULL,
    starts_at    DATETIME(6) NOT NULL,         -- 開打時刻在 [starts_at, ends_at) 的場次才生效
    ends_at      DATETIME(6) NOT NULL,
    PRIMARY KEY (character_id),
    CONSTRAINT fk_character_lodgings_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
