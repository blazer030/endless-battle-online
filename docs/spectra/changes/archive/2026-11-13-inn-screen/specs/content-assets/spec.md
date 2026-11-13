## MODIFIED Requirements

### Requirement: Content image categories and output specifications

The import tool SHALL convert a source image into a WebP whose canvas, composition, transparency and byte limit follow its category:

| Category | Key | Canvas | Composition | Alpha | Quality | Max bytes |
|---|---|---|---|---|---|---|
| portraits | `<job id>-male` or `<job id>-female` | 800×1200 | see "Portrait alignment" | yes | 80 | 163840 |
| job-icons | `<job id>` | 96×96 | trimmed artwork, long side 88% of canvas, centered | yes | 85 | 12288 |
| skills | `<skill id>` | 96×96 | trimmed artwork, long side 88% of canvas, centered | yes | 85 | 12288 |
| items | `<item id>` | 160×160 | trimmed artwork, long side 88% of canvas, centered | yes | 85 | 20480 |
| monsters | `<monster id>` | 540×540 | trimmed artwork scaled to fit 513 wide by 486 tall, horizontally centered, lowest opaque row at y=529 | yes | 80 | 61440 |
| battlefields | `<place id>` | 1024×1024 | scaled to cover, center-cropped | no | 75 | 153600 |
| room-badges | `<inn room id>` | 128×128 | trimmed artwork, long side 88% of canvas, centered | yes | 85 | 16384 |
| room-backgrounds | `<inn room id>` | 800×450 | scaled to cover, center-cropped | no | 75 | 61440 |

A pixel SHALL count as opaque when its alpha is greater than 24. Keys SHALL match `^[a-z0-9-]+$` and SHALL be one of the category's expected keys derived from `backend/content` (see "Missing image report"); with `--dry-run` an unknown key SHALL only print a warning. When the encoded file exceeds the limit, the tool SHALL retry with quality lowered by 5 each time and SHALL reject the image when quality 60 still exceeds the limit. When the source must be enlarged to reach the composition size, the tool SHALL still produce the output and SHALL print a warning.

#### Scenario: Item icon is centered on a square canvas

- **WHEN** an item source image whose opaque area is 152×546 is imported into `items`
- **THEN** the output is 160×160 with transparency, the opaque area's long side is 141 pixels (88% of 160, rounded) and the opaque area is centered

#### Scenario: Room badge is centered on a square canvas

- **WHEN** a room badge source image whose opaque area is 273×272 is imported into `room-badges`
- **THEN** the output is 128×128 with transparency and the opaque area's long side is 113 pixels (88% of 128, rounded), centered

#### Scenario: Room background covers its canvas

- **WHEN** a 1280×720 room background is imported into `room-backgrounds`
- **THEN** the output is 800×450 without transparency, scaled to cover and center-cropped

#### Scenario: Oversized output is rejected

- **WHEN** a battlefield image still exceeds 153600 bytes at quality 60
- **THEN** the tool exits with a non-zero code, prints the reason, uploads nothing and leaves the manifest unchanged

#### Scenario: Unknown category or invalid key

- **WHEN** the category is not one of the eight categories or the key contains characters outside `a-z`, `0-9` and `-`
- **THEN** the tool exits with a non-zero code and leaves the manifest unchanged

#### Scenario: Key not defined in content

- **WHEN** `import portraits job-6 <source>` is run without `--dry-run` while `backend/content` defines job `job-6`
- **THEN** the tool exits with a non-zero code naming `portraits/job-6`, uploads nothing and leaves the manifest unchanged; with `--dry-run` it prints the same message as a warning and exits with code 0

### Requirement: Content manifest

The manifest `frontend/src/assets/content-manifest.json` SHALL contain `baseUrl` (an `https://` URL without a trailing slash) and `assets`, an object keyed by `<category>/<key>` whose entries have `file` (the R2 object key), `original` (the originals object key) and, for portraits imported with an override, `headY`. The tool SHALL write `assets` keys in ascending order. Every entry's category SHALL be one of the eight categories and its `file` SHALL match `<category>/<key>.<12 hex>.webp`.

#### Scenario: Manifest format check

- **WHEN** the frontend tests run against the committed manifest
- **THEN** they fail if any entry has an unknown category, a `file` not matching its key, or a `baseUrl` that is not `https://`

#### Scenario: Room categories are accepted

- **WHEN** the manifest contains `room-badges/apprentice-loft` with file `room-badges/apprentice-loft.0a1b2c3d4e5f.webp`
- **THEN** the format check accepts it

### Requirement: Missing image report

The import tool SHALL provide a `missing [<category>]` command that derives the expected keys from `backend/content` (portraits: every job id with `-male` and `-female`; job-icons: job ids; skills: skill ids; items: item ids; monsters: monster ids; battlefields: place ids; room-badges and room-backgrounds: the room ids in `inn.json`), compares them with the manifest, and prints per category the count with images, the expected count, the missing keys and any manifest keys the content does not define. It SHALL exit with code 0.

#### Scenario: Report counts

- **WHEN** the manifest holds 8 portraits and `backend/content/jobs.json` defines 12 jobs
- **THEN** the report shows portraits as 8 of 24 and lists the 16 missing keys

#### Scenario: Room keys come from the inn

- **WHEN** `backend/content/inn.json` defines 6 rooms and the manifest holds no room images
- **THEN** the report shows room-badges and room-backgrounds each as 0 of 6 and lists the 6 room ids

#### Scenario: Manifest key not in content

- **WHEN** the manifest holds `monsters/monster-l` and `backend/content/monsters.json` has no monster with that id
- **THEN** the monsters line of the report is followed by a line listing `monster-l` as extra
