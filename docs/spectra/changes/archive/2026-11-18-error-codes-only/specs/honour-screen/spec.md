## MODIFIED Requirements

### Requirement: Lise speaks feedback and errors

Feedback and errors SHALL be spoken by Lise and SHALL NOT appear as system messages or toasts. At the counter she SHALL speak in the bubble; while the rack is down she SHALL speak inside the sheet above the seal row with her round portrait and one line, the frame reddish for failures. A line inside the sheet SHALL clear on the next action (another tag, closing the sheet, another exchange). Returning to the counter after at least one exchange SHALL make her say the thanks line; returning without one SHALL make her say the browsing line.

#### Scenario: Bag full names the category

- **WHEN** an exchange of 破山斧 is refused with `BAG_FULL`
- **THEN** Lise says `哎呀，武器的格子已經滿了……` in the sheet with the failure colour

##### Example: clerk lines

| Event | Failure | Lise says |
| --- | --- | --- |
| counter with any medal | no | 歡迎來到勳章所！今天想兌換些什麼呢？ |
| counter with no medals | no | 啊，您還沒有勳章呢……有了再來找我喔！ |
| back to the counter without exchanging | no | 慢慢挑沒關係，陳列板不會跑掉的～ |
| back to the counter after exchanging | no | 兌換好了！祝您冒險一路順風。 |
| loading | no | 歡迎來到勳章所！我翻一下您的帳冊喔…… |
| load failure | yes | 哎呀，帳冊一時翻不到了…… |
| item exchanged | no | 換好了！已經幫您放進背包囉。 |
| mercenary exchanged | no | C級傭兵的契約簽好囉，祝您順利！ |
| `BAG_FULL` for 90000HP回復劑 | yes | 哎呀，道具的格子已經滿了…… |
| `MERCENARY_HIRED` | yes | 您已經有一位傭兵了，一次只能一位喔。 |
| `MEDALS_MISSING` | yes | 勳章好像還差一點呢…… |
| any other reason or a network failure | yes | 咦，剛剛沒送出去……再試一次吧？ |
