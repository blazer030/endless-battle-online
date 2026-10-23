#!/usr/bin/env python3
"""把 WOG 4.5 釋出版的 SQL dump（WOG4.5_sql_CT.rar 內的 .sql）整理成 Markdown 與 JSON。

用法：python3 export.py <wog45.sql> [輸出目錄，預設為本檔所在目錄]

只處理遊戲內容表（怪物、道具、技能、職業、配方、任務、榮譽商店、傭兵、鑰匙、交易所）；
玩家、公會、聊天等執行期資料一律略過。欄位語意依 docs/research/wog-research-*.md。
"""
import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

ELEMENTS = {"0": "無", "1": "地", "2": "水", "3": "火", "4": "木", "5": "風", "6": "毒"}
ITEM_TYPES = {
    "0": "武器", "1": "頭部", "2": "身體", "3": "手部", "4": "腳部", "5": "道具",
    "6": "即時使用物", "7": "魔石", "8": "勳章", "9": "鑰匙", "10": "精鍊石", "11": "補給品",
}
STAT_NAMES = {
    "str": "力", "smart": "智", "agi": "敏", "agl": "敏", "life": "生命", "vit": "體質", "au": "魅力", "be": "信仰",
    "at": "物攻", "mat": "魔攻", "df": "物防", "mdf": "魔防", "hp": "HP", "sp": "SP", "hpmax": "HP上限",
}
USED_TYPES = {
    "0": "能力藥水（永久加能力／經驗）", "1": "背包擴充", "2": "寶箱（抽魔石）", "3": "倉庫擴充",
    "7": "行動力藥水", "9": "密藥（限時倍率）",
}
ELIXIR_LV = {"1": "經驗", "2": "技能經驗", "3": "物攻", "4": "魔攻", "5": "物防", "6": "魔防", "7": "HP", "8": "SP"}
HONOUR_TYPES = {"1": "傭兵", "2": "鑰匙", "3": "武器", "4": "頭部", "5": "身體", "6": "手部", "7": "腳部", "8": "道具"}
REFINE_TYPES = {  # wog_plus_list.d_type 1～18，名稱取自精鍊石的 d_name 前綴
}
# 地點名稱取自本作 backend/content/world.json（移植自 WOG js），wog_area_lv 的 id 與 m_place 不同
PLACES = {
    0: ("全域", []), 99: ("特殊", []),
    1: ("中央平原", ["香茅綠地", "綠風草地", "白沙海灣", "眠獸洞穴"]),
    2: ("試鍊洞窟", ["燈影隧道", "晶礦源地", "狹窄地道", "地底空洞"]),
    3: ("黑暗沼澤", ["巨大沼澤", "腐植草原", "狂火祭壇", "狩者墳場"]),
    4: ("迷霧森林", ["微風樹林", "密林古城", "幻境花園", "濃霧曠野"]),
    5: ("古代遺跡", ["灰雨祭壇", "藤蔓古道", "地底通道", "地下遺跡"]),
    6: ("久遠戰場", ["古老戰地", "接境戰場", "征戰遺址", "赭紅荒原"]),
    7: ("王者之路", ["試煉之道", "邪靈棧橋", "中央高塔", "熔岩斷層"]),
    8: ("幻獸森林", ["沉靜叢林", "秘密獸道", "光輝草原", "精靈幻境"]),
    9: ("星河異界", ["流星之丘", "星絲銀河", "三角祕境", "次元蟲洞"]),
    10: ("灼熱荒漠", ["幻覺沙丘", "海市蜃樓", "大漠絲路", "炎獸沙漠"]),
    11: ("無淵洞窟", ["妖魔聚落", "毒氣沼澤", "大地裂痕", "牲祭洞窟"]),
    12: ("天空之城", ["雲端城堡", "輝煌都市", "天使之廳", "封神殿堂"]),
    13: ("水晶之間", ["幻像步道", "晶花水池", "結晶礦脈", "水晶王座"]),
    14: ("失落古船", ["星空海岸", "古船墳地", "潮汐洞穴", "大幽靈船"]),
    15: ("最果之島", ["黃金沙灘", "猛獸森林", "密林湖泊", "荒者山峰"]),
    16: ("冷峰寒地", ["寒氣苔原", "覆雪大地", "暴風雪山", "極寒冰峰"]),
    17: ("廢棄洞窟", ["幽暗礦坑", "濕漉岩壁", "魔怪階梯", "魔實驗場"]),
    18: ("日沒碉堡", ["刺影高牆", "螺旋迴廊", "機關巨塔", "碉堡中樞"]),
    19: ("靜止之城", ["凝滯地帶", "污泥洞穴", "回朔空間", "片斷時空"]),
    20: ("黑曜神廟", []),
    21: ("血之魔域", ["十字墓地", "惡魔遺蹟", "暗紅神殿", "焦池煉獄"]),
}


# ---------- SQL 解析 ----------

def load(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="replace")


def columns(sql: str, table: str) -> list[str]:
    m = re.search(r"CREATE TABLE (?:IF NOT EXISTS )?`%s` \((.*?)\)\s*(ENGINE|TYPE)=" % table, sql, re.S)
    if not m:
        return []
    return re.findall(r"^\s*`([^`]+)`", m.group(1), re.M)


