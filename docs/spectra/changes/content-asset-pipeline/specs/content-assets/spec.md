## Purpose

Content images (items, monsters and pets, skills, job portraits, job icons, battlefields) are converted to fixed WebP specifications, stored in Cloudflare R2 under immutable content-hashed names, and referenced from a manifest kept in git. The game frontend resolves every content image through that manifest and shows a per-category placeholder when an image is missing.

## ADDED Requirements

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

A pixel SHALL count as opaque when its alpha is greater than 24. Keys SHALL match `^[a-z0-9-]+$`. When the encoded file exceeds the limit, the tool SHALL retry with quality lowered by 5 each time and SHALL reject the image when quality 60 still exceeds the limit. When the source must be enlarged to reach the composition size, the tool SHALL still produce the output and SHALL print a warning.

#### Scenario: Item icon is centered on a square canvas

- **WHEN** an item source image whose opaque area is 152×546 is imported into `items`
- **THEN** the output is 160×160 with transparency, the opaque area's long side is 141 pixels (88% of 160, rounded) and the opaque area is centered

#### Scenario: Oversized output is rejected

- **WHEN** a battlefield image still exceeds 153600 bytes at quality 60
- **THEN** the tool exits with a non-zero code, prints the reason, uploads nothing and leaves the manifest unchanged

#### Scenario: Unknown category or invalid key

- **WHEN** the category is not one of the six categories or the key contains characters outside `a-z`, `0-9` and `-`
- **THEN** the tool exits with a non-zero code and leaves the manifest unchanged

### Requirement: Portrait alignment

For `portraits`, the import tool SHALL place the figure so that the figure height is 1100 pixels, the lowest opaque row lands at y=1180 and the body axis lands at x=400 on the 800×1200 canvas. The figure top SHALL default to the topmost opaque row; when `--head-y <n>` is given, the figure top SHALL be row `n` of the source and that value SHALL be stored in the manifest entry as `headY`. The body axis SHALL be the median of the per-row midpoints of opaque pixels between 35% and 95% of the figure height measured from the figure top. The tool SHALL reject the portrait when any opaque pixel would fall outside the canvas, naming the side that overflows.

#### Scenario: Portraits share feet line and axis

- **WHEN** two portraits of different source sizes are imported without `--head-y`
- **THEN** both outputs are 800×1200, both have their lowest opaque row at y=1180 and their body axes at x=400

##### Example: figure scaling

- **GIVEN** a source whose opaque rows span y=0 to y=999 (height 1000)
- **WHEN** it is imported into `portraits`
- **THEN** it is scaled by 1.1 so its top lands at y=80 and its bottom at y=1180

#### Scenario: Head position override

- **WHEN** a portrait is imported with `--head-y 40`
- **THEN** row 40 of the source is mapped to y=80, the manifest entry records `"headY": 40`, and re-importing the same source with the same value produces the same file name

#### Scenario: Figure overflows the canvas

- **WHEN** scaling and axis alignment would put opaque pixels left of x=0
- **THEN** the tool exits with a non-zero code and reports that the left side overflows

### Requirement: Immutable content-hashed upload

The import tool SHALL name each output `<category>/<key>.<hash>.webp`, where `<hash>` is the first 12 lowercase hexadecimal characters of the SHA-256 of the WebP bytes, and SHALL upload it to the `ebo-content` bucket with `Content-Type: image/webp` and `Cache-Control: public, max-age=31536000, immutable`. It SHALL upload the source file unchanged to the `ebo-originals` bucket as `<category>/<key>.<source hash>.<source extension>`. It SHALL skip uploading an object that already exists and SHALL never overwrite or delete objects. It SHALL write the manifest only after both uploads succeed. With `--dry-run` it SHALL produce the output and preview without uploading or changing the manifest.

#### Scenario: Replacing an image

- **WHEN** a new source is imported for a key that already has a manifest entry
- **THEN** the new file is uploaded under a new hashed name, the old object stays in the bucket, and the manifest entry points to the new file

#### Scenario: Upload failure keeps the manifest

- **WHEN** the upload to `ebo-content` fails
- **THEN** the tool exits with a non-zero code and the manifest file is byte-for-byte unchanged

#### Scenario: Re-import of identical output

