## MODIFIED Requirements

### Requirement: Exchange refusals

An exchange SHALL be refused with reasons and SHALL change nothing. An unknown exchange id SHALL be refused with `HONOUR_UNKNOWN` alone. Otherwise the reasons SHALL be listed in this order: `MEDALS_MISSING` when any medal is short, `MERCENARY_HIRED` when the reward is a mercenary and a contract already exists, `BAG_FULL` when the item reward does not fit in its category of the bag after the medals are paid.

#### Scenario: Short of medals and already hired

- **WHEN** a character with a contract and 5 continent medals exchanges honour-1
- **THEN** the exchange is refused with `MEDALS_MISSING`, `MERCENARY_HIRED` and the bag is unchanged

##### Example: refusals

| Situation | Reasons |
| --- | --- |
| exchange id `honour-999` | `HONOUR_UNKNOWN` |
| honour-10 with 4 continent medals | `MEDALS_MISSING` |
| honour-1 with enough medals and a contract | `MERCENARY_HIRED` |
| honour-1 with 5 continent medals and a contract | `MEDALS_MISSING`, `MERCENARY_HIRED` |
| honour-9 with enough medals and the weapon bag full | `BAG_FULL` |
