## Purpose

The adventure runs on the server whether or not a client is watching: each advance request simulates the time since the character was last settled. This capability defines how much offline time is simulated and how much of it is reported battle by battle, so a client can stop polling (for example while the player stays in the town) without losing progress.

## ADDED Requirements

### Requirement: Advance simulates the elapsed time

`POST /api/adventure/advance` SHALL simulate the character's adventure from the last settled time to the current time, starting a new battle while time remains and always finishing a battle once started, and SHALL save the resulting character and its new settled time. When no time has elapsed, it SHALL change nothing and return no events.

#### Scenario: Advance after a pause

- **WHEN** a character last settled 20 seconds ago requests an advance
- **THEN** the battles of those 20 seconds are applied to the character and the settled time moves to the end of the last started battle

#### Scenario: No time elapsed

- **WHEN** two advance requests arrive at the same moment
- **THEN** the second returns no events, an empty summary and writes nothing

### Requirement: Offline time is capped at 24 hours

When more than 86400 seconds have elapsed since the last settled time, the advance SHALL simulate only the last 86400 seconds and SHALL discard the time before them.

#### Scenario: Thirty hours offline

- **WHEN** a character last settled 108000 seconds ago requests an advance
- **THEN** only the last 86400 seconds are simulated

### Requirement: Only the last 30 seconds are reported as events

The advance response SHALL contain battle events only for battles that start within the last 30 seconds of the simulated time. The summary SHALL cover every simulated battle (fights, wins, losses, timeouts, escapes, experience, money, job experience, levels gained, skills cast, drops, potions used, mission kills, pets captured), and `eventsCoverAll` SHALL be true only when no battle was left out of the events (a skipped rest does not count).

#### Scenario: Long absence reports a summary

- **WHEN** an advance simulates 3600 seconds containing many battles
- **THEN** the events cover only battles starting in the last 30 seconds, the summary counts all battles, and `eventsCoverAll` is false

#### Scenario: Only an opening rest is skipped

- **WHEN** a defeated character advances after 40 seconds, resting first and then fighting from second 15
- **THEN** all battles are in the events and `eventsCoverAll` is true
