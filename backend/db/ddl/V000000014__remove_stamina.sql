-- 行動力系統移除：刪掉已不存在的行動力藥水，再刪角色的行動力欄位（職業熟練度保留）
DELETE FROM character_equipment
WHERE inventory_id IN (SELECT id FROM (SELECT id FROM character_inventory WHERE item_id = 'stamina-potion') AS stamina_potions);
DELETE FROM character_inventory WHERE item_id = 'stamina-potion';
DELETE FROM character_depot WHERE item_id = 'stamina-potion';

ALTER TABLE characters
    DROP COLUMN act_num,
    DROP COLUMN act_num_time;