def tokenize(body: str) -> list[list[str]]:
    rows, cur, buf, inq, depth, i = [], [], "", False, 0, 0
    while i < len(body):
        ch = body[i]
        if inq:
            if ch == "\\":
                buf += body[i + 1]
                i += 2
                continue
            if ch == "'":
                inq = False
            else:
                buf += ch
        elif ch == "'":
            inq = True
        elif ch == "(":
            depth, cur, buf = 1, [], ""
        elif ch == ")":
            cur.append(buf.strip())
            rows.append(cur)
            depth, buf = 0, ""
        elif ch == "," and depth:
            cur.append(buf.strip())
            buf = ""
        elif depth:
            buf += ch
        i += 1
    return rows


def rows(sql: str, table: str) -> list[dict]:
    cols = columns(sql, table)
    out = []
    for blk in re.findall(r"INSERT INTO `%s`[^;]*?VALUES\s*(.*?);\n" % table, sql, re.S):
        out += [dict(zip(cols, r)) for r in tokenize(blk)]
    return out


# ---------- 小工具 ----------

def n(v) -> int:
    try:
        return int(float(v))
    except (TypeError, ValueError):
        return 0


def nz(v) -> str:
    """表格用：0 或空白顯示為 －"""
    return "－" if v in ("", "0", "NULL", None, 0) else str(v)


def md_table(headers: list[str], body: list[list]) -> str:
    out = ["| " + " | ".join(headers) + " |", "|" + "---|" * len(headers)]
    for r in body:
        out.append("| " + " | ".join(str(c).replace("|", "／").replace("\n", " ") for c in r) + " |")
    return "\n".join(out) + "\n"


def pairs(text: str) -> list[tuple[int, int]]:
    """'id*num,id*num' → [(id, num)]"""
    out = []
    for part in (text or "").split(","):
        part = part.strip()
        if not part:
            continue
        if "*" in part:
            a, b = part.split("*", 1)
            i, c = n(a), n(b)
        else:
            i, c = n(part), 1
        if i:
            out.append((i, c))
    return out


def rng(text: str) -> str:
    return "" if text in ("", "NULL") else text.replace(",", "～")


# ---------- 匯出 ----------

