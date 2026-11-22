// 登入的帳號（GET /api/accounts/me），由 api/accountData.ts 轉換
export type Account = { readonly accountId: number; readonly username: string }
