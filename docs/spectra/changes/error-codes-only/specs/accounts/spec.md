## MODIFIED Requirements

### Requirement: Account registration

`POST /api/accounts/register` SHALL create an account when the username is 3 to 32 characters of letters, digits or underscores and the password has at least 8 characters, and SHALL respond 201. Usernames SHALL be unique ignoring letter case. An invalid username SHALL be rejected with 400 and the reason `USERNAME_INVALID`; a short password with 400 and `PASSWORD_TOO_SHORT`; a username already taken with 409 and `USERNAME_TAKEN`. Registration SHALL NOT sign the player in.

#### Scenario: Valid registration

- **WHEN** a client registers username `ethan_01` with password `abcdefgh`
- **THEN** the API responds 201 and the account can sign in with those credentials

#### Scenario: Username taken in another letter case

- **WHEN** `Ethan` exists and a client registers `ethan`
- **THEN** the API responds 409 with the reason `USERNAME_TAKEN`

##### Example: registration input

| Username | Password | Response |
| --- | --- | --- |
| `ab` | `abcdefgh` | 400 `USERNAME_INVALID` |
| `abc` | `abcdefgh` | 201 |
| `has space` | `abcdefgh` | 400 `USERNAME_INVALID` |
| 33 letters | `abcdefgh` | 400 `USERNAME_INVALID` |
| `abc` | `abcdefg` | 400 `PASSWORD_TOO_SHORT` |

### Requirement: Sign in

`POST /api/accounts/login` SHALL respond 200 with an access token when the username and password match an account. A wrong password and an unknown username SHALL both respond 401 with the same reason `AUTHENTICATION_FAILED`, so the response does not reveal whether the account exists. The access token SHALL expire 30 days after it is issued. Endpoints other than registration, sign-in, health and `/api/public/` SHALL respond 401 with the reason `AUTHENTICATION_REQUIRED` without a valid token.

#### Scenario: Unknown user and wrong password look the same

- **WHEN** a client signs in once with an unknown username and once with a known username and a wrong password
- **THEN** both responses are 401 with the reason `AUTHENTICATION_FAILED`

#### Scenario: Protected endpoint without a token

- **WHEN** a client requests `GET /api/characters/me` without a token
- **THEN** the API responds 401 with the reason `AUTHENTICATION_REQUIRED`
