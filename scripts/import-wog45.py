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
GROWTH_FIELDS = ["str", "smart", "life", "vit", "agi", "au", "be"]
# 本作地圖的地點 id 範圍；原版的 99（特殊）不在地圖上
PLACE_IDS = range(0, 22)
MAXIMUM_BOX_STONES = 3
# shops-and-misc.md 的地區鑰匙表（wog_key_main）：鑰匙道具 → 開放的地點；有效秒數都是 86400
KEY_PLACES = {1338: 17, 1339: 18, 1405: 19, 1522: 20}
KEY_SECONDS = 86400
# wog_act_honor.php：h_1～h_5 依序是大陸、王國、雨林、王城、白銀勳章
HONOUR_MEDALS = {"h_1": 1304, "h_2": 1305, "h_3": 1306, "h_4": 1307, "h_5": 2222}


# 原版沒有承擔同樣角色、而現有功能需要的自訂內容；其餘自訂內容都由原版取代（design.md「保留原版沒有的自訂內容」）
KEPT_CUSTOM_ITEMS = {
    "stone-box": "魔石寶箱：原版寶石箱的分級對不上本作寶箱（只開 boxClass 1），沒有匯入",
    "large-stone-box": "大魔石寶箱：同上，一次開 2 顆",
    "refine-box": "精鍊寶盒：原版沒有開出精鍊石的道具",
    "pet-egg": "寵物蛋：原版沒有孵出寵物的道具",
    "safe-refine-stone": "安全精鍊石：原版的 2284 不在匯出的資料裡",
}
# 自訂勳章與原版勳章同名，保留的兌換改付原版勳章
CUSTOM_MEDALS = {"continent-medal": "item-1304", "kingdom-medal": "item-1305", "rainforest-medal": "item-1306",
                 "royal-city-medal": "item-1307", "silver-medal": "item-2222"}
KEPT_TITLE = "保留的自訂內容"
KEPT_CONTENT_FILES = [
    "`inn.json`：旅館房型與價格，原版沒有對應的資料表",
    "`champion.json`：冠軍的初始種子，原版冠軍是玩家",
    "`world.json` 的 `areas`、`recommended`、`startingPlaceId`、`startingSubPlace`：`wog_area_lv` 與地點 id 不同，只當參考",
]


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
                heading = [] if title == KEPT_TITLE else ["", "### 對照方式"]
                lines += heading + [""] + [f"- {text}" for text in section["notes"]]
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


class SourceRecord(dict):
    """原版資料的一筆紀錄（含裡面的子物件）；取不到的欄位以檔名與紀錄 id 報錯，不是 KeyError。"""

    def __init__(self, source, record_id, values):
        super().__init__(values)
        self.source, self.record_id = source, record_id

    def __missing__(self, field):
        raise SourceError(f"{self.source} 的紀錄 {self.record_id} 缺少欄位 {field}")


def wrap(value, source, record_id):
    if isinstance(value, dict):
        return SourceRecord(source, record_id, {key: wrap(item, source, record_id) for key, item in value.items()})
    if isinstance(value, list):
        return [wrap(item, source, record_id) for item in value]
    return value


def load(name):
    records = json.loads((DATA / name).read_text(encoding="utf-8"))
    return [wrap(record, name, record.get("id", record.get("me_id", "？"))) for record in records]


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
    gaps.note(title, "鑰匙：開放地點與有效秒數取自地區鑰匙表（wog_key_main），取代自訂鑰匙。")

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
            if item_id not in KEY_PLACES:
                raise SourceError(f"items.json 的鑰匙 {item_id} 不在地區鑰匙表")
            record["category"] = "Key"
            record["key"] = {"placeId": KEY_PLACES[item_id], "seconds": KEY_SECONDS}
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

    custom = [item for item in custom_records(read_content("items.json")["items"], "item") if item["id"] in KEPT_CUSTOM_ITEMS]
    return records + custom


def import_jobs(gaps):
    title = "職業"
    gaps.note(title, "成長的 `\"a,b\"` 轉成 `[a, b]`；有前置職業的是 2 轉，`requiredProficiency` 轉成 `proficiencyRequired`；原版沒有能力需求，`statRequirements` 為空。")
    records = []
    for job in sorted(load("jobs.json"), key=lambda row: require(row, "id", "jobs.json")):
        growth = require(job, "growth", "jobs.json")
        parent = require(job, "parentId", "jobs.json")
        records.append({
            "id": f"job-{job['id']}",
            "name": require(job, "name", "jobs.json"),
            "tier": 1 if parent is None else 2,
            "parentId": None if parent is None else f"job-{parent}",
            "unlockLevel": require(job, "unlockLevel", "jobs.json"),
            "proficiencyRequired": require(job, "requiredProficiency", "jobs.json"),
            "statRequirements": [],
            "growth": {field: parse_range(require(growth, field, "jobs.json"), "jobs.json", job["id"]) for field in GROWTH_FIELDS},
        })
    return records


def invalid_monster_reason(monster):
    if monster["level"] < 1:
        return "等級必須至少 1（現有內容驗證）"
    if monster["hp"] < 1 or monster["agi"] < 1:
        return "hp 與 agi 必須大於 0（現有內容驗證）"
    return None


def monster_exclusion(monster, places):
    """現有規則載入不了、或在本作遇不到的怪物；任務怪另外看任務。"""
    reason = invalid_monster_reason(monster)
    place = places.get(monster["placeId"])
    if reason is None and monster["placeId"] not in PLACE_IDS:
        reason = f"地點 {monster['placeId']}（{place['name'] if place else '？'}）不在本作地圖"
    if reason is None and place["subPlaces"] and not monster["subPlaces"]:
        reason = "原版子地區遮罩為 0：選了子地區時不會出現"
    return reason


