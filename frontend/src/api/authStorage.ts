const accessTokenKey = "inkbound.accessToken"

export const readAccessToken = (): string | null => {
  try {
    return localStorage.getItem(accessTokenKey)
  } catch {
    return null
  }
}

export const saveAccessToken = (token: string): void => {
  try {
    localStorage.setItem(accessTokenKey, token)
  } catch {
    // 儲存不可用時，token 只在本次請求流程有效
  }
}

export const clearAccessToken = (): void => {
  try {
    localStorage.removeItem(accessTokenKey)
  } catch {
    // 無法存取儲存空間時沒有東西可清
  }
}
