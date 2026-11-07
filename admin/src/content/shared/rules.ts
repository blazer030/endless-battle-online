export const requiredText = (label: string) => ({
  required: true,
  whitespace: true,
  message: `請填寫${label}`,
})

export const requiredNumber = (label: string) => ({ required: true, message: `請填寫${label}` })
