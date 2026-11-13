import { useCallback, useEffect, useId, useState, type FormEvent } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  bagItems,
  bagSlotsOf,
  BankMinimumLevel,
  bonusText,
  canAfford,
  candidatesFor,
  clampQuantity,
  equipmentSlots,
  equippedIn,
  errorMessage,
  isBankLocked,
  isEquipment,
  isSupply,
  isUsable,
  itemUseResultLines,
  itemCategories,
  MaxShopLevel,
  maxBuyQuantity,
  meetsRequirements,
  MinShopLevel,
  requirementRows,
  sellPrice,
  shopCategories,
  supplyEquipQuantity,
  usageText,
  type JobName,
} from "../game/inventory"
import type {
  DepotVo,
  EquipmentSlot,
  InventoryItem,
  InventoryVo,
  ItemCategory,
  ShopItemVo,
} from "../game/inventory"
import type { CharacterVo } from "../game/character"
import { formatNumber } from "../game/format"
import "./BagScreen.css"
import { ForgePage, type ForgeApi } from "./ForgePage"
import { HonourPage, type HonourApi } from "./HonourPage"
import {
  ItemDetail,
  ItemName,
  QuantityStepper,
  Requirements,
  TabPanel,
  Tabs,
  type Mutate,
  type Outcome,
} from "./bagWidgets"
import { usePanelRefresh } from "./usePanelRefresh"

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
  | keyof ForgeApi
  | keyof HonourApi
>

type Props = {
  api: BagApi
  character: CharacterVo
  onCharacterChanged: (character: CharacterVo) => void
  jobNames?: readonly JobName[]
  // 戰報有掉落、喝藥水、捕捉或傭兵報告時遞增，開著的背包跟著重新載入
  refreshSignal?: number
  // 從城鎮直接打開某個子頁（例如旅館）
  initialPage?: SubPage
}

export type SubPage = "bag" | "equipment" | "shop" | "depot" | "bank" | "forge" | "honour"

const subPages: { id: SubPage; label: string }[] = [
  { id: "bag", label: "背包" },
  { id: "equipment", label: "裝備" },
  { id: "shop", label: "商店" },
  { id: "depot", label: "倉庫" },
  { id: "bank", label: "銀行" },
  { id: "forge", label: "鍛造" },
  { id: "honour", label: "勳章所" },
]

const fallbackError = "發生錯誤，請稍後再試"

const describeError = (error: unknown) =>
  error instanceof ApiError ? errorMessage(error.detail, error.reasons) : fallbackError

export function BagScreen({
  api,
  character,
  onCharacterChanged,
  jobNames = [],
  refreshSignal = 0,
  initialPage = "bag",
}: Props) {
  const tabsId = useId()
  const [page, setPage] = useState<SubPage>(initialPage)
  const [inventory, setInventory] = useState<InventoryVo | null>(null)
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

  // 背景重載失敗就保留目前的內容，等下一次訊號
  usePanelRefresh(refreshSignal, () => {
    api.getInventory().then(setInventory, () => {})
  })

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

  const renderPage = (current: InventoryVo) => {
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
        return (
          <ForgePage
            api={api}
            inventory={current}
            character={character}
            busy={busy}
            mutate={mutate}
          />
        )
      case "honour":
        // 兌換的道具進背包、勳章扣掉，背包與角色都重新讀取
        return (
          <HonourPage
            api={api}
            onExchanged={async () => {
              await mutate(async () => ({ inventory: await api.getInventory() }))
            }}
          />
        )
    }
  }

  return (
    <div className="bag">
      <Tabs
        id={tabsId}
        label="背包分頁"
        options={subPages}
        selected={page}
        onSelect={setPage}
        variant="pages"
      />
      <TabPanel tabsId={tabsId} selected={page}>
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
      </TabPanel>
    </div>
  )
}

type PageProps = {
  api: BagApi
  character: CharacterVo
  busy: boolean
  mutate: Mutate
  jobNames: readonly JobName[]
}

