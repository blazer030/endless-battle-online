// 角色頁（登記證、能力頁、技能頁、彈窗、版位）的純資料，畫面只負責排版
import type { CharacterVo, DerivedStats } from "./character"
import type { CreationOptionsVo } from "./creationOptions"
import { formatNumber } from "./format"
import type { LearnableSkill, SkillDetailKind, SkillDetailRow, SkillJob } from "./skillOverview"
import { FullProficiency, LearnConditionText, prerequisiteText, proficiencyPercent } from "./skills"
import { baseStatLabels } from "./stats"

export type CardBar = { readonly text: string; readonly ratio: number }

export type CharacterCard = {
  readonly name: string
  readonly jobName: string
  readonly level: string
  readonly proficiencyPercent: string
  readonly proficiencyText: string
  readonly proficiencyRatio: number
  readonly sexName: string
  readonly elementName: string
  readonly birthName: string
  readonly healthPoints: CardBar
  readonly manaPoints: CardBar
  readonly experience: CardBar
}

const ratioOf = (value: number, maximum: number) =>
  maximum <= 0 ? 0 : Math.min(1, Math.max(0, value / maximum))

const nameOf = (options: readonly { id: number; name: string }[], id: number) =>
  options.find((option) => option.id === id)?.name ?? ""

const bar = (value: number, maximum: number): CardBar => ({
  text: `${formatNumber(value)} / ${formatNumber(maximum)}`,
  ratio: ratioOf(value, maximum),
})

export const characterCard = (
  character: CharacterVo,
  options: CreationOptionsVo,
): CharacterCard => {
  const proficiency =
    character.jobProficiency.find((entry) => entry.jobId === character.jobId)?.proficiency ?? 0
  const experienceRatio = ratioOf(character.experience, character.nextLevelExperience)
  return {
    name: character.name,
    jobName: character.jobName,
    level: `Lv ${formatNumber(character.level)}`,
    proficiencyPercent: `${proficiencyPercent(proficiency)}%`,
    proficiencyText: `${formatNumber(proficiency)} / ${formatNumber(FullProficiency)}`,
    proficiencyRatio: ratioOf(proficiency, FullProficiency),
    sexName: nameOf(options.sexes, character.sex),
    elementName: nameOf(options.elements, character.element),
    birthName: nameOf(options.births, character.birth),
    healthPoints: bar(character.healthPoints, character.stats.maxHealthPoints),
    manaPoints: bar(character.manaPoints, character.stats.maxManaPoints),
    experience: { text: `${Math.floor(experienceRatio * 100)}%`, ratio: experienceRatio },
  }
}

export type AbilityRow = {
  readonly label: string
  readonly value: string
  readonly bonus: string | null
}

// 裸點＋裝備加成（有效值－裸點），加成為 0 時不寫
export const abilityRows = (character: CharacterVo): AbilityRow[] =>
  baseStatLabels.map(({ key, label }) => {
    const bonus = character.stats[key] - character.baseStats[key]
    return {
      label,
      value: formatNumber(character.baseStats[key]),
      bonus: bonus === 0 ? null : `${bonus > 0 ? "+" : "-"}${formatNumber(Math.abs(bonus))}`,
    }
  })

const combatLabels: { key: keyof DerivedStats; label: string }[] = [
  { key: "attack", label: "物攻" },
  { key: "magicAttack", label: "魔攻" },
  { key: "defense", label: "物防" },
  { key: "magicDefense", label: "魔防" },
  { key: "maxHealthPoints", label: "HP 上限" },
  { key: "maxManaPoints", label: "SP 上限" },
]

export const combatRows = (character: CharacterVo) =>
  combatLabels.map(({ key, label }) => ({ label, value: formatNumber(character.stats[key]) }))

const romanNumerals = ["I", "II", "III", "IV", "V"]
const classRanks = ["見習", "正式", "精英", "大師", "傳奇"]
const classIndex = (tier: number) => Math.min(Math.max(tier, 1), romanNumerals.length) - 1

export type JobSwitcher = {
  readonly header: {
    readonly jobId: string
    readonly name: string
    readonly current: boolean
    readonly subtitle: string
    readonly skillPoints: string
  }
  readonly groups: readonly {
    readonly title: string
    readonly jobs: readonly {
      readonly jobId: string
      readonly name: string
      readonly note: string
      readonly selected: boolean
    }[]
  }[]
}

