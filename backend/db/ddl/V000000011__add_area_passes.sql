CREATE TABLE character_area_passes (
    character_id BIGINT      NOT NULL,
    place_id     INT         NOT NULL,          -- 禁區地區 id（17～20）
    expires_at   DATETIME(6) NOT NULL,          -- 到期前可免費重進（WOG wog_key_list.area_time）
    PRIMARY KEY (character_id, place_id),
    CONSTRAINT fk_character_area_passes_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
