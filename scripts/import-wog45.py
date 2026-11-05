#!/usr/bin/env python3
"""把 docs/research/wog45/data 的 WOG 4.5 原版資料匯入 backend/content。

用法（在 repo 根目錄）：python3 -I scripts/import-wog45.py

- 原版紀錄的 id 寫成「種類-原版編號」（item-660），數值以原版為準。
- 自訂內容（kebab-case id）照原檔順序接在原版紀錄後面。
- 現有規則用不到的欄位與紀錄寫進 docs/research/wog45/gaps.md。
- 原版資料缺少必要欄位時以非 0 結束，不寫任何檔案。

輸出格式與 ContentFileFormatter 相同：頂層陣列一筆一行，其餘寫成單行。
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "docs/research/wog45/data"
CONTENT = ROOT / "backend/content"
GAPS = ROOT / "docs/research/wog45/gaps.md"

ELEMENTS = {None: 0, "地": 1, "水": 2, "火": 3, "木": 4, "風": 5, "毒": 6}
EQUIPMENT_CATEGORIES = {"武器": "Weapon", "頭部": "Head", "身體": "Body", "手部": "Hand", "腳部": "Foot"}
BONUS_FIELDS = ["str", "smart", "life", "vit", "agi", "au", "be", "at", "mat", "df", "mdf"]
REQUIREMENT_FIELDS = ["str", "smart", "agi", "au"]
STONE_FIELDS = {"agl": "agi", "hpmax": "hpMax"}
# wog-research-world.md：捕捉道具 258／1837／2236 的倍率依序為 1／8／13
CAPTURE_MULTIPLIERS = {258: 1, 1837: 8, 2236: 13}
# wog_df_used.d_lv 對到密藥的種類與數值欄位；7（HP）、8（SP）本作沒有
ELIXIR_KINDS = {1: ("Exp", "exp"), 2: ("JobExp", "skill_exp"), 3: ("Attack", "at"), 4: ("Magic", "mat"),
                5: ("Defense", "df"), 6: ("MagicDefense", "mdf")}
# 寶箱抽選的分級（stones-and-consumables.md）；0、4 不從寶箱出，99 是鑲嵌時擲骰的隨機魔石
BOX_CLASSES = {0: 0, 1: 1, 2: 2, 3: 3, 4: 0}
RANDOM_STONE_CLASS = 99
MINIMUM_SHOP_LEVEL = 1
MAXIMUM_BOX_STONES = 3


class SourceError(Exception):
    pass


class Gaps:
    """依內容種類收集沒匯入的紀錄、沒搬的欄位與對照說明。"""

    def __init__(self):
        self.sections = {}

    def section(self, title):
        return self.sections.setdefault(title, {"excluded": [], "fields": [], "notes": []})

    def exclude(self, title, record_id, name, reason):
        self.section(title)["excluded"].append((record_id, name, reason))

    def field(self, title, record_id, name, detail):
        self.section(title)["fields"].append((record_id, name, detail))

    def note(self, title, text):
        self.section(title)["notes"].append(text)

    def render(self):
        lines = [
            "# WOG 4.5 匯入缺口",
            "",
            "由 `scripts/import-wog45.py` 產生，不要手改。列出原版有、但現有規則還用不到的紀錄與欄位，"
            "以及匯入時的對照方式；後續補規則的變更以這份清單為範圍。",
        ]
        for title, section in self.sections.items():
            lines += ["", f"## {title}"]
            if section["notes"]:
                lines += ["", "### 對照方式", ""] + [f"- {text}" for text in section["notes"]]
            if section["excluded"]:
                lines += ["", "### 沒有匯入的紀錄", "", "| 原版 id | 名稱 | 原因 |", "|---|---|---|"]
                lines += [f"| {i} | {n} | {r} |" for i, n, r in sorted(section["excluded"], key=lambda row: row[0])]
            if section["fields"]:
                lines += ["", "### 沒有搬的欄位", "", "| 原版 id | 名稱 | 欄位與原版值 |", "|---|---|---|"]
                lines += [f"| {i} | {n} | {d} |" for i, n, d in sorted(section["fields"], key=lambda row: row[0])]
        return "\n".join(lines) + "\n"


def require(record, field, source):
    if field not in record:
        raise SourceError(f"{source} 的紀錄 {record.get('id', '？')} 缺少欄位 {field}")
    return record[field]


def load(name):
    return json.loads((DATA / name).read_text(encoding="utf-8"))


def read_content(name):
    return json.loads((CONTENT / name).read_text(encoding="utf-8"))


def custom_records(records, prefix):
    original = re.compile(rf"{prefix}-\d+")
    return [record for record in records if not original.fullmatch(record["id"])]


ALWAYS_WRITTEN = {"price", "shopLevel", "shopSellable"}


def compact(record):
    """去掉預設值（0、false、空物件），內容檔只寫有意義的欄位。"""
    return {key: value for key, value in record.items() if key in ALWAYS_WRITTEN or value not in (0, False, None, {})}


def stat_values(values, fields, source, record_id):
    result = {}
    for key, value in values.items():
        name = STONE_FIELDS.get(key, key)
        if name not in fields:
            raise SourceError(f"{source} 的紀錄 {record_id} 有無法辨識的能力欄位 {key}")
        result[name] = value
    return {name: result[name] for name in fields if name in result and result[name] != 0}


def shop_fields(item):
    shop_level = require(item, "shopLevel", "items.json")
    banned = require(item, "shopBanned", "items.json")
    # 不在商店的道具 shopLevel 沒有作用，但欄位必填（1～7）
    return {
        "price": require(item, "price", "items.json"),
        "shopLevel": max(shop_level, MINIMUM_SHOP_LEVEL),
        "shopSellable": shop_level >= MINIMUM_SHOP_LEVEL and not banned,
        "bound": require(item, "bound", "items.json"),
    }


def equipment_bonus(item):
    bonus = dict(require(item, "bonus", "items.json"))
    for field in ("at", "mat", "df", "mdf"):
        bonus[field] = require(item, field, "items.json")
    return stat_values(bonus, BONUS_FIELDS, "items.json", item["id"])


def is_accessory(item):
    return any(item[field] for field in ("at", "mat", "df", "mdf")) or item["bonus"] or item["requires"]


def parse_range(value, source, record_id):
    parts = re.split(r"[-,]", str(value))
    if len(parts) != 2:
        raise SourceError(f"{source} 的紀錄 {record_id} 的範圍無法辨識：{value}")
    return [int(parts[0]), int(parts[1])]


def import_items(gaps):
    title = "道具"
    items = load("items.json")
    stones = {stone["id"]: stone for stone in load("magic-stones.json")}
    refines = {refine["id"]: refine for refine in load("refine-stones.json")}
    effects = {effect["id"]: effect for effect in load("consumable-effects.json")}

    gaps.note(title, "裝備：`at`、`mat`、`df`、`mdf` 與 `bonus` 合併成 `bonus`；屬性的地水火木風毒轉成 1～6。")
    gaps.note(title, "原版「道具」欄（d_fie 5）有能力或需求的轉成飾品（Accessory），其餘轉成材料（Material）。")
    gaps.note(title, "商店等級 0 或禁售的道具 `shopSellable` 為 false；不在商店的道具 `shopLevel` 寫 1（欄位必填，沒有作用）。")
    gaps.note(title, "魔石的 class 1～3 是寶箱分級，轉成 `boxClass`；class 0、4 不從寶箱出（`boxClass` 0）；class 99 是隨機魔石。")
    gaps.note(title, "背包、倉庫擴充：原版是「容量設為 d_g_bag」，本作是每張加 `amount` 格，`amount` 取 d_g_bag。")
    gaps.note(title, "心得書：原版綁定職業（ch_id），本作只加目前職業（裁定彙整第 98 行），`amount` 取 d_g_exp。")
    gaps.note(title, "鑰匙在任務 4.1 與榮譽商店一起切換：現有規則每個禁區只能有一把鑰匙，擴充期間先不匯入原版鑰匙。")

    records = []
    for item in sorted(items, key=lambda row: require(row, "id", "items.json")):
        item_id, name = item["id"], require(item, "name", "items.json")
        type_name = require(item, "typeName", "items.json")
        record = {"id": f"item-{item_id}", "name": name}

        if type_name in EQUIPMENT_CATEGORIES or (type_name == "道具" and is_accessory(item)):
            record["category"] = EQUIPMENT_CATEGORIES.get(type_name, "Accessory")
            record["bonus"] = equipment_bonus(item)
            record["requires"] = stat_values(require(item, "requires", "items.json"), REQUIREMENT_FIELDS, "items.json", item_id)
            record["element"] = ELEMENTS[require(item, "element", "items.json")]
            record["holes"] = require(item, "holes", "items.json")
            record["refineMax"] = require(item, "refineMax", "items.json")
            if item.get("skillId") is not None:
                gaps.field(title, item_id, name, f"附帶技能 skillId {item['skillId']}、發動率 {item.get('skillRate')}")
            if item.get("recoverHp") or item.get("recoverSp"):
                gaps.field(title, item_id, name, f"裝備回復 HP {item.get('recoverHp')}、SP {item.get('recoverSp')}")
        elif type_name == "道具":
            record["category"] = "Material"
        elif type_name == "勳章":
            record["category"] = "Medal"
        elif type_name == "鑰匙":
            gaps.exclude(title, item_id, name, "鑰匙在任務 4.1 與榮譽商店一起切換")
            continue
        elif type_name == "魔石":
            stone = stones.get(item_id)
            if stone is None:
                gaps.exclude(title, item_id, name, "魔石表（wog_stone_list）沒有這顆魔石的數值")
                continue
            record["category"] = "Stone"
            record["stone"] = stone_spec(stone)
        elif type_name == "精鍊石":
            refine = refines[item_id]
            record["category"] = "RefineStone"
            record["refine"] = {"level": refine["level"], "kind": refine["type"]}
            for field, value in stat_values(refine["effects"], BONUS_FIELDS, "refine-stones.json", item_id).items():
                record["refine"][field] = parse_range(value, "refine-stones.json", item_id)
        elif type_name == "補給品":
            if item_id in CAPTURE_MULTIPLIERS:
                record.update(category="Consumable", consumable="Capture", amount=CAPTURE_MULTIPLIERS[item_id])
            elif item.get("recoverHp") or item.get("recoverSp"):
                record.update(category="Consumable", consumable="Supply",
                              supplyHp=item.get("recoverHp", 0), supplySp=item.get("recoverSp", 0))
            else:
                gaps.exclude(title, item_id, name, "補給品沒有回復量")
                continue
        elif type_name == "即時使用物":
            consumable = consumable_spec(item, effects[item_id], gaps, title)
            if consumable is None:
                continue
            record.update(category="Consumable", **consumable)
        else:
            raise SourceError(f"items.json 的紀錄 {item_id} 有無法辨識的類型 {type_name}")

        record.update(shop_fields(item))
        records.append(order_item(compact(record)))

    return records + custom_records(read_content("items.json")["items"], "item")


def stone_spec(stone):
    effects = stone["effects"]
    if stone["class"] == RANDOM_STONE_CLASS:
        ranges = {field: parse_range(value, "magic-stones.json", stone["id"])
                  for field, value in stat_values(effects, BONUS_FIELDS + ["hpMax"], "magic-stones.json", stone["id"]).items()}
        return {"random": True, "min": {field: low for field, (low, _) in ranges.items()},
                "max": {field: high for field, (_, high) in ranges.items()}}
    fixed = {field: int(value) for field, value in stat_values(effects, BONUS_FIELDS + ["hpMax"], "magic-stones.json", stone["id"]).items()}
    spec = {"random": False, "fixed": fixed}
    if BOX_CLASSES[stone["class"]]:
        spec["boxClass"] = BOX_CLASSES[stone["class"]]
    return spec


def consumable_spec(item, effect, gaps, title):
    item_id, name = item["id"], item["name"]
    raw = {key: value for key, value in effect["raw"].items() if value != "0"}
    used_type = int(raw.get("d_type", "0"))
    bonus = stat_values(item["bonus"], BONUS_FIELDS, "items.json", item_id)
    if used_type == 0 and bonus:
        return {"consumable": "StatPotion", "bonus": bonus}
    if used_type == 0 and "d_g_exp" in raw:
        return {"consumable": "ExperienceCapsule", "amount": int(raw["d_g_exp"])}
    if used_type == 1:
        return {"consumable": "BagExpansion", "amount": int(raw["d_g_bag"])}
    if used_type == 3:
        return {"consumable": "DepotExpansion", "amount": int(raw["d_g_bag"])}
    if used_type == 6:
        return {"consumable": "ProficiencyBook", "amount": int(raw["d_g_exp"])}
    if used_type == 9:
        level = int(raw["d_lv"])
        if level not in ELIXIR_KINDS:
            gaps.exclude(title, item_id, name, f"密藥的種類 d_lv {level}（HP／SP 倍率）本作沒有")
            return None
        kind, field = ELIXIR_KINDS[level]
        return {"consumable": "Elixir", "elixir": {"kind": kind, "multiplier": float(raw[field]), "seconds": int(raw["use_time"])}}
    reasons = {
        2: "魔石寶箱的分級 d_lv 與本作寶箱（只開 boxClass 1）對不上",
        7: "行動力藥水：本作已移除行動力",
        13: "技能書：本作的技能全部靠熟練度學習（裁定彙整第 200 行）",
    }
    gaps.exclude(title, item_id, name, reasons.get(used_type, f"使用效果種類 d_type {used_type} 不明"))
    return None


ITEM_FIELD_ORDER = ["id", "name", "category", "consumable", "bonus", "supplyHp", "supplySp", "amount", "requires", "jobId",
                    "element", "holes", "refineMax", "price", "shopLevel", "shopSellable", "bound", "refine", "stone", "key", "elixir"]


def order_item(record):
    return {field: record[field] for field in ITEM_FIELD_ORDER if field in record}


def format_inline(value):
    if isinstance(value, dict):
        return "{" + ", ".join(f"{json.dumps(key, ensure_ascii=False)}: {format_inline(item)}" for key, item in value.items()) + "}"
    if isinstance(value, list):
        return "[" + ", ".join(format_inline(item) for item in value) + "]"
    return json.dumps(value, ensure_ascii=False)


def format_content(root):
    lines = ["{"]
    entries = list(root.items())
    for index, (key, value) in enumerate(entries):
        suffix = "," if index < len(entries) - 1 else ""
        if isinstance(value, list) and value:
            lines.append(f"  {json.dumps(key, ensure_ascii=False)}: [")
            lines += [f"    {format_inline(item)}{',' if row < len(value) - 1 else ''}" for row, item in enumerate(value)]
            lines.append(f"  ]{suffix}")
        else:
            lines.append(f"  {json.dumps(key, ensure_ascii=False)}: {format_inline(value)}{suffix}")
    return "\n".join(lines) + "\n}\n"


def main():
    gaps = Gaps()
    try:
        outputs = {"items.json": {"items": import_items(gaps)}}
    except (SourceError, KeyError) as error:
        print(f"匯入失敗：{error}", file=sys.stderr)
        return 1

    for name, root in outputs.items():
        (CONTENT / name).write_text(format_content(root), encoding="utf-8")
    GAPS.write_text(gaps.render(), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
