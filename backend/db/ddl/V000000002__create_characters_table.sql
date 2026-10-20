CREATE TABLE characters (
    id               BIGINT      NOT NULL AUTO_INCREMENT,
    account_id       BIGINT      NOT NULL,
    name             VARCHAR(12) NOT NULL,
    experience       BIGINT      NOT NULL DEFAULT 0,
    spirit_stones    BIGINT      NOT NULL DEFAULT 0,
    current_stage_id VARCHAR(64) NOT NULL,
    last_settled_at  DATETIME(6) NOT NULL,
    version          INT         NOT NULL DEFAULT 0,
    created_at       DATETIME(6) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_characters_account_id (account_id),
    UNIQUE KEY uk_characters_name (name),
    CONSTRAINT fk_characters_account FOREIGN KEY (account_id) REFERENCES accounts (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
