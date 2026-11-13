import { useCallback, useEffect, useState, type CSSProperties } from "react"
import type { CharacterVo } from "../../game/character"
import {
  activationRatio,
  companionsCounterTop,
  feedCooldownElapsed,
  FeedCooldownMilliseconds,
  feedEffect,
  fruitDisabled,
  fruitEffect,
  intimacyRatio,
  keeperLine,
  satietyRatio,
  type KeeperEvent,
} from "../../game/companions"
import { formatNumber } from "../../game/format"
import {
  personalityLabel,
  type Pet,
  type PetFood,
  type PetFoodStat,
  type PetsVo,
} from "../../game/pets"
import { ContentImage } from "../ContentImage"
import { FeedEffectSeconds, PetHopSeconds } from "../../motion/timings"
import { useStage } from "../stage/stageContext"
import { Seal } from "../title/Seal"
import { BackButton } from "./BackButton"
import type { CompanionsApi } from "./CompanionsScreen"
import { CounterHeader } from "./KeeperSay"
import { PenButton } from "./PenButton"
import { RenameField } from "./RenameField"
import { useKeeperPage } from "./useKeeperPage"
import petSilhouette from "../../assets/companions/pet-silhouette.webp"
import stallGate from "../../assets/companions/stall-gate.webp"
import fruitVigor from "../../assets/companions/fruit-vigor.webp"
import fruitAroma from "../../assets/companions/fruit-aroma.webp"
import fruitSweet from "../../assets/companions/fruit-sweet.webp"
import fruitWild from "../../assets/companions/fruit-wild.webp"
import purse from "../../assets/town/purse.webp"

type Props = {
  api: CompanionsApi
  character: CharacterVo
  onCharacterChange: (character: CharacterVo) => void
  onBack: () => void
}

const Stalls = 3
const StableLoadFailed: KeeperEvent = { kind: "stableLoadFailed" }
const PetFailed: KeeperEvent = { kind: "petFailed" }
const PetNameMaxLength = 12
// setTimeout 的上限（32 位元有號整數）
const MaxTimerMilliseconds = 2_147_483_647

const fruitImages: Record<PetFoodStat, string> = {
  Attack: fruitVigor,
  Magic: fruitAroma,
  Defense: fruitSweet,
  Spirit: fruitWild,
}

type Effect = { petId: number; text: string; key: number }

const percentWidth = (ratio: number) => ({ width: `${Math.round(ratio * 100)}%` })

const Bar = ({
  label,
  ratio,
  value,
  kind,
}: {
  label: string
  ratio: number
  value: string
  kind: string
}) => (
  <>
    <span>{label}</span>
    <span className={`stable-bar stable-bar--${kind}`}>
      <i role="progressbar" aria-label={label} style={percentWidth(ratio)} />
    </span>
    <b>{value}</b>
  </>
)

// 果實槽上緣的冷卻金線：一律 10 秒縮完，從出現時已經過去的進度接著縮（同一次冷卻只算一次）
const CooldownLine = ({ until }: { until: Date }) => {
  const [elapsed] = useState(() => feedCooldownElapsed(until, new Date()))
  return (
    <i
      className="stable-trough__cooldown"
      style={{ animationDelay: `${-elapsed}ms` }}
      aria-hidden="true"
    />
  )
}

