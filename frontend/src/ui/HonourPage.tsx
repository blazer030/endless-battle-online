import { useCallback, useEffect, useId, useState, type ReactNode } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  costText,
  exchangeGroups,
  honourErrorMessage,
  isShort,
  medalText,
  rewardText,
} from "../game/honour"
import type { HonourExchange, HonourShopVo } from "../game/honour"
import "./HonourPage.css"

export type HonourApi = Pick<ApiClient, "getHonourShop" | "getMercenaries" | "exchangeHonour">

type Props = {
  api: HonourApi
  // 兌換成功後由背包頁重新讀取背包與角色
  onExchanged: () => Promise<void>
}

const fallbackError = "發生錯誤，請稍後再試"

const describeError = (error: unknown) =>
  error instanceof ApiError ? honourErrorMessage(error.reasons, error.detail) : fallbackError

export function HonourPage({ api, onExchanged }: Props) {
  const [shop, setShop] = useState<HonourShopVo | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [busy, setBusy] = useState(false)
  // 已僱用傭兵時，傭兵兌換不能用
  const [hired, setHired] = useState(false)
  const [error, setError] = useState<{ exchangeId: string; message: string } | null>(null)

  const load = useCallback(
    () =>
      api.getHonourShop().then(
        (loaded) => {
          setShop(loaded)
          setLoadError(false)
        },
        () => setLoadError(true),
      ),
    [api],
  )

  useEffect(() => {
    void load()
    // 讀不到僱用狀態就當作未僱用，伺服器仍會擋下
    void api.getMercenaries().then(
      (overview) => setHired(overview.contract !== null),
      () => undefined,
    )
  }, [load, api])

  const exchange = async (exchangeId: string) => {
    setBusy(true)
    setError(null)
    try {
      setShop(await api.exchangeHonour(exchangeId))
      if (shop?.exchanges.find((entry) => entry.id === exchangeId)?.kind === "mercenary") {
        setHired(true)
      }
      await onExchanged()
    } catch (caught) {
      setError({ exchangeId, message: describeError(caught) })
    } finally {
      setBusy(false)
    }
  }

  if (loadError)
    return (
      <section className="bag__card">
        <p className="error" role="alert">
          無法載入勳章所。
        </p>
        <div className="actions">
          <button type="button" onClick={() => void load()}>
            重試
          </button>
        </div>
      </section>
    )
  if (shop === null) return <p className="bag__empty">載入中…</p>

  return (
    <>
      <section className="bag__card">
        <ul className="honour__medals" aria-label="持有勳章">
          {shop.medals.map((medal) => (
            <li key={medal.itemId}>{medalText(medal)}</li>
          ))}
        </ul>
      </section>
      {exchangeGroups(shop.exchanges).map((group) => (
        <ExchangeGroup key={group.title} title={group.title}>
          {group.exchanges.map((entry) => (
            <ExchangeCard
              key={entry.id}
              exchange={entry}
              busy={busy || (hired && entry.kind === "mercenary")}
              error={error?.exchangeId === entry.id ? error.message : null}
              onExchange={() => void exchange(entry.id)}
            />
          ))}
        </ExchangeGroup>
      ))}
    </>
  )
}

function ExchangeGroup({ title, children }: { title: string; children: ReactNode }) {
  const headingId = useId()
  return (
    <section className="bag__card" aria-labelledby={headingId}>
      <h3 id={headingId} className="bag__panel-title">
        {title}
      </h3>
      {children}
    </section>
  )
}

function ExchangeCard({
  exchange,
  busy,
  error,
  onExchange,
}: {
  exchange: HonourExchange
  busy: boolean
  error: string | null
  onExchange: () => void
}) {
  const titleId = useId()
  return (
    <article className="honour__exchange" aria-labelledby={titleId}>
      <span id={titleId} className="bag__item-name">
        {rewardText(exchange)}
      </span>
      <ul className="honour__cost" aria-label="花費">
        {exchange.cost.map((cost) => (
          <li key={cost.itemId} className={isShort(cost) ? "honour__cost--short" : undefined}>
            {costText(cost)}
          </li>
        ))}
      </ul>
      <button
        type="button"
        className="bag__action honour__button"
        disabled={busy || !exchange.affordable}
        onClick={onExchange}
      >
        兌換
      </button>
      {error !== null && (
        <p className="error honour__error" role="alert">
          {error}
        </p>
      )}
    </article>
  )
}
