# Flower Shop Idle — Claude Code Context

## Project
Unity 6 WebGL idle game, single scene, interview portfolio piece. Clarity and consistency over complexity.

---

## Architecture (strict — do not deviate)

**GameManager GameObject:** All managers attach directly to it — never on child objects.

**Awake:** `Services.Register(this)` → internal init → `SetActive(false)` (panels only, last line)
**Start:** `PanelManager.Register(this)` (panels only) → `AddListener` for buttons → EventBus subscribe (if starts active)

**EventBus subscriptions:**
- Starts active → `OnEnable` / `OnDisable`
- Starts inactive (panel) → `Awake` / `OnDestroy`

**ServiceLocator:** All cross-system refs via `Services.Get<T>()` / `Services.TryGet<T>()`. Register in Awake, access in Start+. Always use `TryGet` in Save/Load paths — not all services may be present.
**Panels:** Never `SetActive` from game logic — always go through `PanelManager`.
**Buttons:** Never wire in inspector onClick — always `AddListener` in code.

**ShopOrderSlot structure:**
```
ShopOrderSlot (root) → EmptyState (sibling) | ActiveState (sibling, order content as children)
```

**Shop grid layout:** OrdersGrid child of ShopPanel has a `GridLayoutGroup` added in the scene inspector (VLG was deleted). `ShopPanel.ConfigureOrdersGrid()` (called from `Start()`) configures it — 2 columns, 400×260 cells, both tunable via the `gridCellSize` / `gridColumns` inspector fields on ShopPanel. No ContentSizeFitter on OrdersGrid — it stays fill-anchored to the panel.

**ShopOrderUI card compaction:** `CompactCardLayout()` runs in `Awake()`. Critical: it sets `childControlHeight = true` and `childForceExpandHeight = false` on ActiveState's VerticalLayoutGroup — without this the VLG uses prefab `sizeDelta` values (~96px per section) and ignores font size entirely. It also adds `LayoutElement.preferredHeight` to each section (name 24px, reward 20px, requirements 100px, timer 18px). Font sizes reduced from 36px → 13–18px. Prefab unchanged on disk.

**Prefabs:** If change isn't propagating, delete instance and re-instantiate — don't force-apply.

---

## Code Style
- Complete copy-pasteable files (not snippets) unless explicitly asked for a diff
- Comments explain intent per logical section — skip obvious lines
- No magic numbers — serialized fields or named constants
- Clear names over brevity

---

## Systems Reference

| System | Class | Notes |
|---|---|---|
| Service locator | `Services` | |
| Event system | `EventBus` | Prefer events over polling |
| Panel routing | `PanelManager` | |
| Core loop | `GardenManager` | Plot state, grow/harvest, flower unlocks |
| Orders | `ShopManager` | Order generation, fulfillment, +2 renown/fill |
| Supply/demand | `MarketManager` | Dynamic pricing |
| Inventory | `InventoryManager` | |
| Upgrades | `UpgradeManager` | Tree + prerequisites |
| Automation | `AutoPlantManager` | Per-plot preferred flower |
| Feedback | `GameJuice` | `PlayUnlock()` `PlayError()` `PunchScale(Transform)` `SetSfxMuted(bool)` |
| Persistence | `SaveSystem` | Save/load + offline progress |
| Audio | `AudioManager` | BGM only — auto-advances tracks. SFX via AudioSource directly |
| Boosts | `BoostManager` | Sunshine boost + instant bloom (gems) |
| Tutorial | `TutorialManager` | See Tutorial System section |
| Pests | `PestManager` + `Pest` + `SprayBottle` | See Pest System section |
| Achievements | `AchievementManager` | 16 milestones, Renown rewards, must be on GameManager object |
| Panel animation | `PanelTransition` | fade+scale OnEnable, added programmatically in Awake |
| Particles | `UIBurst` | Static `Emit(RectTransform, Color, count)` — Screen Space Overlay sort 90 |
| Achievement UI | `AchievementToast` | Static `Show(title, detail, renown)` — non-blocking banner |
| SFX mute | `SfxMuteToggle` | Mirrors MuteToggle, persists to PlayerPrefs "SFX_Muted" |

---

## Currencies & Phases

- `CurrencyType`: Petals, Coins, Renown, Gems
- `GamePhase`: Patch → Garden (50p) → Shop (500p) → Business (1000c)
- Phase unlock Renown awards: Garden +10, Shop +50, Business +100
- Renown: hidden in HUD until balance > 0. Earned via order fills (+2 each) and phase unlocks.
- Gems: IAP only + optional rewarded ad (+3 gems per ad, built in StorePanel)

---

## Flower Unlock System

- `GardenManager.IsFlowerUnlocked(flower)` / `TryUnlockFlower(flower)` — checks/spends petals
- `FlowerSelectPanel` shows locked flowers greyed with cost; tap to unlock in-place
- `unlockCost < 0` = free from start (Daisy, Bluebell). Otherwise costs petals:
  - Rose: 75p | Sunflower: 250p | Orchid: 500p | Lily: 1500p
- Orchid + Lily also require Shop phase (`requiredPhase: 2`) to appear in panel
- Saved in `SaveData.unlockedFlowerNames` (List<string>)
- Fires `FlowerTypeUnlockedEvent` on unlock (used by AchievementManager)

---

## Save System — SaveData Fields

