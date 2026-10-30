- Fix Z04 explosion damage initialization: use valid native armor penetration (50 by default), zero force and native HE cover masks. Previously zero penetration skipped native damage setup.
- Adds configurable `explosive_zombie.armor_penetration` (1-100). Existing death triggers, warning, damage/radius and limits are retained.
- Side role HUD, Locksmith, commands and independent updater remain included.

Built against EXILED 9.15.2 / SCP:SL 14.2.8. Native constructor and compiled damage bridges are covered by regression checks; live gameplay still requires a server playtest.
