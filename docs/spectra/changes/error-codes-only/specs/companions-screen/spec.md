## MODIFIED Requirements

### Requirement: Keepers speak feedback and errors

Results SHALL be spoken in the counter header, replacing its title with the keeper's face and one line, the line marked failed for errors; when a detail sheet is open the line SHALL appear inside the sheet above its seal. The line SHALL return to the title at the next action. No system message box SHALL be shown. When an action is rejected with `NOT_ENOUGH_MONEY` the character SHALL be reloaded so that the shown money is current.

#### Scenario: Lines for each result

- **WHEN** an action ends
- **THEN** the keeper says the line for it

##### Example: lines

| Result | Keeper | Line |
| --- | --- | --- |
| carried 小風 | Mira | 小風會跟著你出門囉！ |
| stored 小風 | Mira | 小風回欄裡休息囉。 |
| renamed to 小風 | Mira | 「小風」，牠很喜歡喔！ |
| released 小風 | Mira | 小風回到野外去了，要保重喔。 |
| feeding rejected `OVER_OWNER` | Mira | 再吃下去，牠就要比你還強了呢。 |
| feeding rejected `NOT_ENOUGH_MONEY` | Mira | 哎呀，這顆果實的錢好像不太夠。 |
| pet rename rejected `NAME_INVALID` | Mira | 這名字牠好像記不住呢。 |
| feeding rejected `FULL` | Mira | 牠已經吃得飽飽的了。 |
| feeding rejected `TOO_SOON` | Mira | 慢慢來，讓牠先嚼完這顆。 |
| pet action failed otherwise | Mira | 咦，剛剛沒弄好……再試一次吧？ |
| hired F grade | Barton | 契約簽好了，F級傭兵歸你使喚。 |
| deployed 葛雷 to 迷霧森林 | Barton | 葛雷往迷霧森林出發了。 |
| rested 葛雷 | Barton | 葛雷回來歇口氣了。 |
| dismissed 葛雷 | Barton | 葛雷的契約，我收回來了。 |
| renamed mercenary to 葛雷 | Barton | 葛雷？行，名冊上改好了。 |
| hire rejected `NOT_ENOUGH_MONEY` | Barton | 錢不夠，這張契約簽不了。 |
| hire rejected `MERCENARY_HIRED` with 葛雷 | Barton | 一次只接一份契約，先把葛雷的處理掉。 |
| hire rejected `HONOUR_ONLY` | Barton | 那兩位得去勳章所換，我這兒不賣。 |
| deploy rejected `PLACE_RESTRICTED` | Barton | 那裡是禁區，我的人不去送死。 |
| mercenary rename rejected `NAME_INVALID` | Barton | 這名字名冊上寫不了，換一個。 |
| mercenary action failed otherwise | Barton | 嘖，文書出了點差錯，再來一次。 |
