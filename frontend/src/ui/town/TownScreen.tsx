import { useRef, useState, type CSSProperties, type ReactNode } from "react"
import type { ApiClient } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import { formatNumber } from "../../game/format"
import { portraitKey } from "../../game/portrait"
import {
  isSwipe,
  panoramaOffsets,
  panoramaScale,
  swipeTarget,
  type TownFacility,
  type TownSegment,
} from "../../game/town"
import { SwipeClickGuardMilliseconds } from "../../motion/timings"
import { ContentImage } from "../ContentImage"
import { useStage } from "../stage/stageContext"
import { TownBonuses } from "./TownBonuses"
import { useTimeOfDay } from "../title/useTimeOfDay"
import iconBag from "../../assets/town/icon-bag.webp"
import iconCharacter from "../../assets/town/icon-character.webp"
import iconCompanions from "../../assets/town/icon-companions.webp"
import purse from "../../assets/town/purse.webp"
import "./TownScreen.css"

export type TownApi = Pick<ApiClient, "getBuffs" | "getInn">

type Props = {
  api: TownApi
  // 進城鎮那一刻的角色；城鎮不連線，所以停留期間不會變
  character: CharacterVo
  onOpen: (facility: TownFacility) => void
  // 從旅館回來停在市集；其他時候從城門口開始
  initialSegment?: TownSegment
}

const percent = (value: number, maximum: number) =>
  maximum <= 0 ? 0 : Math.round((Math.min(value, maximum) / maximum) * 100)

const Bar = ({ label, value, maximum }: { label: "HP" | "SP"; value: number; maximum: number }) => (
  <div className={`town-status__row town-status__row--${label.toLowerCase()}`}>
    <span className="town-status__label">{label}</span>
    <div className="town-status__bar">
      <i
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={maximum}
        aria-valuenow={value}
        style={{ width: `${percent(value, maximum)}%` }}
      />
    </div>
  </div>
)

type SignProps = {
  className: string
  style?: CSSProperties
  label?: string
  onClick: () => void
  children: ReactNode
}

const Sign = ({ className, style, label, onClick, children }: SignProps) => (
  <button
    type="button"
    className={`town-paper ${className}`}
    style={style}
    aria-label={label}
    onClick={onClick}
  >
    {children}
  </button>
)

const Arrow = ({ direction }: { direction: "left" | "right" }) => (
  <svg
    className={`town-arrow town-arrow--${direction}`}
    aria-hidden="true"
    width="18"
    height="18"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2.6"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d={direction === "right" ? "M9 5l7 7-7 7" : "M15 5l-7 7 7 7"} />
  </svg>
)

// 每段的紙籤只在那一段可見、可點；不在的段落對輔助技術隱藏、也不能操作
const Segment = ({ active, children }: { active: boolean; children: ReactNode }) => (
  <div
    className={`town-segment${active ? "" : " town-segment--away"}`}
    aria-hidden={!active}
    inert={!active}
  >
    {children}
  </div>
)

