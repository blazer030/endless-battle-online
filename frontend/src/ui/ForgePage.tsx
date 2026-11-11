import { useCallback, useEffect, useId, useState } from "react"
import type { ApiClient } from "../api/client"
import {
  canDrill,
  crossMergeableStones,
  crossMergeMaxPairs,
  crossMergePartners,
  DrillSuccessPercent,
  drillResultText,
  forgeTargets,
  hasTool,
  holeSlots,
  isAtRefineMax,
  maxSynthesisAttempts,
  mergeablePairs,
  mergeableStones,
  recipeText,
  refinePreview,
  refineResultText,
  refineStoneOptions,
  socketStoneOptions,
  synthesisButtonText,
  synthesisResultText,
  unsocketResultText,
  type ForgeOption,
} from "../game/forge"
import { canAfford, ForgeTools, itemTitle } from "../game/inventory"
import type { RecipeVo } from "../game/forge"
import type { InventoryItem, InventoryVo } from "../game/inventory"
import type { CharacterVo } from "../game/character"
import { ItemName, QuantityStepper, TabPanel, Tabs, type Mutate, type Outcome } from "./bagWidgets"
import "./ForgePage.css"

export type ForgeApi = Pick<
  ApiClient,
  | "refine"
  | "socket"
  | "unsocket"
  | "drill"
  | "mergeStones"
  | "mergeCrossStones"
  | "getRecipes"
  | "synthesize"
>

type Section = "refine" | "socket" | "synthesis"

const sections: { id: Section; label: string }[] = [
  { id: "refine", label: "精鍊" },
  { id: "socket", label: "鑲嵌" },
  { id: "synthesis", label: "合成" },
]

type Props = {
  api: ForgeApi
  inventory: InventoryVo
  character: CharacterVo
  busy: boolean
  mutate: Mutate
}

// 送出鍛造請求；成功時顯示 action 回傳的結果文案，失敗時顯示錯誤
type Run = (action: () => Promise<Outcome & { result: string }>) => Promise<void>

type SectionProps = {
  api: ForgeApi
  inventory: InventoryVo
  busy: boolean
  run: Run
}

