import { useCallback, useEffect, useId, useState, type FormEvent, type ReactNode } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  bagItems,
  bagSlotsOf,
  bankBalanceOf,
  BankMinimumLevel,
  bonusText,
  canAfford,
  candidatesFor,
  clampQuantity,
  equipmentSlots,
  equippedIn,
  errorMessage,
  holeMarks,
  isBankLocked,
  isEquipment,
  isSupply,
  isUsable,
  itemCategories,
  itemTitle,
  MaxShopLevel,
  MaxStackQuantity,
  maxBuyQuantity,
  meetsRequirements,
  MinShopLevel,
  requirementRows,
  sellPrice,
  supplyText,
  usageText,
  type JobName,
  type Requirement,
} from "../game/inventory"
import type {
  CharacterView,
  DepotView,
  EquipmentSlot,
  InventoryItem,
  InventoryView,
  ItemCategory,
  ShopItem,
} from "../game/types"
import { formatNumber } from "./formatNumber"
import "./BagScreen.css"

export type BagApi = Pick<
  ApiClient,
  | "getInventory"
  | "equip"
  | "unequip"
  | "useItem"
  | "getShop"
  | "buy"
  | "sell"
  | "getDepot"
  | "store"
  | "take"
  | "deposit"
  | "withdraw"
  | "getMyCharacter"
>

type Props = {
  api: BagApi
  character: CharacterView
  onCharacterChanged: (character: CharacterView) => void
  jobNames?: JobName[]
}

type SubPage = "bag" | "equipment" | "shop" | "depot" | "bank" | "forge"

const subPages: { id: SubPage; label: string }[] = [
  { id: "bag", label: "背包" },
  { id: "equipment", label: "裝備" },
  { id: "shop", label: "商店" },
  { id: "depot", label: "倉庫" },
  { id: "bank", label: "銀行" },
  { id: "forge", label: "鍛造" },
]

const fallbackError = "發生錯誤，請稍後再試"

const describeError = (error: unknown) =>
  error instanceof ApiError ? errorMessage(error.detail, error.reasons) : fallbackError

// 變更的結果：背包換成回應內容；有角色就直接用，否則重新讀 /me 同步金錢與能力
type Outcome = { inventory?: InventoryView; character?: CharacterView }

// 回傳錯誤訊息，成功時為 null
type Mutate = (action: () => Promise<Outcome>) => Promise<string | null>

export function BagScreen({ api, character, onCharacterChanged, jobNames = [] }: Props) {
  const [page, setPage] = useState<SubPage>("bag")
  const [inventory, setInventory] = useState<InventoryView | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [busy, setBusy] = useState(false)

  const load = useCallback(
    () =>
      api.getInventory().then(
        (loaded) => {
          setInventory(loaded)
          setLoadError(false)
        },
        () => setLoadError(true),
      ),
    [api],
  )

  useEffect(() => {
    void load()
  }, [load])

  // 同一時間只送一個請求
  const mutate: Mutate = async (action) => {
    setBusy(true)
    try {
      const outcome = await action()
      if (outcome.inventory !== undefined) setInventory(outcome.inventory)
      const updated = outcome.character ?? (await api.getMyCharacter().catch(() => null))
      if (updated !== null) onCharacterChanged(updated)
      return null
    } catch (error) {
      return describeError(error)
    } finally {
      setBusy(false)
    }
  }

  const renderPage = (current: InventoryView) => {
    const shared = { character, busy, mutate, jobNames }
    switch (page) {
      case "bag":
        return <BagPage api={api} inventory={current} {...shared} />
      case "equipment":
        return <EquipmentPage api={api} inventory={current} {...shared} />
      case "shop":
        return <ShopPage api={api} {...shared} />
      case "depot":
        return <DepotPage api={api} inventory={current} busy={busy} mutate={mutate} />
      case "bank":
        return <BankPage api={api} character={character} busy={busy} mutate={mutate} />
      case "forge":
        return <p className="bag__empty">鍛造功能即將開放</p>
    }
  }

  return (
    <div className="bag">
      <Tabs
        label="背包分頁"
        options={subPages}
        selected={page}
        onSelect={setPage}
        variant="pages"
      />
      {loadError ? (
        <div className="bag__card">
          <p className="error" role="alert">
            無法載入背包。
          </p>
          <div className="actions">
            <button type="button" onClick={() => void load()}>
              重試
            </button>
          </div>
        </div>
      ) : inventory === null ? (
        <p className="bag__empty">載入中…</p>
      ) : (
        renderPage(inventory)
      )}
    </div>
  )
}