// 城鎮：三段全景（城門口、市集、公會前），點紙籤或左右滑換段；上方固定名字、狀態與金錢
export const TownScreen = ({ api, character, onOpen, initialSegment = 0 }: Props) => {
  const stage = useStage()
  const time = useTimeOfDay()
  const [segment, setSegmentState] = useState<TownSegment>(initialSegment)
  const [bonusOpen, setBonusOpen] = useState(false)
  // 換段時收起加成清單
  const setSegment = (target: TownSegment) => {
    setSegmentState(target)
    setBonusOpen(false)
  }
  const pointer = useRef<{ x: number; y: number } | null>(null)
  const swipedAt = useRef(Number.NEGATIVE_INFINITY)

  const offsets = panoramaOffsets(stage.height)
  const scale = panoramaScale(stage.height)
  // 剛滑完的那一下不算點擊：從紙籤上開始滑時，放開會再觸發紙籤
  const justSwiped = () => Date.now() - swipedAt.current < SwipeClickGuardMilliseconds
  const goTo = (target: TownSegment) => {
    if (!justSwiped()) setSegment(target)
  }
  const open = (facility: TownFacility, at: TownSegment) => {
    if (segment === at && !justSwiped()) onOpen(facility)
  }

  const worldStyle = {
    "--k": `${scale}`,
    "--p1": `${offsets[1]}px`,
    "--p2": `${offsets[2]}px`,
    transform: `translateX(-${offsets[segment]}px)`,
  } as CSSProperties
  const pinned = (x: number, y: number, rotate = 0) =>
    ({
      left: `calc(${x}px * var(--k))`,
      top: `calc(${y}px * var(--k))`,
      "--r": `${rotate}deg`,
    }) as CSSProperties

  return (
    <div
      className="town"
      onPointerDown={(event) => {
        pointer.current = { x: event.clientX, y: event.clientY }
      }}
      onPointerUp={(event) => {
        const start = pointer.current
        pointer.current = null
        if (start === null) return
        // 視窗 px 換成舞台 px
        const deltaX = (event.clientX - start.x) / stage.scale
        const deltaY = (event.clientY - start.y) / stage.scale
        const target = swipeTarget(segment, deltaX, deltaY)
        if (isSwipe(deltaX, deltaY)) swipedAt.current = Date.now()
        if (target !== segment) setSegment(target)
      }}
      onPointerCancel={() => {
        pointer.current = null
      }}
    >
      <div
        className={`town-world town-world--${time}`}
        data-testid="town-world"
        data-segment={segment}
        style={worldStyle}
      >
        <div className="town-world__fade" aria-hidden="true" />

        <div className="town-hero">
          <ContentImage
            className="town-hero__image"
            category="portraits"
            assetKey={portraitKey(character.jobId, character.sex)}
            alt={`${character.name}（${character.jobName}）`}
          />
        </div>

        <Segment active={segment === 0}>
          <Sign
            className="town-menu"
            style={{ left: 222, top: 168, "--r": "-2deg" } as CSSProperties}
            label={`角色 Lv ${character.level}`}
            onClick={() => open("character", 0)}
          >
            <img src={iconCharacter} alt="" />
            <b>角色</b>
            <small>{`Lv ${character.level}`}</small>
          </Sign>
          <Sign
            className="town-menu"
            style={{ left: 230, top: 230, "--r": "1.5deg" } as CSSProperties}
            onClick={() => open("bag", 0)}
          >
            <img src={iconBag} alt="" />
            <b>背包</b>
          </Sign>
          <Sign
            className="town-menu"
            style={{ left: 218, top: 292, "--r": "-1deg" } as CSSProperties}
            onClick={() => open("companions", 0)}
          >
            <img src={iconCompanions} alt="" />
            <b>同伴</b>
          </Sign>
          <Sign className="town-turn town-turn--to-market town-fade" onClick={() => goTo(1)}>
            <b>市集</b>
            <Arrow direction="right" />
          </Sign>
          <button
            type="button"
            className="town-compass"
            aria-label={`出發 ${character.placeName}`}
            onClick={() => open("adventure", 0)}
          >
            <span>
              <b>出發</b>
              <small>{character.placeName}</small>
            </span>
          </button>
        </Segment>

        <Segment active={segment === 1}>
          <Sign className="town-turn town-turn--to-gate town-fade" onClick={() => goTo(0)}>
            <Arrow direction="left" />
            <b>城門口</b>
          </Sign>
          <Sign
            className="town-sign town-fade"
            style={pinned(794, 300, -2)}
            label="旅館"
            onClick={() => open("inn", 1)}
          >
            旅館<i>休息</i>
          </Sign>
          <Sign
            className="town-sign town-fade"
            style={pinned(961.5, 420)}
            label="商店街"
            onClick={() => open("shop", 1)}
          >
            商店街<i>買賣</i>
          </Sign>
          <Sign className="town-turn town-turn--to-guild town-fade" onClick={() => goTo(2)}>
            <b>公會前</b>
            <Arrow direction="right" />
          </Sign>
        </Segment>

        <Segment active={segment === 2}>
          <Sign className="town-turn town-turn--to-market-back town-fade" onClick={() => goTo(1)}>
            <Arrow direction="left" />
            <b>市集</b>
          </Sign>
          <Sign
            className="town-sign town-fade"
            style={pinned(1209, 452)}
            label="鐵舖"
            onClick={() => open("forge", 2)}
          >
            鐵舖<i>精鍊・鑲嵌</i>
          </Sign>
          <Sign
            className="town-sign town-fade"
            style={pinned(1373, 256, 2)}
            onClick={() => open("honour", 2)}
          >
            勳章所
          </Sign>
          <Sign
            className="town-sign town-fade"
            style={pinned(1399, 540)}
            onClick={() => open("missions", 2)}
          >
            任務板
          </Sign>
        </Segment>
      </div>

      <section className="town-status" aria-label="角色狀態">
        <div className="town-status__identity">
          <span className="town-status__name">{character.name}</span>
          <span className="town-status__job">{character.jobName}</span>
          <span className="town-status__level">{`Lv ${character.level}`}</span>
        </div>
        <Bar label="HP" value={character.healthPoints} maximum={character.stats.maxHealthPoints} />
        <Bar label="SP" value={character.manaPoints} maximum={character.stats.maxManaPoints} />
      </section>

      <TownBonuses
        api={api}
        open={bonusOpen}
        onToggle={() => setBonusOpen((current) => !current)}
        onClose={() => setBonusOpen(false)}
      />

      <div className="town-money" aria-label="金錢">
        <img src={purse} alt="" />
        <span>{formatNumber(character.money)}</span>
      </div>
    </div>
  )
}
