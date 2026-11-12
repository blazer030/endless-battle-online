// 職業立繪的內容圖鍵：性別 2 用 -female，其餘（1 男、建角時還沒選）用 -male
export const portraitKey = (jobId: string, sex: number | null) =>
  `${jobId}-${sex === 2 ? "female" : "male"}`
