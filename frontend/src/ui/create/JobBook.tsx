import type { CSSProperties } from "react"
import type { JobVo } from "../../game/jobBook"
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

// 職業介紹冊：從櫃檯下方推上來，書籤換頁、「就職」把這頁的職業填進登記證
export const JobBook = ({ open, jobs, page, onPage, onPick }: Props) => {
  const job = jobs.find((candidate) => candidate.id === page) ?? jobs[0]
  if (job === undefined) return null
  const flavor = jobFlavors[job.id]

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
        <Seal label="就職" className="seal--glow" onClick={() => onPick(job)} />
      </div>
    </div>
  )
}
