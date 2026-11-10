## Purpose

The title screen lets a visitor sign in or register on an adventurer's registry card, and the character creation screen lets a newly registered player fill in that card at the guild counter with the help of the receptionist 莉瑟. Together they take a player from opening the game to the first adventure, including the waits and failures in between.

## ADDED Requirements

### Requirement: Sign in on the registry card

The title screen SHALL show the player's own registry card with an account field, a password field and a 入城 seal. The seal SHALL be enabled only when both fields are non-empty and no request is in progress. Pressing the seal, or pressing Enter in either field, SHALL sign in. On success the screen SHALL load the character. On failure the card SHALL show the API's error detail; when the request fails without an API error the card SHALL show 發生錯誤，請稍後再試.

#### Scenario: Wrong password

- **WHEN** the player signs in with a password the API rejects
- **THEN** the own card shows 帳號或密碼錯誤 and the fields keep their values

#### Scenario: Seal disabled until both fields are filled

- **WHEN** the account field is filled and the password field is empty
- **THEN** the 入城 seal is disabled

### Requirement: Register with a blank card

The title screen SHALL show a stack of blank registry cards beside the own card. Tapping the stack SHALL bring a blank card to the front with its own account field, password field, password confirmation field and 登記 seal; tapping the own card SHALL bring it back. The 登記 seal SHALL stay disabled until all three fields are filled. When the two passwords differ, pressing 登記 SHALL show 兩次密碼不一樣 on the blank card and SHALL NOT call the API. Otherwise pressing 登記 SHALL register the account and then sign in with the same account and password. On success the blank card SHALL be sealed with wax and the screen SHALL load the character. When registration fails the blank card SHALL show the API's error detail. When registration succeeds but the sign-in fails, the blank card SHALL be sealed and, 0.9 seconds later, the own card SHALL come to the front with the new account name filled in, an empty password and the failure's error detail, because registering the same account again can only fail; 0.6 seconds after that the blank card SHALL be cleared and returned to the stack.

#### Scenario: Account already taken

- **WHEN** the player registers an account name that already exists
- **THEN** the blank card shows 帳號已被使用 and no sign-in is attempted

#### Scenario: Passwords do not match

- **WHEN** the player fills the blank card with the password `secret-one` and the confirmation `secret-two` and presses 登記
- **THEN** the blank card shows 兩次密碼不一樣 and no request is sent

#### Scenario: Sign-in fails after registering

- **WHEN** registration succeeds for the account `newcomer` and the following sign-in fails without an API response
- **THEN** the own card comes to the front with the account `newcomer`, an empty password and the connection error detail

#### Scenario: Successful registration leads to character creation

- **WHEN** registration and the following sign-in succeed for a new account
- **THEN** the blank card is sealed, the character is loaded, and because the account has no character the character creation screen opens

### Requirement: Load the character before entering

After signing in, after registering, and when the game opens with a stored access token, the screen SHALL enter a loading state that hides the registry cards and, after 0.45 seconds, shows three animated ink dots on the desk. Loading SHALL request the player's character. When a character exists the game SHALL open: after signing in it SHALL first show 歡迎回來 with the account name; when opened with a stored token it SHALL open the game directly. When the account has no character the character creation screen SHALL open. When the request is rejected as unauthorized the stored token SHALL be cleared and the title screen SHALL show the registry cards. Any other failure SHALL show a note reading 公會那邊沒有回音…… with a 重試 seal that repeats the load with the same destination.

#### Scenario: Opening the game while signed in

- **WHEN** the game opens with a stored access token whose account has a character
- **THEN** the loading state appears without showing the registry cards and the game opens without the 歡迎回來 transition

#### Scenario: Load fails and is retried

- **WHEN** loading the character fails with a network error and the player presses 重試
- **THEN** the note disappears, the loading state returns, and a successful load continues to the same destination as the first attempt

##### Example: destination after loading

| Started from | Character exists | Result |
| --- | --- | --- |
| sign in | yes | 歡迎回來 + account name, then game |
| sign in | no | character creation |
| register | no | character creation |
| stored token | yes | game, no transition |
| stored token | no | character creation |
| any | request unauthorized | title screen with cards, token cleared |

### Requirement: Title background follows the time of day

The title screen's town view SHALL use one of four images chosen by the device's local hour: dawn for 05:00–07:59, day for 08:00–16:59, dusk for 17:00–18:59, and night otherwise. The choice SHALL be re-evaluated at least once a minute while the title screen is shown.

#### Scenario: Evening

- **WHEN** the device's local time is 17:30
- **THEN** the title screen shows the dusk town image

##### Example: hour boundaries

| Local hour | Image |
| --- | --- |
| 04 | night |
| 05 | dawn |
| 07 | dawn |
| 08 | day |
| 16 | day |
| 17 | dusk |
| 18 | dusk |
| 19 | night |

### Requirement: Fill the registry card at the guild counter

