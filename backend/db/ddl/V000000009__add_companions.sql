CREATE TABLE character_pets (
    id           BIGINT      NOT NULL AUTO_INCREMENT,
    character_id BIGINT      NOT NULL,
    monster_id   VARCHAR(32) NOT NULL,             -- 物種（WOG pe_m_id）
    name         VARCHAR(12) NOT NULL,
    personality  TINYINT     NOT NULL,             -- PetPersonality：1 積極 2 冷酷 3 鐵壁 4 危急
    attack       INT         NOT NULL,             -- pe_at
    magic        INT         NOT NULL,             -- pe_mt
    defense      INT         NOT NULL,             -- pe_def，也是代擋量
    spirit       INT         NOT NULL,             -- pe_fi 出擊值
    satiety      INT         NOT NULL,             -- pe_fu 飽腹
    intimacy     INT         NOT NULL,             -- pe_he 親密度
    carried      BOOLEAN     NOT NULL,             -- 攜帶（pe_st 0）或獸欄（pe_st 2）
    born_at      DATETIME(6) NOT NULL,             -- 算歲數
    hunger_since DATETIME(6) NOT NULL,             -- 餵食與飢餓共用的計時起點（pe_dateline）
    PRIMARY KEY (id),
    KEY ix_character_pets_character (character_id),
    CONSTRAINT fk_character_pets_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE character_mercenaries (
    character_id  BIGINT      NOT NULL,
    mercenary_id  VARCHAR(32) NOT NULL,
    name          VARCHAR(8)  NOT NULL,
    remaining     INT         NOT NULL,            -- 剩餘戰鬥次數（me_count）
    place_id      INT         NULL,                -- 出擊地區；NULL＝休息中
    next_fight_at DATETIME(6) NULL,                -- 下一場的時刻；休息中為 NULL
    hired_at      DATETIME(6) NOT NULL,
    PRIMARY KEY (character_id),
    CONSTRAINT fk_character_mercenaries_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE character_champions (
    character_id        BIGINT      NOT NULL,      -- 沒有這一列＝內容的種子冠軍
    name                VARCHAR(12) NOT NULL,
    level               INT         NOT NULL,
    element             TINYINT     NOT NULL,
    special_attack_name VARCHAR(16) NOT NULL,
    hp INT NOT NULL, hp_max INT NOT NULL, sp INT NOT NULL, sp_max INT NOT NULL,
    at INT NOT NULL, mat INT NOT NULL, df INT NOT NULL, mdf INT NOT NULL, agi INT NOT NULL, au INT NOT NULL,
    is_player           BOOLEAN     NOT NULL,
    defenses            INT         NOT NULL,      -- 衛冕次數
    crowned_at          DATETIME(6) NULL,
    locked_until        DATETIME(6) NULL,          -- 這之前不能挑戰
    PRIMARY KEY (character_id),
    CONSTRAINT fk_character_champions_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
