## Purpose

Screens built from the new design are laid out on a fixed-width stage whose height flexes within a range that matches common phone browsers, then scaled to the browser window. Inside the range the stage fills the window; in taller windows the stage stops at its tallest size, and in wider windows (such as desktops) it uses the tallest height the window allows, and the spare space shows one shared background, so every device sees the same layout without stretching or scrolling.

## ADDED Requirements

### Requirement: Stage size and scale

A new-design screen SHALL be laid out on a stage 390 px wide whose height is between 624 px and 780 px. Given the browser window's width `w` and height `h` in CSS pixels, there is no upper limit on the scale, and the scaled stage SHALL be centered in the window:

- When `h / w` is at least 1.6, the stage scale SHALL be `w / 390` and the stage height SHALL be `h / scale` clamped to 624–780. Between ratios 1.6 and 2.0 the scaled stage fills the window with no spare space; above 2.0 the spare space is above and below.
- When `h / w` is below 1.6, the side margins are unavoidable, so the stage SHALL be as tall as possible without shrinking below its base size: the stage height SHALL be `h` clamped to 624–780 and the scale SHALL be `h / stage height`. The stage fills the window height and the spare space is on the left and right; only a window shorter than 624 px shrinks the stage below scale 1.

#### Scenario: Window within the ratio range

- **WHEN** the window is 402×714 CSS px
- **THEN** the stage is scaled by 402/390 and its height flexes so the scaled stage fills the window exactly

#### Scenario: Window outside the ratio range

- **WHEN** the window is taller than 2.0 times its width
- **THEN** the stage is 780 high, scaled by `w / 390`, and centered with equal spare space above and below

#### Scenario: Window wider than the ratio range

- **WHEN** the window is shorter than 1.6 times its width, including a large desktop window
- **THEN** the stage height is the window height clamped to 624–780, the stage fills the window height, and it is centered with equal spare space on the left and right

##### Example: stage fit

| Window (w×h) | Scale | Stage height | Spare space |
| --- | --- | --- | --- |
| 390×624 | 1 | 624 | none |
| 390×780 | 1 | 780 | none |
| 402×714 | 1.0308 | 692.7 | none |
| 412×880 | 1.0564 | 780 | 28.0 above and below |
| 375×555 | 0.8894 | 624 | 14.1 left and right |
| 414×660 | 1 | 660 | 12 left and right |
| 1440×720 | 1 | 720 | 525 left and right |
| 820×1180 | 1.5128 | 780 | 115 left and right |
| 1920×950 | 1.2179 | 780 | 722.5 left and right |

### Requirement: One shared background fills spare space

The spare space around a scaled stage SHALL show one background shared by every new-design screen, defined in a single place; it SHALL be the solid color `#2A1A0C` until a static background image is chosen. A screen SHALL NOT draw outside its stage: anything a screen places beyond the stage edges SHALL be clipped, and interactive content SHALL stay inside the stage.

#### Scenario: Tall phone shows the shared background

- **WHEN** the title screen is shown in a 412×880 window
- **THEN** the 28.0 px above and below the stage show the shared background, the title screen's sky and desk end at the stage edges, and no button or field lies outside the stage

### Requirement: No scrolling at phone widths

A new-design screen SHALL NOT scroll horizontally or vertically as a whole at any window width from 360 to 430 CSS px; a list inside the stage that exceeds its area SHALL scroll within that area instead.

#### Scenario: Narrow phone

- **WHEN** a new-design screen is shown in a 360×640 window
- **THEN** the document has no horizontal or vertical overflow