The character creation screen SHALL show the receptionist 莉瑟 with a speech bubble and an empty registry card with the rows 名字, 性別, 出身, 屬性, 職業 and 能力, plus HP and SP previews. Tapping a row SHALL open that row's answer and change 莉瑟's line to the row's question; tapping the same row again or tapping the dimmed area SHALL close it without changing the row. The player SHALL be able to fill the rows in any order and change a filled row again. The 出發 seal SHALL be enabled only when all six rows are filled.

- 名字 SHALL accept 2 to 12 characters after trimming; otherwise 莉瑟 SHALL say 名字要 2～12 個字喔。 and the row SHALL stay unfilled.
- 性別, 出身 and 屬性 SHALL offer the options from `GET /api/public/settings`; choosing 屬性 SHALL make 莉瑟 name the elements it is strong and weak against.
- 能力 SHALL distribute 10 points over 力量, 智力, 生命 and 敏捷 with at least 1 each, showing each stat as its base value (points + 10). While points are left, the answer's confirm button SHALL be disabled and the row SHALL show how many points remain; the row SHALL be filled only when all 10 points are allocated and confirmed.
- HP and SP previews SHALL be computed from the allocation with the same formula the game uses for derived stats.

#### Scenario: Ready to depart

- **WHEN** 名字, 性別, 出身, 屬性, 職業 and 能力 have all been filled
- **THEN** the 出發 seal becomes enabled

#### Scenario: Points must all be allocated

- **WHEN** the 能力 answer is open with 4 points unspent
- **THEN** its confirm button is disabled and the 能力 row shows 剩 4 點; after the 4 points are allocated the button becomes enabled

##### Example: name length

| Input | Result |
| --- | --- |
| "艾" | rejected, 名字要 2～12 個字喔。 |
| "  艾倫  " | accepted as 艾倫 |
| 12 characters | accepted |
| 13 characters | rejected |

### Requirement: Job introduction book

Tapping the 職業 row SHALL open the job introduction book showing the first-tier jobs from `GET /api/public/jobs`, one page per job selected by bookmarks. A page SHALL show the job's icon, name, portrait, role, description, growth per stat with the fastest-growing stats highlighted, and every skill the job teaches with its prerequisite (or 入門 when it has none) and its one-line description from the skill content. The book SHALL NOT show a skill's maximum level or per-level effect summaries, because it introduces what each job is like rather than how to train its skills. The portrait SHALL be the content image `portraits/<job id>-female` when 性別 is 女 and `portraits/<job id>-male` otherwise, falling back to the portrait placeholder when missing. Pressing 就職 SHALL fill the 職業 row with the shown job and close the book.

#### Scenario: Female portrait

- **WHEN** 性別 is 女 and the book shows 術士 (`job-7`)
- **THEN** the page shows the content image `portraits/job-7-female`

#### Scenario: Skill entries

- **WHEN** the book shows 戰士 (`job-6`)
- **THEN** 爆氣 is marked 入門 with the description 爆發體內的鬥氣，接下來幾回合物攻提升。, 防禦心得 shows its prerequisite 爆氣 Lv3, and no 最高 Lv or level summary appears

### Requirement: Submit or leave character creation

Pressing 出發 SHALL create the character with the filled values. On success the screen SHALL show 踏上旅途 with the character name and then open the game. When the API responds 409 with the reason `account-has-character`, the account already has a character (for example the first press of 出發 succeeded but its response timed out), so the screen SHALL load that character and continue as on success with the loaded character's name. Any other 409 SHALL make 莉瑟 say 哎呀，「<name>」已經有人登記了……換一個名字好嗎？ and the 名字 row SHALL open with the name to edit. When the API responds 400, 莉瑟 SHALL say the API's error detail. Any other failure SHALL make 莉瑟 say 咦，登記簿那邊沒有回應……, keep every filled row, and keep 出發 enabled; tapping a row or pressing 出發 again SHALL replace that line. The 離開 button SHALL ask 登記證還沒填完呢，確定現在就要離開嗎？ with 繼續填寫 and 先離開; 先離開 SHALL clear the stored access token and return to the title screen with the account name on the own card and an empty password.

#### Scenario: Name taken

- **WHEN** the player presses 出發 with the name 收尾者 and the API responds 409 with the reason `character-name-taken`
- **THEN** 莉瑟 says 哎呀，「收尾者」已經有人登記了……換一個名字好嗎？ and the 名字 answer is open containing 收尾者

#### Scenario: Account already has a character

- **WHEN** the player presses 出發 again after an earlier press created the character 收尾者 but timed out, and the API responds 409 with the reason `account-has-character`
- **THEN** the screen loads the character, shows 踏上旅途 with 收尾者, and opens the game

#### Scenario: Network failure keeps the card

- **WHEN** the player presses 出發 and the request fails without an API response
- **THEN** 莉瑟 says 咦，登記簿那邊沒有回應……, all six rows keep their values, and 出發 stays enabled

#### Scenario: Leaving signs out

- **WHEN** the player confirms 先離開
- **THEN** the stored access token is cleared and the title screen shows the own card with the registered account name and an empty password