type PageProps = {
  api: BagApi
  character: CharacterView
  busy: boolean
  mutate: Mutate
  jobNames: JobName[]
}

function BagPage({
  api,
  inventory,
  character,
  busy,
  mutate,
  jobNames,
}: PageProps & { inventory: InventoryView }) {
  const [category, setCategory] = useState<ItemCategory>("Weapon")
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [quantity, setQuantity] = useState(1)
  const [error, setError] = useState<string | null>(null)
  const slots = bagSlotsOf(inventory, category)
  const label = itemCategories.find(({ id }) => id === category)?.label ?? category

  const select = (next: number | null) => {
    setSelectedId(next)
    setQuantity(1)
    setError(null)
  }

  const act = async (action: () => Promise<Outcome>) => {
    const message = await mutate(action)
    setError(message)
    if (message === null) setSelectedId(null)
  }

  return (
    <section className="bag__card">
      <Tabs
        label="道具分類"
        options={itemCategories}
        selected={category}
        onSelect={(next) => {
          setCategory(next)
          select(null)
        }}
      />
      <p className="bag__usage">{`${label} ${usageText(slots)}`}</p>
      {slots.items.length === 0 ? (
        <p className="bag__empty">這個分類沒有道具</p>
      ) : (
        <ul className="bag__list">
          {slots.items.map((item) => {
            const selected = item.inventoryId === selectedId
            return (
              <li key={item.inventoryId} className="bag__item">
                <button
                  type="button"
                  className="bag__item-button"
                  aria-expanded={selected}
                  onClick={() => select(selected ? null : item.inventoryId)}
                >
                  <ItemName item={item} />
                </button>
                <ItemDetail item={item} />
                {selected && (
                  <div className="bag__actions">
                    {isEquipment(item.category) && (
                      <Requirements rows={requirementRows(item, character, jobNames)} />
                    )}
                    {!isEquipment(item.category) && item.quantity > 1 && (
                      <QuantityStepper
                        value={quantity}
                        max={item.quantity}
                        disabled={busy}
                        onChange={setQuantity}
                      />
                    )}
                    <div className="bag__buttons">
                      {(isEquipment(item.category) || isSupply(item)) && (
                        <button
                          type="button"
                          className="bag__action"
                          disabled={
                            busy ||
                            (isEquipment(item.category) && !meetsRequirements(item, character))
                          }
                          onClick={() =>
                            void act(async () => ({
                              inventory: isSupply(item)
                                ? await api.equip(item.inventoryId, quantity)
                                : await api.equip(item.inventoryId),
                            }))
                          }
                        >
                          裝備
                        </button>
                      )}
                      {isUsable(item) && (
                        <button
                          type="button"
                          className="bag__action"
                          disabled={busy}
                          onClick={() =>
                            void act(async () => ({
                              inventory: await api.useItem(item.inventoryId),
                            }))
                          }
                        >
                          使用
                        </button>
                      )}
                      <button
                        type="button"
                        className="bag__action"
                        disabled={busy}
                        onClick={() =>
                          void act(async () => ({
                            inventory: await api.sell(item.inventoryId, quantity),
                          }))
                        }
                      >
                        {`賣出（${formatNumber(sellPrice(item.price, quantity))}）`}
                      </button>
                      <button
                        type="button"
                        className="bag__action"
                        disabled={busy}
                        onClick={() =>
                          void act(async () => ({
                            inventory: (await api.store(item.inventoryId, quantity)).inventory,
                          }))
                        }
                      >
                        存入倉庫
                      </button>
                    </div>
                    {error !== null && (
                      <p className="error" role="alert">
                        {error}
                      </p>
                    )}
                  </div>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}

function EquipmentPage({
  api,
  inventory,
  character,
  busy,
  mutate,
  jobNames,
}: PageProps & { inventory: InventoryView }) {
  const panelId = useId()
  const [slot, setSlot] = useState<EquipmentSlot | null>(null)
  const [supplyQuantity, setSupplyQuantity] = useState(String(MaxStackQuantity))
  const [error, setError] = useState<string | null>(null)

  const act = async (action: () => Promise<Outcome>) => setError(await mutate(action))

  const selected = equipmentSlots.find(({ id }) => id === slot) ?? null
  const current = slot === null ? null : equippedIn(inventory, slot)

  return (
    <section className="bag__card">
      <ul className="bag__slots">
        {equipmentSlots.map(({ id, label }) => {
          const equipped = equippedIn(inventory, id)
          return (
            <li key={id}>
              <button
                type="button"
                className="bag__slot"
                aria-expanded={slot === id}
                aria-controls={slot === id ? panelId : undefined}
                onClick={() => {
                  setSlot(slot === id ? null : id)
                  setError(null)
                }}
              >
                <span className="bag__slot-label">{label}</span>{" "}
                {equipped === null ? (
                  <span className="bag__slot-empty">（空）</span>
                ) : (
                  <ItemName item={{ ...equipped.item, quantity: equipped.quantity }} />
                )}
              </button>
            </li>
          )
        })}
      </ul>
      {selected !== null && (
        <div id={panelId} className="bag__panel">
          <h3 className="bag__panel-title">{selected.label}</h3>
          {current !== null && (
            <div className="bag__current">
              <ItemName item={{ ...current.item, quantity: current.quantity }} />
              <button
                type="button"
                className="bag__action"
                disabled={busy}
                onClick={() =>
                  void act(async () => ({ inventory: await api.unequip(selected.id) }))
                }
              >
                卸下
              </button>
            </div>
          )}
          {selected.id === "Supply" && (
            <label className="bag__field">
              補給數量
              <input
                type="number"
                inputMode="numeric"
                min={1}
                max={MaxStackQuantity}
                value={supplyQuantity}
                onChange={(event) => setSupplyQuantity(event.target.value)}
              />
            </label>
          )}
          {candidatesFor(inventory, selected.id).length === 0 ? (
            <p className="bag__empty">背包裡沒有可裝備的道具</p>
          ) : (
            <ul className="bag__list">
              {candidatesFor(inventory, selected.id).map((item) => {
                const supply = isSupply(item)
                const allowed = supply || meetsRequirements(item, character)
                return (
                  <li key={item.inventoryId} className="bag__item">
                    <ItemName item={item} />
                    <ItemDetail item={item} />
                    {!supply && <Requirements rows={requirementRows(item, character, jobNames)} />}
                    <button
                      type="button"
                      className="bag__action bag__action--end"
                      disabled={busy || !allowed}
                      onClick={() =>
                        void act(async () => ({
                          inventory: supply
                            ? await api.equip(
                                item.inventoryId,
                                clampQuantity(
                                  Number(supplyQuantity),
                                  Math.min(MaxStackQuantity, item.quantity),
                                ),
                              )
                            : await api.equip(item.inventoryId),
                        }))
                      }
                    >
                      裝備
                    </button>
                  </li>
                )
              })}
            </ul>
          )}
          {error !== null && (
            <p className="error" role="alert">
              {error}
            </p>
          )}
        </div>
      )}
    </section>
  )
}

const shopLevels = Array.from({ length: MaxShopLevel - MinShopLevel + 1 }, (_, index) => ({
  id: MinShopLevel + index,
  label: `Lv ${MinShopLevel + index}`,
}))

function ShopPage({ api, character, busy, mutate, jobNames }: PageProps) {
  const [category, setCategory] = useState<ItemCategory>("Weapon")
  const [level, setLevel] = useState(MinShopLevel)
  const [items, setItems] = useState<ShopItem[] | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const [quantities, setQuantities] = useState<Record<string, number>>({})
  const [error, setError] = useState<{ itemId: string; message: string } | null>(null)

  useEffect(() => {
    let isCurrent = true
    api.getShop(category, level).then(
      (loaded) => {
        if (!isCurrent) return
        setItems(loaded)
        setLoadError(false)
      },
      () => {
        if (isCurrent) setLoadError(true)
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, category, level, attempt])

  const browse = (nextCategory: ItemCategory, nextLevel: number) => {
    setCategory(nextCategory)
    setLevel(nextLevel)
    setItems(null)
    setError(null)
  }

  const buy = async (item: ShopItem, quantity: number) => {
    setError(null)
    const message = await mutate(async () => ({ inventory: await api.buy(item.id, quantity) }))
    if (message === null) setQuantities((current) => ({ ...current, [item.id]: 1 }))
    else setError({ itemId: item.id, message })
  }

  return (
    <section className="bag__card">
      <Tabs
        label="商店分類"
        options={itemCategories}
        selected={category}
        onSelect={(next) => browse(next, level)}
      />
      <Tabs
        label="商店等級"
        options={shopLevels}
        selected={level}
        onSelect={(next) => browse(category, next)}
      />
      <p className="bag__usage">{`金錢 ${formatNumber(character.money)}`}</p>
      {loadError ? (
        <>
          <p className="error" role="alert">
            無法載入商店。
          </p>
          <div className="actions">
            <button type="button" onClick={() => setAttempt((count) => count + 1)}>
              重試
            </button>
          </div>
        </>
      ) : items === null ? (
        <p className="bag__empty">載入中…</p>
      ) : items.length === 0 ? (
        <p className="bag__empty">這一頁沒有商品</p>
      ) : (
        <ul className="bag__list">
          {items.map((item) => {
            const quantity = quantities[item.id] ?? 1
            return (
              <li key={item.id} className="bag__item">
                <span className="bag__item-name">{`${item.name} ${formatNumber(item.price)} 金錢`}</span>
                {bonusText(item.bonus) !== "" && (
                  <p className="bag__detail">{bonusText(item.bonus)}</p>
                )}
                <Requirements
                  rows={requirementRows(
                    { requires: item.requires, jobId: null },
                    character,
                    jobNames,
                  )}
                />
                <div className="bag__buy">
                  <QuantityStepper
                    value={quantity}
                    max={maxBuyQuantity(item.category)}
                    disabled={busy}
                    onChange={(next) =>
                      setQuantities((current) => ({ ...current, [item.id]: next }))
                    }
                  />
                  <button
                    type="button"
                    className="bag__action"
                    disabled={busy || !canAfford(character.money, item.price, quantity)}
                    onClick={() => void buy(item, quantity)}
                  >
                    購買
                  </button>
                </div>
                {error?.itemId === item.id && (
                  <p className="error" role="alert">
                    {error.message}
                  </p>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}

function DepotPage({
  api,
  inventory,
  busy,
  mutate,
}: {
  api: BagApi
  inventory: InventoryView
  busy: boolean
  mutate: Mutate
}) {
  const bagId = useId()
  const depotId = useId()
  const [depot, setDepot] = useState<DepotView | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [quantities, setQuantities] = useState<Record<string, number>>({})
  const [error, setError] = useState<{ key: string; message: string } | null>(null)

  const load = useCallback(
    () =>
      api.getDepot().then(
        (loaded) => {
          setDepot(loaded)
          setLoadError(false)
        },
        () => setLoadError(true),
      ),
    [api],
  )

  useEffect(() => {
    void load()
  }, [load])

  const move = async (key: string, action: () => ReturnType<BagApi["store"]>) => {
    setError(null)
    const message = await mutate(async () => {
      const moved = await action()
      setDepot(moved.depot)
      return { inventory: moved.inventory }
    })
    if (message === null) setQuantities((current) => ({ ...current, [key]: 1 }))
    else setError({ key, message })
  }

  const row = (key: string, item: InventoryItem, label: string, onMove: (n: number) => void) => {
    const quantity = quantities[key] ?? 1
    return (
      <li key={key} className="bag__item">
        <ItemName item={item} />
        <div className="bag__buy">
          {!isEquipment(item.category) && item.quantity > 1 && (
            <QuantityStepper
              value={quantity}
              max={item.quantity}
              disabled={busy}
              onChange={(next) => setQuantities((current) => ({ ...current, [key]: next }))}
            />
          )}
          <button
            type="button"
            className="bag__action"
            disabled={busy}
            onClick={() => onMove(quantity)}
          >
            {label}
          </button>
        </div>
        {error?.key === key && (
          <p className="error" role="alert">
            {error.message}
          </p>
        )}
      </li>
    )
  }

  if (loadError) {
    return (
      <section className="bag__card">
        <p className="error" role="alert">
          無法載入倉庫。
        </p>
        <div className="actions">
          <button type="button" onClick={() => void load()}>
            重試
          </button>
        </div>
      </section>
    )
  }
  if (depot === null) return <p className="bag__empty">載入中…</p>

  const carried = bagItems(inventory)
  return (
    <>
      <section className="bag__card" aria-labelledby={depotId}>
        <h3 id={depotId} className="bag__panel-title">
          倉庫
        </h3>
        <p className="bag__usage">{`倉庫 ${depot.entries.length}/${depot.capacity}`}</p>
        {depot.entries.length === 0 ? (
          <p className="bag__empty">倉庫是空的</p>
        ) : (
          <ul className="bag__list">
            {depot.entries.map((item) =>
              row(
                `depot-${item.inventoryId}`,
                item,
                "取出",
                (quantity) =>
                  void move(`depot-${item.inventoryId}`, () =>
                    api.take(item.inventoryId, quantity),
                  ),
              ),
            )}
          </ul>
        )}
      </section>
      <section className="bag__card" aria-labelledby={bagId}>
        <h3 id={bagId} className="bag__panel-title">
          背包
        </h3>
        {carried.length === 0 ? (
          <p className="bag__empty">背包是空的</p>
        ) : (
          <ul className="bag__list">
            {carried.map((item) =>
              row(
                `bag-${item.inventoryId}`,
                item,
                "存入",
                (quantity) =>
                  void move(`bag-${item.inventoryId}`, () => api.store(item.inventoryId, quantity)),
              ),
            )}
          </ul>
        )}
      </section>
    </>
  )
}

function BankPage({
  api,
  character,
  busy,
  mutate,
}: {
  api: BagApi
  character: CharacterView
  busy: boolean
  mutate: Mutate
}) {
  const [depositAmount, setDepositAmount] = useState("")
  const [withdrawAmount, setWithdrawAmount] = useState("")
  const [error, setError] = useState<string | null>(null)
  const locked = isBankLocked(character.level)
  const balance = bankBalanceOf(character)

  const submit =
    (amount: string, send: (value: number) => Promise<CharacterView>, reset: () => void) =>
    async (event: FormEvent) => {
      event.preventDefault()
      setError(null)
      const message = await mutate(async () => ({
        character: await send(Math.trunc(Number(amount))),
      }))
      setError(message)
      if (message === null) reset()
    }

  return (
    <section className="bag__card">
      <p className="bag__balance">{`存款 ${balance === null ? "—" : formatNumber(balance)}`}</p>
      <p className="bag__usage">{`金錢 ${formatNumber(character.money)}`}</p>
      {locked && <p className="bag__locked">{`Lv ${BankMinimumLevel} 開放`}</p>}
      <BankForm
        label="存入"
        value={depositAmount}
        onChange={setDepositAmount}
        disabled={busy || locked}
        onSubmit={submit(depositAmount, api.deposit, () => setDepositAmount(""))}
      />
      <BankForm
        label="提領"
        value={withdrawAmount}
        onChange={setWithdrawAmount}
        disabled={busy}
        onSubmit={submit(withdrawAmount, api.withdraw, () => setWithdrawAmount(""))}
      />
      {error !== null && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
    </section>
  )
}

function BankForm({
  label,
  value,
  onChange,
  disabled,
  onSubmit,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  disabled: boolean
  onSubmit: (event: FormEvent) => Promise<void>
}) {
  return (
    <form className="bag__bank-form" onSubmit={(event) => void onSubmit(event)}>
      <label className="bag__field">
        {`${label}金額`}
        <input
          type="number"
          inputMode="numeric"
          min={1}
          value={value}
          disabled={disabled}
          onChange={(event) => onChange(event.target.value)}
        />
      </label>
      <button type="submit" className="bag__action" disabled={disabled}>
        {label}
      </button>
    </form>
  )
}

function Tabs<T extends string | number>({
  label,
  options,
  selected,
  onSelect,
  variant = "chips",
}: {
  label: string
  options: { id: T; label: string }[]
  selected: T
  onSelect: (id: T) => void
  variant?: "pages" | "chips"
}) {
  return (
    <div className={`bag__tabs bag__tabs--${variant}`} role="tablist" aria-label={label}>
      {options.map((option) => (
        <button
          key={option.id}
          type="button"
          role="tab"
          className="bag__tab"
          aria-selected={option.id === selected}
          onClick={() => onSelect(option.id)}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}

function ItemName({ item }: { item: InventoryItem }) {
  const holes = holeMarks(item)
  return (
    <span className="bag__item-head">
      <span className="bag__item-name">{itemTitle(item)}</span>
      {holes !== "" && (
        <>
          {" "}
          <span className="bag__holes">{holes}</span>
        </>
      )}
    </span>
  )
}

function ItemDetail({ item }: { item: InventoryItem }): ReactNode {
  const text = isSupply(item) ? supplyText(item) : bonusText(item.bonus)
  return text === "" ? null : <p className="bag__detail">{text}</p>
}

function Requirements({ rows }: { rows: Requirement[] }) {
  if (rows.length === 0) return null
  return (
    <ul className="bag__requirements" aria-label="需求">
      {rows.map(({ text, met }) => (
        <li key={text} className={met ? "bag__requirement" : "bag__requirement crimson-text"}>
          {text}
        </li>
      ))}
    </ul>
  )
}

function QuantityStepper({
  value,
  max,
  disabled,
  onChange,
}: {
  value: number
  max: number
  disabled: boolean
  onChange: (value: number) => void
}) {
  return (
    <span className="bag__stepper">
      <button
        type="button"
        aria-label="減少數量"
        disabled={disabled || value <= 1}
        onClick={() => onChange(clampQuantity(value - 1, max))}
      >
        −
      </button>
      <span className="bag__stepper-value">{value}</span>
      <button
        type="button"
        aria-label="增加數量"
        disabled={disabled || value >= max}
        onClick={() => onChange(clampQuantity(value + 1, max))}
      >
        ＋
      </button>
    </span>
  )
}