def import_monsters(gaps, item_ids, mission_monsters):
    """mission_monsters：任務怪的原版 id → 匯入的任務 id；只有任務成功匯入、且以牠為擊殺目標的任務怪才匯入。"""
    title = "怪物"
    gaps.note(title, "屬性的地水火木風毒轉成 1～6；子地區名稱轉成該地點的子地區編號 1～4。")
    gaps.note(title, "`str`、`smart`、`vit`、`be` 給技能的「能力比對手高」條件比較；`life`、`image`、`npcMessage` 沒有搬：現有戰鬥規則用不到。")
    gaps.note(title, "任務怪（`missionId` 有值）跟著任務匯入：任務沒有匯入的，任務怪也不匯入。")
    places = {place["id"]: place for place in load("places.json")}
    records = []
    for monster in sorted(load("monsters.json"), key=lambda row: require(row, "id", "monsters.json")):
        monster_id, name = monster["id"], require(monster, "name", "monsters.json")
        for field in ("level", "hp", "sp", "at", "mat", "df", "mdf", "agi", "au", "jobExp", "meet", "placeId", "subPlaces", "drops"):
            require(monster, field, "monsters.json")
        reason = monster_exclusion(monster, places)
        if reason is None and monster.get("missionId") is not None and monster_id not in mission_monsters:
            reason = f"任務怪：任務 {monster['missionId']} 沒有匯入"
        if reason:
            gaps.exclude(title, monster_id, name, reason)
            continue

        unsupported = []
        if monster.get("specialAttack"):
            unsupported.append(f"必殺技 {monster['specialAttack']}")
        unsupported += [f"技能 {skill['name']} rate {skill['rate']}" for skill in monster.get("skills", [])]
        drops = []
        for drop in monster["drops"]:
            item_id = f"item-{drop['itemId']}"
            if item_id in item_ids:
                drops.append({"itemId": item_id, "topr": drop["topr"]})
            else:
                unsupported.append(f"掉落 {drop['itemId']} {drop['item']}（道具沒有匯入）")
        if unsupported:
            gaps.field(title, monster_id, name, "；".join(unsupported))

        record = {
            "id": f"monster-{monster_id}", "name": name, "level": monster["level"], "hp": monster["hp"], "sp": monster["sp"],
            "at": monster["at"], "mat": monster["mat"], "df": monster["df"], "mdf": monster["mdf"], "agi": monster["agi"],
            "au": monster["au"], "element": ELEMENTS[monster.get("element")], "jobExp": monster["jobExp"], "meet": monster["meet"],
            "placeId": monster["placeId"],
            "subPlaces": [places[monster["placeId"]]["subPlaces"].index(sub) + 1 for sub in monster["subPlaces"]],
            "drops": drops,
            "str": monster.get("str", 0), "smart": monster.get("smart", 0), "vit": monster.get("vit", 0), "be": monster.get("be", 0),
        }
        if monster_id in mission_monsters:
            record["missionId"] = mission_monsters[monster_id]
        records.append(record)
    return records


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


def import_recipes(gaps, item_ids):
    title = "配方"
    gaps.note(title, "成品與材料改引用 `item-<id>`；`amount` 轉 `resultQuantity`、`count` 轉 `quantity`。")
    records = []
    for recipe in sorted(load("recipes.json"), key=lambda row: require(row, "id", "recipes.json")):
        recipe_id, result = recipe["id"], require(recipe, "result", "recipes.json")
        materials = [{"itemId": f"item-{material['itemId']}", "quantity": material["count"]}
                     for material in require(recipe, "materials", "recipes.json")]
        missing = [entry for entry in [f"item-{require(recipe, 'resultId', 'recipes.json')}"] + [m["itemId"] for m in materials]
                   if entry not in item_ids]
        if missing:
            gaps.exclude(title, recipe_id, result, f"引用的道具沒有匯入：{'、'.join(missing)}")
            continue
        if require(recipe, "requiresMissionId", "recipes.json") is not None:
            gaps.field(title, recipe_id, result, f"要先完成任務 {recipe['requiresMissionId']}：本作的配方沒有前置條件")
        if require(recipe, "missionRecipe", "recipes.json"):
            gaps.field(title, recipe_id, result, "原版的任務配方：本作沒有任務配方，一般就能用")
        records.append({
            "id": f"recipe-{recipe_id}", "resultItemId": f"item-{recipe['resultId']}",
            "resultQuantity": require(recipe, "amount", "recipes.json"), "materials": materials,
            "successRate": require(recipe, "successRate", "recipes.json"),
        })
    return records


def import_mercenaries(gaps):
    title = "傭兵"
    gaps.note(title, "`me_count` 轉 `fights`，金錢、道具、經驗的比例照搬，`me_die` 轉 `deathCost`；`me_status` 1 的不在商店賣，`price` 寫 null（只能在勳章所兌換）。")
    records = []
    for mercenary in sorted(load("mercenaries.json"), key=lambda row: int(require(row, "me_id", "mercenaries.json"))):
        on_sale = require(mercenary, "me_status", "mercenaries.json") == "0"
        records.append({
            "id": f"mercenary-{mercenary['me_id']}", "name": require(mercenary, "me_name", "mercenaries.json"),
            "price": int(require(mercenary, "me_money", "mercenaries.json")) if on_sale else None,
            "fights": int(require(mercenary, "me_count", "mercenaries.json")),
            "moneyPercent": int(require(mercenary, "me_get_money", "mercenaries.json")),
            "dropPercent": int(require(mercenary, "me_get_item", "mercenaries.json")),
            "expPercent": int(require(mercenary, "me_get_exp", "mercenaries.json")),
            "deathCost": int(require(mercenary, "me_die", "mercenaries.json")),
        })
    return records