```
petals, coins, renown, gems, phase, lastSaveTime,
plots (List<PlotSaveData>), upgrades, inventory, marketDemand,
boostTimeRemaining, unlockedPlotIndices (List<int>), unlockedFlowerNames (List<string>),
completedAchievements (List<int>), totalHarvests, totalOrdersFilled, totalPetalsEarned
```

Plot unlock state is now in JSON (unlockedPlotIndices). Legacy PlayerPrefs key `"UnlockedPlots"` is migrated on first load then deleted.

---

## Events (ALL in GameEvents.cs — ShopEvents.cs deleted)

```
FlowerPlantedEvent, FlowerBloomedEvent, FlowerHarvestedEvent, FlowerTypeUnlockedEvent,
CurrencyChangedEvent, UpgradePurchasedEvent, PhaseUnlockedEvent, PlotSelectedEvent,
InventoryChangedEvent, OrderSpawnedEvent, OrderFilledEvent, OrderExpiredEvent,
MarketUpdatedEvent, PestEventStartedEvent, PestEventEndedEvent, PestReachedPlotEvent,
PestRepeledEvent, SpritesInitializedEvent
```

---

## Known Gotchas

**FlowerBed.Harvest:** Uses `Services.TryGet<CurrencyManager>()` — NOT the cached `currency` field. Field is null when `AutoHarvest` is called from `SaveSystem.Load()` before `FlowerBed.Start()` runs.

**NotificationBadge:** Has `started` bool — `OnEnable` only calls `Refresh()` after `Start()` has run. Event handlers are guarded by `badgeType` — only relevant events trigger `Refresh()`.

**PhaseHUDLabel:** Reads phase thresholds from `GameManager` public properties (`GardenUnlockPetals` etc.) — do NOT hardcode 50/500/1000.

**InventoryDisplay sprites:** `FlowerSpriteInitializer` generates sprites in `Start()` but `InventoryDisplay.OnEnable()` fires first. Fixed via `SpritesInitializedEvent` — display re-refreshes once sprites are ready.

**AchievementManager:** Must be added as a component to the GameManager object in the Unity scene. Missing component = silent no-op (SaveSystem uses TryGet).

**Renown:** Now earnable — order fills (+2), phase unlocks (+10/50/100), achievement milestones (5–50 each).

**minShopLevel on OrderData:** Maps to `GamePhase` int (Patch=0, Garden=1, Shop=2, Business=3). Use 3 to gate behind Business phase.

**NotificationBadge.CountFillableOrders:** Guard `if (shop.Slots == null) return 0` — Slots is null before ShopManager.Start() (fired during SaveSystem.Load → CurrencyChangedEvent).

**FlowerBedTint:** Event-driven (OnEnable/OnDisable), no Update poll. Applies tint immediately on OnEnable.

**WateringCan / SprayBottle:** Raycast lists are cached readonly fields — call `.Clear()` before each use, never `new List<>()` per frame.

**GardenManager fresh-start detection:** Checks `!save.HasSave()` (not the old PlayerPrefs key) to decide whether to auto-plant first flower.

**Business phase:** Popup says "coming in a future update" — no staff system exists.

---

## Tutorial System

Two modes, run sequentially:

**Sequential steps** (action-gated onboarding):
- Defined in `TutorialManager.SequentialSteps` — static `List<TutorialStep>`
- Each step shows hint + subscribes to a completion event. Hint stays until action done.
- `AlreadyComplete` func per step — skips if condition already met at step start (e.g. auto-plant)
- Dismiss collapses panel; `stepReminderDelay` re-shows hint if step still incomplete
- Saves/resumes `currentStepIndex` in PlayerPrefs

**Contextual hints** (fire-and-forget, post-onboarding):
- Only fire after sequential tutorial complete
- Each shown once (tracked in `shownHints` HashSet), queued, auto-hide timer

---

## Pest System

`PestManager`, `Pest`, `PestWarningIndicator`, `SprayBottle`. Enabled by default (`featureEnabled = true`).

**SprayBottle:** Drag over pest icon to repel — costs `sprayPerRepel` (default 25) per pest, refills at `sprayRefillRate` (5/s). Always available. Has two `AudioSource` serialized fields: `spraySound` and `emptySound` — wire in inspector.

**Pest prefab gotcha:** Image alpha must not be 0 — `Pest.Initialize()` forces `icon.color = Color.white`.

**Key detail:** Pests use `transform.position` for movement. `PestManager` creates its own Screen Space Overlay Canvas (sort order 100) parented to its own transform — destroyed on reset with GameManager.

---

## Build / Quality Settings
- WebGL compression: Brotli (`webGLCompressionFormat: 2`)
- `runInBackground: 1`
- Quality "High" (WebGL default): `shadows=0`, `pixelLightCount=0`, `vSyncCount=0`
- Canvas Scaler: Scale With Screen Size, 1080×1920, match 0.5 — already responsive

---

## User TODO (Editor Tasks)

### Required
- Add `AchievementManager` component to GameManager GameObject

### Audio
- Wire `spraySound` and `emptySound` AudioSource fields on SprayBottle in inspector

### Art
- Assign a real sprite to the SprayBottle icon (fallback color renders in meantime)

### Shop
- In ShopManager inspector, add `BluebellArrangement` to the Order Pool list

---

## What This Project Is Not
- No real backend — social and store are mock implementations (`ISocialService`, `IStoreService`)
- No architectural overhauls or new systems without being asked
