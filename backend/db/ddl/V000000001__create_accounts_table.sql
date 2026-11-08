CREATE TABLE accounts (
    id            BIGINT       NOT NULL AUTO_INCREMENT,
    username      VARCHAR(32)  NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at    DATETIME(6)  NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_accounts_username (username)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