export function ForgePage({ api, inventory, character, busy, mutate }: Props) {
  const tabsId = useId()
  const [section, setSection] = useState<Section>("refine")
  const [result, setResult] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const run: Run = async (action) => {
    setResult(null)
    setError(null)
    const message = await mutate(async () => {
      const outcome = await action()
      setResult(outcome.result)
      return outcome
    })
    setError(message)
  }

  const shared = { api, inventory, busy, run }
  return (
    <section className="bag__card">
      <Tabs
        id={tabsId}
        label="鍛造項目"
        options={sections}
        selected={section}
        onSelect={(next) => {
          setSection(next)
          setResult(null)
          setError(null)
        }}
      />
      <TabPanel tabsId={tabsId} selected={section}>
        {section === "refine" && <RefineSection {...shared} />}
        {section === "socket" && <SocketSection {...shared} />}
        {section === "synthesis" && <SynthesisSection {...shared} money={character.money} />}
        {result !== null && (
          <p className="forge__result" role="status">
            {result}
          </p>
        )}
        {error !== null && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
      </TabPanel>
    </section>
  )
}

// 選擇的裝備以 inventoryId 記住，背包換成回應內容後仍指向同一件
const useTarget = (inventory: InventoryVo) => {
  const targets = forgeTargets(inventory)
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const target = targets.find((item) => item.inventoryId === selectedId) ?? targets[0] ?? null
  return { targets, target, select: setSelectedId }
}

function TargetPicker({
  inventory,
  targets,
  target,
  onSelect,
}: {
  inventory: InventoryVo
  targets: InventoryItem[]
  target: InventoryItem
  onSelect: (inventoryId: number) => void
}) {
  const equippedIds = new Set(inventory.equipment.map(({ item }) => item.inventoryId))
  return (
    <label className="bag__field">
      選擇裝備
      <select
        className="forge__select"
        value={target.inventoryId}
        onChange={(event) => onSelect(Number(event.target.value))}
      >
        {targets.map((item) => (
          <option key={item.inventoryId} value={item.inventoryId}>
            {equippedIds.has(item.inventoryId) ? `${itemTitle(item)}（裝備中）` : itemTitle(item)}
          </option>
        ))}
      </select>
    </label>
  )
}

function NoTargets() {
  return <p className="bag__empty">沒有可鍛造的裝備</p>
}

function StoneChoices({
  name,
  options,
  selectedId,
  blockedNote,
  onSelect,
}: {
  name: string
  options: ForgeOption[]
  selectedId: number | null
  blockedNote: string
  onSelect: (inventoryId: number) => void
}) {
  return (
    <ul className="bag__list">
      {options.map(({ stone, allowed }) => (
        <li key={stone.inventoryId}>
          <label className="forge__choice">
            <input
              type="radio"
              name={name}
              checked={allowed && stone.inventoryId === selectedId}
              disabled={!allowed}
              onChange={() => onSelect(stone.inventoryId)}
            />
            <ItemName item={stone} />
            {!allowed && <span className="forge__note">{blockedNote}</span>}
          </label>
        </li>
      ))}
    </ul>
  )
}

function RefineSection({ api, inventory, busy, run }: SectionProps) {
  const groupName = useId()
  const { targets, target, select } = useTarget(inventory)
  const [stoneId, setStoneId] = useState<number | null>(null)
  const [useSafeStone, setUseSafeStone] = useState(false)

  if (target === null)
    return (
      <>
        <NoTargets />
        <MergeStones api={api} inventory={inventory} busy={busy} run={run} />
        <CrossMergeStones api={api} inventory={inventory} busy={busy} run={run} />
      </>
    )

  const options = refineStoneOptions(inventory, target)
  const stone = options.find((option) => option.allowed && option.stone.inventoryId === stoneId)
  const hasSafeStone = hasTool(inventory, ForgeTools.safeRefineStone)
  const atMax = isAtRefineMax(target)
  const safe = useSafeStone && hasSafeStone

  const refine = (selected: InventoryItem) =>
    run(async () => {
      const response = await api.refine(target.inventoryId, selected.inventoryId, safe)
      return {
        inventory: response.inventory,
        result: refineResultText(
          response.success,
          target.name,
          target.refineLevel + 1,
          selected.name,
          safe,
        ),
      }
    })

  return (
    <>
      <TargetPicker
        inventory={inventory}
        targets={targets}
        target={target}
        onSelect={(next) => {
          select(next)
          setStoneId(null)
        }}
      />
      <p className="forge__preview">{atMax ? "已達上限" : refinePreview(target)}</p>
      {options.length === 0 ? (
        <p className="bag__empty">背包裡沒有精鍊石</p>
      ) : (
        <StoneChoices
          name={groupName}
          options={options}
          selectedId={stoneId}
          blockedNote="等級不符"
          onSelect={setStoneId}
        />
      )}
      <div className="bag__buttons">
        <label className="forge__check">
          <input
            type="checkbox"
            checked={safe}
            disabled={!hasSafeStone}
            onChange={(event) => setUseSafeStone(event.target.checked)}
          />
          使用安全石
        </label>
        <button
          type="button"
          className="bag__action"
          disabled={busy || atMax || stone === undefined}
          onClick={() => {
            if (stone !== undefined) void refine(stone.stone)
          }}
        >
          精鍊
        </button>
      </div>
      <MergeStones api={api} inventory={inventory} busy={busy} run={run} />
      <CrossMergeStones api={api} inventory={inventory} busy={busy} run={run} />
    </>
  )
}

function MergeStones({ api, inventory, busy, run }: SectionProps) {
  const titleId = useId()
  const [pairs, setPairs] = useState<Record<number, number>>({})
  const mergeable = mergeableStones(inventory)
  if (mergeable.length === 0) return null
  return (
    <section className="bag__panel forge__merge" aria-labelledby={titleId}>
      <h3 id={titleId} className="bag__panel-title">
        精鍊石合成
      </h3>
      <p className="bag__detail">兩顆同種同級合成一顆高一級的精鍊石</p>
      <ul className="bag__list">
        {mergeable.map((item) => {
          const count = pairs[item.inventoryId] ?? 1
          return (
            <li key={item.inventoryId} className="bag__item">
              <ItemName item={item} />
              <div className="bag__buy">
                <QuantityStepper
                  value={count}
                  max={mergeablePairs(item)}
                  disabled={busy}
                  onChange={(next) =>
                    setPairs((current) => ({ ...current, [item.inventoryId]: next }))
                  }
                />
                <button
                  type="button"
                  className="bag__action"
                  disabled={busy}
                  onClick={() =>
                    void run(async () => {
                      const response = await api.mergeStones(item.inventoryId, count)
                      setPairs((current) => ({ ...current, [item.inventoryId]: 1 }))
                      return { inventory: response.inventory, result: "精鍊石合成成功！" }
                    })
                  }
                >
                  合成
                </button>
              </div>
            </li>
          )
        })}
      </ul>
    </section>
  )
}

// 選擇以 inventoryId 記住；第二格只列同等級、不同種類的精鍊石
function CrossMergeStones({ api, inventory, busy, run }: SectionProps) {
  const titleId = useId()
  const [firstId, setFirstId] = useState<number | null>(null)
  const [secondId, setSecondId] = useState<number | null>(null)
  const [pairs, setPairs] = useState(1)
  const candidates = crossMergeableStones(inventory)
  const first = candidates.find((item) => item.inventoryId === firstId) ?? candidates[0]
  if (first === undefined) return null
  const partners = crossMergePartners(inventory, first)
  const second = partners.find((item) => item.inventoryId === secondId) ?? partners[0]
  if (second === undefined) return null
  const maxPairs = crossMergeMaxPairs(first, second)
  const count = Math.min(pairs, maxPairs)
  return (
    <section className="bag__panel forge__merge" aria-labelledby={titleId}>
      <h3 id={titleId} className="bag__panel-title">
        異類合成
      </h3>
      <p className="bag__detail">
        兩顆同等級、不同種類的精鍊石合成一顆低一級的其他種類精鍊石（Lv1 不能異類合成）
      </p>
      <label className="bag__field">
        第一格
        <select
          className="forge__select"
          value={first.inventoryId}
          disabled={busy}
          onChange={(event) => {
            setFirstId(Number(event.target.value))
            setSecondId(null)
            setPairs(1)
          }}
        >
          {candidates.map((item) => (
            <option key={item.inventoryId} value={item.inventoryId}>
              {itemTitle(item)}
            </option>
          ))}
        </select>
      </label>
      <label className="bag__field">
        第二格
        <select
          className="forge__select"
          value={second.inventoryId}
          disabled={busy}
          onChange={(event) => {
            setSecondId(Number(event.target.value))
            setPairs(1)
          }}
        >
          {partners.map((item) => (
            <option key={item.inventoryId} value={item.inventoryId}>
              {itemTitle(item)}
            </option>
          ))}
        </select>
      </label>
      <div className="bag__buy">
        <QuantityStepper value={count} max={maxPairs} disabled={busy} onChange={setPairs} />
        <button
          type="button"
          className="bag__action"
          disabled={busy}
          onClick={() =>
            void run(async () => {
              const response = await api.mergeCrossStones(
                first.inventoryId,
                second.inventoryId,
                count,
              )
              setPairs(1)
              return { inventory: response.inventory, result: "異類合成成功！" }
            })
          }
        >
          異類合成
        </button>
      </div>
    </section>
  )
}

function SocketSection({ api, inventory, busy, run }: SectionProps) {
  const groupName = useId()
  const { targets, target, select } = useTarget(inventory)
  const [hole, setHole] = useState<number | null>(null)
  const [stoneId, setStoneId] = useState<number | null>(null)

  if (target === null) return <NoTargets />

  const slots = holeSlots(target)
  const selectedHole = slots.find((slot) => slot.hole === hole) ?? null
  const options = socketStoneOptions(inventory, target)
  const stone = options.find((option) => option.allowed && option.stone.inventoryId === stoneId)

  const socket = (holeNumber: number, selected: InventoryItem) =>
    run(async () => {
      const response = await api.socket(target.inventoryId, holeNumber, selected.inventoryId)
      setStoneId(null)
      return { inventory: response.inventory, result: "鑲嵌成功！" }
    })

  const unsocket = (holeNumber: number, stoneName: string) =>
    run(async () => {
      const response = await api.unsocket(target.inventoryId, holeNumber)
      return {
        inventory: response.inventory,
        result: unsocketResultText(response.recovered, stoneName),
      }
    })

  const drill = () =>
    run(async () => {
      const response = await api.drill(target.inventoryId)
      return { inventory: response.inventory, result: drillResultText(response.success) }
    })

  return (
    <>
      <TargetPicker
        inventory={inventory}
        targets={targets}
        target={target}
        onSelect={(next) => {
          select(next)
          setHole(null)
          setStoneId(null)
        }}
      />
      {slots.length === 0 ? (
        <p className="bag__empty">這件裝備沒有孔</p>
      ) : (
        <div className="forge__holes" role="group" aria-label="孔位">
          {slots.map((slot) => (
            <button
              key={slot.hole}
              type="button"
              className="forge__hole"
              aria-label={`孔 ${slot.hole}：${slot.stoneName ?? "空"}`}
              aria-pressed={slot.hole === hole}
              onClick={() => {
                setHole(slot.hole)
                setStoneId(null)
              }}
            >
              {slot.stoneName === null ? "◇" : "◆"}
            </button>
          ))}
        </div>
      )}
      {selectedHole !== null &&
        (selectedHole.stoneName === null ? (
          <div className="bag__panel">
            {options.length === 0 ? (
              <p className="bag__empty">背包裡沒有魔石</p>
            ) : (
              <StoneChoices
                name={groupName}
                options={options}
                selectedId={stoneId}
                blockedNote="已有同種魔石"
                onSelect={setStoneId}
              />
            )}
            <button
              type="button"
              className="bag__action bag__action--end"
              disabled={busy || stone === undefined}
              onClick={() => {
                if (stone !== undefined) void socket(selectedHole.hole, stone.stone)
              }}
            >
              鑲嵌
            </button>
          </div>
        ) : (
          <div className="bag__current">
            <span className="bag__item-name">{selectedHole.stoneName}</span>
            <button
              type="button"
              className="bag__action"
              disabled={busy || !hasTool(inventory, ForgeTools.stoneRemover)}
              onClick={() => {
                if (selectedHole.stoneName !== null)
                  void unsocket(selectedHole.hole, selectedHole.stoneName)
              }}
            >
              拆除（需要拆石器）
            </button>
          </div>
        ))}
      <button
        type="button"
        className="bag__action bag__action--end"
        disabled={busy || !canDrill(target) || !hasTool(inventory, ForgeTools.holeDriller)}
        onClick={() => void drill()}
      >
        {`打洞（需要鑽孔器，${DrillSuccessPercent}%）`}
      </button>
    </>
  )
}

function SynthesisSection({ api, inventory, busy, run, money }: SectionProps & { money: number }) {
  const [recipes, setRecipes] = useState<RecipeVo[] | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [attempts, setAttempts] = useState<Record<string, number>>({})

  const load = useCallback(
    () =>
      api.getRecipes().then(
        (loaded) => {
          setRecipes(loaded)
          setLoadError(false)
        },
        () => setLoadError(true),
      ),
    [api],
  )

  useEffect(() => {
    void load()
  }, [load])

  if (loadError) {
    return (
      <>
        <p className="error" role="alert">
          無法載入配方。
        </p>
        <div className="actions">
          <button type="button" onClick={() => void load()}>
            重試
          </button>
        </div>
      </>
    )
  }
  if (recipes === null) return <p className="bag__empty">載入中…</p>
  if (recipes.length === 0) return <p className="bag__empty">沒有配方</p>

  return (
    <ul className="bag__list">
      {recipes.map((recipe) => {
        const lockedBy = recipe.requiredMission?.completed === false ? recipe.requiredMission : null
        const max = lockedBy === null ? maxSynthesisAttempts(recipe, inventory) : 0
        const count = Math.min(attempts[recipe.id] ?? 1, Math.max(1, max))
        const allowed = max > 0 && canAfford(money, recipe.costPerAttempt, count)
        return (
          <li key={recipe.id} className="bag__item">
            <span className="bag__item-name">{recipeText(recipe)}</span>
            {lockedBy !== null && (
              <span className="bag__item-lock">{`需完成「${lockedBy.title}」`}</span>
            )}
            <div className="bag__buy">
              <QuantityStepper
                value={count}
                max={Math.max(1, max)}
                disabled={busy || max === 0}
                onChange={(next) => setAttempts((current) => ({ ...current, [recipe.id]: next }))}
              />
              <button
                type="button"
                className="bag__action"
                disabled={busy || !allowed}
                onClick={() =>
                  void run(async () => {
                    const response = await api.synthesize(recipe.id, count)
                    return { inventory: response.inventory, result: synthesisResultText(response) }
                  })
                }
              >
                {synthesisButtonText(recipe.costPerAttempt, count)}
              </button>
            </div>
          </li>
        )
      })}
    </ul>
  )
}