// 技能頁上方的職業切換：標頭是正在看的職業；清單只列當過的職業、依 Class 分組
export const jobSwitcher = (jobs: readonly SkillJob[], shownJobId: string): JobSwitcher => {
  const shown = jobs.find((job) => job.jobId === shownJobId) ?? jobs[0]
  const learned = shown.skills.filter((skill) => skill.level > 0).length
  const index = classIndex(shown.tier)
  const tiers = [...new Set(jobs.map((job) => classIndex(job.tier)))].toSorted((a, b) => a - b)
  return {
    header: {
      jobId: shown.jobId,
      name: shown.jobName,
      current: shown.current,
      subtitle: `Class ${romanNumerals[index]}・${classRanks[index]}・已學 ${learned}${shown.current ? ` / ${shown.skills.length}` : ""} 招`,
      skillPoints: formatNumber(shown.skillPoints),
    },
    groups: tiers.map((tier) => ({
      title: `CLASS ${romanNumerals[tier]}・${classRanks[tier]}`,
      jobs: jobs
        .filter((job) => classIndex(job.tier) === tier)
        .map((job) => ({
          jobId: job.jobId,
          name: job.jobName,
          note: job.current ? "現職" : `技能點 ${formatNumber(job.skillPoints)}`,
          selected: job.jobId === shown.jobId,
        })),
    })),
  }
}

const detailLabels: Record<SkillDetailKind, string> = {
  Trigger: "發動",
  Cost: "消耗",
  Effect: "效果",
  Cooldown: "冷卻",
}

export type DetailLine = {
  readonly label: string
  readonly text: string
  readonly changed: boolean
}

// changed：跟前一份同種類的列文字不同（沒有前一份時都不標）
const detailLines = (
  rows: readonly SkillDetailRow[],
  previous: readonly SkillDetailRow[] | null,
): DetailLine[] =>
  rows.map((row) => ({
    label: detailLabels[row.kind],
    text: row.text,
    changed:
      previous !== null && previous.find((before) => before.kind === row.kind)?.text !== row.text,
  }))

const pipsOf = (level: number, maxLevel: number) =>
  Array.from({ length: maxLevel }, (_, index) => index < level)

export type SkillCardFooter =
  | { readonly kind: "max" }
  | {
      readonly kind: "learn"
      readonly costLabel: string
      readonly money: string
      readonly moneyShort: boolean
      readonly skillPoints: string
      readonly skillPointsShort: boolean
      readonly button: string
      readonly disabled: boolean
      readonly reasons: string | null
    }

export type SkillCard = {
  readonly levelText: string
  readonly pips: readonly boolean[]
  readonly typeLabel: string
  readonly description: string
  readonly currentRows: readonly DetailLine[] | null
  readonly nextRows: readonly DetailLine[] | null
  readonly footer: SkillCardFooter
}

const unmetReasons = (skill: LearnableSkill) => {
  const { prerequisite, next } = skill
  if (next === null || next.canLearn) return null
  const reasons = [
    ...(prerequisite === null || prerequisite.met
      ? []
      : [prerequisiteText(prerequisite.name, prerequisite.level)]),
    ...(next.enoughSkillPoints ? [] : [LearnConditionText.notEnoughProficiency]),
    ...(next.enoughMoney ? [] : [LearnConditionText.notEnoughMoney]),
  ]
  return reasons.length === 0 ? null : reasons.join("・")
}

export const skillCard = (skill: LearnableSkill): SkillCard => {
  const learned = skill.level > 0
  const { next } = skill
  return {
    levelText: learned ? `Lv ${skill.level}/${skill.maxLevel}` : "未習得",
    pips: pipsOf(skill.level, skill.maxLevel),
    typeLabel: skill.slotType === "Active" ? "主動" : "被動",
    description: skill.description === "" ? "（說明待補）" : skill.description,
    currentRows: skill.rows === null ? null : detailLines(skill.rows, null),
    nextRows: next === null ? null : detailLines(next.rows, skill.rows),
    footer:
      next === null
        ? { kind: "max" }
        : {
            kind: "learn",
            costLabel: learned ? "升級需要" : "學習需要",
            money: formatNumber(next.money),
            moneyShort: !next.enoughMoney,
            skillPoints: formatNumber(next.skillPoints),
            skillPointsShort: !next.enoughSkillPoints,
            button: learned ? "升級" : "學習",
            disabled: !next.canLearn,
            reasons: unmetReasons(skill),
          },
  }
}

