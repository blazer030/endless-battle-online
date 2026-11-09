// 建角畫面上莉瑟的台詞與初階職業的介紹文字；內容檔只有數值，文字在這裡維護

export const Questions = {
  name: "請問該怎麼稱呼您呢？",
  sex: "請問是先生還是小姐呢？",
  birth: "您是從哪裡來的呢？",
  element: "守護您的是哪一種屬性呢？",
  job: "想成為哪一種冒險者呢？",
  stats: "您擅長些什麼呢？",
  leave: "登記證還沒填完呢，確定現在就要離開嗎？",
} as const

export const Lines = {
  greeting: "初次見面！我是公會接待員莉瑟。請填寫這張登記證吧。",
  keepGoing: "其他欄也麻煩填一下喔。",
  badName: "名字要 2～12 個字喔。",
  noResponse: "咦，登記簿那邊沒有回應……",
  goodName: (name: string) => `${name}，好名字！`,
  allDone: (name: string) => `都寫好了，${name}！準備好就出發吧。`,
  nameTaken: (name: string) => `哎呀，「${name}」已經有人登記了……換一個名字好嗎？`,
  element: (name: string, strong: string, weak: string) =>
    `「${name}」屬性——剋${strong}，怕${weak}。`,
  jobPicked: (job: string) => `那就寫上「${job}」囉。`,
}

// 性別選項的稱呼；登記證上寫 API 的名稱（男、女）
export const sexAddress: Record<number, string> = { 1: "先生", 2: "小姐" }

export const birthLines: Record<number, string> = {
  1: "中央大陸啊，跟我是同鄉呢！",
  2: "魔法王國來的？難怪氣質不太一樣。",
  3: "從熱帶雨林一路過來，辛苦了！",
  4: "末日王城……那裡現在還好嗎？",
}

export type JobFlavor = { english: string; role: string; description: string; line: string }

export const jobFlavors: Record<string, JobFlavor> = {
  "job-6": {
    english: "Warrior",
    role: "近身前排，攻守兼備",
    description: "持劍披甲站在最前線，力量、生命、體質都長得快，是最穩的物理職業。",
    line: "站在最前面保護大家的人——公會裡最多的就是戰士喔。",
  },
  "job-7": {
    english: "Sorcerer",
    role: "遠距施法，魔攻見長",
    description: "以法杖驅使元素的施法者。智力長得最快，之後還學得會治癒。",
    line: "術士要讀很多書呢……不過魔法打起來真的很厲害！",
  },
  "job-8": {
    english: "Thief",
    role: "身手敏捷，先發制人",
    description: "身手俐落的遊走者，靠敏捷搶先出手、一回合連砍好幾下。",
    line: "動作快、出手多——盜賊總是比怪物先動手呢。",
  },
  "job-9": {
    english: "Bard",
    role: "樂聲支援，攻守靈活",
    description: "以樂聲鼓舞自己、擾亂敵人的旅人。魅力長得最快，曲子越練越多。",
    line: "樂手會用歌聲讓自己更堅強，也能擾亂敵人喔。",
  },
}
