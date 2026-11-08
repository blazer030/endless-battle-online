ALTER TABLE characters
    ADD COLUMN act_num INT NOT NULL DEFAULT 50,
    ADD COLUMN act_num_time DATETIME(6) NULL;
CREATE TABLE character_job_proficiency (
    character_id BIGINT      NOT NULL,
    job_id       VARCHAR(32) NOT NULL,
    proficiency  BIGINT      NOT NULL DEFAULT 0,   -- ch_<job>
    skill_points BIGINT      NOT NULL DEFAULT 0,   -- sk_<job>
    PRIMARY KEY (character_id, job_id),
    CONSTRAINT fk_job_proficiency_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
