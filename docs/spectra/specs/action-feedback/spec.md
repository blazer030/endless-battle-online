# action-feedback Specification

## Purpose

Action feedback is the shared way a screen shows that a player action is waiting for the server and that it failed. It gives every new screen the same waiting ink dots and the same failure popup, so feedback does not depend on an NPC being on screen.

## Requirements

### Requirement: Waiting ink dots while an action is sent

While a player action's request is in flight, the screen SHALL cover the stage with a transparent layer that blocks every click and tap, so the action cannot be sent twice and nothing else on the screen can be pressed. When the request has not settled after 0.45 seconds, the ink dots (the same three dots as the title screen's loading, on a small dark plate at the centre of the stage) SHALL fade in over 0.2 seconds; when the request settles earlier, the ink dots SHALL NOT appear. Once the ink dots have appeared they SHALL stay for at least 300 milliseconds before fading out over 0.15 seconds. The blocking layer SHALL be removed when the request settles and the ink dots, if shown, have faded out. The button the player pressed SHALL keep its text while waiting.

#### Scenario: A quick response shows no ink dots

- **WHEN** an action's request settles 0.2 seconds after it was sent
- **THEN** the ink dots never appear and the blocking layer is removed when the request settles

#### Scenario: A slow response shows the ink dots

- **WHEN** an action's request settles 1.5 seconds after it was sent
- **THEN** the ink dots appear 0.45 seconds after sending and fade out after the request settles

#### Scenario: The ink dots stay at least the minimum time

- **WHEN** an action's request settles 0.5 seconds after it was sent
- **THEN** the ink dots, which appeared at 0.45 seconds, stay until at least 0.75 seconds before fading out

#### Scenario: Nothing can be pressed while waiting

- **WHEN** the player presses the same button again or another button while a request is in flight
- **THEN** no second request is sent and nothing else happens

---
### Requirement: Shared failure popup

When a player action fails, the screen SHALL show the failure popup: a parchment frame like the logout confirmation, a red wax seal with a white "！", one line of red text and a "確定" button. The text SHALL be `連線失敗` when the request did not reach the server or the server answered without reason codes the screen knows, and otherwise the screen's own text for the first reason code it knows. The popup SHALL fade in while scaling from 0.92 to 1 over 0.22 seconds and fade out over 0.15 seconds. Pressing "確定" or anywhere outside the popup SHALL close it, and the screen SHALL keep the state it had before the action.

#### Scenario: Connection failure

- **WHEN** an action's request fails without reaching the server
- **THEN** the failure popup shows `連線失敗`

#### Scenario: Server rejection

- **WHEN** an action's request fails with reason codes the screen knows
- **THEN** the failure popup shows the screen's text for the first known reason

#### Scenario: Closing the popup

- **WHEN** the player presses outside the failure popup
- **THEN** the popup closes and the screen is unchanged

---
### Requirement: Feedback timings live with the other motion constants

The waiting delay (0.45 seconds), the minimum time the ink dots stay (300 milliseconds), the ink dots' fade-in and fade-out (0.2 and 0.15 seconds) and the popup's fade-in and fade-out (0.22 and 0.15 seconds) SHALL be defined in `frontend/src/motion/timings.ts`, and the waiting delay SHALL equal the title screen's ink dots delay.

#### Scenario: Waiting delay matches the title screen

- **WHEN** the timing constants are read
- **THEN** the action waiting delay equals the title screen's ink dots delay