def main() -> None:
    sql = load(Path(sys.argv[1]))
    out = Path(sys.argv[2]) if len(sys.argv) > 2 else Path(__file__).parent
    data = out / "data"
    data.mkdir(parents=True, exist_ok=True)

    items = rows(sql, "wog_df")
    monsters = rows(sql, "wog_monster")
    skills = rows(sql, "wog_ch_skill")
    jobs = rows(sql, "wog_character")
    recipes = rows(sql, "wog_syn")
    missions = rows(sql, "wog_mission_main")
    repeats = rows(sql, "wog_mission_repeat")
    honour = rows(sql, "wog_honor_main")
    stones = rows(sql, "wog_stone_list")
    refines = rows(sql, "wog_plus_list")
    used = rows(sql, "wog_df_used")
    keys = rows(sql, "wog_key_main")
    mercs = rows(sql, "wog_mercenary_main")
    exchange = rows(sql, "wog_exchange_main")
    area_lv = rows(sql, "wog_area_lv")

    item_by_id = {n(r["d_id"]): r for r in items}
    skill_by_id = {n(r["s_id"]): r for r in skills}
    monster_by_id = {n(r["m_id"]): r for r in monsters}
    job_by_id = {n(r["ch_id"]): r for r in jobs}

    def item_name(i: int) -> str:
        r = item_by_id.get(i)
        return f"{r['d_name']}（{i}）" if r else f"道具 {i}"

    def skill_name(i: int) -> str:
        r = skill_by_id.get(i)
        return f"{r['s_name']} Lv{r['s_lv']}" if r else f"技能 {i}"

    def monster_name(i: int) -> str:
        r = monster_by_id.get(i)
        return f"{r['m_name']}（{i}）" if r else f"怪物 {i}"

    def job_name(i) -> str:
        if str(i) == "99":
            return "不限"
        r = job_by_id.get(n(i))
        return r["ch_name"] if r else str(i)

    # ===== 怪物 =====
    mon_json = []
    by_place = defaultdict(list)
    for m in monsters:
        drops = [(n(i), n(t)) for i, t in zip((m["d_id"] or "").split(","), (m["m_topr"] or "").split(",")) if n(i)]
        sk = []
        for k in range(1, 6):
            sid = n(m.get(f"skill_{k}"))
            if sid:
                sk.append({"id": sid, "name": skill_name(sid), "rate": n(m.get(f"time_{k}")) if k <= 3 else None})
        subs = []
        mask = n(m["m_subplace"])
        names = PLACES.get(n(m["m_place"]), ("", []))[1]
        for b in range(4):
            if mask & (1 << b):
                subs.append(names[b] if b < len(names) else f"子地區 {b + 1}")
        rec = {
            "id": n(m["m_id"]), "name": m["m_name"], "level": n(m["m_lv"]), "element": ELEMENTS.get(m["s_property"], m["s_property"]),
            "hp": n(m["hp"]), "sp": n(m["sp"]), "at": n(m["at"]), "df": n(m["df"]), "mat": n(m["mat"]), "mdf": n(m["mdf"]), "agi": n(m["agi"]),
            "str": n(m["str"]), "smart": n(m["smart"]), "life": n(m["life"]), "vit": n(m["vit"]), "au": n(m["au"]), "be": n(m["be"]),
            "specialAttack": m["m_sat_name"], "jobExp": n(m["m_job_exp"]), "meet": n(m["m_meet"]),
            "placeId": n(m["m_place"]), "place": PLACES.get(n(m["m_place"]), (f"地點 {m['m_place']}", []))[0], "subPlaces": subs,
            "image": m["m_img"], "missionId": n(m["m_mission"]) or None, "npc": n(m["m_npc"]) or None,
            "npcMessage": m["m_npc_message"] if m["m_npc_message"] not in ("", "NULL") else None,
            "skills": sk,
            "drops": [{"itemId": i, "item": item_by_id.get(i, {}).get("d_name", f"道具 {i}"), "topr": t,
                       "chance": f"1/{int(t * 0.9)}" if t > 1 else "必掉"} for i, t in drops],
        }
        mon_json.append(rec)
        by_place[rec["placeId"]].append(rec)
    (data / "monsters.json").write_text(json.dumps(mon_json, ensure_ascii=False, indent=1), encoding="utf-8")

    md = ["# WOG 4.5 怪物總表（464 隻）\n",
          "來源：`wog_monster`。遭遇率欄是 `m_meet`：每次遇敵擲一次 0～100，`m_meet` 不高於擲值的怪才入選，所以 0 最常見、數字愈大愈稀有。"
          "掉落機率 = 1／⌊topr×0.9⌋（行動力不足時分母再 ×1.5）。技能後的百分比是發動率（`time_1～3`）。"
          "子地區依 `m_subplace` 位元遮罩還原；地點名稱取自本作 world.json。\n",
          "欄位：Lv／屬性／HP／SP／物攻／物防／魔攻／魔防／敏捷，七項能力依序為 力、智、生命、體質、魅力、信仰（`str, smart, life, vit, au, be`）。\n"]
    md.append("## 目錄\n")
    for pid in sorted(by_place):
        md.append(f"- [{PLACES.get(pid, (f'地點 {pid}', []))[0]}](#地點-{pid}) — {len(by_place[pid])} 隻")
    md.append("")
    for pid in sorted(by_place):
        name = PLACES.get(pid, (f"地點 {pid}", []))[0]
        md.append(f"## 地點 {pid}\n\n### {name}\n")
        body = []
        for r in sorted(by_place[pid], key=lambda x: (x["level"], x["id"])):
            flags = []
            if r["missionId"]:
                flags.append(f"任務 {r['missionId']}")
            if r["npc"]:
                flags.append("NPC")
            body.append([
                r["id"], r["name"], r["level"], r["element"], r["hp"], r["sp"], r["at"], r["df"], r["mat"], r["mdf"], r["agi"],
                f"{r['str']}/{r['smart']}/{r['life']}/{r['vit']}/{r['au']}/{r['be']}",
                nz(r["specialAttack"]), r["meet"], r["jobExp"],
                "、".join(r["subPlaces"]) or "－",
                "；".join(f"{s['name']}" + (f" {s['rate']}%" if s["rate"] else "") for s in r["skills"]) or "－",
                "；".join(f"{d['item']} {d['chance']}" for d in r["drops"]) or "－",
                "、".join(flags) or "－",
            ])
        md.append(md_table(["id", "名稱", "Lv", "屬", "HP", "SP", "物攻", "物防", "魔攻", "魔防", "敏", "力/智/生/體/魅/信", "必殺技", "遭遇", "職業exp", "子地區", "技能", "掉落", "備註"], body))
    (out / "monsters.md").write_text("\n".join(md), encoding="utf-8")

    # ===== 道具 =====
    def stat_bonus(r: dict) -> str:
        parts = []
        for k in ("str", "smart", "agi", "life", "vit", "au", "be"):
            v = n(r.get(f"d_g_{k}"))
            if v:
                parts.append(f"{STAT_NAMES[k]}{v:+d}")
        return "、".join(parts) or "－"

    def requirement(r: dict) -> str:
        parts = []
        for col, k in (("d_mstr", "str"), ("d_msmart", "smart"), ("d_magl", "agi"), ("d_mau", "au")):
            v = n(r.get(col))
            if v:
                parts.append(f"{STAT_NAMES[k]}{v}")
        return "、".join(parts) or "－"

    item_json = []
    groups: dict[str, list[dict]] = defaultdict(list)
    for r in items:
        rec = {
            "id": n(r["d_id"]), "name": r["d_name"], "type": r["d_type"], "typeName": ITEM_TYPES.get(r["d_type"], r["d_type"]),
            "field": r["d_fie"], "price": n(r["d_money"]), "shopLevel": n(r["d_lv"]), "shopBanned": n(r["d_dbst"]) != 0,
            "bound": n(r["d_send"]) == 1, "element": ELEMENTS.get(r["d_s"]) if r["d_s"] not in ("NULL", "") else None,
            "at": n(r["d_at"]), "mat": n(r["d_mat"]), "df": n(r["d_df"]), "mdf": n(r["d_mdf"]),
            "bonus": {k: n(r.get(f"d_g_{k}")) for k in ("str", "smart", "agi", "life", "vit", "au", "be") if n(r.get(f"d_g_{k}"))},
            "recoverHp": n(r["d_g_hp"]), "recoverSp": n(r["d_g_sp"]),
            "requires": {k: n(r.get(c)) for c, k in (("d_mstr", "str"), ("d_msmart", "smart"), ("d_magl", "agi"), ("d_mau", "au")) if n(r.get(c))},
            "holes": n(r["d_hole"]), "refineMax": n(r["d_plus"]), "skillId": n(r["skill"]) or None, "skillRate": n(r["stime"]) or None,
        }
        item_json.append(rec)
        groups[r["d_type"]].append(rec)
    (data / "items.json").write_text(json.dumps(item_json, ensure_ascii=False, indent=1), encoding="utf-8")

    md = ["# WOG 4.5 道具總表\n",
          f"來源：`wog_df`，共 {len(items)} 列、{len({r['d_name'] for r in items})} 個不同名稱。"
          "同名多列是「孔數／精鍊上限不同的變體」（打洞成功會把裝備換成孔數 +1 的另一個 id），表中同名裝備只列一次，變體 id 與孔數寫在最後一欄。\n",
          "欄位：商店欄是商店分頁等級 1～7（0 表示不在商店；`d_dbst≠0` 也不會進商店，標為「禁售」）；賣給 NPC 得售價一半；"
          "需求比對玩家基礎能力（力／智／敏／魅）；綁定（`d_send=1`）不可贈送、轉倉、交易。\n"]
    md.append("## 目錄\n")
    for t in sorted(groups, key=int):
        md.append(f"- [{ITEM_TYPES.get(t, t)}（type {t}）](#type-{t}) — {len({g['name'] for g in groups[t]})} 種、{len(groups[t])} 列")
    md.append("")
    for t in sorted(groups, key=int):
        md.append(f"## type {t}\n\n### {ITEM_TYPES.get(t, t)}\n")
        merged: dict[str, list[dict]] = defaultdict(list)
        for g in groups[t]:
            merged[g["name"]].append(g)
        body = []
        equip = t in ("0", "1", "2", "3", "4", "5")
        for name, vs in sorted(merged.items(), key=lambda kv: (kv[1][0]["shopLevel"] or 99, kv[1][0]["price"], kv[1][0]["id"])):
            b = vs[0]
            variants = "；".join(f"{v['id']}:{v['holes']}孔/精{v['refineMax']}" for v in vs) if len(vs) > 1 else f"{b['holes']}孔/精{b['refineMax']}"
            flags = []
            if b["shopBanned"]:
                flags.append("禁售")
            if b["bound"]:
                flags.append("綁定")
            if b["skillId"]:
                flags.append(f"技能 {skill_name(b['skillId'])}" + (f" {b['skillRate']}%" if b["skillRate"] else ""))
            if equip:
                body.append([b["id"], name, nz(b["shopLevel"]), b["price"], nz(b["at"]), nz(b["mat"]), nz(b["df"]), nz(b["mdf"]),
                             "、".join(f"{STAT_NAMES[k]}{v:+d}" for k, v in b["bonus"].items()) or "－",
                             "、".join(f"{STAT_NAMES[k]}{v}" for k, v in b["requires"].items()) or "－",
                             b["element"] or "－", variants, "、".join(flags) or "－"])
            else:
                extra = []
                if b["recoverHp"] or b["recoverSp"]:
                    extra.append(f"回復 HP{b['recoverHp']}／SP{b['recoverSp']}")
                if b["bonus"]:
                    extra.append("、".join(f"{STAT_NAMES[k]}{v:+d}" for k, v in b["bonus"].items()))
                body.append([b["id"], name, nz(b["shopLevel"]), b["price"], "；".join(extra) or "－",
                             "、".join(str(v["id"]) for v in vs) if len(vs) > 1 else "－", "、".join(flags) or "－"])
        if equip:
            md.append(md_table(["id", "名稱", "商店", "售價", "物攻", "魔攻", "物防", "魔防", "能力加成", "需求", "屬", "孔/精鍊上限（變體）", "備註"], body))
        else:
            md.append(md_table(["id", "名稱", "商店", "售價", "效果", "其他 id", "備註"], body))
    (out / "items.md").write_text("\n".join(md), encoding="utf-8")

    # ===== 魔石、精鍊石、消耗品效果 =====
    md = ["# WOG 4.5 魔石、精鍊石與道具效果\n", "## 魔石（`wog_stone_list`）\n",
          "鑲嵌在裝備孔位。`d_class` 1～3 是寶箱抽選的分級（≤60% 抽 class1、61～90% class2、91～100% class3）；99 是鑲嵌當下擲骰的隨機魔石，數值欄為範圍。\n"]
    body = []
    stone_json = []
    for s in sorted(stones, key=lambda x: (n(x["d_lv"]), n(x["d_class"]), n(x["d_id"]))):
        eff = []
        for k in ("at", "mat", "df", "mdf", "agl", "str", "life", "vit", "smart", "au", "be", "hpmax"):
            v = s.get(f"s_{k}", "0")
            if v not in ("0", "", "NULL"):
                eff.append(f"{STAT_NAMES[k]}{'+' if '-' not in v else ''}{v}")
        stone_json.append({"id": n(s["d_id"]), "name": item_by_id.get(n(s["d_id"]), {}).get("d_name"), "level": n(s["d_lv"]), "class": n(s["d_class"]),
                           "effects": {k: s.get(f"s_{k}") for k in ("at", "mat", "df", "mdf", "agl", "str", "life", "vit", "smart", "au", "be", "hpmax") if s.get(f"s_{k}", "0") not in ("0", "", "NULL")}})
        body.append([s["d_id"], item_by_id.get(n(s["d_id"]), {}).get("d_name", "？"), s["d_lv"], s["d_class"], "、".join(eff) or "－"])
    md.append(md_table(["id", "名稱", "Lv", "class", "效果"], body))
    (data / "magic-stones.json").write_text(json.dumps(stone_json, ensure_ascii=False, indent=1), encoding="utf-8")

    md.append("\n## 精鍊石（`wog_plus_list`）\n")
    md.append("精鍊第 N 級必須用 Lv N 的石頭，上限為裝備的精鍊上限；能力欄是「最小～最大」隨機。同類合成必成 +1 級，異類合成得 −1 級。\n")
    body = []
    refine_json = []
    for s in sorted(refines, key=lambda x: (n(x["d_type"]), n(x["d_lv"]))):
        eff = []
        for k in ("at", "mat", "df", "mdf", "str", "agi", "smart", "life", "vit", "au", "be"):
            v = s.get(f"d_{k}", "")
            if v not in ("", "NULL", "0"):
                eff.append(f"{STAT_NAMES[k]}+{rng(v)}")
        nm = item_by_id.get(n(s["d_id"]), {}).get("d_name", "？")
        refine_json.append({"id": n(s["d_id"]), "name": nm, "type": n(s["d_type"]), "level": n(s["d_lv"]),
                            "effects": {k: s.get(f"d_{k}") for k in ("at", "mat", "df", "mdf", "str", "agi", "smart", "life", "vit", "au", "be") if s.get(f"d_{k}", "") not in ("", "NULL", "0")}})
        body.append([s["d_id"], nm, s["d_type"], s["d_lv"], "、".join(eff) or "－"])
    md.append(md_table(["id", "名稱", "種類", "Lv", "效果（每級隨機範圍）"], body))
    (data / "refine-stones.json").write_text(json.dumps(refine_json, ensure_ascii=False, indent=1), encoding="utf-8")

    md.append("\n## 使用型道具效果（`wog_df_used`）\n")
    md.append("能力藥水永久加基礎能力與經驗；擴充券是「設為」該值不是累加；密藥在 `use_time` 秒內把對應項目乘上倍率。\n")
    body = []
    used_json = []
    for u in sorted(used, key=lambda x: n(x["d_id"])):
        base = item_by_id.get(n(u["d_id"]), {})
        eff = []
        for k in ("str", "smart", "agi", "life", "vit", "au", "be"):
            v = n(base.get(f"d_g_{k}"))
            if v:
                eff.append(f"{STAT_NAMES[k]}{v:+d}")
        if n(u["d_g_exp"]):
            eff.append(f"經驗/行動力 +{u['d_g_exp']}")
        if n(u["d_g_bag"]):
            eff.append(f"容量設為 {u['d_g_bag']}")
        if u["d_type"] == "9":
            eff.append(f"{ELIXIR_LV.get(u['d_lv'], u['d_lv'])} 倍率（{u['use_time']} 秒）")
        for k in ("exp", "skill_exp", "at", "mat", "df", "mdf", "hp", "sp"):
            if u.get(k, "0") not in ("0", "", "NULL"):
                eff.append(f"{k} ×{u[k]}")
        if n(u["skill_id"]):
            eff.append(f"技能 {skill_name(n(u['skill_id']))}")
        used_json.append({"id": n(u["d_id"]), "name": base.get("d_name"), "kind": USED_TYPES.get(u["d_type"], u["d_type"]), "raw": u})
        body.append([u["d_id"], base.get("d_name", "？"), USED_TYPES.get(u["d_type"], u["d_type"]), "、".join(eff) or "－"])
    md.append(md_table(["id", "名稱", "種類", "效果"], body))
    (data / "consumable-effects.json").write_text(json.dumps(used_json, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "stones-and-consumables.md").write_text("\n".join(md), encoding="utf-8")

    # ===== 職業與技能 =====
    md = ["# WOG 4.5 職業與技能\n", "## 職業（`wog_character`）\n",
          "成長欄是每升一級的隨機範圍（最小～最大）。解鎖等級 `ch_mlv`；進階職要求前置職業熟練度 ≥ `ch_map`（前端以 3500 為 100%）。\n"]
    body = []
    job_json = []
    for j in sorted(jobs, key=lambda x: n(x["ch_id"])):
        parent = job_by_id.get(n(j["ch_main"]), {}).get("ch_name", "－") if n(j["ch_main"]) else "－"
        job_json.append({"id": n(j["ch_id"]), "name": j["ch_name"], "unlockLevel": n(j["ch_mlv"]), "parentId": n(j["ch_main"]) or None, "parent": None if parent == "－" else parent,
                         "requiredProficiency": n(j["ch_map"]),
                         "growth": {k: j[f"ch_{k}"] for k in ("str", "smart", "agi", "life", "vit", "au", "be")}})
        body.append([j["ch_id"], j["ch_name"], j["ch_mlv"], parent, nz(j["ch_map"]),
                     rng(j["ch_str"]), rng(j["ch_smart"]), rng(j["ch_agi"]), rng(j["ch_life"]), rng(j["ch_vit"]), rng(j["ch_au"]), rng(j["ch_be"])])
    md.append(md_table(["id", "職業", "解鎖Lv", "前置職業", "需熟練度", "力", "智", "敏", "生命", "體質", "魅力", "信仰"], body))
    (data / "jobs.json").write_text(json.dumps(job_json, ensure_ascii=False, indent=1), encoding="utf-8")

    md.append(f"\n## 技能（`wog_ch_skill`，{len(skills)} 列）\n")
    md.append("同一技能每級一列，依職業分節、同名合併。主動技能裝在 1～3 欄，發動率由玩家設定；被動技能裝在 4～5 欄，發動率為「被動率」欄（`stime`）。"
              "學習條件：該職業可用熟練度 ≥「熟練度」且金錢 ≥「費用」，並已學前一級。時機：0 攻擊前、2 攻擊後帶傷害。\n")
    skill_json = []
    by_job = defaultdict(lambda: defaultdict(list))
    for s in skills:
        by_job[n(s["ch_id"])][(n(s["main_sid"]), s["s_name"])].append(s)
        skill_json.append({"id": n(s["s_id"]), "mainId": n(s["main_sid"]), "jobId": n(s["ch_id"]), "job": job_name(s["ch_id"]), "name": s["s_name"], "level": n(s["s_lv"]),
                           "passive": s["type"] == "1", "sp": n(s["s_sp"]), "timing": n(s["s_st"]), "duration": n(s["s_count"]), "passiveRate": n(s["stime"]),
                           "text": s["s_text"], "cost": n(s["s_money"]), "proficiency": n(s["ch_exp"]), "requiresSkillId": n(s["need_sid"]) or None, "master": n(s["s_master"])})
    for jid in sorted(by_job):
        md.append(f"### {job_name(jid)}（職業 {jid}）\n")
        body = []
        for (mid, name), levels in sorted(by_job[jid].items(), key=lambda kv: n(kv[1][0]["s_id"])):
            levels.sort(key=lambda x: n(x["s_lv"]))
            first = levels[0]
            kind = "被動" if first["type"] == "1" else "主動"
            per = "<br>".join(f"Lv{l['s_lv']}（{l['s_id']}）：{l['s_text']}；SP {l['s_sp']}" + (f"、{l['s_count']} 回合" if n(l["s_count"]) > 1 else "")
                              + (f"、被動率 {l['stime']}%" if n(l["stime"]) else "") + f"；熟練 {l['ch_exp']}、{l['s_money']} 元" for l in levels)
            body.append([mid, name, kind, first["s_st"], per])
        md.append(md_table(["主id", "技能", "類型", "時機", "各等級"], body))
    (data / "skills.json").write_text(json.dumps(skill_json, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "jobs-and-skills.md").write_text("\n".join(md), encoding="utf-8")

    # ===== 配方 =====
    md = ["# WOG 4.5 合成配方（`wog_syn`）\n", "每次合成 1000 元（成功才扣），材料不論成敗都消耗，成功率逐次獨立擲骰。任務配方要先接過指定任務。\n"]
    body = []
    recipe_json = []
    for r in sorted(recipes, key=lambda x: n(x["syn_id"])):
        mats = pairs(r["syn_element"])
        recipe_json.append({"id": n(r["syn_id"]), "resultId": n(r["syn_result"]), "result": item_by_id.get(n(r["syn_result"]), {}).get("d_name"), "amount": n(r["syn_num"]),
                            "successRate": n(r["syn_topr"]), "materials": [{"itemId": i, "item": item_by_id.get(i, {}).get("d_name"), "count": c} for i, c in mats],
                            "missionRecipe": n(r["syn_mission"]) != 0, "requiresMissionId": n(r["syn_need_mission"]) or None})
        body.append([r["syn_id"], item_name(n(r["syn_result"])), r["syn_num"], f"{r['syn_topr']}%",
                     "、".join(f"{item_name(i)}×{c}" for i, c in mats), "任務配方" if n(r["syn_mission"]) else "－", nz(r["syn_need_mission"])])
    md.append(md_table(["id", "成品", "數量", "成功率", "材料", "類型", "需任務"], body))
    (data / "recipes.json").write_text(json.dumps(recipe_json, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "recipes.md").write_text("\n".join(md), encoding="utf-8")

    # ===== 任務 =====
    repeat_by_id = {n(r["m_id"]): r for r in repeats}
    md = ["# WOG 4.5 任務（`wog_mission_main`）\n",
          f"共 {len(missions)} 個。條件：等級 ≥ Lv、職業符合或不限、性別（3 不限）、需先完成「前置」、未完成「互斥」。"
          "目標：擊殺怪物（id×數量）、繳交道具、寵物。獎勵：道具（id×數量）、金錢、技能、寵物、交易所物資。"
          "每日任務（`wog_mission_repeat`）每次重置會隨機換目標與獎勵。\n"]
    mission_json = []
    def item_list(prefix: str, m: dict) -> list[dict]:
        out = []
        for col in ("a_id", "d_head_id", "d_body_id", "d_hand_id", "d_foot_id", "d_item_id", "d_stone_id", "d_honor_id", "d_plus_id"):
            for i, c in pairs(m.get(f"{prefix}{col}", "")):
                out.append({"itemId": i, "item": item_by_id.get(i, {}).get("d_name"), "count": c})
        return out
    body = []
    for m in sorted(missions, key=lambda x: n(x["m_id"])):
        need = item_list("n_", m)
        give = item_list("g_", m)
        kills = [{"monsterId": i, "monster": monster_by_id.get(i, {}).get("m_name"), "count": c} for i, c in pairs(m["m_monster_id"])]
        rewards = ["、".join(f"{g['item']}×{g['count']}" for g in give)]
        if n(m["g_money"]):
            rewards.append(f"{m['g_money']} 元")
        if n(m["g_skill_id"]):
            rewards.append(f"技能 {skill_name(n(m['g_skill_id']))}")
        if n(m["g_pet_id"]):
            rewards.append(f"寵物 {monster_name(n(m['g_pet_id']))} Lv{m['g_pet_lv']}")
        if n(m["ex_id"]):
            rewards.append(f"物資 {m['ex_id']}×{m['ex_num']}")
        rec = {"id": n(m["m_id"]), "subject": m["m_subject"], "npc": m["m_name"], "level": n(m["m_lv"]), "job": job_name(m["m_job"]), "sex": n(m["m_sex"]),
               "requiresMissionId": n(m["m_need_id"]) or None, "excludesMissionId": n(m["m_not_id"]) or None, "storeId": n(m["m_store_id"]), "areaId": n(m["m_area_id"]),
               "kills": kills, "petId": n(m["m_pet_id"]) or None, "needItems": need, "needSkillId": n(m["n_skill_id"]) or None,
               "rewardItems": give, "rewardMoney": n(m["g_money"]), "rewardSkillId": n(m["g_skill_id"]) or None, "rewardPet": {"id": n(m["g_pet_id"]), "level": n(m["g_pet_lv"])} if n(m["g_pet_id"]) else None,
               "repeatable": n(m["m_repeat"]) != 0, "daily": repeat_by_id.get(n(m["m_id"])) is not None, "body": m["m_body"].replace("rn", "\n"), "endMessage": m["m_end_message"].replace("rn", "\n")}
        mission_json.append(rec)
        body.append([m["m_id"], m["m_subject"], m["m_name"], m["m_lv"], job_name(m["m_job"]), {"1": "男", "2": "女"}.get(m["m_sex"], "不限"),
                     nz(m["m_need_id"]), nz(m["m_not_id"]),
                     "、".join(f"{k['monster']}×{k['count']}" for k in kills) or "－",
                     "、".join(f"{g['item']}×{g['count']}" for g in need) or "－",
                     "；".join(x for x in rewards if x) or "－",
                     "每日" if rec["daily"] else ("可重複" if rec["repeatable"] else "－")])
    md.append(md_table(["id", "任務", "委託人", "Lv", "職業", "性別", "前置", "互斥", "擊殺", "繳交", "獎勵", "重複"], body))
    md.append("\n## 任務文本\n")
    for rec in mission_json:
        md.append(f"### {rec['id']} {rec['subject']}（{rec['npc']}）\n\n{rec['body'].strip()}\n\n**完成：**{rec['endMessage'].strip()}\n")
    (data / "missions.json").write_text(json.dumps(mission_json, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "missions.md").write_text("\n".join(md), encoding="utf-8")

    # ===== 榮譽商店、鑰匙、傭兵、交易所、推薦等級 =====
    md = ["# WOG 4.5 榮譽商店、鑰匙、傭兵、交易所\n", "## 榮譽商店（`wog_honor_main`）\n", "以五種榮譽道具兌換，`h_1～h_5` 是各需幾個。\n"]
    body = []
    honour_json = []
    for h in sorted(honour, key=lambda x: n(x["h_id"])):
        kind = HONOUR_TYPES.get(h["h_type"], h["h_type"])
        target = mercs[n(h["item_id"]) - 1]["me_name"] if h["h_type"] == "1" and 0 < n(h["item_id"]) <= len(mercs) else item_name(n(h["item_id"]))
        cost = {f"h_{k}": n(h[f"h_{k}"]) for k in range(1, 6) if n(h[f"h_{k}"])}
        honour_json.append({"id": n(h["h_id"]), "kind": kind, "itemId": n(h["item_id"]), "item": target, "count": n(h["item_num"]), "cost": cost})
        body.append([h["h_id"], kind, target, h["item_num"], "、".join(f"榮譽{k[-1]}×{v}" for k, v in cost.items())])
    md.append(md_table(["id", "類型", "商品", "數量", "代價"], body))
    (data / "honour-shop.json").write_text(json.dumps(honour_json, ensure_ascii=False, indent=1), encoding="utf-8")

    md.append("\n## 地區鑰匙（`wog_key_main`）\n")
    md.append(md_table(["道具", "開放地點", "有效秒數"], [[item_name(n(k["d_id"])), PLACES.get(n(k["area_id"]), (k["area_id"], []))[0], k["area_time"]] for k in keys]))
    md.append("\n## 傭兵（`wog_mercenary_main`）\n")
    md.append("次數每勝一場 −1，敗一場 −「戰敗扣」；三個百分比是傭兵代打時金錢、道具、經驗的取得比例。`me_status=1` 的不在商店販售（榮譽商店兌換）。\n")
    md.append(md_table(["id", "名稱", "價格", "次數", "金錢%", "道具%", "經驗%", "戰敗扣", "商店"],
                       [[m["me_id"], m["me_name"], m["me_money"], m["me_count"], m["me_get_money"], m["me_get_item"], m["me_get_exp"], m["me_die"], "否" if m["me_status"] == "1" else "是"] for m in mercs]))
    (data / "mercenaries.json").write_text(json.dumps([{k: v for k, v in m.items()} for m in mercs], ensure_ascii=False, indent=1), encoding="utf-8")
    md.append("\n## 交易所物資（`wog_exchange_main`）\n")
    md.append("公會系統用的物資，單機不需要，列出名稱備查。\n")
    md.append(md_table(["id", "名稱", "基準價"], [[e["ex_id"], e["ex_name"], e["ex_money"]] for e in exchange]))
    md.append("\n## 推薦等級（`wog_area_lv`）\n")
    md.append("前端「選擇地點」顯示的建議等級；此表的 id 是自己的順序，不等於 `m_place`。\n")
    md.append(md_table(["名稱", "建議 Lv"], [[a["area_name"], f"{a['min_lv']}～{a['max_lv']}"] for a in area_lv]))
    (out / "shops-and-misc.md").write_text("\n".join(md), encoding="utf-8")

    # ===== 地點 =====
    place_json = [{"id": pid, "name": nm, "subPlaces": subs, "monsters": len(by_place.get(pid, []))} for pid, (nm, subs) in sorted(PLACES.items())]
    (data / "places.json").write_text(json.dumps(place_json, ensure_ascii=False, indent=1), encoding="utf-8")

    # ===== README =====
    counts = Counter(r["d_type"] for r in items)
    names = Counter(r["d_name"] for r in items)
    md = ["# WOG 4.5 原版內容資料\n",
          "從 WOG 4.5 繁體釋出版的資料庫 SQL（`WOG4.5_sql_CT.rar` → `WOG4.5_sql_utf-8.sql`，2026-10-23 由使用者提供）整理出來的遊戲內容。"
          "SQL 本身不放進 repo；本目錄的 Markdown 與 `data/*.json` 就是記錄，`export.py` 可從 SQL 重新產生。"
          "欄位語意依 `docs/research/wog-research-*.md`，當初標為「資料表遺失」的數值以這裡為準。\n",
          "## 檔案\n",
          "| 文件 | 內容 | JSON |",
          "|---|---|---|",
          f"| [`monsters.md`](monsters.md) | 怪物 {len(monsters)} 隻，依地點分節：數值、必殺技、技能與發動率、掉落與機率、任務怪／NPC | `data/monsters.json` |",
          f"| [`items.md`](items.md) | 道具 {len(items)} 列（{len(names)} 種名稱），依類型分節；裝備含數值、需求、孔數、精鍊上限與變體 | `data/items.json` |",
          f"| [`stones-and-consumables.md`](stones-and-consumables.md) | 魔石 {len(stones)} 顆、精鍊石 {len(refines)} 顆的數值，使用型道具效果 {len(used)} 項 | `data/magic-stones.json`、`data/refine-stones.json`、`data/consumable-effects.json` |",
          f"| [`jobs-and-skills.md`](jobs-and-skills.md) | 職業 {len(jobs)} 種成長與轉職條件；技能 {len(skills)} 列依職業與等級 | `data/jobs.json`、`data/skills.json` |",
          f"| [`recipes.md`](recipes.md) | 合成配方 {len(recipes)} 條 | `data/recipes.json` |",
          f"| [`missions.md`](missions.md) | 任務 {len(missions)} 個：條件、目標、獎勵、全文 | `data/missions.json` |",
          f"| [`shops-and-misc.md`](shops-and-misc.md) | 榮譽商店 {len(honour)} 項、地區鑰匙、傭兵、交易所物資、推薦等級 | `data/honour-shop.json`、`data/mercenaries.json` |",
          "| － | 地點與子地區對照（名稱取自本作 `world.json`） | `data/places.json` |",
          "\n## 數量總覽\n",
          f"- 怪物 {len(monsters)}：{len(PLACES) - 2} 個地點，任務怪 {sum(1 for m in monsters if n(m['m_mission']))}、NPC {sum(1 for m in monsters if n(m['m_npc']))}，"
          f"獨立圖檔 {len({m['m_img'] for m in monsters})} 張，等級 {min(n(m['m_lv']) for m in monsters)}～{max(n(m['m_lv']) for m in monsters)}。",
          "- 道具（不同名稱）：" + "、".join(f"{ITEM_TYPES.get(t, t)} {len({r['d_name'] for r in items if r['d_type'] == t})}" for t in sorted(counts, key=int)) + "。",
          f"- 職業 {len(jobs)}、技能 {len({(s['ch_id'], s['main_sid'], s['s_name']) for s in skills})} 種（{len(skills)} 列）、配方 {len(recipes)}、任務 {len(missions)}（每日 {len(repeats)}）、榮譽商店 {len(honour)}、傭兵 {len(mercs)}。",
          "\n## 沒有整理的表\n",
          "公會（`wog_group_*`、`wog_build_*`、`wog_weapon_*`）、玩家與存檔（`wog_player*`、`wog_item`、`wog_skill_book`、`wog_stone_setup`、`wog_plus_setup`、`wog_pet`、`wog_message`、`wog_log_main`）、"
          "聊天與論壇。這些是執行期資料或確定不做的多人系統。\n",
          "## 已知限制\n",
          "- 怪物子地區用 `m_subplace` 位元遮罩還原；黑曜神廟（20）沒有子地區。",
          "- 怪物技能欄 `skill_1～5` 以 `wog_ch_skill.s_id` 查名稱；查不到的顯示為「技能 id」。",
          "- 任務文本中的 `rn` 是原資料的換行符殘留，已轉成換行。",
          "- 精鍊石的「種類」1～18 只在 `wog_plus_list.d_type` 出現，名稱對照見精鍊石名稱前綴。",
          "- `wog_area_lv` 的 id 與 `m_place` 編號不同，只當建議等級參考。",
          "\n## 重新產生\n",
          "```bash\npython3 docs/research/wog45/export.py /path/to/WOG4.5_sql_utf-8.sql\n```\n"]
    (out / "README.md").write_text("\n".join(md), encoding="utf-8")
    print("done:", out)


if __name__ == "__main__":
    main()
