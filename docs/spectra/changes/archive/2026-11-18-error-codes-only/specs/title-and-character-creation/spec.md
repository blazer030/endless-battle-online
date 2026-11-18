## MODIFIED Requirements

### Requirement: Sign in on the registry card

The title screen SHALL show the player's own registry card with an account field, a password field and a 入城 seal. The seal SHALL be enabled only when both fields are non-empty and no request is in progress. Pressing the seal, or pressing Enter in either field, SHALL sign in. On success the screen SHALL load the character. On failure the card SHALL show the text for the response's first reason code (`AUTHENTICATION_FAILED` reads 帳號或密碼錯誤; see the `game-api-contract` capability for unknown codes); when the request fails without an API error the card SHALL show 發生錯誤，請稍後再試.

#### Scenario: Wrong password

- **WHEN** the player signs in with a password the API rejects
- **THEN** the own card shows 帳號或密碼錯誤 and the fields keep their values

#### Scenario: Seal disabled until both fields are filled

- **WHEN** the account field is filled and the password field is empty
- **THEN** the 入城 seal is disabled

### Requirement: Register with a blank card

The title screen SHALL show a stack of blank registry cards beside the own card. Tapping the stack SHALL bring a blank card to the front with its own account field, password field, password confirmation field and 登記 seal; tapping the own card SHALL bring it back. The 登記 seal SHALL stay disabled until all three fields are filled. When the two passwords differ, pressing 登記 SHALL show 兩次密碼不一樣 on the blank card and SHALL NOT call the API. Otherwise pressing 登記 SHALL register the account and then sign in with the same account and password. On success the blank card SHALL be sealed with wax and the screen SHALL load the character. When registration fails the blank card SHALL show the text for the response's first reason code (`USERNAME_INVALID` reads 帳號必須是 3～32 個英數字或底線, `PASSWORD_TOO_SHORT` reads 密碼至少要 8 個字元, `USERNAME_TAKEN` reads 帳號已被使用). When registration succeeds but the sign-in fails, the blank card SHALL be sealed and, 0.9 seconds later, the own card SHALL come to the front with the new account name filled in, an empty password and the failure's error text, because registering the same account again can only fail; 0.6 seconds after that the blank card SHALL be cleared and returned to the stack.

#### Scenario: Account already taken

- **WHEN** the player registers an account name that already exists
- **THEN** the blank card shows 帳號已被使用 and no sign-in is attempted

#### Scenario: Passwords do not match

- **WHEN** the player fills the blank card with the password `secret-one` and the confirmation `secret-two` and presses 登記
- **THEN** the blank card shows 兩次密碼不一樣 and no request is sent

#### Scenario: Sign-in fails after registering

- **WHEN** registration succeeds for the account `newcomer` and the following sign-in fails without an API response
- **THEN** the own card comes to the front with the account `newcomer`, an empty password and 發生錯誤，請稍後再試

#### Scenario: Successful registration leads to character creation

- **WHEN** registration and the following sign-in succeed for a new account
- **THEN** the blank card is sealed, the character is loaded, and because the account has no character the character creation screen opens

### Requirement: Submit or leave character creation

Pressing 出發 SHALL create the character with the filled values. On success the screen SHALL show 踏上旅途 with the character name and then open the game. When the API responds 409 with the reason `ACCOUNT_HAS_CHARACTER`, the account already has a character (for example the first press of 出發 succeeded but its response timed out), so the screen SHALL load that character and continue as on success with the loaded character's name. When the API responds 409 with the reason `CHARACTER_NAME_TAKEN`, 莉瑟 SHALL say 哎呀，「<name>」已經有人登記了……換一個名字好嗎？ and the 名字 row SHALL open with the name to edit. When the API responds with any other reason code, 莉瑟 SHALL say the text for the response's first reason code (for example 400 `POINTS_NOT_SPENT` reads 能力點數必須剛好分完 10 點, 409 `CONCURRENT_UPDATE` reads 資料正被其他請求使用，請重試) and the 名字 row SHALL stay closed. Any other failure SHALL make 莉瑟 say 咦，登記簿那邊沒有回應……, keep every filled row, and keep 出發 enabled; tapping a row or pressing 出發 again SHALL replace that line. The 離開 button SHALL ask 登記證還沒填完呢，確定現在就要離開嗎？ with 繼續填寫 and 先離開; 先離開 SHALL clear the stored access token and return to the title screen with the account name on the own card and an empty password.

#### Scenario: Name taken

- **WHEN** the player presses 出發 with the name 收尾者 and the API responds 409 with the reason `CHARACTER_NAME_TAKEN`
- **THEN** 莉瑟 says 哎呀，「收尾者」已經有人登記了……換一個名字好嗎？ and the 名字 answer is open containing 收尾者

#### Scenario: Account already has a character

- **WHEN** the player presses 出發 again after an earlier press created the character 收尾者 but timed out, and the API responds 409 with the reason `ACCOUNT_HAS_CHARACTER`
- **THEN** the screen loads the character, shows 踏上旅途 with 收尾者, and opens the game

#### Scenario: Another conflict speaks its reason

- **WHEN** the player presses 出發 and the API responds 409 with the reason `CONCURRENT_UPDATE`
- **THEN** 莉瑟 says 資料正被其他請求使用，請重試, the 名字 answer stays closed, and 出發 stays enabled

#### Scenario: Network failure keeps the card

- **WHEN** the player presses 出發 and the request fails without an API response
- **THEN** 莉瑟 says 咦，登記簿那邊沒有回應……, all six rows keep their values, and 出發 stays enabled

#### Scenario: Leaving signs out

- **WHEN** the player confirms 先離開
- **THEN** the stored access token is cleared and the title screen shows the own card with the registered account name and an empty password
