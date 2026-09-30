# CLAUDE.md

Context for working on this repository. It was written in September 2026 by reading the whole codebase after the project had sat untouched since May 2025. Treat the "Audit backlog" and "Status" sections as a snapshot and re-verify before relying on them.

## What this game is

A **2D side-scrolling pixel-art action platformer** meant to become an **endless dungeon-crawler quest game**. The hero explores procedurally generated dungeons, fights monsters and bosses, collects loot, trades with a merchant, and gets stronger through equipment. The long-term goal or ending was **never designed**. The only hint of a story is in the merchant's intro dialog: *"Many have fallen trying to defeat the Dark Lord."*

The intended core loop, which is partly implemented:

```
Level0 (village hub)  --ExitPoint-->  RoomGenerator (procedural dungeon, gets bigger each run)
      ^                                         |
      +------------ bossRoom ExitPoint ---------+
```

Every trip into `RoomGenerator` builds a fresh dungeon, and `DungeonManager.DungeonLevel` goes up by one each time. That repeating loop is what makes the game "endless".

### Development timeline (from git history, 99 commits)
- **Feb 2025**: tilemap, platforms, wall jumping, health and damage, Bandit enemies, merchant NPC, level transitions, hero state machine (FSM).
- **Mar 2025**: pause menu, respawn and checkpoints, fade transitions, dialog boxes, enemy patrol and chase, inventory, character stats, equipment, ScriptableObject items, first room-grid dungeon generator.
- **Apr 2025**: rewrite of the dungeon generator around node-based rooms (Tiled maps), rule-based room selection, and the fix for rooms overlapping. The Unity Behavior and NavMesh packages were added but no script uses them.
- **May 2025**: data-driven item database loaded from JSON, NPC behaviors as ScriptableObjects, world-state flags, gold and prices, shop UI with buy and sell, loot chests. The last commit (2025-05-14) is "implementing chest UI", which is **unfinished** (see Status).

## Environment

- **Unity `6000.6.3f1`**. The project was upgraded from `6000.0.33f1` on 2026-09-29 (see "Unity 6000.6 upgrade notes" below). URP is 17.6 and always uses Render Graph, because the old compatibility mode was removed.
- **URP 2D** renderer (`Assets/Settings`, `UniversalRenderPipelineGlobalSettings.asset`), linear color space, default resolution 1920x1080.
- **Input**: the **Input System package** (1.20). Gameplay reads every control through the static `Core/GameInput` class (keyboard and mouse device API), and UI goes through the EventSystem's `InputSystemUIInputModule` in `UI.prefab`. **Never call `UnityEngine.Input` in game code**; add a property to `GameInput` instead. `activeInputHandler` is still `2` (Both) until the migration is play-tested. After that, set *Player Settings → Active Input Handling* to *Input System Package (New)*, which also silences Unity 6000.6's "Input Manager is marked for deprecation" message. Only unused vendor demo scripts (`HeroKnight.cs`, `Bandit.cs`) still call the old API. `Assets/InputSystem_Actions.inputactions` exists but nothing uses it.
- **Key packages**: `com.unity.nuget.newtonsoft-json` (item DB and save file), `com.unity.2d.*`, `com.unity.ugui` + TextMeshPro, UI Toolkit, `com.unity.behavior` and `com.unity.ai.navigation` (installed but unused).
- **SuperTiled2Unity** is an **embedded package** at `Packages/com.seanba.super-tiled2unity/`. It imports Tiled `.tmx` maps as prefabs. The dungeon generator depends on its `SuperMap` type. It is committed to git, so a fresh clone compiles.
- IDE: Visual Studio or Rider. `*.sln` and `*.csproj` are generated and git-ignored.
- **No tests, no asmdefs, no CI.** All game code compiles into `Assembly-CSharp`. The only way to verify behavior is Play mode in the Unity Editor. Claude cannot run the game and can only reason from code and serialized YAML.

## Repository layout

Only `Assets/Scripts`, `Assets/Editor` and the content folders listed below are project code. Everything else in `Assets/` is a third-party asset pack. **Don't modify vendor packs** except where the project already does (see HeroKnight below).

```
Assets/
  Scripts/                     <- ALL game code (~70 files, ~4.6k lines)
    Core/                      Singleton<T>, DialogSystem, Interactable, PauseMenu, CoreUI,
                               PersistentPlayerHealth, SensorManager, SaveSystem/WorldState*
    Character/
      Hero/                    Hero (player controller), HeroState (FSM), WeaponSensor
      Enemies/                 Enemy (patrol/chase/attack/trap), EnemyGenerator
      Core/                    CharacterStats, CameraFollow, FadeTransition, GameRespawn,
                               LevelTransition, NPC, IEntity, AnimatorParams (cached animator ids),
                               NPCInteractionBehavior + NPCInteractionBehaviors/*
      Inventory/               InventorySystem, EquipmentSystem, InventoryUI, EquipmentUI
    Health/                    Health, Healthbar (player HUD), FloatingHealthBar (enemies)
    Items/                     Item, RuntimeItem, ItemData, ItemLoader, ItemDatabase, ItemSystem,
                               Collectable, ItemGrid + ItemTooltip (shared item-window UI),
                               Effects/*, ItemVarients/* (sic), LootSystem/*, TradeSystem/*
    LevelGeneration/           DungeonGenerator, DungeonManager, RoomGeneration (rules), Room, Node
    WorldObjects/              RespawnCheckpoint
  Editor/DungeonGeneratorEditor.cs   Inspector buttons "Expand Dungeon" / "Expand Dungeon To Max"
  Sprites/Hero/, Animations/Hero/   The hero's per-weapon sprite sheets and animations (generated, see below)
Tools/HeroWeapons/            Python generator for those (outside Assets, so Unity ignores it)
  Levels/                      Level0.unity (hub), RoomGenerator.unity (dungeon)
  Prefabs/                     Player-facing prefabs (see "Prefab map")
  ScriptedItems/               ScriptableObject assets: NPC behaviors, shop and loot tables
  StreamingAssets/items.json   THE item database (data-driven)
  Resources/                   Loaded by name at runtime: InteractKey, PatrolPoint, Sprites/*
  Sprites/                     UI sprites + Tilesets/ (Tiled .tmx rooms, .tsx tileset, autotile rules)
  UI Toolkit/                  PanelSettings + runtime theme
```

Third-party packs: `Hero Knight - Pixel Art` (the player), `Bandits - Pixel Art` (enemies), `Merchant - Pixel Art`, `Cainos` (village props, including the `Chest` script used by loot chests), `2D Pixel Art Platformer Biome - American Forest`, `RPG Icons Pixel Art`, `JohnFarmer` (keyboard key sprites for tutorial signs), `Violet Theme Ui`, `NaughtyAttributes`, `TextMesh Pro`, and `Imported Assets/` (Monsters Creatures Fantasy, a simple UI pack, and a Pet Cats pack, which were imported but not used yet, probably meant for future monsters and bosses).

**HeroKnight exception:** the player prefab is the vendor's `Assets/Hero Knight - Pixel Art/Demo/HeroKnight.prefab`, modified in place. The vendor's `HeroKnight.cs` controller is not used. The project's `Hero.cs` and friends are attached instead, and only the vendor's `Sensor_HeroKnight` is reused. The vendor's animator controller (`Animations/HeroKnight_AnimController.controller`) is also **extended in place** (movement rework, 2026-10-01): a `LedgeGrab` bool parameter and `Ledge Grab` state, and a Wall Slide → Fall transition for letting go of a wall. The prefab's Rigidbody2D uses the pack's frictionless `Environment/Walls_noFriction` material.

**Hero weapon looks** (2026-10-01): the hero appears holding the equipped weapon type. The design choice, made by the user, was one copy of the hero per type.
- There are five looks: the vendor sheet for Sword (and Greatweapon), plus generated copies for Dagger, Bow, Staff and Wand.
- `Tools/HeroWeapons/hero_weapon_sheets.py` (Python standard library) builds each copy from the vendor sheet. For every frame it:
  - finds the sword (the gold guard plus the straight lavender blade from it),
  - erases it,
  - draws the new weapon at the same grip point and angle, as simple vector shapes rasterised to pixels,
  - and keeps the swing trails for the Dagger, turns them magic blue for the Staff and Wand, and removes them for the Bow.

  Only pixels that were transparent or part of the old sword get painted, so a hand or head that covered the sword also covers the new weapon.
- The white hit-flash frames 45 and 48 borrow the sword's position from their matching frames. Frame 24, where the blade runs behind the body, has a manual entry in `OVERRIDES`.
- Outputs:
  - `Assets/Sprites/Hero/HeroKnight_<Weapon>.png`, sliced exactly like the vendor sheet
  - the 15 hero clips copied into `Assets/Animations/Hero/<Weapon>/` and pointed at that sheet
  - one Animator Override Controller of `HeroKnight_AnimController` per weapon type

  Rerunning keeps the existing GUIDs. Run `python Tools/HeroWeapons/hero_weapon_sheets.py --preview <folder>` to also get contact sheets of every frame.
- `Hero.UpdateWeaponLook` swaps the Animator's controller when the equipped weapon changes (`daggerLook`, `bowLook`, `staffLook`, `wandLook` on the prefab; the sword look is the prefab's own controller). It keeps the animator parameters across the swap, because swapping resets them.
- **Limits:**
  - Every weapon uses the sword's poses, so the bow is swung before it shoots.
  - The weapons are plain placeholder pixel art; redraw them by editing `WEAPONS` in the tool and rerunning it.
  - The no-blood death and no-effect block clips use other vendor sheets and still show the sword. Neither is used today.
  - **The shield is still drawn into every frame**, even with a bow or staff. The plan is to cut it into an overlay layer that shows only while a shield is equipped; that needs the body patched where the shield covered it.

## Running the game

- Build scenes: `Level0` (index 0), `RoomGenerator` (index 1).
- **Always enter Play mode from `Level0`.** The player, HUD, dialog canvas, pause menu, inventory/shop/loot UIs, fade canvas, `ItemDatabase` and `WorldStateManager` only exist in Level0 and survive into the dungeon through `DontDestroyOnLoad`. `RoomGenerator` has only the generator, camera and HUD, so starting there logs "Player not found" and `Singleton` getters create empty components that throw null-reference errors.
- Level0 contains the village tilemap (`village.tmx`, 32x16 tiles), the Town Merchant, a loot chest, a Respawn Stone checkpoint, tutorial signs, and an `ExitPoint` whose `nextSceneName` is `RoomGenerator`.

### Controls (all defined in `Core/GameInput.cs`)
| Input | Action | Where |
|---|---|---|
| A/D, arrow keys | Move, smoothed like the old `Input.GetAxis` (ramps at 3/s, snaps to 0 on reversing) | `Hero.Update` (`GameInput.Horizontal`) |
| Space | Jump (hold for full height, tap for a short hop), wall jump, pull up onto a ledge | `IdleState` → `JumpingState`, `LedgeGrabState` |
| S, down arrow | Drop from a ledge | `LedgeGrabState` |
| Left Shift | Roll (damage immune while rolling) | `RollingState` |
| Left mouse | Attack with the equipped weapon (3-swing combo `Attack1..3`; bows, staffs and wands shoot); also advances dialog | `AttackingState`, `DialogSystem` |
| Right mouse (hold) | Block. **Only works if the equipped item has a `Block` effect** (`stats.canUseBlock`) | `BlockingState` |
| E | Interact (NPC, chest, checkpoint), advance dialog | `Interactable`, `DialogSystem` |
| I | Open inventory | `InventoryUI` |
| Esc | Pause menu, or close an open inventory/shop/loot window | `PauseMenu`, `*UI` |

## Architecture