export type LearnedPopup = {
  readonly title: string
  readonly name: string
  readonly levelText: string
  // 分開排版用：習得時 fromLevel 為 null
  readonly fromLevel: number | null
  readonly toLevel: number
  readonly maxLevel: number
  readonly mastered: boolean
  readonly pips: readonly boolean[]
  readonly rows: readonly DetailLine[]
}

// 升級成功（舊 → 新）或習得成功（從 0 學到 1）；新等級的數值列，跟上一級不同的標出來
export const learnedPopup = (before: LearnableSkill, after: LearnableSkill): LearnedPopup => {
  const learned = before.level === 0
  return {
    title: learned ? "習得成功" : "升級成功",
    name: after.name,
    levelText: learned
      ? `Lv ${after.level} / ${after.maxLevel}`
      : `Lv ${before.level} → Lv ${after.level} / ${after.maxLevel}`,
    fromLevel: learned ? null : before.level,
    toLevel: after.level,
    maxLevel: after.maxLevel,
    mastered: after.level >= after.maxLevel,
    pips: pipsOf(after.level, after.maxLevel),
    rows: detailLines(after.rows ?? [], learned ? null : before.rows),
  }
}

const ConnectionFailedText = "連線失敗"

// 失敗彈窗的字：reasons 為 null 表示沒連到伺服器；取第一個認得的原因，都不認得也算連線失敗
export const learnFailureText = (reasons: readonly string[] | null, skill: LearnableSkill) => {
  const texts: Record<string, string> = {
    NOT_ENOUGH_MONEY: LearnConditionText.notEnoughMoney,
    NOT_ENOUGH_PROFICIENCY: LearnConditionText.notEnoughProficiency,
    MAX_LEVEL: LearnConditionText.maxLevel,
    WRONG_JOB: LearnConditionText.wrongShownJob,
    NOT_LEARNED: LearnConditionText.notLearned,
    ...(skill.prerequisite === null
      ? {}
      : { PREREQUISITE: prerequisiteText(skill.prerequisite.name, skill.prerequisite.level) }),
  }
  const known = (reasons ?? []).find((reason) => reason in texts)
  return known === undefined ? ConnectionFailedText : texts[known]
}

export type CharacterLayout = {
  readonly scale: number
  readonly deskTop: number
  readonly portraitTop: number
  readonly portraitHeight: number
  readonly feetCenter: number
  readonly journalTop: number
}

// 上半（立繪、登記證）跟高度縮放 0.88～1；桌緣＝登記證下緣＋7；立繪腳底藏在桌緣下 6；手帳從桌緣下 40 到底
export const characterLayout = (stageHeight: number): CharacterLayout => {
  const scale = 0.88 + ((stageHeight - 624) / 156) * 0.12
  const deskTop = 16 + 329 * scale + 7
  const portraitHeight = 336 * scale
  return {
    scale,
    deskTop,
    portraitTop: deskTop + 6 - portraitHeight,
    portraitHeight,
    feetCenter: 110.5 * scale - 10,
    journalTop: deskTop + 40,
  }
}

// 立繪雙腳中心離圖片中線多遠（以圖片高為 1，正＝偏右；量圖得到，換立繪要重量）；[男, 女]，沒量過的是 0
const feetOffsets: Record<string, readonly [number, number]> = {
  "job-6": [0, 0.01],
  "job-7": [0.035, 0.046],
  "job-8": [-0.02, 0.008],
  "job-9": [0.011, -0.012],
}

export const portraitOffset = (jobId: string, sex: number) =>
  feetOffsets[jobId]?.[sex === 2 ? 1 : 0] ?? 0

// 從城門口「角色」紙籤展開：城門口那段不跟高度縮放，圓心固定
export const characterRevealOrigin = () => ({ x: 311, y: 193 })
