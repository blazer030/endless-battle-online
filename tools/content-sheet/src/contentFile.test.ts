import { readdir, readFile } from "node:fs/promises"
import { join } from "node:path"
import { describe, expect, test } from "vitest"
import { formatContentFile, parseContentFile } from "./contentFile.ts"
import { contentDirectory } from "./testing/paths.ts"

describe("parseContentFile", () => {
  test("ParseContentFile_KeepsEachRecordsOriginalText", () => {
    const text = '{\n  "rooms": [\n    {"id": "a", "multiplier": 1.10},\n    {"id": "b"}\n  ]\n}\n'

    const file = parseContentFile(text)

    expect(file.properties).toEqual([
      {
        name: "rooms",
        value: null,
        raw: null,
        records: [
          { value: { id: "a", multiplier: 1.1 }, raw: '{"id": "a", "multiplier": 1.10}' },
          { value: { id: "b" }, raw: '{"id": "b"}' },
        ],
      },
    ])
  })

  test("ParseContentFile_KeepsScalarPropertiesText", () => {
    const text = '{\n  "seconds": 28800,\n  "rooms": []\n}\n'

    const file = parseContentFile(text)

    expect(file.properties).toEqual([
      { name: "seconds", value: 28800, raw: "28800", records: null },
      { name: "rooms", value: [], raw: "[]", records: null },
    ])
  })
})

describe("formatContentFile", () => {
  test("FormatContentFile_WritesChangedValuesInline", () => {
    const text = formatContentFile([
      { name: "seconds", value: 60, raw: null, records: null },
      {
        name: "rooms",
        value: null,
        raw: null,
        records: [
          { value: { id: "a", tags: [1, 2], note: '說"明' }, raw: null },
          { value: { id: "b" }, raw: '{"id": "b"}' },
        ],
      },
    ])

    expect(text).toBe(
      '{\n  "seconds": 60,\n  "rooms": [\n    {"id": "a", "tags": [1, 2], "note": "說\\"明"},\n    {"id": "b"}\n  ]\n}\n',
    )
  })

  test("FormatContentFile_ReproducesEveryContentFile", async () => {
    const names = (await readdir(contentDirectory)).filter((name) => name.endsWith(".json"))
    expect(names.length).toBeGreaterThan(10)
    for (const name of names) {
      const text = await readFile(join(contentDirectory, name), "utf8")

      expect(formatContentFile(parseContentFile(text).properties), name).toBe(text)
    }
  })
})