### Singletons and persistence
- `Core/Singleton.cs`: `Singleton<T>` looks up `Instance` with `FindAnyObjectByType` (`FindFirstObjectByType` is deprecated in 6000.6). **If none exists, it creates a new GameObject.** Singletons meant to work that way (no prefab) are marked `[AutoCreatedSingleton]`: `ItemSystem`, `ShopSystem`, `DungeonManager`, `SaveSystem`. For any other type it logs a warning ("No X found, so an empty one was created"), which points at a missing or destroyed prefab. `HasInstance` checks for an instance without creating one; use it in `OnDestroy`. `Awake` calls `DontDestroyOnLoad` and destroys duplicates. Subclasses override `protected override void Awake()` and must call `base.Awake()`.
- Singletons: `DialogSystem`, `PauseMenu`, `FadeTransition`, `GameRespawn`, `SensorManager`, `InventorySystem`, `EquipmentSystem`, `ItemDatabase`, `ItemSystem`, `ShopSystem`, `DungeonManager`, `WorldStateManager`, `SaveSystem`. `ItemSystem`, `ShopSystem`, `DungeonManager` and `SaveSystem` have no prefab and are always created lazily by the getter.
- `PersistentPlayerHealth` (on the player) has its own static `Instance` and `DontDestroyOnLoad`. Other code reaches the player through it, for example `PersistentPlayerHealth.Instance.GetComponent<Hero>()` in `CameraFollow`.
- The player GameObject carries **several singletons at once**: `Hero`, `PersistentPlayerHealth`, `CharacterStats`, `GameRespawn`, `SensorManager`, `WeaponSensor` (child `AttackSensor`), plus 5 `Sensor_HeroKnight` children. Their names must stay exactly `GroundSensor`, `WallSensor_R1/R2/L1/L2`, because `Hero.Start` looks them up with `transform.Find`. Only `WallSensor_R2/L2` are still used, as spawn points for the wall-slide dust; ground and wall detection are casts in `Hero` now.
- InventoryUI, ShopUI and LootUI do their own `DontDestroyOnLoad` instead of using `Singleton<T>`, and expose the kept copy as a static `Instance` (used by `OpenShopBehavior` and `LootChest`).

### UI state and pausing
- Opening inventory, shop or loot sets `Time.timeScale = 0`, sets `PauseMenu.GameIsPaused = true`, and sets `CoreUI.IsUIOpen` **one frame later** (`DelayUIFlagClear`) so the same Esc press doesn't also open the pause menu.
- Two UI technologies are in use:
  - **uGUI Canvas**: HUD health bar (`Prefabs/Scening/UI.prefab`), dialog box, pause menu, fade.
  - **UI Toolkit** (`UIDocument` + UXML): inventory, equipment, shop, loot. UXML lives next to the prefabs in `Prefabs/InterfaceGraphics/**`. Scripts query elements by name (`"InventoryContainer"`, `"Items"`, `"Gold"`, `"ExitButton"`, `"ShopItems"`, `"PlayerItems"`, `"SellButton"`, `"BuyButton"`, `"LootContainer"`, `"LootItems"`, `"Loadout"`, `"EquipmentContainer"`, `"Weapon"`…). **Renaming an element in UXML breaks the script silently.**
  - Item grids and tooltips are built in C# by the shared `ItemGrid.Build` (slots, hover, click, selection highlight) and `ItemTooltip` (Show, Hide, MoveTo). Each window keeps its own `UpdateTooltipPosition`, because their layouts need different hand-tuned offsets.

### Player: `Hero` + `HeroState` FSM
- `HeroStates` enum: `Idle, Run, Jump, Roll, Attack, Block, Dead, LedgeGrab`. `Run` has no state class; running is handled in `Hero.Update` by setting animator `AnimState=1`. `Jump` means "in the air" (jumping, falling, wall sliding).
- Each state class (`IdleState`, `JumpingState`, `LedgeGrabState`, `AttackingState`, `BlockingState`, `RollingState`, `DeadState` in `HeroState.cs`) returns the next state from `handleInput()`, and `Hero.ChangeState` swaps it in when the enum differs. That calls the old state's `exitState()` (undoing its gravity, animator bools and so on, even when a dialog or death interrupts it) and the new state's `startState()`. `ControlsFacing` lets a state set the facing itself (wall slide, ledge, wall-jump lock) instead of following the steering.
- Ground states (attack, block, roll) are only reachable from `Idle`, which only exists on the ground: the hero can't attack or roll in the air. `Hero.handleInput` switches to `DeadState` from any state when health hits 0.
- `Hero.Update` reads input, records a Space press for the jump buffer, and checks contacts once per frame (`UpdateContacts`) before running the state:
  - `isGrounded()` box-casts a strip 0.04 narrower than the collider, so walls beside the hero don't count as ground.
  - `WallSide` is ±1 when raycasts at a quarter of the body height *and* near the top both hit a wall on that side. A platform corner reaching only part of the body is a ledge, not a wall.
- Horizontal velocity is set in `Hero.FixedUpdate` from `stats.TotalMoveSpeed`, except during `Roll`, `Dead`, `LedgeGrab`, and for `wallJumpControlLock` seconds after a wall jump.
- **Movement (rework of 2026-10-01; tuning fields are on the `Hero` component, grouped as Jump feel, Walls and Ledges):**
  - *Jump*: `m_jumpForce` 9 (apex about 4.1 units). **Coyote time** (0.1 s) allows a jump just after running off an edge. The **jump buffer** (0.12 s) makes a press just before landing still jump. Releasing Space while rising multiplies the upward speed by `jumpCutMultiplier` (0.5), giving **variable jump height**. Landing only counts when not rising, so the state can't flip back to Idle on the frame after takeoff.
  - *Wall slide*: touching a wall while falling and not steering away caps the fall at `wallSlideSpeed` (2.5) and faces the wall (WallSlide animation). Steering away lets go.
  - *Wall jump*: Space while touching a wall (or within coyote time of touching one) launches at `wallJumpVelocity` (6 away, 9 up). Steering is ignored for `wallJumpControlLock` (0.2 s) so holding toward the wall can't cancel the push; afterwards the hero can drift back to climb the same wall or reach the opposite one.
  - *Ledge grab* (`LedgeGrabState`): falling past a platform edge whose top is level with the hero's hands (within 0.3 below `ledgeHangOffset`) on the facing side snaps the hero to hang (`Hero.FindLedge`, LedgeGrab animation). Space, or holding toward the ledge for 0.2 s, pulls up over `pullUpDuration`: first up beside the wall, then over, so the collider never cuts the corner. It won't pull up if something blocks the space on top. S or down drops, and Space while holding away jumps off. Regrabbing is blocked for `ledgeRegrabDelay` after dropping or jumping off.
  - Position snaps go through `Hero.Teleport`, which calls `Physics2D.SyncTransforms()` because Auto Sync Transforms is off.
  - The hero's Rigidbody2D is frictionless, because steering pushes into walls every physics step and friction used to brake the fall or stick the hero to walls and corners. `DeadState` zeroes the horizontal speed so a dead hero doesn't slide.
- The hero can't move while `DialogSystem.DialogActive` (it is forced back to Idle).
- **Starting kit** is hard-coded in `Hero.Start`: inventory gets item ids `18, 18, 19, 20` (heal potions), and equipment gets `69, 420, 1337` (Basic Metal Shield, Broadsword, Leather Armor). The easter-egg ids are intentional.
- Animator parameters used: `AnimState`, `Grounded`, `AirSpeedY`, `WallSlide`, `LedgeGrab`, `Jump`, `Roll`, `Attack1-3`, `Block`, `IdleBlock`, `Hurt`, `Death`, `Revive`, `noBlood`.

### Stats, health and damage
- `CharacterStats` holds base plus bonus values for move speed, jump height, damage, armor and **magic power** (added to magic bolt damage). It also has **agility**, where each point adds `MoveSpeedPerAgility` (0.1) to move speed, a `canUseBlock` flag, and **on-hit status effects**: `bleed`/`poison`/`burn` `Damage` (per second) and `Duration` (seconds), mostly set by equipment. `TotalJumpHeight` is **unused**; jumping uses `Hero.m_jumpForce`.
- Armor formula: `damage * (1 - armor / (armor + 50))`, floored (`CalculateDamage`).
- `Health` has `baseHealth + bonusHealth = MaxHealth`, `CurrentHealth`, and delegates to `IEntity` (`Hero` or `Enemy`) for `IsBlocking`, `TakeDamage` (returns the final damage) and `Die`. It has three damage paths:
  - `Health.TakeDamage(float)` **returns whether the hit landed**. A hit doesn't land if it was blocked or rolled through, fully absorbed by armor, landed during i-frames, or the target was already dead.
  - `TakeStatusDamage` is used for damage over time. It ignores armor, blocking and i-frames, and plays no hurt animation.
  - `Kill()` is instant death, used for falling out of the level.

  All three end in `ReduceHealth`, which calls `IEntity.Die()` and the virtual `OnDied()` exactly once. `PersistentPlayerHealth` overrides `OnDied` to start the death sequence.
- **Shield** (in `Health`) soaks damage before HP. Hit damage is absorbed after armor, and damage over time goes through the shield too. There are two pools:
  - The **recharging shield** goes up to `MaxShield = baseShield + bonusShield`. Equipment adds to `bonusShield`. It refills at `shieldRechargeRate` (10/s) once no damage has come in for `shieldRechargeDelay` (3 s).
  - The **temporary shield** comes from consumables (`AddTemporaryShield(amount, duration)`), doesn't recharge, and expires after its duration. The timer uses scaled time, so it doesn't run while menus are open.

  A hit that's fully absorbed counts as not landed, so it applies no on-hit effects.
- **Mana** (`Health/Mana.cs`) is a component the hero adds to itself in `Start` (it isn't on the prefab yet). It has `MaxMana = baseMana (50) + bonusMana` and regenerates `baseManaRegen (1) + bonusManaRegen` per second. Staffs and wands spend it (`TrySpend`) on magic bolts. Future spells should do the same and scale with `CharacterStats.TotalMagicPower`.
- I-frames with a red flash and ignored Player/Enemy layer collision **only run when the configured player layer is layer 6** (hard-coded check).
- Hero `TakeDamage` returns 0 while in Block or Roll.
- Player defaults: 100 HP, speed 4, damage 15, armor 5. With the starting kit that becomes 120 HP, 20 damage and 30 armor.
- HUD `Healthbar` reads `PersistentPlayerHealth.Instance` and draws a breakpoint marker every `breakpointEveryX` HP (50 on the HUD in `UI.prefab`, 25 on the Bandit, 50 on the Bandit Chief). `createBreakpoints` re-runs when max HP changes. Markers are anchored to a fraction of the fill's width and stretched to its height, so they work with any fill layout. The HP text shows the shield as `95 (+30)`.
- **The mana bar is created in code.** In `Start`, the HUD's health bar (`resource = Health`, no `entityHealth`) clones itself right below as a `resource = Mana` bar with the blue fill (`manaFillSprite`, which is Violet's `Progress Bar Blue_0`, set in `UI.prefab`). The copy hides the heart (`healthIcon`) and HP number (`healthText`) it cloned, so it's a plain bar with no number. There's no mana bar object in any prefab. To restyle or move it, change the clone code in `Healthbar.CreateManaBar`, or turn off `spawnManaBar` and build one in the Editor.
- Death: `PersistentPlayerHealth` waits 2 s, then force-opens the pause menu, whose Respawn button calls `GameRespawn.RespawnPlayer` (fade, full heal, teleport to the checkpoint or the start position). Falling below `GameRespawn.threshold` (−200 in Level0, −15 on the prefab) calls `Health.Kill()`.