def import_honour_shop(gaps, item_ids, mercenary_ids):
    title = "勳章所"
    gaps.note(title, "`h_1`～`h_5` 依序是大陸、王國、雨林、王城、白銀勳章（`item-1304`～`item-1307`、`item-2222`）。")
    gaps.note(title, "傭兵商品的 `itemId` 是傭兵編號，轉成 `mercenaryId`；其餘商品轉成 `itemId`，`count` 轉 `quantity`。")
    records = []
    for exchange in sorted(load("honour-shop.json"), key=lambda row: require(row, "id", "honour-shop.json")):
        exchange_id, name, kind = exchange["id"], require(exchange, "item", "honour-shop.json"), require(exchange, "kind", "honour-shop.json")
        if kind == "0":
            gaps.exclude(title, exchange_id, name, "類型 0：原版的勳章所沒有這個分頁，兌換不到")
            continue
        cost = [{"itemId": f"item-{HONOUR_MEDALS[field]}", "quantity": quantity}
                for field, quantity in sorted(require(exchange, "cost", "honour-shop.json").items()) if quantity]
        record = {"id": f"honour-{exchange_id}"}
        if kind == "傭兵":
            target = f"mercenary-{exchange['itemId']}"
            if target not in mercenary_ids:
                raise SourceError(f"honour-shop.json 的紀錄 {exchange_id} 引用了不存在的傭兵 {exchange['itemId']}")
            record["mercenaryId"] = target
        else:
            target = f"item-{exchange['itemId']}"
            if target not in item_ids:
                gaps.exclude(title, exchange_id, name, f"商品 {target} 沒有匯入")
                continue
            record["itemId"] = target
        record.update(quantity=require(exchange, "count", "honour-shop.json"), cost=cost)
        records.append(record)
    # 保留的自訂道具，兌換也一起保留
    custom = [{**exchange, "cost": [{**cost, "itemId": CUSTOM_MEDALS.get(cost["itemId"], cost["itemId"])} for cost in exchange["cost"]]}
              for exchange in custom_records(read_content("honour-shop.json")["exchanges"], "honour")
              if exchange.get("itemId") in KEPT_CUSTOM_ITEMS]
    return records + custom


# ---- 任務 ----
# 每日任務的目標池與報酬池寫在原版 mission/wog_mission_<id>.php，不在資料表裡，照抄在這裡。
# kills：(怪物 id, 數量)；pets：寵物從這些地點的一般怪（meet 0、非任務怪、非 NPC）擲；
# bounty：擊殺目標從這些地點的一般怪擲，數量 5～7；rewards：每一筆是 (道具 id, 數量) 的清單
DAILY_MISSIONS = {
    222: {"kills": [(1, 6), (49, 6), (19, 6), (104, 6), (22, 5), (26, 5), (36, 4), (57, 4)],
          "rewards": [[(1304, 1)], [(1305, 1)], [(1306, 1)], [(2222, 1)], [(1307, 1)]]},
    223: {"pets": [1, 2, 3, 4],
          "rewards": [[(1422, 1), (1304, 1)], [(1422, 1), (1305, 1)], [(1422, 1), (1306, 1)], [(2222, 1)], [(1422, 1), (1307, 1)]]},
    224: {"kills": [(57, 6), (59, 6), (65, 6), (67, 6), (73, 5), (77, 5), (109, 5), (44, 5)],
          "rewards": [[(260, 3), (1304, 2)], [(260, 3), (1305, 2)], [(260, 3), (1306, 2)], [(260, 3), (2222, 2)], [(260, 3), (1307, 2)]]},
    225: {"pets": [5, 6, 7, 8],
          "rewards": [[(1423, 1), (1304, 1)], [(1423, 1), (1305, 1)], [(1423, 1), (1306, 1)], [(1423, 1), (2222, 1)], [(1423, 1), (1307, 1)]]},
    # 原版報酬是交易所物資（本作沒有），沿用計畫 8 的金錢報酬
    271: {"bounty": [3, 4], "money": [400, 500, 600]},
    272: {"bounty": [5, 6], "money": [2400, 3000, 3600]},
    273: {"bounty": [7, 13], "money": [1000, 1250, 1500]},
    274: {"bounty": [8, 16], "money": [800, 1000, 1200]},
}
BOUNTY_COUNTS = [5, 6, 7]
SEX_ANY = 3


def text(value):
    return (value or "").strip()


def merge_quantities(entries):
    merged = {}
    for item_id, quantity in entries:
        merged[item_id] = merged.get(item_id, 0) + quantity
    return [{"itemId": item_id, "quantity": quantity} for item_id, quantity in merged.items()]


def skill_owners():
    groups = {}
    for row in sorted(load("skills.json"), key=lambda row: row["id"]):
        groups.setdefault(row["name"], []).append(row)
    return {row["id"]: (f"skill-{rows[0]['id']}", index + 1) for rows in groups.values() for index, row in enumerate(rows)}


