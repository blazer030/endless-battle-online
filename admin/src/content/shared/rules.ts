export const requiredText = (label: string) => ({
  required: true,
  whitespace: true,
  message: `請填寫${label}`,
})

export const requiredNumber = (label: string) => ({ required: true, message: `請填寫${label}` })

// 技能等級不能超過所選技能的上限；技能改變時要重新檢查，所以每次都從表單讀目前選的技能
export const skillLevelRule = (
  skills: readonly { readonly id: string; readonly name: string; readonly maxLevel: number }[],
  selectedSkillId: () => unknown,
) => ({
  validator: async (_: unknown, level: number | null) => {
    const skill = skills.find((option) => option.id === selectedSkillId())
    if (skill && level !== null && level > skill.maxLevel) {
      throw new Error(`${skill.name}的等級上限是 ${skill.maxLevel}`)
    }
  },
})