function BagPage({
  api,
  inventory,
  character,
  busy,
  mutate,
  jobNames,
}: PageProps & { inventory: InventoryVo }) {
  const tabsId = useId()
  const [category, setCategory] = useState<ItemCategory>("Weapon")
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [quantity, setQuantity] = useState(1)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string[]>([])
  // 精鍊或鑲嵌過的裝備賣出前多確認一次
  const [confirmingSale, setConfirmingSale] = useState(false)
  const slots = bagSlotsOf(inventory, category)
  const label = itemCategories.find(({ id }) => id === category)?.label ?? category

  const select = (next: number | null) => {
    setSelectedId(next)
    setQuantity(1)
    setError(null)
    setNotice([])
    setConfirmingSale(false)
  }

  const act = async (action: () => Promise<Outcome>) => {
    setNotice([])
    const message = await mutate(action)
    setError(message)
    if (message === null) setSelectedId(null)
  }

  const sell = async (item: InventoryItem) => {
    setConfirmingSale(false)
    await act(async () => ({ inventory: await api.sell(item.inventoryId, quantity) }))
  }

  return (
    <section className="bag__card">
      <Tabs
        id={tabsId}
        label="道具分類"
        options={itemCategories}
        selected={category}
        onSelect={(next) => {
          setCategory(next)
          select(null)
        }}
      />
      <TabPanel tabsId={tabsId} selected={category}>
        <p className="bag__usage">{`${label} ${usageText(slots)}`}</p>
        {notice.length > 0 && (
          <div className="bag__notice" role="status">
            {notice.map((line) => (
              <p key={line}>{line}</p>
            ))}
          </div>
        )}
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
                                  ? await api.equip(item.inventoryId, supplyEquipQuantity(item))
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
                              void act(async () => {
                                const result = await api.useItem(item.inventoryId)
                                setNotice(itemUseResultLines(item, result, new Date()))
                                return { inventory: result.inventory }
                              })
                            }
                          >
                            使用
                          </button>
                        )}
                        {confirmingSale ? (
                          <>
                            <button
                              type="button"
                              className="bag__action bag__danger"
                              disabled={busy}
                              onClick={() => void sell(item)}
                            >
                              確定賣出
                            </button>
                            <button
                              type="button"
                              className="bag__action"
                              disabled={busy}
                              onClick={() => setConfirmingSale(false)}
                            >
                              取消
                            </button>
                          </>
                        ) : (
                          <button
                            type="button"
                            className="bag__action"
                            disabled={busy}
                            onClick={() => {
                              if (item.enhanced) setConfirmingSale(true)
                              else void sell(item)
                            }}
                          >
                            {`賣出（${formatNumber(sellPrice(item.price, quantity))}）`}
                          </button>
                        )}
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
                      {confirmingSale && (
                        <p className="bag__warning crimson-text">精鍊與鑲嵌不會補償，確定賣出？</p>
                      )}
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
      </TabPanel>
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
}: PageProps & { inventory: InventoryVo }) {
  const panelId = useId()
  const [slot, setSlot] = useState<EquipmentSlot | null>(null)
  // 每疊補給品各自的數量欄；沒改過時預設整疊（上限 99）
  const [supplyQuantities, setSupplyQuantities] = useState<Record<number, string>>({})
  const [error, setError] = useState<string | null>(null)

  const act = async (action: () => Promise<Outcome>) => {
    const message = await mutate(action)
    setError(message)
    // 堆疊數量變了，數量欄回到新的整疊預設
    if (message === null) setSupplyQuantities({})
  }

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
            <>
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
              <ItemDetail item={current.item} />
            </>
          )}
          {candidatesFor(inventory, selected.id).length === 0 ? (
            <p className="bag__empty">背包裡沒有可裝備的道具</p>
          ) : (
            <ul className="bag__list">
              {candidatesFor(inventory, selected.id).map((item) => {
                const supply = isSupply(item)
                const allowed = supply || meetsRequirements(item, character)
                const supplyQuantity =
                  supplyQuantities[item.inventoryId] ?? String(supplyEquipQuantity(item))
                return (
                  <li key={item.inventoryId} className="bag__item">
                    <ItemName item={item} />
                    <ItemDetail item={item} />
                    {!supply && <Requirements rows={requirementRows(item, character, jobNames)} />}
                    {supply && (
                      <label className="bag__field">
                        補給數量
                        <input
                          type="number"
                          inputMode="numeric"
                          min={1}
                          max={supplyEquipQuantity(item)}
                          value={supplyQuantity}
                          onChange={(event) =>
                            setSupplyQuantities((current) => ({
                              ...current,
                              [item.inventoryId]: event.target.value,
                            }))
                          }
                        />
                      </label>
                    )}
                    <button
                      type="button"
                      className="bag__action bag__action--end"
                      disabled={busy || !allowed}
                      onClick={() =>
                        void act(async () => ({
                          inventory: supply
                            ? await api.equip(
                                item.inventoryId,
                                clampQuantity(Number(supplyQuantity), supplyEquipQuantity(item)),
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
  const categoryTabsId = useId()
  const levelTabsId = useId()
  const [category, setCategory] = useState<ItemCategory>("Weapon")
  const [level, setLevel] = useState(MinShopLevel)
  const [items, setItems] = useState<ShopItemVo[] | null>(null)
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

  const buy = async (item: ShopItemVo, quantity: number) => {
    setError(null)
    const message = await mutate(async () => ({ inventory: await api.buy(item.id, quantity) }))
    if (message === null) setQuantities((current) => ({ ...current, [item.id]: 1 }))
    else setError({ itemId: item.id, message })
  }

  return (
    <section className="bag__card">
      <Tabs
        id={categoryTabsId}
        label="商店分類"
        options={shopCategories}
        selected={category}
        onSelect={(next) => browse(next, level)}
      />
      {/* 等級分頁放在分類的內容區裡，商品列表同時屬於兩者 */}
      <TabPanel tabsId={categoryTabsId} selected={category}>
        <Tabs
          id={levelTabsId}
          label="商店等級"
          options={shopLevels}
          selected={level}
          onSelect={(next) => browse(category, next)}
        />
        <TabPanel tabsId={levelTabsId} selected={level}>
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
        </TabPanel>
      </TabPanel>
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
  inventory: InventoryVo
  busy: boolean
  mutate: Mutate
}) {
  const bagId = useId()
  const depotId = useId()
  const [depot, setDepot] = useState<DepotVo | null>(null)
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
  character: CharacterVo
  busy: boolean
  mutate: Mutate
}) {
  const [depositAmount, setDepositAmount] = useState("")
  const [withdrawAmount, setWithdrawAmount] = useState("")
  const [error, setError] = useState<string | null>(null)
  const locked = isBankLocked(character.level)

  const submit =
    (amount: string, send: (value: number) => Promise<CharacterVo>, reset: () => void) =>
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
      <p className="bag__balance">{`存款 ${formatNumber(character.bank)}`}</p>
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