def import_missions(gaps, item_ids, skill_ids, job_skills):
    """回傳 (任務紀錄, 任務怪的原版 id → 任務 id)。"""
    title = "任務"
    gaps.note(title, "`npc`、`subject`、`body`、`endMessage` 轉 `giver`、`title`、`description`、`completionMessage`；單一擊殺轉 `kill`、`needItems` 轉 `deliver`。")
    gaps.note(title, "`job`、`sex`、`requiresMissionId`、`excludesMissionId` 轉 `requirements`；`rewardMoney`、`rewardItems`、`rewardSkillId`、`rewardPet` 轉 `rewards`。")
    gaps.note(title, "`placeId`：有擊殺目標的是目標所在的地點；沒有的用原版 `areaId`，`areaId` 是 0 或禁區時寫起始地點（只用來顯示）。`storeId`、`repeatable` 沒有搬。")
    gaps.note(title, "每日任務的目標池與報酬池照抄原版 `mission/wog_mission_<id>.php`；271～274 原版報酬是交易所物資（本作沒有），沿用計畫 8 的金錢報酬。")
    gaps.note(title, "任務引用的道具、技能、寵物沒有匯入時，那一項獎勵不給；前置或互斥的任務沒有匯入時，拿掉那項條件。")
    sources = sorted(load("missions.json"), key=lambda row: require(row, "id", "missions.json"))
    monsters = {monster["id"]: monster for monster in load("monsters.json")}
    places = {place["id"]: place for place in load("places.json")}
    jobs = {job["name"]: f"job-{job['id']}" for job in load("jobs.json")}
    owners = skill_owners()
    starting_place = read_content("world.json")["startingPlaceId"]
    restricted = set(KEY_PLACES.values())
    daily_ids = {mission["id"] for mission in sources if require(mission, "daily", "missions.json")}

    def monster_ok(monster_id, mission_id):
        monster = monsters.get(monster_id)
        if monster is None or monster_exclusion(monster, places) is not None:
            return False
        return monster.get("missionId") in (None, mission_id)

    # 第一輪：決定哪些一般任務能匯入
    accepted, mission_monsters = {}, {}
    for mission in sources:
        mission_id, name = mission["id"], require(mission, "subject", "missions.json")
        if mission_id in daily_ids:
            continue
        kills, needs = require(mission, "kills", "missions.json"), require(mission, "needItems", "missions.json")
        reason = None
        if len(kills) > 1:
            reason = f"擊殺目標 {len(kills)} 種：一般任務只能有一個擊殺目標"
        elif require(mission, "petId", "missions.json") is not None:
            reason = "交寵物的目標：一般任務只能擊殺或交付道具"
        elif not kills and not needs:
            reason = "沒有擊殺或交付目標"
        elif any(f"item-{need['itemId']}" not in item_ids for need in needs):
            reason = "交付的道具沒有匯入：" + "、".join(f"item-{need['itemId']}" for need in needs if f"item-{need['itemId']}" not in item_ids)
        elif kills and not monster_ok(kills[0]["monsterId"], mission_id):
            reason = f"擊殺目標 monster-{kills[0]['monsterId']} 沒有匯入"
        elif kills and monsters[kills[0]["monsterId"]]["placeId"] in restricted:
            reason = "擊殺目標在禁區：任務不能在禁區"
        elif kills and monsters[kills[0]["monsterId"]]["placeId"] == 0:
            reason = "擊殺目標是全域怪（地點 0）：任務要有所在的地點"
        if reason:
            gaps.exclude(title, mission_id, name, reason)
            continue
        accepted[mission_id] = mission
        if kills and monsters[kills[0]["monsterId"]].get("missionId") == mission_id:
            mission_monsters[kills[0]["monsterId"]] = f"mission-{mission_id}"

    records = []
    for mission in sources:
        mission_id = mission["id"]
        if mission_id in daily_ids:
            records.append(daily_mission(mission, gaps, title, item_ids, monsters, places))
            continue
        if mission_id not in accepted:
            continue
        name = mission["subject"]
        kills = mission["kills"]
        if kills:
            place_id = monsters[kills[0]["monsterId"]]["placeId"]
        else:
            area = require(mission, "areaId", "missions.json")
            place_id = area if area in places and area in PLACE_IDS and area != 0 and area not in restricted else starting_place

        requirements = {}
        job = require(mission, "job", "missions.json")
        if job != "不限":
            requirements["jobId"] = jobs[job]
        if require(mission, "sex", "missions.json") != SEX_ANY:
            requirements["sex"] = mission["sex"]
        for field, source in (("prerequisiteId", "requiresMissionId"), ("conflictsWith", "excludesMissionId")):
            target = require(mission, source, "missions.json")
            if target is None:
                continue
            if target == mission_id:
                gaps.field(title, mission_id, name, f"{source} 是自己（{target}），拿掉")
            elif target not in accepted:
                gaps.field(title, mission_id, name, f"{source} {target} 沒有匯入，拿掉這項條件")
            else:
                requirements[field] = f"mission-{target}"
        if mission.get("needSkillId") is not None:
            gaps.field(title, mission_id, name, f"needSkillId {mission['needSkillId']}：本作的任務沒有技能條件")

        reward_items = []
        for reward in require(mission, "rewardItems", "missions.json"):
            item_id = f"item-{reward['itemId']}"
            if item_id in item_ids:
                reward_items.append((item_id, reward["count"]))
            else:
                gaps.field(title, mission_id, name, f"獎勵 {item_id} {reward['item']} 沒有匯入，不給")
        rewards = {"money": require(mission, "rewardMoney", "missions.json")}
        if reward_items:
            rewards["items"] = merge_quantities(reward_items)
        skill_row = require(mission, "rewardSkillId", "missions.json")
        if skill_row is not None:
            skill_id, level = owners.get(skill_row, (None, None))
            if skill_id not in skill_ids or level != 1:
                gaps.field(title, mission_id, name, f"技能獎勵 {skill_row} 沒有匯入或不是第 1 級，不給")
            elif (requirements.get("jobId"), skill_id) not in job_skills:
                gaps.field(title, mission_id, name, f"技能獎勵 {skill_id} 不是任務職業能學的技能，不給")
            else:
                rewards["skill"] = {"skillId": skill_id, "level": 1}
        pet = require(mission, "rewardPet", "missions.json")
        if pet is not None:
            if monster_ok(pet["id"], None) and monsters[pet["id"]].get("missionId") is None:
                rewards["pet"] = {"monsterId": f"monster-{pet['id']}", "level": pet["level"]}
            else:
                gaps.field(title, mission_id, name, f"寵物獎勵 monster-{pet['id']} 沒有匯入或是任務怪，不給")

        record = {"id": f"mission-{mission_id}", "giver": text(mission["npc"]), "title": text(name),
                  "description": text(mission["body"]), "completionMessage": text(mission["endMessage"]),
                  "level": require(mission, "level", "missions.json"), "placeId": place_id, "daily": False}
        if kills:
            record["kill"] = {"monsterId": f"monster-{kills[0]['monsterId']}", "count": kills[0]["count"]}
        if mission["needItems"]:
            record["deliver"] = merge_quantities((f"item-{need['itemId']}", need["count"]) for need in mission["needItems"])
        if requirements:
            record["requirements"] = requirements
        record["rewards"] = rewards
        records.append(record)
    return records, mission_monsters


