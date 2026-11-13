export const PollIntervalMilliseconds = 10_000
export const RequestTimeoutMilliseconds = 8_000
export const RestAfterBatchSeconds = 1
export const HitShakeSeconds = 0.25
export const SlashSeconds = 0.3
export const DamageNumberSeconds = 0.9
export const CriticalNumberPopSeconds = 0.18
export const PlateSeconds = 1.2
export const ImpactBurstSeconds = 0.8
export const BreathingCycleSeconds = 3
export const BeamSeconds = 0.35
export const MissSeconds = 0.8
export const StandOffSeconds = 0.6
export const LevelUpFlashSeconds = 1.2
export const DropFloatSeconds = 1.4
// 同一場掉落多件時，每件飄字依序晚這麼久出現
export const DropFloatStaggerSeconds = 0.3
// 住宿等待 5～15 秒，舞台淡出淡入最多只花這麼久
export const RestFadeMaxSeconds = 1.5
export const LevelUpHintMilliseconds = 3_000
// 開著的背包、任務、技能頁收到戰報訊號時重新載入，同一頁兩次至少間隔這麼久
export const PanelRefreshMilliseconds = 5_000
// 角色頁密藥剩餘分鐘的重算間隔
export const BuffTickMilliseconds = 30_000
// 城鎮加成列與旅館的剩餘時間：不到 1 小時要顯示秒，每秒重算
export const CountdownTickMilliseconds = 1_000
// 標題背景依時段換圖，畫面開著時隔這麼久重算一次
export const TimeOfDayRefreshMilliseconds = 60_000
// 註冊成功蓋上蠟封後，停這麼久再開始讀角色
export const RegisterSealSettleMilliseconds = 900
// 註冊成功但登入失敗、換回自己那張證後，空白證停這麼久再清空放回那疊（兩張證互換的動畫 0.55 秒）
export const RegisterCardReturnMilliseconds = 600
// 讀角色時，過這麼久還沒好才浮出墨點，快的時候不閃
export const InkDotsDelaySeconds = 0.45
// 「歡迎回來」「踏上旅途」過場停留多久再進遊戲
export const PassageMilliseconds = 2_400
// 打開網頁沒角色或讀不到時，過場背景疊在下一個畫面上淡出（延遲 0.3 秒＋0.6 秒，與 Passage.css 一致）
export const PassageLeaveMilliseconds = 900
// 城鎮左右滑完後這麼久內的紙籤點擊不算（從紙籤上開始滑時，放開會再觸發點擊）
export const SwipeClickGuardMilliseconds = 300
// 旅館的自動住宿門檻：最後一次按 −／＋ 後這麼久才送出（連按只送一次）
export const InnThresholdSaveDelayMilliseconds = 600
// 旅館從城鎮市集的名牌圓形展開／收回的時間（與 InnReveal.css 一致）
export const InnRevealMilliseconds = 550
