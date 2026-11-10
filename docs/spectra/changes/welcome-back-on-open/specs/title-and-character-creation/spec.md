## MODIFIED Requirements

### Requirement: Load the character before entering

After signing in or registering, the screen SHALL enter a loading state that hides the registry cards and, after 0.45 seconds, shows three animated ink dots on the desk. When the game opens with a stored access token, the first screen SHALL be the passage background of the current time of day with three animated ink dots from the start, and SHALL NOT show the title sign, the desk or the registry cards. Loading SHALL request the player's character. When a character exists the screen SHALL show 歡迎回來 with the character's name over the passage background, and SHALL then open the game; this SHALL be the same whether loading started from signing in, registering or a stored access token. When the account has no character the character creation screen SHALL open. When the game opens with a stored access token and loading fails for any reason, including an unauthorized response, a network error or a server error, the stored token SHALL be cleared and the title screen SHALL show the registry cards with an empty account and no note. After signing in or registering, when the request is rejected as unauthorized the stored token SHALL be cleared and the title screen SHALL show the registry cards; any other failure SHALL show a note reading 公會那邊沒有回音…… with a 重試 seal that repeats the load.

#### Scenario: Opening the game while signed in

- **WHEN** the game opens with a stored access token whose account has a character named 青衣
- **THEN** the first screen shows the passage background with ink dots and neither the title sign nor the registry cards, then the ink dots give way to 歡迎回來 with 青衣, and the game opens after the transition

#### Scenario: Opening the game while signed in fails

- **WHEN** the game opens with a stored access token and loading the character fails with a network error
- **THEN** the stored token is cleared and the title screen shows the registry cards with an empty account and no 讀取失敗 note

#### Scenario: Signing in shows the character name

- **WHEN** the player signs in as ethan and the account has a character named 青衣
- **THEN** 歡迎回來 appears with 青衣 and not with the account name ethan, and the game opens after the transition

#### Scenario: Load fails and is retried

- **WHEN** after signing in, loading the character fails with a network error and the player presses 重試
- **THEN** the note disappears, the loading state returns, and a successful load continues as the first attempt would have

##### Example: result after loading

| Started from | Load result | Result |
| --- | --- | --- |
| sign in | character exists | 歡迎回來 + character name, then game |
| sign in | no character | character creation |
| sign in | unauthorized | title screen with cards, token cleared |
| sign in | network or server error | note with 重試, token kept |
| register | no character | character creation |
| stored token | character exists | passage background with ink dots, then 歡迎回來 + character name, then game |
| stored token | no character | character creation |
| stored token | unauthorized, network or server error | title screen with cards and empty account, token cleared, no note |