def daily_mission(mission, gaps, title, item_ids, monsters, places):
    mission_id, name = mission["id"], mission["subject"]
    pools = DAILY_MISSIONS.get(mission_id)
    if pools is None:
        raise SourceError(f"missions.json 的每日任務 {mission_id} 沒有目標池")

    def ordinary(place_ids):
        return [monster_id for monster_id, monster in sorted(monsters.items())
                if monster["placeId"] in place_ids and monster["meet"] == 0 and monster.get("missionId") is None
                and not monster.get("npc") and monster_exclusion(monster, places) is None]

    record = {"id": f"mission-{mission_id}", "giver": text(mission["npc"]), "title": text(name),
              "description": text(mission["body"]), "completionMessage": text(mission["endMessage"]),
              "level": require(mission, "level", "missions.json"), "placeId": None, "daily": True}
    if "kills" in pools:
        kills = []
        for monster_id, count in pools["kills"]:
            if monsters.get(monster_id) is None or monster_exclusion(monsters[monster_id], places) is not None:
                gaps.field(title, mission_id, name, f"目標池的 monster-{monster_id} 沒有匯入，拿掉")
            else:
                kills.append({"monsterId": f"monster-{monster_id}", "count": count})
        record["dailyKills"] = kills
    elif "pets" in pools:
        record["dailyPets"] = [f"monster-{monster_id}" for monster_id in ordinary(pools["pets"])]
    else:
        record["dailyKills"] = [{"monsterId": f"monster-{monster_id}", "count": count}
                                for monster_id in ordinary(pools["bounty"]) for count in BOUNTY_COUNTS]

    if "money" in pools:
        record["dailyRewards"] = [{"money": money} for money in pools["money"]]
    else:
        rewards = []
        for entry in pools["rewards"]:
            items = []
            for item_id, quantity in entry:
                if f"item-{item_id}" in item_ids:
                    items.append((f"item-{item_id}", quantity))
                else:
                    gaps.field(title, mission_id, name, f"報酬池的 item-{item_id} 沒有匯入，不給")
            rewards.append({"money": 0, "items": merge_quantities(items)})
        record["dailyRewards"] = rewards
    return record


# ---- 技能 ----
# 每種技能一列樣板：從每一級的 text 擷取數值的正規表示式，以及用擷取到的數值組出條件、消耗、效果的函式。
# 規則見 design.md「原版技能的發動方式照現行規則」「原版技能的每級數值取自各級說明」。

NUMBER = r"(\d+(?:\.\d+)?)"


def number(text):
    value = float(text)
    return int(value) if value.is_integer() else value


def ratio(percent):
    return round(percent / 100, 6)


def scale(*terms):
    return [{"basis": basis, "ratio": value} for basis, value in terms]


def stat(name, target="Caster", percent=None, amount=None, scaling=None, chance=None):
    effect = {"kind": "StatChange", "target": target, "stat": name}
    if percent is not None:
        effect["percent"] = percent
    if amount is not None:
        effect["amount"] = amount
    if scaling is not None:
        effect["scaling"] = scaling
    if chance is not None:
        effect["chance"] = chance
    return effect


def bonus(attack_kind, scaling, chance=None):
    effect = {"kind": "BonusDamage", "attackKind": attack_kind, "scaling": scaling}
    if chance is not None:
        effect["chance"] = chance
    return effect


def multi_hit(attack_kind, hits):
    return {"kind": "MultiHit", "attackKind": attack_kind, "hits": hits}


def reduction(attack_kind, percent):
    return {"kind": "DamageReduction", "attackKind": attack_kind, "percent": percent}


def restore(resource, **value):
    return {"kind": "Restore", "resource": resource, **value}


def stun(chance):
    return {"kind": "Stun", "chance": chance}


def silence(chance):
    return {"kind": "Silence", "chance": chance}


def above(basis):
    return {"kind": "StatAbove", "stat": basis}


def hp_cost(percent):
    return {"kind": "HitPoints", "percent": percent}


def parts(effects, conditions=(), chance=None, costs=(), all_skill_points=False):
    """conditions：放在 Chance 前面的其他條件；chance：說明裡涵蓋全部效果的機率；costs：SP 以外的消耗。"""
    return {"effects": effects, "conditions": list(conditions), "chance": chance, "costs": list(costs),
            "allSkillPoints": all_skill_points}


ELEMENT_NAMES = {"地": "Earth", "水": "Water", "火": "Fire", "木": "Wood", "風": "Wind", "毒": "Poison"}

