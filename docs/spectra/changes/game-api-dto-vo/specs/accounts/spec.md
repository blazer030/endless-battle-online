## MODIFIED Requirements

### Requirement: Sign in

`POST /api/accounts/login` SHALL respond 200 with an access token when the username and password match an account. A wrong password and an unknown username SHALL both respond 401 with the same detail 帳號或密碼錯誤, so the response does not reveal whether the account exists. The access token SHALL expire 30 days after it is issued. Endpoints other than registration, sign-in, health and `/api/public/` SHALL respond 401 without a valid token.

#### Scenario: Unknown user and wrong password look the same

- **WHEN** a client signs in once with an unknown username and once with a known username and a wrong password
- **THEN** both responses are 401 with the detail 帳號或密碼錯誤

#### Scenario: Protected endpoint without a token

- **WHEN** a client requests `GET /api/characters/me` without a token
- **THEN** the API responds 401
