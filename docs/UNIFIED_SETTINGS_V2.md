# Unified Settings v2 — Stable/Beta (2026-08-31)

## Scope and architecture

The existing Loader, detection, assembly association and Godot SDK architecture are unchanged. One package still contains `lib/beta/MCEnchantingTable.Content.dll`, `lib/release/MCEnchantingTable.Content.dll` and one shared PCK. Stable is the existing `STS2_RELEASE` build target; it is not a third variant.

`GameVersionCompatibility` exposes the variant already selected by Loader. An unknown build family disables new enchant entrances rather than guessing APIs. The audited common entrance APIs use one `EnchantEntranceAdapter`; separate near-identical Stable/Beta adapter implementations are deliberately unnecessary. Existing RNG differences remain in `RngCompat`.

The BaseLib 3.4.5 package is common to both builds. Only this installed API has been audited; compatibility with arbitrary future BaseLib releases is not claimed.

## API audit

| Area | Stable | Beta | Integration |
|---|---|---|---|
| Settings | BaseLib 3.4.5 `SimpleModConfig`, `ModConfigRegistry.Register` | Same package | Existing registration/page retained |
| Storage | `ModConfig` converts static properties through TypeConverter strings, writes an atomic `.cfg.new` replacement | Same | One `GameplayJson` string property |
| Rest | `RestSiteOption.Generate(Player)`, `Icon`, `OnSelect`, `Hook.ModifyRestSiteOptions` | Same signatures; adds test-only `generateForTests` | Existing postfix; common entrance adapter |
| Ancient | `NEventLayout.SetEvent`, `NAncientEventLayout` dialogue methods | Same hook signatures; hotkey icon changes from TextureRect to NHotkeyIcon | Existing local-player patch; no dependency on changed hotkey child |
| Heal | `CreatureCmd.Heal(creature, decimal)` | Same used signature | Independent heal after successful enchant, no Rest hooks |
| Enchant | `EnchantmentModel.CanEnchant`, `CardCmd.Enchant`, `CardModel.Enchantment` | Same used signatures | Native application and MC safety filter retained |
| Save | `EnchantmentModel` serialization retains Amount; relic SavedProperty remains instance-owned | Same relevant contract | No rewrite of existing enchantments or forced SaveRun |
| RNG | `Rng(uint, int)`; uint seed | `Rng(ulong)`; ulong seed | Existing RngCompat folding/native constructors retained |
| UI/localization | BaseLib raw MegaRichText labels, Godot controls, LocString, NGenericPopup/NModalContainer | Same referenced signatures | Existing settings scroll surface; shared eng/zhs resources |

Original sources: sibling `StS2_Decompiled_Release/src/Core` and `StS2_Decompiled/src/Core`. BaseLib signatures were checked against the installed 3.4.5 DLL XML and its read-only decompilation in the system temp directory.

## Schema and defaults

The complete canonical schema/default document is `config/MCEnchantingConfig.json`. It is exported at `res://config/MCEnchantingConfig.json` and embedded from that SAME file as `MCEnchantingTable.GameplayDefaults.json`; no hand-maintained second default table exists.

`DefaultSettingsFactory.CreateDefaultConfig()` returns a fresh instance. Reset, migration and validation fallbacks all use it.

```text
schemaVersion: 2
bookGain
  act1NormalCombatsPerReward, act2NormalCombatsPerReward, act3NormalCombatsPerReward
  normalCombatBookRewardAmount, eliteBookRewardAmount, bossBookRewardAmount
  remainderCompensation
candidateGeneration
  bookCountBands[]
    minBookCount, maxBookCount (null for unbounded)
    slots[1..3].levelWeights { I, II, III, IV }
  repeatWeights { firstOccurrence, secondOccurrence, thirdAndLaterOccurrence }
enchantments[22]
  id, baseWeight, iconPath
  levels { I, II, III, IV }
    enabled: bool
    amount: integer
campfire { enabled, healPercent }
ancient { enabled, healPercent }
```