SKILL_TEMPLATES = {
    "爆氣": (r"物攻上升#%", lambda n: parts([stat("attack", percent=n)])),
    "魔力增幅": (r"魔攻上升#%", lambda n: parts([stat("magicAttack", percent=n)])),
    # 「連續攻擊 n 次」是額外的次數：總共 n + 1 下（研究文件 at_count = lv + 1）
    "連擊": (r"連續攻擊#次，攻擊力下降#%，命中下降#%",
             lambda hits, attack, hit: parts([multi_hit("Physical", hits + 1), stat("attack", percent=-attack), stat("hitRate", amount=-hit)])),
    "急影": (r"提升速度#%", lambda n: parts([stat("agility", percent=n)])),
    "硬甲之樂": (r"物防提升#%", lambda n: parts([stat("defense", percent=n)])),
    "魔甲之樂": (r"魔防提升#%", lambda n: parts([stat("magicDefense", percent=n)])),
    "防禦心得": (r"減少物攻傷害#%", lambda n: parts([reduction("Physical", n)])),
    "魔力盾": (r"減少魔攻傷害#%", lambda n: parts([reduction("Magic", n)])),
    "破滅一擊": (r"物攻上升#%", lambda n: parts([stat("attack", percent=n)])),
    "嗜血": (r"給予物理傷害的同時，回復HP\(傷害\*#%\)", lambda n: parts([restore("HitPoints", percentOfDamage=n)])),
    "治癒術": (r"回復hp\(智力\*#\)", lambda n: parts([restore("HitPoints", scaling=scale(("intelligence", n)))])),
    "要害狙擊": (r"提升物攻必殺技機率#%", lambda n: parts([stat("physicalCriticalChance", amount=n)])),
    "煙遁": (r"提高逃跑成功率#%", lambda n: parts([stat("escapeRate", amount=n)])),
    "靡靡之音": (r"降低敵人速度#%", lambda n: parts([stat("agility", "Opponent", percent=-n)])),
    "暗月之曲": (r"增加物攻魔攻傷害\(魅力\*#%\)", lambda n: parts([bonus("Any", scale(("charisma", ratio(n))))])),
    "祝福之曲": (r"回復hp\(魅力\*#\)", lambda n: parts([restore("HitPoints", scaling=scale(("charisma", n)))])),
    "捨身一擊": (r"提升物攻必殺技機率#%，物防降低#%",
                 lambda chance, defense: parts([stat("physicalCriticalChance", amount=chance), stat("defense", percent=-defense)])),
    "戰神之怒": (r"提高物理傷害\(力量\*#%\)，命中降低#%",
                 lambda strength, hit: parts([stat("attack", scaling=scale(("strength", ratio(strength)))), stat("hitRate", amount=-hit)])),
    "無我": (r"物攻提高#%，速度上升#%，物攻連擊#次",
             lambda attack, agility, hits: parts([stat("attack", percent=attack), stat("agility", percent=agility), multi_hit("Physical", hits)])),
    "滅世": (r"物攻提高#%，hp減少#%，命中提高#%",
             lambda attack, hp, hit: parts([stat("attack", percent=attack), stat("hitRate", amount=hit)], costs=[hp_cost(hp)])),
    "暈擊": (r"#%機率使敵人無法動作", lambda n: parts([stun(n)])),
    "重擊": (r"#%物防轉換成物攻傷害值", lambda n: parts([bonus("Physical", scale(("defense", ratio(n))))])),
    "反擊": (r"減少物攻傷害#%，物攻上升#%", lambda damage, attack: parts([reduction("Physical", damage), stat("attack", percent=attack)])),
    # 原版是 SP 增加目前 SP 的 n%，HP 扣最大 HP 的 m%
    "仁義": (r"sp增加#%，hp減少#%",
             lambda sp, hp: parts([restore("SkillPoints", scaling=scale(("skillPoints", ratio(sp))))], costs=[hp_cost(hp)])),
    "魔法連擊": (r"魔法連續攻擊#次", lambda hits: parts([multi_hit("Magic", hits + 1)])),
    "魔力吸收": (r"回復sp #%，該回合無法攻擊", lambda n: parts([restore("SkillPoints", percentOfMaximum=n)])),
    "弱化詛咒": (r"降低敵人魔攻物攻#%",
                 lambda n: parts([stat("attack", "Opponent", percent=-n), stat("magicAttack", "Opponent", percent=-n)])),
    "魔力爆發": (r"魔攻增加\(sp\*#\)，並消耗所有sp，hp低於#%情況下才能使用",
                 lambda sp, hp: parts([stat("magicAttack", scaling=scale(("skillPoints", sp)))],
                                      conditions=[{"kind": "HitPointsBelow", "percent": hp}], all_skill_points=True)),
    **{f"魔劍-{element}": (rf"攻擊屬性轉換成{element}屬性", lambda name=name: parts([{"kind": "AttackElement", "element": name}]))
       for element, name in ELEMENT_NAMES.items()},
    **{f"魔盾-{element}": (rf"防禦屬性轉換成{element}屬性", lambda name=name: parts([{"kind": "DefenseElement", "element": name}]))
       for element, name in ELEMENT_NAMES.items()},
    "雷火": (r"魔攻時必殺技機率提升#%", lambda n: parts([stat("magicCriticalChance", amount=n)])),
    "暴雪": (r"降低敵人速度#%，給予敵人固定攻擊傷害加成\(智力\*#%\)",
             lambda agility, smart: parts([stat("agility", "Opponent", percent=-agility), bonus("Any", scale(("intelligence", ratio(smart))))])),
    "劍雨": (r"魔攻比敵人高時，給予敵人固定攻擊傷害加成\(力量\*#%\+智力\*#%\)",
             lambda strength, smart: parts([bonus("Any", scale(("strength", ratio(strength)), ("intelligence", ratio(smart))))],
                                           conditions=[above("magicAttack")])),
    # 原版的物防魔防降低是施放者自己（skill_50）
    "星芒": (r"物防魔防降低#%，減少敵人hp#%",
             lambda defense, hp: parts([stat("defense", percent=-defense), stat("magicDefense", percent=-defense),
                                        {"kind": "PercentDamage", "percent": hp}])),
    "心眼": (r"迴避力提升#%", lambda n: parts([stat("evasion", amount=n)])),
    "背襲": (r"提高命中率#%", lambda n: parts([stat("hitRate", amount=n)])),
    "暗器": (r"提升物攻必殺技機率#%，#%機率增加物攻固定傷害\(敏捷\*#%\)，迴避力降低#%",
             lambda critical, chance, agility, evasion: parts([
                 stat("physicalCriticalChance", amount=critical), bonus("Physical", scale(("agility", ratio(agility))), chance=chance),
                 stat("evasion", amount=-evasion)])),
    "昇龍拳": (r"使用物攻時，給予敵人固定攻擊傷害加成\(物攻\*#%\)", lambda n: parts([bonus("Physical", scale(("attack", ratio(n))))])),
    "流星拳": (r"物攻連擊#次，命中率降低#%", lambda hits, hit: parts([multi_hit("Physical", hits), stat("hitRate", amount=-hit)])),
    "氣波": (r"提高物攻\(體質\*#%\)", lambda n: parts([stat("attack", scaling=scale(("vitality", ratio(n))))])),
    # 原版的附加傷害在範圍內隨機，本作取中間值
    "犧牲": (r"用自身HP #%給予敵人物攻附加傷害\(物攻\*#-#%傷害\)，提高命中#%，提升物攻必殺技機率#%",
             lambda hp, low, high, hit, critical: parts([
                 bonus("Physical", scale(("attack", ratio((low + high) / 2)))), stat("hitRate", amount=hit),
                 stat("physicalCriticalChance", amount=critical)], costs=[hp_cost(hp)])),
    "力量之舞": (r"提高物攻\(魅力\*#%\)", lambda n: parts([stat("attack", scaling=scale(("charisma", ratio(n))))])),
    "魔力之舞": (r"提高魔攻\(魅力\*#%\)", lambda n: parts([stat("magicAttack", scaling=scale(("charisma", ratio(n))))])),
    "敏捷之舞": (r"提高速度\(魅力\*#%\)", lambda n: parts([stat("agility", scaling=scale(("charisma", ratio(n))))])),
    "毀滅之舞": (r"提高物攻魔攻必殺技機率#%",
                 lambda n: parts([stat("physicalCriticalChance", amount=n), stat("magicCriticalChance", amount=n)])),
    "鋒針陷阱": (r"#%機率使敵人暫停行動，並給予傷害加成\(魅力\*#%\)",
                 lambda chance, charisma: parts([stun(100), bonus("Any", scale(("charisma", ratio(charisma))))], chance=chance)),
    "精靈之力": (r"魔攻提升#%", lambda n: parts([stat("magicAttack", percent=n)])),
    "屬性迫害": (r"屬性剋制情況下，提高屬性傷害#%", lambda n: parts([{"kind": "ElementAdvantage", "percent": n}])),
    "燃燒魔力": (r"降低對方#點sp", lambda n: parts([{"kind": "ReduceSkillPoints", "amount": n}])),
    "葬心": (r"#%機率對方無法使用技能", lambda n: parts([silence(n)])),
    "瘟疫的微笑": (r"當智力比對手高時，有#%使癱瘓對手攻擊，使用魔攻時給予敵人固定攻擊傷害加成\(智力\*#%\)",
                   lambda chance, smart: parts([stun(chance), bonus("Magic", scale(("intelligence", ratio(smart))))],
                                               conditions=[above("intelligence")])),
    "斯巴達": (r"力量大於敵人時，增加物攻必殺技機率#%，並有#%機率使敵人無法行動",
               lambda critical, chance: parts([stat("physicalCriticalChance", amount=critical), stun(chance)], conditions=[above("strength")])),
    "壓制": (r"體質大於敵人時，敵人敏捷降低#%，物防降低#%",
             lambda agility, defense: parts([stat("agility", "Opponent", percent=-agility), stat("defense", "Opponent", percent=-defense)],
                                            conditions=[above("vitality")])),
    "施毒": (r"#%機率使敵人，物攻降低#%，並給予固定傷害\(智力\*#%\)",
             lambda chance, attack, smart: parts([stat("attack", "Opponent", percent=-attack),
                                                  {"kind": "DamageOverTime", "scaling": scale(("intelligence", ratio(smart)))}], chance=chance)),
    "明王": (r"增加物攻必殺技機率#%，#%機率使敵人，物防降低#%",
             lambda critical, chance, defense: parts([stat("physicalCriticalChance", amount=critical),
                                                      stat("defense", "Opponent", percent=-defense, chance=chance)])),
    "復仇": (r"HP低於敵人時，HP恢復\(體質\*#%\)，命中率上升#%，物理攻擊提高#%",
             lambda vitality, hit, attack: parts([restore("HitPoints", scaling=scale(("vitality", ratio(vitality)))),
                                                  stat("hitRate", amount=hit), stat("attack", percent=attack)],
                                                 conditions=[{"kind": "HitPointsBelowOpponent"}])),
    "豪鬼": (r"物攻必殺攻擊力上限提升#%，#%機率使敵人無法使用技能",
             lambda cap, chance: parts([stat("physicalCriticalCap", amount=cap), silence(chance)])),
    "煉獄": (r"當智力比對手高時，魔攻時必殺技機率提升#%",
             lambda n: parts([stat("magicCriticalChance", amount=n)], conditions=[above("intelligence")])),
    "熱情律動": (r"魅力大於敵人時，魔攻必殺攻擊力上限提升#%",
                 lambda n: parts([stat("magicCriticalCap", amount=n)], conditions=[above("charisma")])),
    "華爾滋": (r"魔攻必殺技機率提升#%", lambda n: parts([stat("magicCriticalChance", amount=n)])),
    "毒傷陷阱": (r"固定傷害\(智力\*#%\+力量\*#%\)",
                 lambda smart, strength: parts([bonus("Any", scale(("intelligence", ratio(smart)), ("strength", ratio(strength))))])),
    "正義火雷": (r"降低敵人敏捷#", lambda n: parts([stat("agility", "Opponent", amount=-n)])),
    "石人陣": (r"提高物防#", lambda n: parts([stat("defense", amount=n)])),
    "黑渦": (r"降低敵人魅力#", lambda n: parts([stat("charisma", "Opponent", amount=-n)])),
    "獸騎": (r"提高敏捷#", lambda n: parts([stat("agility", amount=n)])),
}

