## Purpose

The honour hall lets a character trade the medals in their bag for equipment, consumables, keys, materials or an honour-only mercenary. This capability covers the exchange rules and the honour API, including the details the honour screen shows for each exchange.

## ADDED Requirements

### Requirement: Honour listing

`GET /api/honour` SHALL return every medal item in content order with the quantity in the character's bag, and every exchange in content order with its reward (kind `item` or `mercenary`, reward id and name, quantity), its cost (each medal with the quantity needed and the quantity owned) and `affordable`, which SHALL be true only when every medal in the cost is owned in at least the needed quantity. Medals SHALL be counted from the bag only, not from equipment or the depot.

#### Scenario: Listing with medals in the bag

- **WHEN** a character with 14 continent medals and 6 kingdom medals requests the honour listing
- **THEN** the medals list the five medals in content order with owned 14, 6, 0, 0, 0, and each exchange reports its cost with the owned quantities

##### Example: affordability

| Exchange | Cost | Owned | Affordable |
| --- | --- | --- | --- |
| honour-9 (巨石槌) | continent 2 | continent 14 | true |
| honour-12 | continent 12 | continent 14 | true |
| honour-1 (C級傭兵) | continent 13, kingdom 13 | continent 14, kingdom 6 | false |
| honour-142 (劣質打孔石 ×3) | one of each medal | continent 14, kingdom 6, others 0 | false |

### Requirement: Exchanging medals for a reward

`POST /api/honour/exchange` with an exchange id SHALL take the cost from the bag in the same order as mission delivery (unenhanced rows first, then fewer to more, then lower id), then add the reward: an item reward SHALL go into the bag in the reward's quantity, and a mercenary reward SHALL start a new contract that is resting. The character's version SHALL be written first so a concurrent change conflicts. The response SHALL be the honour listing after the exchange.

#### Scenario: Exchanging for an item

- **WHEN** a character with 14 continent medals exchanges honour-9
- **THEN** the bag has 12 continent medals and one 巨石槌, and the returned listing shows continent owned 12

#### Scenario: Exchanging for a mercenary

- **WHEN** a character without a contract and with 13 continent and 13 kingdom medals exchanges honour-1
- **THEN** both medals are used up and the character has a resting contract for C級傭兵

### Requirement: Exchange refusals

An exchange SHALL be refused with reasons and SHALL change nothing. An unknown exchange id SHALL be refused with `honourUnknown` alone. Otherwise the reasons SHALL be listed in this order: `medalsMissing` when any medal is short, `mercenaryHired` when the reward is a mercenary and a contract already exists, `bagFull` when the item reward does not fit in its category of the bag after the medals are paid.

#### Scenario: Short of medals and already hired

- **WHEN** a character with a contract and 5 continent medals exchanges honour-1
- **THEN** the exchange is refused with `medalsMissing`, `mercenaryHired` and the bag is unchanged

##### Example: refusals

| Situation | Reasons |
| --- | --- |
| exchange id `honour-999` | `honourUnknown` |
| honour-10 with 4 continent medals | `medalsMissing` |
| honour-1 with enough medals and a contract | `mercenaryHired` |
| honour-1 with 5 continent medals and a contract | `medalsMissing`, `mercenaryHired` |
| honour-9 with enough medals and the weapon bag full | `bagFull` |

### Requirement: Exchange details in the listing

Each exchange in the honour listing SHALL carry exactly one of `item` and `mercenary` according to its kind. `item` SHALL give the reward item's category, consumable kind, stat bonus, equipment requirements, holes, refine maximum, bound flag, supply HP and SP, amount, the elixir (kind, multiplier, seconds) when it is an elixir, and the key (place name, seconds) when it is a key. Equipment requirements SHALL list only stats with a minimum above zero, each with `stat` (`strength`, `intelligence`, `agility` or `charisma`), `minimum` and `met`, where `met` SHALL compare the character's base stats without equipment bonuses, as when equipping. `mercenary` SHALL give the catalogue's fights, money, drop and experience percents and death cost. The listing SHALL also give `hiredMercenaryName`, the current contract's name, or null without a contract.

#### Scenario: Weapon requirement not met

- **WHEN** a character with base strength 300 requests the honour listing
- **THEN** honour-11 (刺心者) has category `Weapon`, holes 2, refine maximum 4, bound true, and requirements `[strength 435 met false]`

##### Example: details

| Exchange | Detail |
| --- | --- |
| honour-9 (巨石槌) | Weapon; bonus strength 5, intelligence 13, vitality 14, charisma 15, faith 16, attack 225; requirement strength 145; holes 2; refine maximum 5; bound |
| honour-133 (90000HP回復劑 ×10) | Consumable, consumable kind Supply, supply HP 90000, no requirements |
| honour-29 (靜止鑰匙) | Key; key place 靜止之城, 86400 seconds; bound |
| honour-1 (C級傭兵) | mercenary: fights 900, money 70, drop 70, experience 100, death cost 2 |

#### Scenario: Hired mercenary name

- **WHEN** a character whose contract was renamed 葛雷 requests the honour listing
- **THEN** `hiredMercenaryName` is `葛雷`; without a contract it is null
