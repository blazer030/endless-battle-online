## MODIFIED Requirements

### Requirement: Public data prefix

Endpoints that serve game data readable without signing in SHALL live under `/api/public/` and SHALL respond without requiring authentication. A request carrying an expired or invalid access token SHALL receive the same public response as a request without a token. Screens SHALL read sex, element, birthplace and job data from these endpoints.

#### Scenario: Reading public data without signing in

- **WHEN** a client requests `GET /api/public/jobs` or `GET /api/public/settings` without an `Authorization` header
- **THEN** the API responds 200 with the data

#### Scenario: Expired token does not block public data

- **WHEN** a client requests `GET /api/public/settings` with an expired access token
- **THEN** the API responds 200 with the same body as a request without a token