PET_SKILLS = {"物攻命令", "魔攻命令", "戰意驅使", "狂性連結", "魔性連結", "閃電痛擊", "獸靈", "精密陷阱"}
TEAM_SKILLS = {"神頌曲", "地獄禮讚", "詐騙", "友情之舞"}
EXCLUDED_SKILLS = {
    **{name: "寵物技能：等寵物系統的調整確定後再做" for name in PET_SKILLS},
    **{name: "組隊技能：本作沒有隊友" for name in TEAM_SKILLS},
    "假死": "智力比較會改變機率：目錄沒有「條件改變機率」",
}
SKILL_NOTES = {
    "連擊": "「連續攻擊 n 次」是額外的次數，連擊總數 n + 1（與研究文件 at_count = lv + 1 一致）",
    "魔法連擊": "「魔法連續攻擊 n 次」是額外的次數，連擊總數 n + 1（與研究文件 mat_count = lv + 1 一致）",
    "魔力吸收": "「該回合無法攻擊」沒有搬：目前不做「該回合不攻擊」",
    "犧牲": "附加傷害原版在物攻×a～b% 之間隨機，本作取中間值",
    "滅世": "原版是目前 HP 降為 (0.5 + 0.05·lv) 倍，本作是消耗最大 HP 的 n%（HP 不夠就不發動）",
    "星芒": "研究文件寫 (0.9 − 0.01·lv) 倍；原始碼與說明都是第 1 級 10%、每級 +1%，以說明為準",
    "鋒針陷阱": "說明的機率涵蓋全部效果，轉成 Chance 條件，暫停行動寫成 Stun 機率 100",
    "暴雪": "「固定攻擊傷害加成」沒寫物攻或魔攻，寫成 Any",
    "劍雨": "「固定攻擊傷害加成」沒寫物攻或魔攻，寫成 Any",
}
DEFAULT_CHANCE_BASE = 30
DEFAULT_CHANCE_STEP = 10
MAXIMUM_CHANCE = 100


def skill_values(row, template):
    pattern, build = template
    regex = "^" + pattern.replace("#", NUMBER) + "$"
    match = re.fullmatch(regex, row["text"])
    if match is None:
        raise SourceError(f"skills.json 的紀錄 {row['id']}（{row['name']} 第 {row['level']} 級）的說明對不上樣板：{row['text']}")
    return build(*[number(value) for value in match.groups()])


