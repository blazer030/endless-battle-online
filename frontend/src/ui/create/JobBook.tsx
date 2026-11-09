import type { CSSProperties } from "react"
import { growthStatKeys, type GrowthStatKey, type JobVo } from "../../game/jobBook"
import { contentArtCandidates } from "../../render/contentAssets"
import { ContentImage } from "../ContentImage"
import { Seal } from "../title/Seal"
import { bookmarkOf } from "./art"
import { jobFlavors } from "./flavor"
import "./JobBook.css"

type Props = {
  open: boolean
  jobs: readonly JobVo[]
  page: string
  sex: number | null
  onPage: (jobId: string) => void
  onPick: (job: JobVo) => void
}

const growthLabels: Record<GrowthStatKey, string> = {
  strength: "力量",
  intelligence: "智力",
  life: "生命",
  vitality: "體質",
  agility: "敏捷",
  charisma: "魅力",
  faith: "信仰",
}

const growthText = ({ minimum, maximum }: { minimum: number; maximum: number }) =>
  minimum === maximum ? `+${minimum}` : `+${minimum}～${maximum}`

// 立繪依登記證上的性別：女用 -female，其餘（男、還沒選）用 -male
const portraitKey = (jobId: string, sex: number | null) =>
  `${jobId}-${sex === 2 ? "female" : "male"}`

// 職業介紹冊：從櫃檯下方推上來，書籤換頁、「就職」把這頁的職業填進登記證
export const JobBook = ({ open, jobs, page, sex, onPage, onPick }: Props) => {
  const job = jobs.find((candidate) => candidate.id === page) ?? jobs[0]
  if (job === undefined) return null
  const flavor = jobFlavors[job.id]
  const fastest = Math.max(...growthStatKeys.map((key) => job.growth[key].maximum))

  return (
    <div
      className={`job-book${open ? " job-book--open" : ""}`}
      aria-hidden={!open || undefined}
      inert={!open}
    >
      <div className="job-book-marks" role="tablist" aria-label="職業">
        {jobs.map((candidate) => (
          <button
            key={candidate.id}
            type="button"
            role="tab"
            className="job-book-mark"
            aria-selected={candidate.id === job.id}
            aria-label={candidate.name}
            style={
              {
                "--mark": `url(${bookmarkOf(candidate.id)})`,
                "--mark-icon": `url(${contentArtCandidates("job-icons", candidate.id)[0]})`,
              } as CSSProperties
            }
            onClick={() => onPage(candidate.id)}
          />
        ))}
      </div>
      <div className="job-book-page" role="tabpanel" aria-label={job.name}>
        <div className="job-book-head">
          <ContentImage category="job-icons" assetKey={job.id} alt="" />
          <b>{job.name}</b>
          {flavor !== undefined && <small>{flavor.english}</small>}
        </div>
        <div className="job-book-art">
          <ContentImage
            className="job-book-portrait"
            category="portraits"
            assetKey={portraitKey(job.id, sex)}
            alt={job.name}
          />
        </div>
        <div className="job-book-info">
          {flavor !== undefined && (
            <>
              <p className="job-book-role">{flavor.role}</p>
              <p className="job-book-description">{flavor.description}</p>
            </>
          )}
          <h3>成長</h3>
          <ul className="job-book-growth" aria-label="成長">
            {growthStatKeys.map((key) => (
              <li
                key={key}
                className={job.growth[key].maximum === fastest ? "job-book-growth--top" : undefined}
              >
                <i>{growthLabels[key]}</i>
                {growthText(job.growth[key])}
              </li>
            ))}
          </ul>
        </div>
        <div className="job-book-skills">
          <h3>技能</h3>
          <ul aria-label="技能">
            {job.skills.map((skill) => {
              const top = skill.levels[skill.levels.length - 1]
              return (
                <li
                  key={skill.id}
                  className={skill.prerequisite === null ? "job-book-skill--first" : undefined}
                >
                  <b>{skill.name}</b>
                  <small>
                    {skill.prerequisite === null
                      ? "入門"
                      : `前置 ${skill.prerequisite.name} Lv${skill.prerequisite.level}`}
                  </small>
                  <span className="job-book-max">最高 Lv{skill.maxLevel}</span>
                  {skill.levels[0] !== undefined && (
                    <p>
                      <em>Lv1</em>
                      {skill.levels[0].summary}
                    </p>
                  )}
                  {top !== undefined && top.level > 1 && (
                    <p>
                      <em>Lv{top.level}</em>
                      {top.summary}
                    </p>
                  )}
                </li>
              )
            })}
          </ul>
        </div>
        <Seal label="就職" className="seal--glow" onClick={() => onPick(job)} />
      </div>
    </div>
  )
}
