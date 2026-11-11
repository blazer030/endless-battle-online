import { buffKinds, type Buff } from "../game/buffs"
import { parseNumber, parseObject, parseOneOf, parseTimestamp } from "./parse"

export const toBuff = (value: unknown, path: string): Buff => {
  const dto = parseObject(value, path)
  return {
    kind: parseOneOf(dto.kind, buffKinds, `${path}.kind`),
    multiplier: parseNumber(dto.multiplier, `${path}.multiplier`),
    endsAt: parseTimestamp(dto.endsAt, `${path}.endsAt`),
  }
}