def import_skills(gaps):
    title = "技能"
    gaps.note(title, "原版技能以 `skill-<第 1 級的 id>` 匯入；每級的 `cost` 轉 `money`、`proficiency` 照搬；`description` 留空，`text` 只用來擷取數值。")
    gaps.note(title, "主動技能：說明有涵蓋全部效果的機率就當 `Chance` 條件，否則每級加 `Chance min(100, 30 + 10·lv)`；`sp` 轉成 SP 消耗、`duration` 轉成各效果的 `rounds`。")
    gaps.note(title, "被動技能：說明沒寫條件的是常駐，原版的 `passiveRate`、`sp`、`duration` 不採用；說明有條件的照主動技能的方式轉。")
    gaps.note(title, "寫在「使敵人無法行動」、追加傷害、對手能力降低、封鎖技能上的機率，轉成那個效果自己的 `chance`。")
    gaps.note(title, "`need_sid` 轉成 `prerequisite`；魔劍、魔盾的每種屬性只有一列（原版等級 2～6 是同系列的編號），本作各為 1 級。")
    groups = {}
    for row in sorted(load("skills.json"), key=lambda row: require(row, "id", "skills.json")):
        for field in ("name", "jobId", "level", "passive", "sp", "timing", "duration", "passiveRate", "text", "cost", "proficiency"):
            require(row, field, "skills.json")
        groups.setdefault(row["name"], []).append(row)
    owners = {row["id"]: (rows[0]["id"], index + 1) for rows in groups.values() for index, row in enumerate(rows)}
    excluded = {rows[0]["id"] for name, rows in groups.items() if name in EXCLUDED_SKILLS}

    records, job_skills = [], []
    for name, rows in groups.items():
        first = rows[0]
        if name in EXCLUDED_SKILLS:
            gaps.exclude(title, first["id"], name, EXCLUDED_SKILLS[name])
            continue
        if name not in SKILL_TEMPLATES:
            raise SourceError(f"skills.json 的技能 {first['id']}（{name}）沒有樣板")
        if name in SKILL_NOTES:
            gaps.field(title, first["id"], name, SKILL_NOTES[name])
        if first["master"]:
            gaps.field(title, first["id"], name, "原版 s_master 1：不在學習列表、由任務獎勵取得；本作照職業關聯表也能學")

        levels = []
        always_on = False
        for index, row in enumerate(rows):
            level = index + 1
            values = skill_values(row, SKILL_TEMPLATES[name])
            always_on = row["passive"] and not values["conditions"] and values["chance"] is None
            if always_on:
                conditions, costs, rounds = [], [], 0
            else:
                chance = values["chance"]
                if chance is None:
                    chance = min(MAXIMUM_CHANCE, DEFAULT_CHANCE_BASE + DEFAULT_CHANCE_STEP * level)
                conditions = values["conditions"] + [{"kind": "Chance", "percent": chance}]
                skill_points = {"kind": "SkillPoints", "all": True} if values["allSkillPoints"] else {"kind": "SkillPoints", "amount": row["sp"]}
                costs = [skill_points] + values["costs"]
                rounds = row["duration"]
            levels.append({
                "level": level, "money": row["cost"], "proficiency": row["proficiency"], "cooldown": 0,
                "conditions": conditions, "costs": costs,
                "effects": [{**effect, "rounds": rounds} for effect in values["effects"]],
            })
        if always_on:
            gaps.field(title, first["id"], name, f"原版被動發動率 {first['passiveRate']}%，本作常駐")

        skill_id = f"skill-{first['id']}"
        record = {"id": skill_id, "name": name, "slotType": "Passive" if first["passive"] else "Active",
                  "timing": "AfterHit" if first["timing"] == 2 else "BeforeAttack", "description": "", "maxLevel": len(levels)}
        required = first["requiresSkillId"]
        if required:
            if required not in owners:
                raise SourceError(f"skills.json 的紀錄 {first['id']} 的 requiresSkillId {required} 不存在")
            owner, owner_level = owners[required]
            if owner in excluded:
                gaps.field(title, first["id"], name, f"前置技能 {owner} 沒有匯入，不設前置技能")
            else:
                record["prerequisite"] = {"skillId": f"skill-{owner}", "level": owner_level}
        record["levels"] = levels
        records.append(record)
        job_skills.append({"jobId": f"job-{first['jobId']}", "skillId": skill_id})

    return records, job_skills


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
    # gaps.md 依內容種類固定順序
    for title in ("道具", "職業", "怪物", "技能", "任務", "配方", "傭兵", "勳章所", KEPT_TITLE):
        gaps.section(title)
    try:
        items = import_items(gaps)
        item_ids = {item["id"] for item in items}
        mercenaries = import_mercenaries(gaps)
        skills, job_skills = import_skills(gaps)
        missions, mission_monsters = import_missions(
            gaps, item_ids, {skill["id"] for skill in skills}, {(row["jobId"], row["skillId"]) for row in job_skills})
        kept = gaps.section(KEPT_TITLE)
        kept["notes"] += [f"`{item['id']}` {KEPT_CUSTOM_ITEMS[item['id']]}" for item in items if item["id"] in KEPT_CUSTOM_ITEMS]
        outputs = {
            "items.json": {"items": items},
            "jobs.json": {"jobs": import_jobs(gaps)},
            "monsters.json": {"monsters": import_monsters(gaps, item_ids, mission_monsters)},
            "recipes.json": {"recipes": import_recipes(gaps, item_ids)},
            "mercenaries.json": {"mercenaries": mercenaries},
            "honour-shop.json": {"exchanges": import_honour_shop(gaps, item_ids, {row["id"] for row in mercenaries})},
            "skills.json": {"skills": skills},
            "job-skills.json": {"jobSkills": job_skills},
            "missions.json": {"missions": missions},
        }
        kept["notes"] += [f"`{exchange['id']}`：兌換保留的自訂道具 `{exchange['itemId']}`"
                          for exchange in outputs["honour-shop.json"]["exchanges"] if exchange.get("itemId") in KEPT_CUSTOM_ITEMS]
        kept["notes"] += KEPT_CONTENT_FILES
    except (SourceError, KeyError) as error:
        print(f"匯入失敗：{error}", file=sys.stderr)
        return 1

    for name, root in outputs.items():
        (CONTENT / name).write_text(format_content(root), encoding="utf-8")
    GAPS.write_text(gaps.render(), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