// 獸欄：三格柵門（寵物從門後探出），木台上是選中寵物的資料、果實槽與動作
export const StablePage = ({ api, character, onCharacterChange, onBack }: Props) => {
  const stage = useStage()
  const load = useCallback(() => api.getPets(), [api])
  const {
    view,
    setView,
    loadFailed,
    event,
    setEvent,
    busy,
    perform,
    run: send,
    retry,
  } = useKeeperPage<PetsVo>({
    api,
    load,
    onCharacterChange,
    loadFailed: StableLoadFailed,
    rejected: (reason) => ({ kind: "petRejected", reason }),
    failed: PetFailed,
  })
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [renaming, setRenaming] = useState(false)
  const [releasing, setReleasing] = useState(false)
  const [effect, setEffect] = useState<Effect | null>(null)
  const [now, setNow] = useState(() => new Date())

  // 餵食冷卻結束時重繪，果實才會恢復可按；等多久用實際時間算（讀資料的往返不會讓它晚到）
  useEffect(() => {
    if (view === null) return
    const cooling = view.pets.filter((pet) => pet.nextFeedAt.getTime() > now.getTime())
    if (cooling.length === 0) return
    const current = Date.now()
    const wait = Math.min(
      ...cooling.map((pet) => Math.max(0, pet.nextFeedAt.getTime() - current)),
      MaxTimerMilliseconds,
    )
    const timer = setTimeout(() => setNow(new Date()), wait)
    return () => clearTimeout(timer)
  }, [view, now])

  const pets = view?.pets ?? []
  const pet = pets.find((candidate) => candidate.id === selectedId) ?? pets[0] ?? null

  // 成功時換成回應裡的寵物清單並由米菈說一句
  const run = (action: () => Promise<PetsVo>, success: KeeperEvent) =>
    send(action, success).then((done) => {
      if (done) setNow(new Date())
      return done
    })

  const feed = (target: Pet, food: PetFood) =>
    perform(
      () => api.feedPet(target.id, food.id),
      (result) => {
        onCharacterChange(result.character)
        const after = result.pets.pets.find((candidate) => candidate.id === target.id)
        setView(result.pets)
        setNow(new Date())
        if (after !== undefined)
          setEffect((previous) => ({
            petId: target.id,
            text: feedEffect(target, after, food),
            key: (previous?.key ?? 0) + 1,
          }))
        return null
      },
    )

  const select = (id: number) => {
    setSelectedId(id)
    setEvent(null)
    setRenaming(false)
    setReleasing(false)
  }

  const stalls = Array.from({ length: Stalls }, (_, index) => pets[index] ?? null)

  return (
    <section
      className="companions-page companions-page--stable"
      aria-label="獸欄"
      style={
        {
          "--pt": `${companionsCounterTop(stage.height)}px`,
          "--pet-hop": `${PetHopSeconds}s`,
          "--feed-effect": `${FeedEffectSeconds}s`,
          "--feed-cooldown": `${FeedCooldownMilliseconds}ms`,
        } as CSSProperties
      }
    >
      <div className="companions-scene companions-scene--stable">
        <ul className="stable-stalls" aria-label="柵欄">
          {stalls.map((stall, index) =>
            stall === null ? (
              <li key={`empty-${index}`}>
                <button
                  type="button"
                  className="stable-stall stable-stall--empty"
                  aria-label="空的柵欄"
                  disabled
                >
                  <img className="stable-stall__gate" src={stallGate} alt="" />
                  <span className="stable-stall__plate" />
                </button>
              </li>
            ) : (
              <li key={stall.id}>
                <button
                  type="button"
                  className={`stable-stall${stall.id === pet?.id ? " stable-stall--selected" : ""}${effect?.petId === stall.id ? " stable-stall--fed" : ""}`}
                  aria-label={`${stall.name}（${stall.species}）${stall.carried ? "，攜帶中" : ""}`}
                  aria-pressed={stall.id === pet?.id}
                  onClick={() => select(stall.id)}
                >
                  <ContentImage
                    key={effect?.petId === stall.id ? effect.key : 0}
                    className="stable-stall__pet"
                    category="monsters"
                    assetKey={stall.monsterId}
                    alt=""
                    placeholder={petSilhouette}
                  />
                  <img className="stable-stall__gate" src={stallGate} alt="" />
                  <span className="stable-stall__plate">{stall.name}</span>
                  {stall.carried && <span className="stable-stall__lead">攜帶</span>}
                </button>
                {effect?.petId === stall.id && (
                  <span key={effect.key} className="stable-stall__effect" data-testid="feed-effect">
                    {effect.text}
                  </span>
                )}
              </li>
            ),
          )}
        </ul>
      </div>
      <BackButton label="同伴" onClick={onBack} />
      <section className="inn__counter" aria-label="寵物">
        <CounterHeader
          title={
            <>
              <b>獸欄</b>
              <small>{view === null ? "" : `寵物 ${pets.length} / ${Stalls}`}</small>
            </>
          }
          line={event === null ? null : keeperLine(event)}
          money={character.money}
        />
        <i className="inn__rule" aria-hidden="true" />
        {loadFailed ? (
          <div className="companions-retry">
            <Seal className="companions-retry__seal" label="再試一次" onClick={retry} />
          </div>
        ) : (
          pet !== null && (
            <div className="companions-body">
              <div className="companions-head">
                {renaming ? (
                  <RenameField
                    label="寵物名字"
                    initial={pet.name}
                    maxLength={PetNameMaxLength}
                    busy={busy}
                    onCancel={() => setRenaming(false)}
                    onConfirm={async (name) => {
                      if (
                        await run(() => api.renamePet(pet.id, name), { kind: "petRenamed", name })
                      )
                        setRenaming(false)
                    }}
                  />
                ) : (
                  <>
                    <b className="companions-head__name">{pet.name}</b>
                    <PenButton
                      disabled={busy}
                      onClick={() => {
                        setEvent(null)
                        setReleasing(false)
                        setRenaming(true)
                      }}
                    />
                  </>
                )}
                <span className="companions-head__tag">{personalityLabel(pet.personality)}</span>
              </div>
              <p className="companions-sub">{`${pet.species}・${pet.age} 歲`}</p>
              <ul className="stable-stats" aria-label="能力">
                {(
                  [
                    ["AT", pet.attack],
                    ["MT", pet.magic],
                    ["DEF", pet.defense],
                    ["出擊值", pet.spirit],
                  ] as const
                ).map(([label, value]) => (
                  <li key={label}>
                    <span>{label}</span>
                    <b>{formatNumber(value)}</b>
                  </li>
                ))}
              </ul>
              <div className="stable-bars">
                <Bar
                  label="飽腹"
                  kind="satiety"
                  ratio={satietyRatio(pet.satiety)}
                  value={formatNumber(pet.satiety)}
                />
                <Bar
                  label="親密"
                  kind="intimacy"
                  ratio={intimacyRatio(pet.intimacy)}
                  value={formatNumber(pet.intimacy)}
                />
                <Bar
                  label="出擊率"
                  kind="activation"
                  ratio={activationRatio(pet.activationPercent)}
                  value={`${pet.activationPercent.toFixed(1)}%`}
                />
              </div>
              <div className="stable-trough" role="group" aria-label="果實">
                {now.getTime() < pet.nextFeedAt.getTime() && (
                  <CooldownLine
                    key={`${pet.id}-${pet.nextFeedAt.getTime()}`}
                    until={pet.nextFeedAt}
                  />
                )}
                {(view?.foods ?? []).map((food) => {
                  const short = character.money < food.cost
                  return (
                    <button
                      key={food.id}
                      type="button"
                      className={`stable-fruit${food.amount > 2 ? " stable-fruit--medium" : ""}`}
                      aria-label={`${food.name}，${fruitEffect(food)}，${food.cost} 金錢`}
                      data-short={short}
                      disabled={busy || fruitDisabled(pet, food, character.money, now)}
                      onClick={() => void feed(pet, food)}
                    >
                      <img className="stable-fruit__image" src={fruitImages[food.stat]} alt="" />
                      <span className="stable-fruit__effect">{fruitEffect(food)}</span>
                      <span className="stable-fruit__price">
                        <img src={purse} alt="" />
                        {formatNumber(food.cost)}
                      </span>
                    </button>
                  )
                })}
              </div>
              <div className="companions-actions">
                {releasing ? (
                  <>
                    <button
                      type="button"
                      className="companions-button companions-button--danger"
                      disabled={busy}
                      onClick={() =>
                        void run(() => api.releasePet(pet.id), {
                          kind: "released",
                          name: pet.name,
                        }).then((done) => {
                          setReleasing(false)
                          if (done) setSelectedId(null)
                        })
                      }
                    >
                      確定放生
                    </button>
                    <button
                      type="button"
                      className="companions-button companions-button--quiet"
                      onClick={() => setReleasing(false)}
                    >
                      取消
                    </button>
                  </>
                ) : (
                  <>
                    <button
                      type="button"
                      className={`companions-button${pet.carried ? " companions-button--muted" : ""}`}
                      disabled={busy}
                      onClick={() =>
                        void (pet.carried
                          ? run(() => api.storePet(pet.id), { kind: "stored", name: pet.name })
                          : run(() => api.carryPet(pet.id), { kind: "carried", name: pet.name }))
                      }
                    >
                      {pet.carried ? "放回獸欄" : "攜帶"}
                    </button>
                    <button
                      type="button"
                      className="companions-button companions-button--quiet"
                      disabled={busy}
                      onClick={() => {
                        setEvent(null)
                        setRenaming(false)
                        setReleasing(true)
                      }}
                    >
                      放生
                    </button>
                  </>
                )}
              </div>
            </div>
          )
        )}
      </section>
    </section>
  )
}
