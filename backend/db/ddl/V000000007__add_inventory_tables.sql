CREATE TABLE character_inventory (
    id           BIGINT      NOT NULL AUTO_INCREMENT,
    character_id BIGINT      NOT NULL,
    item_id      VARCHAR(32) NOT NULL,
    quantity     INT         NOT NULL DEFAULT 1,   -- 可堆疊道具的數量；裝備為 1
    refine_level INT         NOT NULL DEFAULT 0,   -- 精鍊等級
    refine_bonus JSON        NULL,                 -- 精鍊累積的能力加成，無則 NULL
    sockets      JSON        NULL,                 -- 已鑲嵌的魔石，無則 NULL
    holes        INT         NOT NULL DEFAULT 0,   -- 已開的孔數
    PRIMARY KEY (id),
    KEY ix_character_inventory_character (character_id),
    CONSTRAINT fk_character_inventory_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE character_equipment (
    character_id    BIGINT      NOT NULL,
    slot            VARCHAR(16) NOT NULL,   -- EquipmentSlot 名稱：Weapon、Head、Body、Hand、Foot、Accessory、Supply
    inventory_id    BIGINT      NOT NULL,
    supply_quantity INT         NULL,       -- 只有 Supply 欄使用：裝備的補給數量
    PRIMARY KEY (character_id, slot),
    CONSTRAINT fk_character_equipment_character FOREIGN KEY (character_id) REFERENCES characters (id),
    CONSTRAINT fk_character_equipment_inventory FOREIGN KEY (inventory_id) REFERENCES character_inventory (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE character_depot (
    id           BIGINT      NOT NULL AUTO_INCREMENT,
    character_id BIGINT      NOT NULL,
    item_id      VARCHAR(32) NOT NULL,
    quantity     INT         NOT NULL,
    refine_level INT         NOT NULL DEFAULT 0,
    refine_bonus JSON        NULL,
    sockets      JSON        NULL,
    holes        INT         NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    KEY ix_character_depot_character (character_id),
    CONSTRAINT fk_character_depot_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

ALTER TABLE characters
    ADD COLUMN bank        BIGINT NOT NULL DEFAULT 0,   -- 銀行存款
    ADD COLUMN bag_bonus   INT    NOT NULL DEFAULT 0,   -- 背包擴充格數
    ADD COLUMN depot_bonus INT    NOT NULL DEFAULT 0;   -- 倉庫擴充格數
