-- 旅館改成每次入住一列：換房時舊列截斷到入住時刻、另外新增一列，冒險結算依每場開打時刻找所在那列
-- 先建 (character_id, ends_at) 索引讓外鍵改用它，才能拿掉以 character_id 為主鍵；既有資料保留成第一筆
ALTER TABLE character_lodgings ADD INDEX ix_character_lodgings_character_ends (character_id, ends_at);
ALTER TABLE character_lodgings DROP PRIMARY KEY, ADD COLUMN id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY FIRST;
