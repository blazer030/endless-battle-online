## MODIFIED Requirements

### Requirement: Load the character before entering

After signing in, after registering, and when the game opens with a stored access token, the screen SHALL enter a loading state that hides the registry cards and, after 0.45 seconds, shows three animated ink dots on the desk. Loading SHALL request the player's character. When a character exists the screen SHALL show 歡迎回來 with the character's name over the title background moving into the town, and SHALL then open the game; this SHALL be the same whether loading started from signing in, registering or a stored access token. When the account has no character the character creation screen SHALL open. When the request is rejected as unauthorized the stored token SHALL be cleared and the title screen SHALL show the registry cards. Any other failure SHALL show a note reading 公會那邊沒有回音…… with a 重試 seal that repeats the load.

#### Scenario: Opening the game while signed in

- **WHEN** the game opens with a stored access token whose account has a character named 青衣
- **THEN** the loading state appears without showing the registry cards, then 歡迎回來 appears with 青衣, and the game opens after the transition

#### Scenario: Signing in shows the character name

- **WHEN** the player signs in as ethan and the account has a character named 青衣
- **THEN** 歡迎回來 appears with 青衣 and not with the account name ethan, and the game opens after the transition

#### Scenario: Load fails and is retried

- **WHEN** loading the character fails with a network error and the player presses 重試
- **THEN** the note disappears, the loading state returns, and a successful load continues as the first attempt would have

##### Example: result after loading

| Started from | Character exists | Result |
| --- | --- | --- |
| sign in | yes | 歡迎回來 + character name, then game |
| sign in | no | character creation |
| register | no | character creation |
| stored token | yes | 歡迎回來 + character name, then game |
| stored token | no | character creation |
| any | request unauthorized | title screen with cards, token cleared |