### Combat and enemies
- **Player attacks** depend on the equipped weapon's **type** (`WeaponType` in `items.json`, `WeaponProfile` in `Items/WeaponType.cs`; no weapon fights like a sword). Every type still plays the hero's sword swing, sped up or slowed down:

  | Type | Weapons | Swing time | Animation speed | Reach | Two-handed | Attack |
  |---|---|---|---|---|---|---|
  | Sword | Broadsword, Crimson Blade, Emberfang | 0.5 s | 1 | 1 | no | melee |
  | Dagger | Venomfang Dagger | 0.3 s | 1.6 | 0.7 | no | melee |
  | Greatweapon | Giant's Cleaver | 0.85 s | 0.65 | 1.4 | yes | melee |
  | Bow | Elven Longbow | 0.7 s | 1 | – | yes | arrow (14 u/s, range 12) |
  | Staff | Staff of Frostbite | 0.8 s | 0.9 | – | yes | magic bolt, 8 mana (10 u/s, range 9) |
  | Wand | Spark Wand | 0.45 s | 1.3 | – | no | magic bolt, 4 mana |

  - `AttackingState` plays one swing per click and chains the Attack1-2-3 combo. A click during a swing starts the next swing when the swing time is up. Each swing **strikes** once, 0.18 s into the swing divided by the animation speed (`Hero.Strike`).
  - A melee strike hits every enemy inside the `AttackSensor` hitbox (widened forward by reach) once, through a physics overlap query (`WeaponSensor.Strike`). Before 2026-10-01 hits came from trigger-stay callbacks: a whole combo could only damage once, and a sleeping physics body could miss.
  - A ranged strike launches a `Projectile`: it moves by raycasts, hits the first living enemy or wall, and uses code-drawn pixel sprites until there's projectile art. Arrows deal `TotalDamage`; magic bolts deal `TotalDamage + TotalMagicPower` and cost mana. Without enough mana, a staff or wand strikes as a melee weapon instead.
  - Both kinds apply the wearer's bleed, poison and burn through `CharacterStats.ApplyOnHitEffects` when a hit lands. The hitbox turns with the hero's facing.
  - **Two-handed rule** (`EquipmentSystem.EquipItem`): equipping a two-handed weapon puts the shield back in the inventory, and equipping a shield puts a two-handed weapon back. So no blocking with bows, staffs or the cleaver.
  - Weapon tooltips end with the type's summary line.
- **Status effects** (`Health/StatusEffects.cs`) are damage per second for N seconds. They tick once per second, starting one second after the hit, and briefly tint the sprite (red for bleed, green for poison, orange for burn). The component is added to a target the first time it's affected. Each type runs independently, and reapplying one **restarts it with the new values** rather than stacking. It stops when the target dies. Enemies call the same `ApplyOnHitEffects` when their attacks land (the Bandit melee and the spike trap), so giving an enemy's `CharacterStats` bleed or poison values makes its attacks apply them.
- **Enemy** (`Enemy.cs`) implements `IEntity`:
  - When `isTrap` is true it deals damage on trigger enter (Spiketrap).
  - Otherwise it patrols between `patrolPoints`, chases within `detectionRange`, and attacks within `attackRange` in a loop. Damage lands via the **animation event `DealDamage()`**.
  - It moves by setting `transform.position` directly, not through the Rigidbody, and only horizontally (it used to float up toward a jumping player). When the player is straight above and out of reach, it stands still.
  - With no patrol points it stands still until the player comes within `detectionRange`, and returns to idle after a chase.
- Enemy prefabs: `Prefabs/Enemies/Bandit.prefab` (100 HP, speed 2, damage 15, armor 5, attackDelay 5.5) and `Spiketrap.prefab`. The Bandit also has an `NPC` component, but `NPC` doesn't flip enemies: `Enemy.FaceTowards` owns the facing, because the two scripts flipping the sprite drifted out of sync and could turn an enemy's back to the player. `Enemy.spriteFacesRight` says which way the art faces (Bandit art faces left, Monsters pack art right). Patrol points that are empty are dropped in `Enemy.Start`, so an enemy placed or spawned without them just stands guard.
- **Monster enemies** (added 2026-10-01) are prefab variants of `Bandit` built from the Monsters Creatures Fantasy pack. They share the Bandit's AI and animator logic, and differ in art, collider, stats and on-hit effect:

  | Prefab | HP | Damage | Armor | Speed | Attack delay / range | Detection | On hit | Appears from dungeon level |
  |---|---|---|---|---|---|---|---|---|
  | `Goblin` | 60 | 10 | 2 | 3.2 | 2 s / 1.3 | 6 | bleed 2/s for 3 s | 1 |
  | `Mushroom` | 90 | 8 | 5 | 1.8 | 3 s / 1.2 | 4.5 | poison 3/s for 4 s | 2 |
  | `Skeleton` | 150 | 16 | 15 | 1.5 | 3.5 s / 1.8 | 5 | none | 3 |

  - **Art setup:** the pack's sheets for Idle, Run/Walk, Attack1, Take Hit and Death are re-imported at 32 pixels per unit like the hero and Bandit art. Each sprite's pivot is at the feet (pixel row 101 of 150), centred on the body, so spawn points work like the Bandit's and flipping doesn't shift the body.
  - **Clips and controllers** are in `Assets/Animations/Enemies/<Monster>/`. Each monster has an Animator Override Controller of the Bandits' `LightBandit_AnimController`, so `AnimState`, `Attack`, `Hurt` and `Death` work unchanged. The attack clip calls `DealDamage` at 0.5 s, the frame where the weapon reaches furthest.
  - All of this was generated by script (clips at 12 fps). Retune by editing the prefabs and clips in the Editor.
  - The Flying Eye isn't used yet: it needs flying AI, because `Enemy` only walks.
- **Boss: the Bandit Chief** (`Prefabs/Enemies/BanditChief.prefab`, a prefab variant of `Bandit`) is 1.5x the size and tinted red, with 250 HP, damage 25, armor 10, speed 2.5, bleed 4/s for 3s, attackDelay 2.5 and detection range 6. Its `BossEnemy` component:
  - adds `healthPerLevel` (75), `damagePerLevel` (4) and `armorPerLevel` (2) for each dungeon level after the first
  - locks the room's exit (`LevelTransition.Lock`, which shows a message and adds an invisible wall) until it dies
  - on death (`Health.Died` event), unlocks the exit, pays `goldReward + goldPerLevel` per extra level (30, +20), reveals the room's hidden `RewardChest` and shows a dialog
  - finds the exit and chest with `GetComponentInParent<Room>()`, so it only works inside a room prefab
- `EnemyGenerator` does a weighted random pick from the `enemies[]` entries whose `minDungeonLevel` is at most the current dungeon level, and spawns the enemy plus a single `PatrolPoint` at `spawnPoint`. An entry without a prefab means no enemy. The `enemyRoomLR` prefab's list (weight, from level): Bandit 20 (1), Spiketrap 5 (1), Goblin 20 (1), Mushroom 20 (2), Skeleton 15 (3), no enemy 10 (1).

