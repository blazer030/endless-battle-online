## Purpose

Screens built from the new design are laid out on a fixed-width stage whose height flexes within a range that matches common phone browsers, then scaled to the browser window. Inside the range the stage fills the window; outside it the stage keeps its nearest size and the spare space shows one shared background, so every device sees the same layout without stretching or scrolling.

## ADDED Requirements

### Requirement: Stage size and scale

A new-design screen SHALL be laid out on a stage 390 px wide whose height is between 624 px and 780 px. Given the browser window's width `w` and height `h` in CSS pixels, the stage scale SHALL be the smaller of `w / 390` and `h / 624`, with no upper limit, and the stage height SHALL be `h / scale` clamped to 624–780. The scaled stage SHALL be centered in the window. When the window's height-to-width ratio is between 1.6 and 2.0, the scaled stage SHALL fill the window with no spare space; otherwise it SHALL fill the tighter dimension and leave spare space only on the two opposite sides of the other.

#### Scenario: Window within the ratio range

- **WHEN** the window is 402×714 CSS px
- **THEN** the stage is scaled by 402/390 and its height flexes so the scaled stage fills the window exactly

#### Scenario: Window outside the ratio range

- **WHEN** the window is taller than 2.0 times its width or shorter than 1.6 times its width, including a large desktop window
- **THEN** the stage keeps the nearest height in 624–780, is scaled to fill the tighter dimension, and is centered with equal spare space on the two opposite sides of the other dimension

##### Example: stage fit

| Window (w×h) | Scale | Stage height | Spare space |
| --- | --- | --- | --- |
| 390×624 | 1 | 624 | none |
| 390×780 | 1 | 780 | none |
| 402×714 | 1.0308 | 692.7 | none |
| 412×880 | 1.0564 | 780 | 28.0 above and below |
| 375×555 | 0.8894 | 624 | 14.1 left and right |
| 1920×950 | 1.5224 | 624 | 663.1 left and right |

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