Defaults preserve the previous published JSON, including SHARP 1/3/5/7 (the requested 3/5/7/10 was an example, not a default change), GOOPY I only/Amount 1, SPIRAL III only/Amount 1. Disabled levels have retained editable amounts; all four false disables an enchantment. `maxMCLevel` is no longer a source of eligibility.

Bands: 0–4: I; 5–9: I / (50% I,50% II); 10–14: I / (70% II,30% III) / (60% II,40% III); 15+: (30% I,70% II) / (50% II,50% III) / (70% III,30% IV). Repeat weights 1/0.5/0.1; duplicate (ID,level) remains forbidden.

Campfire defaults: enabled, heal 10%. Ancient defaults: enabled, heal 0% because the preceding code did not heal at Ancient. Healing is `MaxHp * percent / 100m` through CreatureCmd.Heal, after successful application/commit and before success feedback.

## Persistence and migration

The original BaseLib file remains `user://mod_configs/MCEnchantingTable.cfg` (user:// resolved by OS.GetUserDataDir). BaseLib stores a JSON dictionary of strings, so the v2 document lives in its `GameplayJson` property. This is one settings system, not a second persistence store.

At first load without a unified value, import the packaged config (including a v1 document if present), overlay the six legacy book-setting properties, validate and save. Legacy property aliases are removed from BaseLib's serialization property list after the initial load; they are not continually emitted as an alternative config. Subsequent startup uses the saved unified document.

v1 migration maps each `availableLevels` entry to explicit enabled=true; absent levels become false. Existing `amountByLevel` values are retained. Missing fields obtain factory defaults. Original card SerializableEnchantment.Amount is never modified by this migration.

Validation: reward counts 0–20; normal-combat threshold 1–20; Amount 1–999; heal 0–100%; nonnegative finite weights up to 1000; 1–3 slots; continuous bands beginning at 0 and ending unbounded; known enchantment IDs. Bad scalar fields fall back individually. Invalid band topology restores only bands. Positive slot probabilities are normalized; all-zero/invalid slot probabilities restore that slot. Zero repeat/base weights are allowed and never passed as an all-zero pool to WeightedNextItem. An entirely disabled enchantment pool is valid.

## UsesAmount audit (both snapshots)

| ID | UsesAmount | Semantics / difference |
|---|---|---|
| ADROIT | yes | Amount block |
| CLONE | no | Native rest-site clone mechanism; existing MC compatibility retained |
| CORRUPTED | no | Fixed multiplier/HP cost; Beta passes CardPlay to Damage |
| GLAM | no | Fixed replay |
| GOOPY | yes | Initial extra block = Amount − 1; each play increments Amount by 1 |
| IMBUED | no | Fixed autoplay mechanism |
| INKY | no | Fixed Weak; Stable also has fixed powered-attack damage +1, removed in Beta |
| INSTINCT | no | Fixed multiplier |
| MOMENTUM | yes | Amount damage growth |
| NIMBLE | yes | Amount block modifier |
| PERFECT_FIT | no | Fixed deck-placement mechanism |
| ROYALLY_APPROVED | no | Fixed keywords |
| SHARP | yes | Amount powered-attack damage |
| SLITHER | no | Fixed random-cost range |
| SLUMBERING_ESSENCE | no | Fixed cost decrement |
| SOULS_POWER | no | Removes local Exhaust |
| SOWN | yes | Amount energy; Beta marks Disabled before awaiting gain |
| SPIRAL | no | Fixed Replay 1 |
| STEADY | no | Fixed Retain |
| SWIFT | yes | Amount draw; Beta marks Disabled before awaiting draw |
| TEZCATARAS_EMBER | no | Fixed cost/damage/keyword changes |
| VIGOROUS | yes | Amount first-play damage bonus |

The compatibility metadata controls visibility of Amount editors and native Amount=1 for fixed effects. Model effects remain native. INKY still goes through MCEnchantCompatibility; settings cannot bypass its enemy-target safety rule.

## Settings UI and refresh

`GameplaySettings.SetupConfigUI` extends the original BaseLib page using `UnifiedSettingsUi`: Book Gain, Candidate Generation, Enchantments, Campfire, Ancient, Reset. BaseLib's existing scroll surface hosts the 22 entries. Labels use BaseLib MegaRichText controls; all wording comes from shared eng/zhs settings_ui localization or canonical enchantment title keys. Numeric edits use bounded SpinBox controls; each tier has its own checkbox, with disabled tiers retaining but disabling Amount editing.

Reset uses native NGenericPopup/NModalContainer with localized confirmation. It writes immediately through BaseLib.Save and rebuilds only this settings content. Configuration saves are not Run saves.

Runtime candidate model resolution is revision-cached instead of permanently Lazy. Future new candidates use the current config; already cached candidates and already enchanted cards retain their values. Enabled=false prevents NEW entrance creation and blocks an existing entrance's submission check; the current room is not forcibly rebuilt. Single-player future book rewards read current settings. Multiplayer book rules remain a host-supplied, per-relic saved snapshot; remainder compensation was added to that snapshot and its existing lobby payload. Mixed old/new Mod versions must not share a lobby.

## Determinism and multiplayer boundary

No changes to EnchantSession seed composition, DeckIndex cache, or RngCompat. UI has no gameplay RNG calls. Sound/VFX retains its independent chaotic RNG. Same host version + game seed + gameplay config + player slot + encounter/card identity gives repeatable generation; no promise is made that different native Stable/Beta RNG algorithms produce byte-identical rolls.

`SerializeGameplaySettings()` produces key-ordered JSON. `GetGameplayConfigFingerprint()` hashes canonical gameplay data with SHA-256, excluding art paths; no .NET GetHashCode. The configuration and model entries have deterministic ordering.

Full v2 Host-authoritative configuration negotiation is NOT implemented here. Interfaces permit future transfer/comparison; currently peers must use identical complete settings. The previous multiplayer Rest Screen/Ancient result-sync defects are outside this stage and remain unresolved. No claim of multiplayer release readiness is made.

## Validation record

- Pure configuration harness: 90 assertions passed (22 v1 migrations, defaults, isolated fallbacks, all-off, added tier probabilities, zero weights, stable hash).
- Default fingerprint: `6F80066461664AA2DEB360B086661B8E3DA7FDDD0CE40A1F38465340E31F89FE`.
- Native RNG isolated probe: 1,000 draws repeated identically per version. Stable and Beta sequences differ, as expected from native RNGs.
- Debug build of both Content variants: 0 warnings, 0 errors.
- ExportRelease/package verification: see completion report in DEV_NOTES.
- Real-game tests are pending, not inferred from compilation.

## Required real-game matrix (run separately on Stable and Beta)

1. Start with defaults; inspect all six settings sections and 22 localized titles.
2. Change normal/elite/boss drops and remainder; only future rewards change.
3. Toggle SHARP tiers and change Amount; new cards change, existing enchantments retain saved Amount.
4. GOOPY Amount 1 gives initial +0 block; changed Amount N gives initial +(N−1).
5. SPIRAL has tier checkboxes but no Amount fields; INKY/CLONE retain existing compatibility.
6. Disable Campfire/Ancient before entering a new room: no corresponding new button.
7. Change both heal percentages; only successful enchant heals, cancel does not; no Rest smoke/sound.
8. Reset → cancel leaves settings; Reset → confirm restores all defaults and survives restart.
9. Save/Continue preserves existing card Amount; immediate SL semantics remain original-room semantics.
10. Same seed/config/version, same concrete card/encounter: candidate result repeats after SL. Settings changes do not invalidate already generated cache entries.

No interactive game process was driven by this validation harness.