### Items (data-driven)
- **Source of truth: `Assets/StreamingAssets/items.json`**. It is loaded once by `ItemDatabase` (a singleton prefab in Level0) through `ItemLoader`, which uses Newtonsoft to parse `ItemData` and then `RuntimeItem.SetData`.
- `Item` (abstract ScriptableObject) holds id, name, description template, sprite, `ItemType` (`Consumable, Weapon, Shield, Armor, Accessory, Helmet, Gloves`; in `items.json` the Iron Helm is a `Helmet` and Thief's Gloves are `Gloves`), price and isSellable, plus four effect lists:
  - `characterStatsEffects` / `healthEffects`: applied on **equip**, removed on **unequip** (`ApplyEffects` / `RemoveEffects`).
  - `onActivateCharacterStatsEffects` / `onActivateHealthEffects`: applied on **use** (consumables are then removed from the inventory).
- The JSON `effectType` strings are mapped in `RuntimeItem.Convert*Effects`:
  - CharacterStats lists: `Armor`, `Block`, `Damage`, `MagicPower`, `Agility`, `Mana` (max), `ManaRegen`, `RestoreMana` (consumables), and `{Bleed,Poison,Burn}{Damage,Duration}`, which all map to one `OnHitStatusEffect`.
  - Health lists: `Health` (max HP), `Heal`, `Shield` (equipment, recharging) and `TemporaryShield` (consumables). The last two are one `ShieldEffect`.
  - An effect entry can have an optional `"duration"` in seconds. Only `TemporaryShield` uses it so far.
  - **Unknown types, or types in a list the loader doesn't read for them, are silently dropped.** Check new data against this list.
- Description placeholders are replaced by each effect's `AdjustDescription`: `{bonusArmor}`, `{bonusDamage}`, `{bonusHealth}`, `{healAmount}`, `{bonusShield}`, `{shieldDuration}`, `{bonusMagicPower}`, `{bonusAgility}`, `{bonusMana}`, `{bonusManaRegen}`, `{restoreMana}`, and `{bleed|poison|burn}{Damage|Duration}`. `Block` appends a paragraph. A placeholder is only filled if the item has the matching effect. As of 2026-09-29 every placeholder in `items.json` resolves.
- Sprites: `spriteName` is loaded as `Resources/Sprites/<name>`. If there's no such file, it's taken as sub-sprite `<sheet>_<n>` of the sheet `Resources/Sprites/<sheet>`, for example `basic_clothing_10`. A missing sprite logs a warning.
  - **New icons:** as of 2026-09-29 every item has one. The 14 previously missing icons were copied from the `RPG Icons Pixel Art` pack (`<Category>/PNG/Transperent/IconN.png`) into `Resources/Sprites` under the item's `spriteName`.
  - **Import settings for new icons:** Point filter, no compression, new GUID, matching the other item icons. Import new icons the same way; the pack's own settings blur pixel art.
- `ItemSystem.AddToPlayerInventory(int[])` and `AddAndEquipOnPlayer(int[])` are the helpers for granting items by id.
- The old ScriptableObject item workflow (`ItemVarients/*` with `CreateAssetMenu`, `ScriptedItems/HealCollectable`, the `Collectable` pickup) still exists alongside the JSON system. New items should go into JSON.
- Economy: shop sell price is `floor(price * 0.6)`. The player starts with 50 gold (`InventorySystem.gold`).

### Inventory, equipment, shop, loot
- `InventorySystem` (list of `Item` + gold, event `onInventoryChanged`) and `EquipmentSystem` (event `OnEquipmentChanged`) both live on `Prefabs/InterfaceGraphics/InventoryGraphics/InventoryManager.prefab`. Both raise their events through `SafeEvent.Invoke`, which calls every listener even if one throws (and logs the exception), so a broken window can't block the others or abort the change. Pass `notify: false` to `AddItem`/`RemoveItem` to batch changes and call `NotifyChanged()` once.
- **Equipment slots** (`EquipmentSlot`): `Weapon, Shield, Helmet, Armor, Gloves, Accessory1, Accessory2`. `SlotFor` maps an item type to its slot. An accessory takes a free accessory slot, or replaces the one in `Accessory1` when both are full.
- **Rule: items move between the inventory and the slots only through `EquipItem` and `UnequipItem`, and those are the only places effects are applied or removed.** `EquipItem` only equips an item that is in the inventory, and puts the slot's previous item back with its effects removed. So stats always match what is worn, and re-equipping can't stack. `ItemSystem.AddAndEquipOnPlayer` (starting kit, loading a save) adds each item to the inventory first.
- **Windows keep no copies**: `InventoryUI`, `EquipmentUI`, `ShopUI` and `LootUI` read the systems' current state on every redraw. They redraw on each change event, whenever they open, and after each click that changes something. Grid slots remember the item they were drawn with. Shop buy and sell act on the selected item (`ShopSystem.BuyItem(shop, item)`, `SellItem(item)`), not on a slot index, so an out-of-date slot can't act on the wrong item.
- `EquipmentUI` finds each slot's element by the slot's name in `EquipmentUI.uxml` (`Weapon` … `Accessory2`, two rows of labelled columns). The empty-slot icon is that element's UXML background image, remembered at startup. The Helmet and Gloves icons are Violet's `White Helmet` and `White Boxing Glove 1`. The slot callbacks are registered once and look up the slot's item when they fire.
- Clicking an item in the inventory equips it or uses it if it's a consumable. Clicking an equipment slot unequips.
- **Shop**: an NPC's `OpenShopBehavior` hands a `ShopInventory` asset to `ShopUI`. The asset lists item IDs and a `quantity` per entry, where -1 means unlimited. Selecting an item enables Buy or Sell, which call `ShopSystem.BuyItem` / `SellItem`.
  - **Limited stock:** purchases are counted in world state as `Shop_<asset name>_<itemId>_Bought` and saved with the game. `SetItems()` rebuilds the runtime `items` list (`[NonSerialized]`) from what's still in stock.
  - **The Town Merchant sells** the three Instant Heal potions (unlimited) and one each of the Iron Helm, Thief's Gloves, Oakwood Shield, Amulet of Lifeglow, Venomfang Dagger and Elven Longbow.
- **Loot**: `LootChest` (on `Prefabs/Environment/LootChest.prefab`, wrapping Cainos' `Chest`) rolls `LootInventory.GetLoot()` once in `Start`. Interacting opens the chest animation and then `LootUI`, where **Take** moves the selected item into the inventory and **Take All** empties the chest and closes the window. Emptied chests stay open. Loot tables are `LootInventory` assets (lists of item-id groups with weights), for example `ScriptedItems/LootInventories/TestLootInventory/LootInventory_0.asset`.
  - **Loot by depth (2026-09-29):** `LootItemData.minDungeonLevel` unlocks groups by `DungeonManager.DungeonLevel`. The only table (`LootInventory_0`, used by every chest) now covers all items, with potions at level 0 and the best weapons at level 4. Chests *placed in a scene* (not inside a dungeon `Room`) keep their remaining contents in world state (`Chest_<scene>_<x>_<y>`), so the village chest no longer re-rolls on every visit. **Not play-tested yet.**

### NPCs, dialog and world state
- `NPC` has a `currentAction` (`None, Attention, Information, Trade`) that controls the overhead indicator prefab (! / ? / trader mark) and which behavior runs on interaction (E). `onSpawnBehavior` runs in `Start`. If `isTrader` is set, the NPC switches to `Trade` after any interaction.
- Behaviors are **ScriptableObject assets** derived from `NPCInteractionBehavior` (`IEnumerator Execute(NPC)`), created from the `NPC/Behaviors/*` menu:
  - `ShowDialogBehavior` (one-time, gated by a world-state bool)
  - `RepeatableDialogBehavior`
  - `GiveItemBehavior` (one-time, gated by a world-state bool)
  - `OpenShopBehavior`
  - `ConditionalActionBehavior` (switches the NPC action when a world-state bool is true)
  - `CompositeNPCBehavior` (runs a list of behaviors in sequence)
- **Town Merchant** (Level0) wiring in `ScriptedItems/NPCBehaviors/Merchant/`:
  - onSpawn is `Merchant_Conditional_Action`: if `Merchant_Amulet_Given` is set, switch to Trade.
  - Attention is `MerchantIntro_Composite`: intro dialog, then give item 21 (Merchant's Amulet, +5 damage).
  - Trade is `MerchentTrade_Composite`: "Let's trade." dialog, then open the `Merchant_Shop_Inventory` shop (potions plus limited gear, see above).
- `DialogSystem` splits text into **pages on `\n`**, uses a typewriter effect, and advances on E or left click. `ShowDialog(name, text, onClose)`.
- `Interactable` is added at runtime by `NPC`, `LootChest` and `RespawnCheckpoint`. It needs a trigger collider on the object and a player tagged `Player`. It spawns the `Resources/InteractKey` "E" prompt.
- `WorldStateManager` holds string-keyed bool, int and string flags. It holds the two merchant bools, shop purchase counts (`Shop_*`) and placed chests' contents (`Chest_*`). It restores them from the save in `Awake`, before any `NPC.Start` reads them, and `SaveSystem` writes them (see "Save and load").

### Scene transitions and checkpoints
- `LevelTransition` (on `Prefabs/Scening/ExitPoint.prefab`) calls `FadeTransition.LoadScene(nextSceneName)` when the player walks in. The persistent `FadeTransition` then runs the whole transition: fade to black, load the scene, move the player to the GameObject named **`EntryPoint`**, save, and fade back. It ignores new requests while one is running (`IsTransitioning`), and logs an error for a scene that isn't in the build settings. The prefab default `"Level2"` no longer exists; every instance overrides it.
- Respawning fades through `FadeTransition.FadeAndExecute` instead, since it doesn't change scenes.
- `RespawnCheckpoint` (`Prefabs/Environment/Respawn Stone.prefab`) records a checkpoint in `GameRespawn` on interaction, saves the game, and shows a dialog. `GameRespawn` keeps the checkpoint as **scene name plus position**, so it survives dungeon trips and save/load. Respawning uses the checkpoint while that scene is loaded, and otherwise the scene's `EntryPoint`.

### Save and load (`Core/SaveSystem/`)
- **What's saved:** `SaveSystem` writes `savegame.json` to `Application.persistentDataPath`. It holds the `SaveData` version, the world flags, and `PlayerSaveData`: gold, current HP, dungeon level, current mana (nullable, because older saves lack it), the IDs of equipped items (in slot order, restored by type, so no slot data is stored) and of inventory items, and the last checkpoint. The shield isn't saved; it starts full and recharges.
- **When it saves:** at checkpoints, on every scene change (`FadeTransition.LoadScene`, `DungeonGenerator.SpawnPlayer`), and on Quit from the pause menu. It does **not** save when Play mode stops, so editor sessions don't overwrite the save.
- **Writing is safe:** it writes a `.tmp` file and then `File.Replace`s the real one. An unreadable save is copied to `savegame.json.corrupt` and the game starts fresh.
- **Loading:** the file is read once, when `SaveSystem` is first created (from `WorldStateManager.Awake` during Level0's load). `Hero.Start` then calls `SaveSystem.RestorePlayer`. That equips the saved gear *before* filling the inventory, because `EquipItem` removes the item from the inventory and would take a spare copy. It sets HP after equipping (so bonuses count) and moves the player to the checkpoint when it's in the current scene. **With no save, `Hero.Start` grants the starting kit** at full health.
- **Items are saved by `items.json` id.** ScriptableObject items from `ItemVarients/` all report id 0, so they're skipped with a warning. IDs that no longer exist are skipped when loading.
- **No main menu:** the game always continues the existing save. Quitting in the dungeon resumes in Level0, and the dungeon level stays where it was, so the next run is one level deeper.
- **New game:** `SaveSystem.StartNewGame()` deletes the save, destroys the game's objects in the DontDestroyOnLoad scene (only roots with game scripts, so package helpers survive), and loads build index 0 (Level0) so everything is rebuilt. The pause menu exposes it as `PauseMenu.OnNewGameClicked()`. The **New Game button** (added by the user on 2026-09-30, meant for future features such as a main menu) is `NewGameButton`, added in `Level0.unity` as a scene override on the `PauseMenuCanvas` instance, not in the prefab itself. It starts a new game straight away, with no confirmation.
- **Editor tools**, under **Tools → Save Game**: *Delete Save File* (with confirmation), *Open Save Folder*, and *Start New Game* (Play mode only). **Tools → Debug** (Play mode) can give the status effect weapons, all items, and 100 gold, and **Spawn Enemy** drops a Bandit, Goblin, Mushroom or Skeleton in front of the player. Use *Delete Save File* to test the new-game path, because stopping Play mode doesn't reset the save. A `worldstate.json` in the same folder is left over from the old system and is unused.
- `CameraFollow` lerps toward the player, clamped to `minBounds` and `maxBounds`. The dungeon generator rewrites the bounds as tiles are placed.

### Procedural dungeon (`LevelGeneration/`)
- **Rooms** are 12x12-tile (32 px) Tiled maps in `Assets/Sprites/Tilesets/*.tmx`, imported by SuperTiled2Unity and wrapped in prefabs in `Assets/Prefabs/LevelGeneration/Rooms/`. Each room prefab has:
  - a `Room` component with a `RoomType`: `Empty, Start, Enemy, Loot, CorridorLR, ParkourLTRB, ParkourLTR, ParkourLRB, Boss, End`.
  - `Node` children at each doorway. Nodes come in **pairs** (`pairedNode`, the two edges of an opening), and both nodes must line up for rooms to connect. Nodes are flagged `isEntrance` and/or `isExit`.
  - Name suffixes give the open sides: L(eft), T(op), R(ight), B(ottom).
- **Rules** are on the `RoomGeneration` component in `RoomGenerator.unity`. `ruleEntries` is a list of (current room type, exit direction) → weighted `roomOptions`, which becomes a dictionary through `InitializeRules()`. Only these prefabs are wired into the rules: `startRoom`, `corridorLR`, `enemyRoomLR`, `lootRoomLR`, `parkourLTRB`, `parkourLTR`, `parkourLRB`, `bossRoom`, `empty`. The `*L`, `*R` and other corridor or parkour variants are unused Tiled maps or prefabs without a `Room` component.
- **Algorithm** (`DungeonGenerator.Awake` → `GenerateDungeon`):
  1. `DungeonManager.RegenerateDungeon()` increments `DungeonLevel` and sets `DungeonSize = 8 + 8*level` for levels below 5. That gives 16, 24, 32 and 40, and stays at 40 after that.
  2. It places `startRoom` at grid (0,0), marks its exits as going Right, and places an `empty` cap at (−1,0).
  3. `ExpandToMaxDungeon()` repeatedly takes `activeNodes[0]` and calls `SpawnTile`. That computes the neighbour grid cell and world offset (map width and height), skips occupied cells, rolls a weighted room from the rules (weights adjusted by the room mix, below), reassigns node directions by position relative to the tile's bounds center, and searches for an entrance node pair that aligns with the exit pair (up to 10 attempts). On success it links the nodes, marks the cell occupied, grows the camera bounds, and queues the new room's other exits (sorted by direction).
  4. **Fill phase**: while exits remain open, the first open exit facing **Right** gets the `bossRoom` and every other exit gets an `empty` cap.
  5. If the fill phase found no open right-facing exit for the boss room, the dungeon would have no exit, so `ClearDungeon` destroys the layout and `BuildDungeon` tries again (up to 5 attempts, logging a warning each time). The dungeon level only goes up once.
  6. It moves the player to the `EntryPoint` inside `startRoom`.
- **Room mix** (2026-10-01): `DungeonManager.EnemyRoomTarget` and `LootRoomTarget` set how many enemy and loot rooms a run aims for.
  - Enemy rooms: `EnemyRoomBaseCount` (4) plus 2 per level after the first, at most 40% of `DungeonSize`.
  - Loot rooms: `LootRoomBaseCount` (3) plus 1 every second level, at most 20% of `DungeonSize`.
  - `DungeonGenerator.RoomWeight` multiplies a rule's weight by `overTargetWeight` (0.1) once that room type has reached its target. While a type is short and the expansion steps left barely cover what's missing, it multiplies by `catchUpWeight` (10) instead.
  - A layout that still misses a target is rebuilt like one without a boss room; the last attempt is kept if it has an exit.
  - Why both directions: the rules weight loot and enemy rooms 25 each against 5 for corridors, so dungeons rarely lacked them, but a 40-room dungeon had about 14 loot rooms. A rough simulation of the room-type chain, which ignores geometry, gives about 5 enemy and 4 loot rooms at level 1 and about 13 and 8 at level 4, with targets met in essentially every run.
- `bossRoom` contains an `ExitPoint` back to `Level0`, the **Bandit Chief** in the lower corridor between the entrance and the exit, and an inactive `RewardChest` (a `LootChest`) that appears when the boss dies. The chest is inside a `Room`, so its loot is rolled fresh each run.
- Enemy rooms contain an `EnemyGenerator`, and loot rooms contain a `LootChest`.
- `DungeonGeneratorEditor` adds Inspector buttons to step the generator manually in the Editor.

## Prefab map (which script lives where)

| Prefab | Scripts |
|---|---|
| `Hero Knight - Pixel Art/Demo/HeroKnight.prefab` | Hero, PersistentPlayerHealth, CharacterStats, GameRespawn, SensorManager, WeaponSensor, Sensor_HeroKnight x5 |
| `Prefabs/InterfaceGraphics/InventoryGraphics/InventoryManager.prefab` | InventorySystem, EquipmentSystem |
| `.../InventoryGraphics/InventoryUI.prefab` | InventoryUI, EquipmentUI |
| `.../ShopGraphics/ShopUI.prefab`, `.../LootGraphics/LootUI.prefab` | ShopUI, LootUI |
| `.../QuestGraphics/DialogCanvas.prefab` | DialogSystem |
| `.../FadeCanvas.prefab` | FadeTransition |
| `Prefabs/Scening/PauseMenuCanvas.prefab`, `UI.prefab`, `Main Camera.prefab`, `ExitPoint.prefab` | PauseMenu, Healthbar, CameraFollow, LevelTransition |
| `Prefabs/WorldStateItems/ItemDatabase.prefab`, `WorldStateManager.prefab` | ItemDatabase, WorldStateManager |
| `Prefabs/NPCs/Merchant.prefab` | NPC |
| `Prefabs/Enemies/Bandit.prefab` | Enemy, Health, CharacterStats, NPC, FloatingHealthBar |
| `Prefabs/Enemies/Spiketrap.prefab` | Enemy (isTrap), CharacterStats |
| `Prefabs/Enemies/BanditChief.prefab` (variant of Bandit) | + BossEnemy |
| `Prefabs/Enemies/Goblin.prefab`, `Mushroom.prefab`, `Skeleton.prefab` (variants of Bandit) | same as Bandit, own art, animator and stats |
| `Prefabs/Environment/LootChest.prefab`, `Respawn Stone.prefab` | Chest (Cainos) + LootChest, RespawnCheckpoint |
| `Prefabs/LevelGeneration/EnemyGenerator.prefab`, `Node.prefab`, `Rooms/*` | EnemyGenerator, Node, Room + Nodes |

Tags in use: `Player`, `Enemy`, `NPC`, `Sensor`. Layers: `Ground`, `Player` (must be layer 6 for i-frames), `Enemy`, `NPC`, `Sensor`, `Weapon`.

## How to extend

- **Add an item**: append to `items.json` with a unique `id`, a `type` that matches the `ItemType` name (for example `"Weapon"`; weapons also get a `weaponType` such as `"Dagger"`), a `spriteName` that exists in `Resources/Sprites` (or a sheet sub-sprite), `price`, `isSellable`, and effects. Use only supported `effectType`s, and put the matching `{placeholder}` in the description.
- **Add a status effect type**: add it to `StatusEffectType`, give it a tint in `StatusEffects.TintFor`, add its damage and duration fields plus the `switch` cases in `CharacterStats` (`AddStatusEffectBonus`, `ApplyOnHitEffects`), and map `<Name>Damage` / `<Name>Duration` in `RuntimeItem.ConvertCharacterStatsEffects`. The tooltip placeholders are `{<name>Damage}` / `{<name>Duration}`, with the name lower-cased.
- **Add an effect type**: subclass `ItemEffect<CharacterStats>` or `ItemEffect<Health>` in `Items/Effects/` (implement `AdjustDescription`, `ApplyEffect`, `RemoveEffect`, `UseItem`), then add a `case` in `RuntimeItem.ConvertCharacterStatsEffects` / `ConvertHealthEffects`. New stats need fields and `Total*` properties in `CharacterStats`.
- **Save something new**: add a public field to `PlayerSaveData` (or use `WorldStateManager` flags for quest-style state), fill it in `SaveSystem.CapturePlayer`, and apply it in `SaveSystem.RestorePlayer`. Only use JSON-friendly types (no `Vector3` or Unity objects). Missing fields in older saves load as defaults, but renaming a field loses its data: bump `SaveSystem.CurrentVersion` and migrate instead.
- **Add an NPC behavior**: subclass `NPCInteractionBehavior` with `[CreateAssetMenu(menuName = "NPC/Behaviors/...")]`, create the asset under `ScriptedItems/NPCBehaviors/<NPC>/`, and assign it to an NPC slot. Use `WorldStateManager` bools with unique, descriptive keys (for example `Merchant_Amulet_Given`) for one-time actions.
- **Add an enemy from a sprite pack**: follow the monster setup (see "Combat and enemies"). Import the sheets at 32 pixels per unit with the pivot at the feet, make clips and an Animator Override Controller of `LightBandit_AnimController` (the attack clip needs a `DealDamage` event), then make a prefab variant of `Bandit` with that controller, a fitted collider and feet width, `spriteFacesRight` set to match the art, and its stats. Finally add it to an `EnemyGenerator` list (`enemyRoomLR`) with a weight and `minDungeonLevel`.
- **Add a boss**: make a prefab variant of an enemy, add `BossEnemy`, set its patrol points to none, and place it inside a room prefab next to that room's `ExitPoint`. Add an inactive `LootChest` to the room for a reward. Scaling and rewards are Inspector fields on `BossEnemy`.
- **Change a weapon's look**: edit its shapes in `WEAPONS` in `Tools/HeroWeapons/hero_weapon_sheets.py`, rerun the script, and check the frames with `--preview`. A new weapon type look also needs a field on `Hero` and a case in `Hero.UpdateWeaponLook`.
- **Add a room**: create a 12x12 `.tmx` in `Assets/Sprites/Tilesets/` using `2D-Platformer-Tileset.tsx`. Make a prefab in `Prefabs/LevelGeneration/Rooms/` with the imported map, a `Room` component, and paired `Node`s at each opening, placed exactly where neighbouring rooms' nodes will sit. Then add it to `RoomGeneration.ruleEntries` in `RoomGenerator.unity`, both as an option under existing room types and as a source type with its own directions.
- **Add enum values at the end only.** `RoomType`, `ItemType`, `NPCAction`, `NodeShouldGoTo` and `HeroStates` are serialized as integers in scenes, prefabs and assets, so inserting a value in the middle silently remaps existing data.

## Conventions and gotchas

- Code style mostly follows Unity conventions, with some inconsistency: `m_` fields inherited from the HeroKnight demo in `Hero.cs`, camelCase method names in places (`handleInput`, `isGrounded`, `startState`), PascalCase elsewhere. Public fields and `[SerializeField] private` fields are used for Inspector wiring. There are no namespaces except `Assets.Scripts.IEntity`. Some files start with a UTF-8 BOM, and git warns about LF/CRLF. Match the surrounding file.
- **Misspellings are load-bearing**: `ItemVarients/`, `numberOffFlashes`, `Invunerability`, `Merchent*` asset names. (Item names in `items.json` aren't keys, since saves and shops use ids, so those were fixed.) Serialized field names are the keys Unity uses in YAML, so renaming a serialized field loses its Inspector values unless you add `[FormerlySerializedAs("oldName")]`. Renaming or moving scripts is safe only if the `.meta` file (GUID) moves with it.
- Two ScriptableObject files don't match their class names: `DIalogBehavior.cs` contains `ShowDialogBehavior`, and `CompositeBehavior.cs` contains `CompositeNPCBehavior`. Unity expects them to match for ScriptableObject assets, so if those assets show "missing script" after the Unity upgrade, this is why.
- When editing `.unity` or `.prefab` YAML directly, references are `{fileID, guid}` pairs. Look up the GUID in the matching `.meta` file. Prefer telling the user what to change in the Editor over hand-editing complex scenes.
- `ItemLoader` reads `StreamingAssets/items.json` with `File.ReadAllText`. That works on desktop, which is the current build target, but not on Android or WebGL, where StreamingAssets must be read with `UnityWebRequest`.
- Lookups by name or tag are fragile: `"Player"` tag, `"EntryPoint"` GameObject, `"InteractKey"` and `"Sprites/..."` Resources paths, sensor child names, and UXML element names.
- Commit messages in this repo are short, lowercase, present-participle summaries ("implementing chest UI", "fixing camera follow in dungeon").

## Status: what's done vs. missing

**Working, or mostly working:**
- Movement, wall slide and wall jump, roll, 3-hit combo, block with a shield
- Health with armor, i-frames, HUD, enemy health bars
- Death, respawn, checkpoints, pause menu, fade transitions
- Merchant quest-style intro, dialog system, world-state flags
- Inventory and equipment with stat effects
- JSON item database
- Shop UI (buy and sell with working gold since the 2026-09-29 fixes)
- Loot chests: open, browse, Take or Take All (finished 2026-09-29), with loot that improves with dungeon depth
- Procedural dungeon with enemy, loot and parkour rooms that grows each run
- Save and load, status effects (bleed, poison, burn), shield, mana, magic power and agility stats (added 2026-09-29)
- Every item has an icon and a price, and can be found in the shop or in chests (2026-09-29)

Everything added on 2026-09-29 compiles. The first play-test (2026-09-30) found the bugs listed under "Play-test bugs" in the audit backlog; equipment, the shop and the inventory windows are **not reliable** until they're fixed.

**Unfinished, where work stopped in May 2025:**
- One boss, the Bandit Chief, which is a stronger Bandit with the same AI. There is no boss-specific attack pattern.
- Enemies: Bandit, Goblin, Mushroom, Skeleton, Spiketrap, and the Bandit Chief boss. They all share one walking melee AI. The Flying Eye and the Pet Cats pack are unused.
- No meta-progression, no win condition, and no story beyond the "Dark Lord" line.
- Unity Behavior and NavMesh packages are installed but unused. The commit history shows they were tried and dropped in favour of the transform-based `Enemy` AI.

## Audit backlog (2026-09-29)

Found by reading the code after the Unity 6000.6 upgrade; **nothing here has been play-tested yet**. Items are grouped the way they are committed on the `unity-6000.6-upgrade` branch. Tick an item and note its commit when it lands; add new findings here instead of losing them.

### 1. Gameplay logic bugs
- [x] **Gold never changes.** In `InventorySystem.UpdateGold(int gold)`, `gold += gold;` updates the parameter, not the field, so buying is free and selling pays nothing.
- [x] **Loot roll always returns the last entry.** `LootInventory.GetLoot` doesn't stop at the first match, and `randomValue <= currentChance` stays true for every later entry.
- [x] **Dead entities keep taking damage.** `Health.TakeDamage` has no "already dead" guard. Below the fall threshold, `GameRespawn` deals 999 damage every `FixedUpdate`, so `PersistentPlayerHealth` starts a new death coroutine each physics step and re-forces the pause menu. Enemies re-trigger `Die()` and the `Death` animation on every hit.
- [x] **`EquipmentSystem.ApplyInitialStats` waits on `&&` instead of `||`.** It continues as soon as either the stats or the health component exists.

### 2. Returning to Level0 duplicates persistent objects
- [x] `InventoryUI`, `ShopUI` and `LootUI` check `GetComponent<Self>() != this`, which is never true, so they're never de-duplicated. Every return to Level0 adds another copy of each UI, all listening for input. `LootUI` checks for `InventoryUI` (copy-paste).
- [x] `Hero.Start` grants the starting kit. The duplicate hero in the reloaded Level0 is only destroyed at the end of the frame, so its `Start` may re-grant items and re-apply equipment stats to the persistent player. Guard it so only the persistent hero initialises.
- [x] `LevelTransition.OnSceneLoaded` moves `FindGameObjectWithTag("Player")`, which can be the doomed duplicate instead of the persistent player. `WeaponSensor` finds its hero with `FindAnyObjectByType<Hero>()` instead of its parent.
- [x] **Respawn point after a scene change**: `GameRespawn.startingPosition` is recorded once, in Level0. Dying in the dungeon without a checkpoint teleports the player to Level0 coordinates. Use the `EntryPoint` the player was placed at.
- [x] `LevelTransition` can fire more than once while its fade runs (no re-entry guard).

### 2b. Health bars
- [x] **Every enemy threw a `NullReferenceException` on spawn.** `FloatingHealthBar` inherits `Healthbar.Start`, which wrote the *player's* HP into `healthText`, and enemy bars have no `healthText`. The exception also stopped the enemy's breakpoints from being drawn. `Healthbar` now reads its assigned `entityHealth`, or the persistent player when none is set (the HUD). `healthText` is optional.
- [x] HUD breakpoints were built only once in `Start`. `AddMaxHealth` (the only refresh path) is never called, because `HealthEffect` changes `bonusHealth` directly, so equipping HP gear left stale markers. The bar now rebuilds its breakpoints whenever max HP changes, and clears its `markers` list. The now-unused `PersistentPlayerHealth.Healthbar` reference was removed.
- Note: enemy bars now draw their breakpoints, but **drawn in the wrong place** (see "Play-test bugs", P1).

### 3. Enemy AI
- [x] **Patrol coroutines stack.** `StopCoroutine(Patrol())` creates a new enumerator and stops nothing, and a new `Patrol()` starts each time the player leaves detection range. Called every frame while chasing, it also allocates.

### 4. Dungeon generator
- [x] **Failed tiles are left in the scene at the origin.** `Destroy(newTile)` destroys only the `Room` component, not the tile GameObject, and `newTile = null; DestroyImmediate(newTile);` destroys nothing. This is the real cause of the "tiles spawning on vector zero - still buggy" commit.
- [x] Possible `NullReferenceException`: at the end of `SpawnTile`, `bestEntrance.pairedNode` is dereferenced even when no entrance matched (fill-phase boss room).
- [x] `SpawnTile`'s `isBossTile` parameter is unused.
- [x] A tile that matched on the 10th attempt was still thrown away (the `checkTime == 10` branch returned before checking for a match), leaving a correctly placed but unregistered tile in the scene.
- [x] Fill-phase tiles (empty caps, boss room) were never added to `occupiedTiles`, so two open exits facing the same cell could stack fillers there, for example a wall block on top of the boss room.

### 5. UI: event leaks and the unfinished loot window
- [x] Inventory, shop and loot UIs subscribe with a lambda but unsubscribe the method group, so they're never unsubscribed. `OnDisable` can throw if it runs before the subscription coroutine. `EquipmentUI` subscribes twice, so `UpdateUI` runs twice per change.
- [x] **Loot window can't take items.** `TakeAllButton` and `TakeSelectedButton` exist in `LootUI.uxml` but aren't wired up. The grid also doesn't refresh once the chest is empty. Now wired up: Take moves the selected item into the inventory, Take All moves everything and closes the window, the buttons are only enabled when they can act, and emptied chests stay open.
- [x] Dead code: unused `Label tooltip` locals in `Start`, and the unused `gridScrollView` plus wheel handler in `InventoryUI`.

### 6. Refactor: shared item-grid and tooltip code
- [x] `SetupTooltip` (~45 identical lines) and the item-grid builder are copy-pasted across `InventoryUI`, `EquipmentUI`, `ShopUI` and `LootUI`. Extract a shared helper.

### 7. Performance
- [x] `Hero.Update` calls `GetComponent<SpriteRenderer>()` every frame with input, box-casts `isGrounded()` twice for the same animator bool, and allocates a new `IdleState` every frame during dialog. `Hero.FixedUpdate` computes an unused `BoxCast`.
- [x] Animator parameters are set by string every frame. Cache `Animator.StringToHash` ids in `Hero`, `HeroState` and `Enemy`.
- [x] `DialogSystem` types by `text += letter` (a new string per character) and allocates a `WaitForSeconds` per character. Use TMP `maxVisibleCharacters` and a cached wait.
- [x] `Healthbar.Update` rebuilds the HP text string every frame. Only update it when the value changes.
- [x] ~~`ItemDatabase.GetItemById` linear scan, `RuntimeItem.SetSprite` re-loading a sheet per item~~: **not worth changing.** Both are one-time startup work or rare lookups over ~22 items. Revisit if the item list grows large.

- [x] **The hero kept sliding during dialogs.** `Hero.Update` returns early while a dialog is open, but `m_horizontalInput` kept its last value and `FixedUpdate` kept applying it. It's now zeroed.

### 8. Item data (`StreamingAssets/items.json`)
- [x] Item 21 has a lowercase `"accessory"` type, and "Oakwood Shield" (id 4) is typed `Armor` although it carries `Block`.

### 9. Cleanup and repo hygiene
- [x] Unused `using System.Dynamic;` and `using Unity.VisualScripting;` in `Healthbar.cs` (removed with the 2b rewrite).
- [x] `PlayerSpawnManager` is unused, since nothing attaches it. Deleted.
- [x] `UserSettings/` is per-user editor state (layouts, search settings) and is tracked. Untrack and ignore it, like the standard Unity `.gitignore` does, and add build-output ignores.

### 10. Input hand-off and invincibility frames (found on the second pass)
- [x] **One key press could be handled twice.** Update order between `DialogSystem`, `Interactable` and `Hero` is undefined, so:
  - the E press that closed a dialog could re-open the interaction at once, so a checkpoint's "saved" dialog could keep coming back
  - the E press that opened a dialog could skip its first page's typing
  - the click that closed a dialog could swing the sword

  `DialogSystem.InputConsumedThisFrame` (true on the open and close frames) is now checked by all three.
- [x] **Invincibility frames didn't block enemy attacks.** They only ignored Player/Enemy collisions, but `Enemy.DealDamage` calls `TakeDamage` directly. `Health` now ignores damage while the player is flashing.

### 11. Save system prerequisites
- [x] **Starting equipment bonuses were applied twice.** `EquipmentSystem.ApplyInitialStats` waits for `Hero.Start`, then applied the effects of *whatever was equipped by then*, which is the starting kit `Hero.Start` had just equipped (and applied) through `EquipItem`. Players effectively started with 25 damage, 55 armor and 140 max HP instead of 20, 30 and 120, and unequipping only removed one copy. It now applies only the items that were assigned in the Inspector when it started.

- [x] **Save/load implemented** (see "Save and load" under Architecture). It replaces the old `WorldStateManager` file I/O: `Awake` overwrote `worldstate.json` with empty state on every launch, `Load()` was never called, and `Load()` would have thrown anyway, because `WorldStateData` had no parameterless constructor.

- [x] `ShowDialogBehavior` and `GiveItemBehavior` cached their world-state flag in public ScriptableObject fields (`HasShownDialog`, `HasGivenItem`). That state is shared by every user of the asset and persists in the Editor. It's now a local, read from `WorldStateManager` each time.

### 12. Status effects
- [x] **Bleed, poison and burn are real damage over time** (see "Combat and enemies"). Before, `BleedDamage` silently added flat damage, `BleedDuration` did nothing, and the poison and burn weapons had no effect at all. Their tooltips showed raw `{poisonDamage}` / `{burnDamage}`.
- [x] Weapons whose descriptions promise an effect now have one. The values are tunable in `items.json`:
  - Elven Longbow: poison 3/s for 5s
  - Staff of Frostbite: poison 3/s for 4s. The description says poison despite the frost name.
  - Venomfang Dagger: poison 4/s for 6s
  - Emberfang: burn 6/s for 3s
  - Giant's Cleaver: bleed 5/s for 4s
  - Crimson Blade keeps bleed 5/s for 5s but loses the +5 flat damage the old bleed effect was wrongly adding.
- [x] Falling out of the level used `TakeDamage(999)`, which blocking, rolling or i-frames could ignore, leaving the player falling forever. It now uses `Health.Kill()`.

### 13. Shield, mana and agility effects
Design choices (made by the user): shield is an absorbing barrier, there's a mana foundation without spells, and agility means move speed.
- [x] Shield in `Health`: a recharging shield from equipment plus a temporary shield from consumables. The HUD shows it in the HP text.
- [x] `Mana` component, magic power stat, and an HUD mana bar cloned from the health bar. Mana is saved.
- [x] Agility stat, which adds move speed.
- [x] Item data, all tunable in `items.json`:
  - Oakwood Shield: +20 shield
  - Guardian Ring: +15 shield
  - Potion of Invincibility: 999 temporary shield for 10s (moved from the stats list, where it was ignored)
  - Mana Vial: the unsupported `Buff` became *restore 30 mana*
  - Arcane Robe: +10 magic power
  - Thief's Gloves: +5 agility
  - Ring of Wisdom: "intellect" became +25 max mana and +1 mana/s

  The Mana Vial and Ring of Wisdom descriptions were reworded to match.

### 14. Item content (branch `item-content`)
Before this, most items had no price (unsellable, free in a shop), and only items 18–21 could be obtained in-game. **Not play-tested yet.**
- [x] **Icons** (`97c4d5e`): the 14 items without an icon got one from `RPG Icons Pixel Art` (see "Items"). `RuntimeItem.SetSprite` now tries a standalone sprite before a sheet sub-sprite, which the Oakwood Shield's icon needed.
- [x] **Prices and tooltips** (`64038cb`): every item has a price (potions 8–60, gear 20–130) and can be sold, except the merchant's quest amulet (id 21). Tooltips now list every stat the item gives. "Lether Armor" and "Merchents Amulet" became "Leather Armor" and "Merchant's Amulet".
- [x] **Shop stock** (`e6fbc76`): limited stock works and is saved (see "Inventory, equipment, shop, loot"). The merchant sells one of each of six pieces of gear besides the potions.
- [x] **Loot by depth** (`37220c5`): loot tables unlock groups by dungeon level, the one table covers every item, and placed chests keep their contents instead of re-rolling on every visit (the village chest was a free item farm).
- Balance is a first pass: prices, stock and the dungeon levels in `LootInventory_0` are all tunable without code.

### 15. Scene transitions (branch `scene-transitions`)
- [x] `LevelTransition` ran its fade coroutine on the exit object, which the scene load destroys, so the fade back never ran and `FadeTransition.FadeBack` patched over it. The transition now lives on the persistent `FadeTransition.LoadScene`, `FadeBack` is gone, and the re-entry guard is global, so an exit trigger the player spawns inside can't start a second transition. The unused `LevelTransition.spawnPoint` field was removed.

### 16. Boss (branch `boss`)
Design choice (made by the user): a Bandit Chief built from the existing Bandit art.
- [x] `BossEnemy`, the `BanditChief` prefab variant, and the boss room wiring (see "Combat and enemies"). `Health.Died` is a new event for anything that needs to react to a death.
- [x] `LevelTransition.Lock`/`Unlock` for exits that need a condition.
- [x] **Enemies floated up toward a jumping player**, because they moved toward the player's full position with `transform.position`. They now move horizontally only, and patrol arrival checks only x.
- [x] Patrol with no patrol points left the enemy playing its run animation after a chase.
- The prefab and room edits were made in YAML. **Open `bossRoom` and `BanditChief` in the Editor to check placement**: the boss is at local (8, -10.9) and the chest at (5.5, -11.06), on the corridor floor.

### 17. Input System migration (branch `input-system`)
Design choice (made by the user): migrate with the same keys and feel, no gamepad yet.
- [x] `GameInput` replaces all 13 legacy `Input` calls in `Hero`, `HeroState`, `DialogSystem`, `Interactable`, `PauseMenu` and the inventory, shop and loot UIs. `Horizontal` reproduces the Input Manager's smoothing (sensitivity 3, gravity 3, snap, dead zone 0.001) from `ProjectSettings/InputManager.asset`.
- [x] Blocking now ends whenever the right button isn't held, instead of on the frame it's released, so a release missed during a skipped frame can't leave the hero stuck blocking.
- [ ] **For the user:** play-test movement feel, then switch *Active Input Handling* to *Input System Package (New)*. Gamepad support means adding bindings to `GameInput`'s properties (for example `Gamepad.current?.buttonSouth`).

### Play-test bugs (reported 2026-09-30, not fixed yet)
Found by the user in the first play-test of everything above. Each entry has the symptom, then what the code confirms or what is only suspected. **P2–P5 were fixed together by the inventory and equipment refactor** (2026-10-01, see "Planned features").

- [x] **P1. Enemy health bar breakpoints are drawn in the wrong place** (fixed in `a37dcd2`: markers are anchored and stretched to the fill, and the Bandit's `breakpointEveryX` is 25). The markers start in the middle of the bar and run past its right end, as a dense black comb (19 markers at every 5 HP on a 100 HP Bandit, 4 on the Bandit Chief).
  - Likely cause: `Healthbar.CreateBreakpoint` sets `localPosition.x = normalizedPos * healthBarFill.rect.width`, which assumes x = 0 is the fill's left edge. That holds for the HUD bar but not the enemy bar (`InterfaceGraphics/Healthbar.prefab`), whose fill appears to be centered. Position markers from `rect.xMin` or with anchors instead.
  - Also raise the Bandit's `breakpointEveryX` from 5 (for example to 25).
- [x] **P2. Gear swaps never removed the old item's stats.** Fixed: `EquipItem` removes the previous item's effects. Confirmed in code: `EquipmentSystem.EquipItem` → `Swap` puts the old item back in the inventory but never calls its `RemoveEffects`. Equipping Thief's Gloves over Leather Armor therefore kept the armor's +20 armor and +10 HP and added the gloves' stats on top.
  - Thief's Gloves are typed `Armor` in `items.json`, so they replace the body armor. Decided: gloves, helmets and body armor get separate slots in the refactor.
- [x] **P3. The same item could be equipped again and again, stacking its stats** (fixed: items are shared objects, so with P2 even swapping between two copies stacked; `EquipItem` now requires the item to be in the inventory and removes the old copy's effects) (agility made the hero very fast). Confirmed: equipping an item into the slot it already occupies re-applies its effects. It was only possible because the inventory window kept showing the gloves after they were equipped (P5), so they could be clicked again.
- [x] **P4. Unequipping the armor also unequipped the sword, shield and amulet, and all of them vanished.** Addressed by the rewrite, not explained: `EquipmentUI` now registers one callback per slot at startup, and the windows redraw from the systems. If it happens again, check the console for the new Singleton warning. The armor slot then showed the gloves. **Cause not found.** `EquipmentUI` binds one click callback per slot and `UnequipItem` only touches one slot, so something else is involved.
  - Leads: `UnequipItem` raises `OnEquipmentChanged` (which re-registers every slot's callbacks in `UpdateSlot`) while the click is still being dispatched, and it raises it before `RemoveEffects`.
  - The "vanished" part matches P5: the unequipped items probably went into an inventory the window isn't showing.
- [x] **P5. The inventory and shop windows showed stale data.** Addressed, root cause still unconfirmed: no window keeps a copy of the inventory any more, all of them redraw on open and after each action, events can't be cut short by a failing listener, and a replaced system now logs "No InventorySystem found, so an empty one was created". Also found: `InventoryManager.prefab` still pointed its old equipment fields at three ScriptableObject items that no longer exist; those fields and `ApplyInitialStats` are gone. Symptoms:
  - After buying, the shop's gold label went down (80 → 20 → 0), but the bought item stayed in the shop grid and never appeared in the player's grid. The inventory window (I) still showed 80 gold.
  - Reopening the shop made the bought item disappear.
  - Clicking the stale Elven Longbow slot threw `ArgumentOutOfRangeException`. The grid's click handler indexes `shopInventory.items`, which `RecordPurchase` → `SetItems` had already shortened, so this is a consequence of the stale grid, not a separate bug. The purchase itself had gone through, which is why the gold was spent.
  - Suspected cause: the windows are displaying a different `InventorySystem`/`EquipmentSystem` (or a visual tree) than the one that changed. Possibilities:
    - a duplicate UI or system surviving a return to Level0 (the test went village → dungeon → village)
    - a `UIDocument` rebuilding its tree after the scripts cached element references
    - a refresh that throws partway

    Check with a debugger or logs, starting with `InventorySystem.Instance` versus each window's cached `inventory`/`playerInventory`, and how many `InventoryUI`/`ShopUI` objects exist after a return to Level0.
- [x] **P6. The HUD mana bar shows a red heart with "999"** (fixed in `a37dcd2`: new `Healthbar.healthIcon` reference, and the copy hides both). Confirmed: `Healthbar.CreateManaBar` clones the whole HP bar, including the heart icon and the HP text. It only unhooks the text (`healthText = null`), so the text keeps the prefab's placeholder "999". The clone should hide or destroy those children, or show mana in its own text.
- [x] **P7. The hero stays tinted red after being hit by a bleed** (fixed in `fb9f565`: `Health.BaseColor` is recorded at spawn, and both the i-frame flash and `StatusEffects` restore it). Confirmed sequence:
  - `Health.TakeDamage` starts the i-frame flash, which sets the sprite red before its first yield. `Enemy.DealDamage` then calls `ApplyOnHitEffects`, which adds `StatusEffects` to the hero.
  - `StatusEffects.Awake` records the sprite's *current* color (red) as the base color, and every flash end resets the sprite to it.
  - Fix: capture `Color.white` or the prefab color, not the current color, or keep the base color outside the flash. The same can happen to enemies hit during a flash.

### Movement problems (reported 2026-09-30, reworked 2026-10-01 on branch `playtest-fixes`, not play-tested yet)
The user found reaching upper platforms very hard: it took 5-6 wall jumps, there's no jump across from a wall, and the hero can get stuck hanging with no way to jump. **The user wants all movement issues reworked.** What the code shows (`HeroState.cs` `JumpingState`, `Hero.FixedUpdate`):
- [x] **P8. Wall jumps never pushed away from the wall.** Fixed by the wall-jump steering lock (see "Player"). What was wrong:
  - `JumpingState.Jump` sets x velocity to `-facing * JumpModifierX` (32), but `Hero.FixedUpdate` overwrites x with `horizontalInput * TotalMoveSpeed` on the next physics step. Only `Roll` and `Dead` are exempt (`noMovementStates`).
  - The push therefore lasts one step. With the key held toward the wall, a wall jump only goes up the same wall, which is why reaching a ledge takes a chain of wall jumps followed by steering onto it while falling.
  - `gravityScale = 5f` in the same branch is dead code, overwritten two lines later.
- [x] **P9. Stuck hanging on a wall or ledge, unable to jump.** Fixed: walls are slid down instead of stuck to, jumping works at any time, corners are ledges to grab, and the hero is frictionless. What was wrong:
  - After 1 s in `JumpingState` (`m_wallCooldown > 1`), touching a wall sets the velocity to zero every frame, so there's no slide and the hero hangs indefinitely.
  - Jump input is only read in the `else if` branch that runs while the cooldown is ≤ 1 s. Once the hero has hung (or fallen) for more than 1 s, jumping is impossible until grounded. Chained wall jumps only work because each resets the cooldown.
  - Platform **corners count as walls**: `Hero.onWall()` box-casts the whole collider 0.1 units in the facing direction. A falling hero that catches a platform's side edge hangs there, as in the user's screenshot of the hero stuck on a platform corner.
- [x] **P10. The hero may have dropped out of `JumpingState` right after takeoff** (fixed: landing needs a non-rising velocity) (suspected, depends on frame rate). `JumpingState.handleInput` returns to Idle as soon as `isGrounded()` is true, which is a 0.05-unit box-cast down. At high frame rates, several `Update`s can run before the physics step moves the hero, so the state flips back to Idle mid-air. Wall sliding and wall jumping then only happen through `IdleState` → `JumpingState` on a later Space press.
- [x] **P11. Jump height is fixed at about 4 tiles.** Kept: `m_jumpForce` 9 gives an apex of about 4.1 units, but a ledge grab at the apex now reaches platform tops about 5.4 units above the takeoff floor (the hands are at the collider's top, 1.26 above the feet). `CharacterStats.TotalJumpHeight` is still unused. Raise `m_jumpForce` if platforms still feel out of reach.
- [x] **P12. Missing platformer basics.** Done: coyote time, jump buffering, variable jump height, a controlled wall slide, one wall-detection method. Not done, by design choice: air attacks and double jump. The list was:
  - coyote time and jump buffering
  - variable jump height (release to cut the jump)
  - a controlled wall slide instead of a full stop
  - air attacks (every state transition goes through Idle)
  - a single wall-detection method: `Hero` computes `m_isWallSliding` from the four wall sensors but never uses it, while `JumpingState` uses the box-cast
- [x] **P13. The pause menu's New Game button called `OnRespawnClicked`** instead of `OnNewGameClicked` (fixed in `5d7956e`).

### 18. Found while fixing the play-test bugs (2026-10-01)
- [x] **The dungeon could have no exit.** The boss room (the only way back to Level0) was only placed if an open exit facing right was left for the fill phase. If the layout used them all up, the player was trapped. The generator now rebuilds such layouts (see "Procedural dungeon", step 5).

### 19. Monster enemies (branch `monsters`)
Design choice (made by the user): turn the imported Monsters pack into enemies; the Flying Eye later.
- [x] Goblin, Mushroom and Skeleton (see "Combat and enemies"), added to `enemyRoomLR` by dungeon level, plus Tools → Debug → Spawn Enemy.
- [x] **Enemies could turn their backs to the player.** `NPC` toggled `flipX` while `Enemy` set it outright, and the two drifted apart. `Enemy` now owns facing and also faces the player while standing in attack range.
- [x] `EnemyGenerator` entries have a `minDungeonLevel`.
- **Local vendor change:** the used monster sheets' `.png.meta` files have a new pixels-per-unit value and pivot. Re-importing the pack from the Asset Store would reset them.
- **Not play-tested yet.** Things to check: sizes against the hero, the collider and health-bar fit, whether hits land on the attack frame, and walking direction.

### 20. Weapon types (branch `weapon-types`)
- [x] `WeaponType` per weapon, with speed, reach, two-handed and projectile profiles, arrows and mana-costing magic bolts, and the shield rule (see "Player attacks").
- [x] **A combo could only damage an enemy once**, because `WeaponSensor` allowed one hit per attack state, and hits depended on trigger callbacks. Hits are now a query at each swing's strike.
- [x] One click could play two swings: the press that started the attack was also read as the next swing, and the old code did the same. Presses are ignored on the frame a swing starts.
- [x] Clicking a menu button while paused no longer starts an attack.
- **Not play-tested yet.** Tune strike timing (0.18 s), swing times and reach against how the swings look, and check projectile speed, range and sprites.

### 21. Dungeon room mix (branch `dungeon-mix`)
- [x] `EnemyRoomBaseCount` / `LootRoomBaseCount` finally do something: they drive per-level room targets that the generator steers toward (see "Procedural dungeon"). Tunable on `DungeonManager` (no prefab, so change the defaults in code) and on the generator in `RoomGenerator.unity`.
- **Not play-tested yet.** Check the Console for "short of the targets" warnings and how loot-heavy runs feel.

### 22. Hero weapon art (branch `hero-weapon-art`)
- [x] Dagger, Bow, Staff and Wand copies of the hero sheet and animations, swapped by weapon type (see "Hero weapon looks" near the top).
- [ ] **Shield overlay**: cut the shield out of the frames into a layer shown only while a shield is equipped, and patch the torso where it overlapped.
- **Not play-tested yet.** Look at every animation with each weapon, and watch for leftover sword pixels or weapons in odd places.

### Needs a design decision (not scheduled)
- More bosses, or boss attack patterns. Monster bosses could now be made like the Bandit Chief, from the monster prefabs. The Skeleton's unused `Shield` and every monster's `Attack2` sheet could give them a block or a second attack.
- A flying enemy (the Flying Eye) needs its own movement AI.

## Planned features

Bigger pieces of work the user has asked for. Each needs a design pass (ask the user) before implementation.

### 1. Movement rework (implemented 2026-10-01, needs play-testing)
Design choices (made by the user): slide down walls slowly, wall jumps always push away, grab ledges and pull up, and add coyote time, jump buffering and variable jump height. No air attacks or double jump. See "Player: `Hero` + `HeroState` FSM" for how it works and P8–P12 for what it fixed. **Play-test checklist:**
- the ledge-hang pose lines up with the ledge (tune `ledgeHangOffset` on the Hero component)
- wall slide speed, wall jump strength and the steering lock feel right
- short hops versus full jumps
- walking off edges, and standing right at a platform's edge
- rolling and dying don't slide oddly with the frictionless collider

### 2. Inventory and equipment refactor
Requested by the user on 2026-09-30. It bundles the inventory play-test bugs with two design changes. **Parts a) and b) were implemented on 2026-10-01** (design choices by the user: Helmet and Gloves slots and two interchangeable accessory slots, no boots, weapon types later). They're described under "Inventory, equipment, shop, loot" and need play-testing. Part c) is still open.

**a) Fix the inventory bugs as one rework** (P2–P5), done:
- Swapping gear must remove the old item's effects.
- Re-equipping the same item must not stack its effects.
- The inventory and shop windows show stale data (gold, items, stock).
- Unequipping one slot emptied others and lost the items.

The rework should make the systems the single source of truth. Each window should redraw from `InventorySystem`/`EquipmentSystem` on every change event and whenever it opens, with no cached lists or indexes that can go stale. Apply and remove effects in one place: an item's effects go on when it enters a slot and come off when it leaves, whether through equip, swap, unequip or load. Add a check that stats return to their base values after unequipping everything, to catch stacking.

**b) One equipment slot per gear type**, done. The original plan: There's only one `Armor` slot today, so the Iron Helm (11), Thief's Gloves (15), Leather Armor (1337) and Arcane Robe (12) all compete for it, and every ring and amulet shares the one `Accessory` slot.
- Candidate slots: weapon, shield (off-hand), helmet, body armor, gloves, boots, and one or two accessories (amulet, ring). The user named helmet and gloves; the rest are to be decided.
- `ItemType` is serialized as an integer, so **append new values at the end** (`Helmet`, `Gloves`, …). Don't reuse `Armor` for body armor under a new name. `items.json` uses the type *names*, so move the helm and gloves to their new types there.
- `EquipmentSystem`'s four hard-coded fields and `UnequipItem`'s if-chain should become a slot → item map, so adding a slot is a data change.
- `EquipmentUI.uxml` has four fixed slot elements (`Weapon`, `Shield`, `Armor`, `Accessory`) plus labels, and `EquipmentUI` queries them by name. It needs the new slots and default icons (the Violet Theme UI `White Icons` folder has more).
- **Saves already work**: `PlayerSaveData.equippedItemIds` is a plain id list and `RestorePlayer` re-equips each through `EquipItem`, so new slots need no save migration.

**c) Weapon types with their own animations.** The mechanics were done on 2026-10-01 (see "Player attacks"; design choice by the user: bow, staff and wand fire projectiles). Still open: animations per type, since the art decision below is still waiting. The original plan: The weapons are already different kinds: Broadsword, Crimson Blade and Emberfang (swords), Venomfang Dagger (dagger), Giant's Cleaver (heavy two-hander), Elven Longbow (bow), Staff of Frostbite (staff) and Spark Wand (wand). All of them currently swing the same sword combo (`Attack1-3`), and the hero sprite always shows the same sword.
- Data: add a weapon type to items (for example `"weaponType": "Sword" | "Dagger" | "Greatweapon" | "Bow" | "Staff" | "Wand"` in `items.json`, mapped to an enum in `RuntimeItem`).
- Combat: the type should choose the attack animation set, attack speed and range (`WeaponSensor` hitbox), and possibly shield compatibility, since two-handers and bows can't be used with a shield. Bows need projectiles, and staffs or wands are the natural first users of the mana foundation (`Mana.TrySpend`, `CharacterStats.TotalMagicPower`).
- **Art was the blocker** (resolved by the weapon-look copies, see "Hero weapon looks"): the Hero Knight pack only has sword animations, with the sword baked into the sprite sheet. Options to discuss with the user:
  - Find a character pack with multiple weapon animations.
  - Draw the weapon as a separate sprite on top of a weaponless body.
  - Keep the sword animations and change only timing, range and effects per type.
- Animator: `AnimatorParams.HeroAttack(n)` and the `AttackingState` combo assume three sword attacks. Per-type animations probably mean an Animator Override Controller per weapon type, which is the approach the Bandit art already uses (`HeavyBandit_AnimController.overrideController`).

## Unity 6000.6 upgrade notes (2026-09-29)

The upgrade from 6000.0.33f1 needed these changes. **Vendor code was patched locally**, so re-importing or updating one of these packs will undo the patch and bring the compile error back.

- **NaughtyAttributes 2.1.4** (latest release, unused by game code): `Object.GetInstanceID()` is now a compile error. It was replaced with `GetEntityId()` in `Scripts/Editor/NaughtyInspector.cs` and `PropertyDrawers_SpecialCase/ReorderableListPropertyDrawer.cs`.
- **Cainos Lucid Editor** (`Assets/Cainos/Third Party/Lucid Editor/Editor/`): the non-generic `TreeView`, `TreeViewItem` and `TreeViewState` are now compile errors. `Experimental/SimpleTreeView.cs` and `TreeMenu.cs` now use the `<int>` generic versions. In `SerializeReferenceDropdown.cs`, `AdvancedDropdownItem.children` became `childList`.
- **Game code**:
  - `Singleton.cs` uses `FindAnyObjectByType`.
  - Unity 6000.6 added a serialization analyzer (UAC1001/UAC1015). Runtime-only fields got `[NonSerialized]`: `DungeonGenerator.occupiedTiles`, `Room.location`, `RoomGeneration.rules`.
  - `WorldStateData` lost its unused `[System.Serializable]`. **Don't add `[NonSerialized]` to its fields**, because Newtonsoft would then skip them and save an empty file.
- **PSD Importer crash**: the JohnFarmer pack's two ~165 MB source files (`*.psb`) crashed the 6000.6 PSD importer (native assertion `m_size < max_size()`). Their folder was renamed to `Assets/JohnFarmer/Keyboard Keys & Mouse Sprites/PSB File~`, and the trailing `~` makes Unity skip it. Nothing referenced them; the game uses the PNGs in `Sprites/KeyboardKeys&Mouse/`. **Don't rename it back.**
- **SuperTiled2Unity** was left at the embedded 2.2.4. It compiles, with warnings about `AssetDatabase.ExportPackage` and `AppDomain.GetAssemblies`. The newest release (2.3.1) doesn't fix those, and 2.3.0 changed tileset texture handling, which could break the room prefabs that are variants of the imported `.tmx` prefabs.
- **Expected harmless noise**:
  - On a full re-import, room prefabs can log "Missing Prefab Variant parent" if they import before their `.tmx` parent. They're re-imported right after the maps.
  - Terrain and HDRP TMP shader "unsupported" warnings.
  - The `TextMesh Pro/Fonts/LiberationSans.ttf.meta` "below the supported minimum" warning, which goes away when that meta is re-saved.
- **How to check compile errors without the Editor**: Unity's compiler arguments are in `Library/Bee/artifacts/1900b0aEDbg.dag/<Assembly>.rsp`. Copy one, point `-out:` and `-refout:` somewhere outside `Library/`, then run `"<Editor>/Data/DotNetSdk/dotnet.exe" exec "<Editor>/Data/DotNetSdk/sdk/<ver>/Roslyn/bincore/csc.dll" -nostdlib -noconfig @file.rsp`. Use `-nostdlib`, not `/nostdlib`, because Git Bash rewrites `/…` arguments into paths.

## Repo hygiene notes

- **Policy: only commit vendor assets the game actually uses.** Vendor packs are imported whole into `Assets/`, but only the files that tracked scenes, prefabs and UI reference (directly or through other referenced assets) get committed, together with their `.meta` and parent-folder `.meta` files. Mostly or fully untracked packs: `Assets/RPG Icons Pixel Art/` (73 MB, unused), `Assets/Violet Theme Ui/` (a few icons and the red progress bar used), `Assets/Imported Assets/` (one icon sheet used, plus the Goblin, Mushroom and Skeleton sheets the monster enemies use), `Assets/JohnFarmer/Keyboard Keys & Mouse Sprites/` (a few key sprites used), and `Assets/NaughtyAttributes/` (unused, carries a local 6000.6 patch).
- **Before committing, check that new references resolve.** A fresh clone only has tracked files, so a scene or prefab pointing at an untracked asset breaks. Resolve the referenced GUIDs (`[0-9a-f]{32}` in YAML and UXML) against `.meta` files, and commit any untracked dependency. The 2026-09-29 baseline did this with a small script that walks references transitively.
- Never commit `Assets/JohnFarmer/Keyboard Keys & Mouse Sprites/PSB File~/` (~335 MB of unused Photoshop source). It is in `.gitignore`.
- `.gitignore` excludes `Library/`, `Temp/`, `Logs/`, `obj/`, `.vs/`, `.idea/`, `UserSettings/` (per-user, untracked since 2026-09-29), build output, `*.csproj`, `*.sln`, and TextMesh Pro Examples. Binary files go through **Git LFS** (see below).
- **Git LFS** (since 2026-10-01, branch `git-lfs`): `.gitattributes` routes images (png, jpg, psd, psb, tga, …), audio, video, 3D models, fonts, archives (including `.unitypackage`), native libraries and PDFs through LFS. Unity's text assets (`.unity`, `.prefab`, `.asset`, `.meta`, `.anim`, …) stay in plain git so they can be diffed and merged; the project uses text serialization, so don't add them to LFS.
  - This clone was set up with `git lfs install --local`, so the hooks live in `.git/hooks` and the filter in `.git/config`. On another machine, install Git LFS and run `git lfs install` once before cloning; without it, the binaries check out as small pointer files.
  - The 111 binaries that were already tracked were converted in one commit (`git add --renormalize`). **History was not rewritten**: commits before that, including the 99 already on GitHub (`origin/main`), still contain their binaries directly. Rewriting them (`git lfs migrate import`) would need a force-push, and the saving is small (about 8 MB of art).
  - The first push after this uploads the LFS objects. Check the GitHub account's LFS storage and bandwidth quota before committing large packs.
  - The used-assets policy above still applies. LFS only makes committed art cheaper to store.
