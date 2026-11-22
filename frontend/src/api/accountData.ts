import type { Account } from "../game/account"
import { parseNumber, parseObject, parseString } from "./parse"

export const AccountConverter = {
  toVo(value: unknown, path = "account"): Account {
    const dto = parseObject(value, path)
    return {
      accountId: parseNumber(dto.accountId, `${path}.accountId`),
      username: parseString(dto.username, `${path}.username`),
    }
  },
}
