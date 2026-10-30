- Candy Man now receives a native SCP-330 bag with exactly three candies by default, replacing the empty-allowlist pill fallback.
- `candy_man_candy_count` supports 1-6. An empty allowlist uses registered standard non-explosive colors; explicit registered colors remain configurable.
- Clears any native seed candy before filling to avoid an accidental extra/random candy. Existing stock hints migrate on startup/reload.
- Existing explosive zombie fixes, role HUD, Locksmith and independent updater are retained.

Built against EXILED 9.15.2 / SCP:SL 14.2.8. Candy pool selection and compiled bag-fill path are regression checked; native bag consumption still requires a server playtest.
