import { useEffect, useState, type CSSProperties } from "react"
import type { ApiClient } from "../../api/client"
import type { Account } from "../../game/account"
import type { BuffVo } from "../../game/buffs"
import type { CharacterVo } from "../../game/character"
import { characterLayout, portraitOffset } from "../../game/characterScreen"
import type { CreationOptionsVo } from "../../game/creationOptions"
import type { InnVo } from "../../game/inn"
import { portraitKey } from "../../game/portrait"
import type { SkillOverviewVo } from "../../game/skillOverview"
import { formatNumber } from "../../game/format"
import { contentArtKeys } from "../../render/contentAssets"
import { ContentImage } from "../ContentImage"
import { BackButton } from "../companions/BackButton"
import { useStage } from "../stage/stageContext"
import { Seal } from "../title/Seal"
import purse from "../../assets/town/purse.webp"
import { Popup } from "../feedback/Popup"
import { AbilitiesPage } from "./AbilitiesPage"
import { RegistrationCard } from "./RegistrationCard"
import { SkillsPage } from "./SkillsPage"
import "../inn/InnScreen.css"
import "./CharacterScreen.css"

export type CharacterScreenApi = Pick<
  ApiClient,
  | "getMyCharacter"
  | "getAccount"
  | "getBuffs"
  | "getInn"
  | "getSkills"
  | "getCreationOptions"
  | "learnSkill"
>

type Props = {
  api: CharacterScreenApi
  character: CharacterVo
  onCharacterChange: (character: CharacterVo) => void
  // 左上「城鎮」：回城門口
  onBack: () => void
  // 登出確認後：清掉登入、回標題
  onSignOut: () => void
  // 登記證的「轉職」：這一段先開舊外殼技能分頁
  onOpenJobChange: () => void
}

export type CharacterData = {
  readonly character: CharacterVo
  readonly account: Account
  readonly buffs: readonly BuffVo[]
  readonly inn: InnVo
  readonly skills: SkillOverviewVo
  readonly options: CreationOptionsVo
}

type Load = CharacterData | "failed" | null
type Page = "abilities" | "skills"

const portraitKeys = new Set(contentArtKeys("portraits"))

// 打開時一次讀齊；任何一個失敗（含沒有角色）就整頁讀取失敗
const loadAll = async (api: CharacterScreenApi): Promise<CharacterData> => {
  const [character, account, buffs, inn, skills, options] = await Promise.all([
    api.getMyCharacter(),
    api.getAccount(),
    api.getBuffs(),
    api.getInn(),
    api.getSkills(),
    api.getCreationOptions(),
  ])
  if (character === null) throw new Error("沒有角色")
  return { character, account, buffs, inn, skills, options }
}

// 角色頁（設計板 CharacterFit）：裝備間的立繪與冒險者登記證，桌上攤開的手帳（能力／技能）
export const CharacterScreen = ({
  api,
  onCharacterChange,
  onBack,
  onSignOut,
  onOpenJobChange,
}: Props) => {
  const stage = useStage()
  const [load, setLoad] = useState<Load>(null)
  const [attempt, setAttempt] = useState(0)
  const [page, setPage] = useState<Page>("abilities")
  const [askingSignOut, setAskingSignOut] = useState(false)

  useEffect(() => {
    let isCurrent = true
    loadAll(api).then(
      (loaded) => {
        if (!isCurrent) return
        setLoad(loaded)
        onCharacterChange(loaded.character)
      },
      () => {
        if (isCurrent) setLoad("failed")
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, attempt, onCharacterChange])

  const data = load !== null && load !== "failed" ? load : null
  const layout = characterLayout(stage.height)
  const character = data?.character ?? null
  const hasPortrait =
    character !== null && portraitKeys.has(portraitKey(character.jobId, character.sex))
  const offset = character === null ? 0 : portraitOffset(character.jobId, character.sex)

  return (
    <div
      className="character"
      style={
        {
          "--h": `${stage.height}px`,
          "--cs": layout.scale,
          "--dt": `${layout.deskTop}px`,
          "--portrait-top": `${layout.portraitTop}px`,
          "--portrait-height": `${layout.portraitHeight}px`,
          "--portrait-shift": `${-offset * layout.portraitHeight}px`,
        } as CSSProperties
      }
    >
      <div className="character__desk" aria-hidden="true" />
      <div className="character__portrait">
        {character !== null &&
          (hasPortrait ? (
            <ContentImage
              category="portraits"
              assetKey={portraitKey(character.jobId, character.sex)}
              alt={`${character.name}（${character.jobName}）`}
            />
          ) : (
            <span className="character__portrait-pending">
              <ContentImage category="job-icons" assetKey={character.jobId} alt="" />
              立繪待補
            </span>
          ))}
      </div>

      <RegistrationCard data={data} onOpenJobChange={onOpenJobChange} />

      <div className="character__tabs" role="tablist" aria-label="手帳">
        {(
          [
            ["abilities", "能力"],
            ["skills", "技能"],
          ] as const
        ).map(([id, label]) => (
          <button
            key={id}
            type="button"
            role="tab"
            className="character__tab"
            aria-selected={page === id}
            onClick={() => setPage(id)}
          >
            {label}
          </button>
        ))}
      </div>
      <div className="character__purse" aria-label="持有金錢">
        <img src={purse} alt="" />
        <b>{data === null ? "" : formatNumber(data.character.money)}</b>
      </div>

      <div className="character__journal">
        <div className="character__page">
          {load === null && (
            <div className="character__blank" aria-label="讀取中">
              {Array.from({ length: 8 }, (_, index) => (
                <i key={index} />
              ))}
            </div>
          )}
          {load === "failed" && (
            <div className="character__failed">
              <p className="character__failed-line">登記證的資料讀不到。</p>
              <Seal
                className="character__retry"
                label="再試一次"
                onClick={() => {
                  setLoad(null)
                  setAttempt((count) => count + 1)
                }}
              />
            </div>
          )}
          {data !== null && page === "abilities" && (
            <AbilitiesPage data={data} onAskSignOut={() => setAskingSignOut(true)} />
          )}
          {data !== null && page === "skills" && (
            <SkillsPage jobs={data.skills.jobs} onLearn={() => undefined} />
          )}
        </div>
      </div>

      <BackButton label="城鎮" onClick={onBack} />

      {askingSignOut && data !== null && (
        <Popup
          label="登出"
          className="character-sign-out-ask"
          onClose={() => setAskingSignOut(false)}
          actions={(close) => (
            <div className="character-sign-out-ask__actions">
              <button type="button" className="popup__button" onClick={close}>
                取消
              </button>
              <button type="button" className="popup__button popup__button--go" onClick={onSignOut}>
                登出
              </button>
            </div>
          )}
        >
          <p className="character-sign-out-ask__question">要登出 {data.account.username} 嗎？</p>
        </Popup>
      )}
    </div>
  )
}
