## Purpose

Accounts let a player register a name and password and sign in to receive an access token for the game API. This spec records the account rules that existed before Spectra was adopted, so screens and later changes can rely on them.

## ADDED Requirements

### Requirement: Account registration

`POST /api/accounts/register` SHALL create an account when the username is 3 to 32 characters of letters, digits or underscores and the password has at least 8 characters, and SHALL respond 201. Usernames SHALL be unique ignoring letter case. An invalid username SHALL be rejected with 400 and the detail 帳號必須是 3～32 個英數字或底線; a short password with 400 and 密碼至少要 8 個字元; a username already taken with 409 and 帳號已被使用. Registration SHALL NOT sign the player in.

#### Scenario: Valid registration

- **WHEN** a client registers username `ethan_01` with password `abcdefgh`
- **THEN** the API responds 201 and the account can sign in with those credentials

#### Scenario: Username taken in another letter case

- **WHEN** `Ethan` exists and a client registers `ethan`
- **THEN** the API responds 409 with the detail 帳號已被使用

##### Example: registration input

| Username | Password | Response |
| --- | --- | --- |
| `ab` | `abcdefgh` | 400 帳號必須是 3～32 個英數字或底線 |
| `abc` | `abcdefgh` | 201 |
| `has space` | `abcdefgh` | 400 帳號必須是 3～32 個英數字或底線 |
| 33 letters | `abcdefgh` | 400 帳號必須是 3～32 個英數字或底線 |
| `abc` | `abcdefg` | 400 密碼至少要 8 個字元 |

### Requirement: Sign in

`POST /api/accounts/login` SHALL respond 200 with an access token when the username and password match an account. A wrong password and an unknown username SHALL both respond 401 with the same detail 帳號或密碼錯誤, so the response does not reveal whether the account exists. The access token SHALL expire 30 days after it is issued. Endpoints other than registration, sign-in, health, `/api/content` and `/api/public/` SHALL respond 401 without a valid token.

#### Scenario: Unknown user and wrong password look the same

- **WHEN** a client signs in once with an unknown username and once with a known username and a wrong password
- **THEN** both responses are 401 with the detail 帳號或密碼錯誤

#### Scenario: Protected endpoint without a token

- **WHEN** a client requests `GET /api/characters/me` without a token
- **THEN** the API responds 401