- **WHEN** the same source is imported again with the same options
- **THEN** no upload is performed and the manifest is unchanged

### Requirement: Upload credentials

The import tool SHALL read `R2_ENDPOINT`, `R2_ACCESS_KEY_ID` and `R2_SECRET_ACCESS_KEY` from `.env.r2` at the repository root, a file ignored by git, and SHALL NOT print their values. When the file or a field is missing and the run is not `--dry-run`, the tool SHALL exit with a non-zero code naming the missing field.

#### Scenario: Missing credentials

- **WHEN** `.env.r2` lacks `R2_SECRET_ACCESS_KEY` and an import without `--dry-run` is run
- **THEN** the tool exits with a non-zero code, names `R2_SECRET_ACCESS_KEY` as missing, and uploads nothing

#### Scenario: Dry run needs no credentials

- **WHEN** `.env.r2` does not exist and an import with `--dry-run` is run
- **THEN** the output and preview are produced and the tool exits with code 0

### Requirement: Content manifest

The manifest `frontend/src/assets/content-manifest.json` SHALL contain `baseUrl` (an `https://` URL without a trailing slash) and `assets`, an object keyed by `<category>/<key>` whose entries have `file` (the R2 object key), `original` (the originals object key) and, for portraits imported with an override, `headY`. The tool SHALL write `assets` keys in ascending order. Every entry's category SHALL be one of the six categories and its `file` SHALL match `<category>/<key>.<12 hex>.webp`.

#### Scenario: Manifest format check

- **WHEN** the frontend tests run against the committed manifest
- **THEN** they fail if any entry has an unknown category, a `file` not matching its key, or a `baseUrl` that is not `https://`

### Requirement: Frontend content image lookup

The game frontend SHALL obtain every content image through a single lookup that takes a category and a key and returns a URL and whether it is a placeholder. When the manifest has the entry, the URL SHALL be `baseUrl + "/" + file`. When the manifest lacks the entry, the URL SHALL be the bundled placeholder for that category. When an image fails to load, the frontend SHALL display the category placeholder instead.

#### Scenario: Image present

- **WHEN** the manifest contains `monsters/monster-1` with file `monsters/monster-1.3fa2c1d04e5b.webp` and base URL `https://ebo-assets.blazer030.dev`
- **THEN** the lookup for (`monsters`, `monster-1`) returns `https://ebo-assets.blazer030.dev/monsters/monster-1.3fa2c1d04e5b.webp` and not a placeholder

#### Scenario: Image missing

- **WHEN** the manifest has no entry for (`items`, `item-9999`)
- **THEN** the lookup returns the items placeholder and marks it as a placeholder

#### Scenario: Battle stage without monster art

- **WHEN** the stage shows a monster or pet whose image is not in the manifest
- **THEN** the stage displays the monsters placeholder in the creature's slot and the creature's name in its health bar

### Requirement: Missing image report

The import tool SHALL provide a `missing [<category>]` command that derives the expected keys from `backend/content` (portraits: every job id with `-male` and `-female`; job-icons: job ids; skills: skill ids; items: item ids; monsters: monster ids; battlefields: place ids), compares them with the manifest, and prints per category the count with images, the expected count and the missing keys. It SHALL exit with code 0.

#### Scenario: Report counts

- **WHEN** the manifest holds 8 portraits and `backend/content/jobs.json` defines 12 jobs
- **THEN** the report shows portraits as 8 of 24 and lists the 16 missing keys

### Requirement: Design asset import

The import tool SHALL provide a `design <group> <name> <source> [--max-width <n>]` command that writes `frontend/src/assets/<group>/<name>.webp` at quality 85, keeping transparency and without trimming, resizing proportionally when `--max-width` is smaller than the source width. Group and name SHALL match `^[a-z0-9-]+$`. The command SHALL reject outputs larger than 307200 bytes without writing the file.

#### Scenario: Design asset written

- **WHEN** `design ui paper-strip ./strip.png --max-width 600` is run with a 1200-pixel-wide source
- **THEN** `frontend/src/assets/ui/paper-strip.webp` is written 600 pixels wide

#### Scenario: Design asset too large

- **WHEN** the encoded output exceeds 307200 bytes
- **THEN** the command exits with a non-zero code and no file is written
