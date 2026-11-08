-- 技能欄不再存發動率：主動技能用技能自己每級固定的 chance，被動技能常駐沒有發動率
ALTER TABLE character_skill_slots DROP COLUMN chance;
