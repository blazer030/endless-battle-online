import bookmarkBard from "../../assets/title/bookmark-bard.webp"
import bookmarkSorcerer from "../../assets/title/bookmark-sorcerer.webp"
import bookmarkThief from "../../assets/title/bookmark-thief.webp"
import bookmarkWarrior from "../../assets/title/bookmark-warrior.webp"
import elementEarth from "../../assets/title/element-earth.webp"
import elementFire from "../../assets/title/element-fire.webp"
import elementPoison from "../../assets/title/element-poison.webp"
import elementWater from "../../assets/title/element-water.webp"
import elementWind from "../../assets/title/element-wind.webp"
import elementWood from "../../assets/title/element-wood.webp"

// 屬性徽章，依屬性 id
export const elementIcons: Record<number, string> = {
  1: elementEarth,
  2: elementWater,
  3: elementFire,
  4: elementWood,
  5: elementWind,
  6: elementPoison,
}

// 職業介紹冊的書籤，依職業 id；沒有對應的職業用戰士的書籤
export const bookmarks: Record<string, string> = {
  "job-6": bookmarkWarrior,
  "job-7": bookmarkSorcerer,
  "job-8": bookmarkThief,
  "job-9": bookmarkBard,
}

export const bookmarkOf = (jobId: string) => bookmarks[jobId] ?? bookmarkWarrior
