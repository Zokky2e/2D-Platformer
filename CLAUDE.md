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
- **Input**: all gameplay code uses the **legacy `UnityEngine.Input`** API (`Input.GetKeyDown`, `Input.GetAxis("Horizontal")`). `activeInputHandler: 2` (Both) must stay enabled. Unity 6000.6 logs "Input Manager is marked for deprecation" on startup, so migrating to the Input System package is future work. `Assets/InputSystem_Actions.inputactions` exists but nothing uses it.
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
  Levels/                      Level0.unity (hub), RoomGenerator.unity (dungeon)
  Prefabs/                     Player-facing prefabs (see "Prefab map")
  ScriptedItems/               ScriptableObject assets: NPC behaviors, shop and loot tables
  StreamingAssets/items.json   THE item database (data-driven)
  Resources/                   Loaded by name at runtime: InteractKey, PatrolPoint, Sprites/*
  Sprites/                     UI sprites + Tilesets/ (Tiled .tmx rooms, .tsx tileset, autotile rules)
  UI Toolkit/                  PanelSettings + runtime theme
```

Third-party packs: `Hero Knight - Pixel Art` (the player), `Bandits - Pixel Art` (enemies), `Merchant - Pixel Art`, `Cainos` (village props, including the `Chest` script used by loot chests), `2D Pixel Art Platformer Biome - American Forest`, `RPG Icons Pixel Art`, `JohnFarmer` (keyboard key sprites for tutorial signs), `Violet Theme Ui`, `NaughtyAttributes`, `TextMesh Pro`, and `Imported Assets/` (Monsters Creatures Fantasy, a simple UI pack, and a Pet Cats pack, which were imported but not used yet, probably meant for future monsters and bosses).

**HeroKnight exception:** the player prefab is the vendor's `Assets/Hero Knight - Pixel Art/Demo/HeroKnight.prefab`, modified in place. The vendor's `HeroKnight.cs` controller is not used. The project's `Hero.cs` and friends are attached instead, and only the vendor's `Sensor_HeroKnight` is reused.

## Running the game

- Build scenes: `Level0` (index 0), `RoomGenerator` (index 1).
- **Always enter Play mode from `Level0`.** The player, HUD, dialog canvas, pause menu, inventory/shop/loot UIs, fade canvas, `ItemDatabase` and `WorldStateManager` only exist in Level0 and survive into the dungeon through `DontDestroyOnLoad`. `RoomGenerator` has only the generator, camera and HUD, so starting there logs "Player not found" and `Singleton` getters create empty components that throw null-reference errors.
- Level0 contains the village tilemap (`village.tmx`, 32x16 tiles), the Town Merchant, a loot chest, a Respawn Stone checkpoint, tutorial signs, and an `ExitPoint` whose `nextSceneName` is `RoomGenerator`.

### Controls (all hard-coded in scripts)
| Input | Action | Where |
|---|---|---|
| A/D, arrow keys | Move | `Hero.Update` (`Input.GetAxis("Horizontal")`) |
| Space | Jump / wall jump | `IdleState` → `JumpingState` |
| Left Shift | Roll (damage immune while rolling) | `RollingState` |
| Left mouse | Attack (3-hit combo `Attack1..3`); also advances dialog | `AttackingState`, `DialogSystem` |
| Right mouse (hold) | Block. **Only works if the equipped item has a `Block` effect** (`stats.canUseBlock`) | `BlockingState` |
| E | Interact (NPC, chest, checkpoint), advance dialog | `Interactable`, `DialogSystem` |
| I | Open inventory | `InventoryUI` |
| Esc | Pause menu, or close an open inventory/shop/loot window | `PauseMenu`, `*UI` |

## Architecture

### Singletons and persistence
- `Core/Singleton.cs`: `Singleton<T>` looks up `Instance` with `FindAnyObjectByType` (`FindFirstObjectByType` is deprecated in 6000.6). **If none exists, it creates a new GameObject**, which hides missing-prefab mistakes. `Awake` calls `DontDestroyOnLoad` and destroys duplicates. Subclasses override `protected override void Awake()` and must call `base.Awake()`.
- Singletons: `DialogSystem`, `PauseMenu`, `FadeTransition`, `GameRespawn`, `SensorManager`, `InventorySystem`, `EquipmentSystem`, `ItemDatabase`, `ItemSystem`, `ShopSystem`, `DungeonManager`, `WorldStateManager`. `ItemSystem`, `ShopSystem` and `DungeonManager` have no prefab and are always created lazily by the getter.
- `PersistentPlayerHealth` (on the player) has its own static `Instance` and `DontDestroyOnLoad`. Other code reaches the player through it, for example `PersistentPlayerHealth.Instance.GetComponent<Hero>()` in `CameraFollow`.
- The player GameObject carries **several singletons at once**: `Hero`, `PersistentPlayerHealth`, `CharacterStats`, `GameRespawn`, `SensorManager`, `WeaponSensor` (child `AttackSensor`), plus 5 `Sensor_HeroKnight` children. Their names must stay exactly `GroundSensor`, `WallSensor_R1/R2/L1/L2`, because `Hero.Start` looks them up with `transform.Find`.
- InventoryUI, ShopUI and LootUI do their own `DontDestroyOnLoad` instead of using `Singleton<T>`.

### UI state and pausing
- Opening inventory, shop or loot sets `Time.timeScale = 0`, sets `PauseMenu.GameIsPaused = true`, and sets `CoreUI.IsUIOpen` **one frame later** (`DelayUIFlagClear`) so the same Esc press doesn't also open the pause menu.
- Two UI technologies are in use:
  - **uGUI Canvas**: HUD health bar (`Prefabs/Scening/UI.prefab`), dialog box, pause menu, fade.
  - **UI Toolkit** (`UIDocument` + UXML): inventory, equipment, shop, loot. UXML lives next to the prefabs in `Prefabs/InterfaceGraphics/**`. Scripts query elements by name (`"InventoryContainer"`, `"Items"`, `"Gold"`, `"ExitButton"`, `"ShopItems"`, `"PlayerItems"`, `"SellButton"`, `"BuyButton"`, `"LootContainer"`, `"LootItems"`, `"Loadout"`, `"EquipmentContainer"`, `"Weapon"`…). **Renaming an element in UXML breaks the script silently.**
  - Item grids and tooltips are built in C# by the shared `ItemGrid.Build` (slots, hover, click, selection highlight) and `ItemTooltip` (Show, Hide, MoveTo). Each window keeps its own `UpdateTooltipPosition`, because their layouts need different hand-tuned offsets.

### Player: `Hero` + `HeroState` FSM
- `HeroStates` enum: `Idle, Run, Jump, Roll, Attack, Block, Dead`. `Run` has no state class; running is handled in `Hero.Update` by setting animator `AnimState=1`.
- Each state class (`IdleState`, `JumpingState`, `AttackingState`, `BlockingState`, `RollingState`, `DeadState` in `HeroState.cs`) returns the next state from `handleInput()`, and `Hero.handleInput` swaps it in when the enum differs. **Almost every transition goes through Idle**, so you can't attack in the air, for example.
- Horizontal velocity is set in `Hero.FixedUpdate` from `stats.TotalMoveSpeed`, except during `Roll` and `Dead`.
- Wall mechanics are in `JumpingState`: box-cast `onWall()`, wall slide (velocity zeroed), and a wall jump that uses `jump_modifier_x/y` (32/16 on the prefab). The 1 s `m_wallCooldown` gates wall sticking.
- The hero can't move while `DialogSystem.DialogActive` (it is forced back to Idle).
- **Starting kit** is hard-coded in `Hero.Start`: inventory gets item ids `18, 18, 19, 20` (heal potions), and equipment gets `69, 420, 1337` (Basic Metal Shield, Broadsword, Lether Armor). The easter-egg ids are intentional.
- Animator parameters used: `AnimState`, `Grounded`, `AirSpeedY`, `WallSlide`, `Jump`, `Roll`, `Attack1-3`, `Block`, `IdleBlock`, `Hurt`, `Death`, `Revive`, `noBlood`.

### Stats, health and damage
- `CharacterStats` holds base plus bonus values for move speed, jump height, damage and armor, and a `canUseBlock` flag. `TotalJumpHeight` is **unused**; jumping uses `Hero.m_jumpForce`.
- Armor formula: `damage * (1 - armor / (armor + 50))`, floored (`CalculateDamage`).
- `Health` has `baseHealth + bonusHealth = MaxHealth`, `CurrentHealth`, and delegates to `IEntity` (`Hero` or `Enemy`) for `IsBlocking`, `TakeDamage` (returns the final damage) and `Die`. I-frames with a red flash and ignored Player/Enemy layer collision **only run when the configured player layer is layer 6** (hard-coded check).
- Hero `TakeDamage` returns 0 while in Block or Roll.
- Player defaults: 100 HP, speed 4, damage 15, armor 5. With the starting kit that becomes 120 HP, 20 damage and 30 armor.
- HUD `Healthbar` reads `PersistentPlayerHealth.Instance` and draws a breakpoint marker every 25 HP (`createBreakpoints`, which is re-run when max HP changes).
- Death: `PersistentPlayerHealth` waits 2 s, then force-opens the pause menu, whose Respawn button calls `GameRespawn.RespawnPlayer` (fade, full heal, teleport to the checkpoint or the start position). Falling below `GameRespawn.threshold` (−200 in Level0, −15 on the prefab) deals 999 damage.

### Combat and enemies
- **Player hits**: the `WeaponSensor` trigger on the `AttackSensor` child damages objects tagged `Enemy` once per attack state, using `stats.TotalDamage`. It flips with the hero's facing direction.
- **Enemy** (`Enemy.cs`) implements `IEntity`:
  - When `isTrap` is true it deals damage on trigger enter (Spiketrap).
  - Otherwise it patrols between `patrolPoints`, chases within `detectionRange`, and attacks within `attackRange` in a loop. Damage lands via the **animation event `DealDamage()`**.
  - It moves by setting `transform.position` directly, not through the Rigidbody.
- Enemy prefabs: `Prefabs/Enemies/Bandit.prefab` (100 HP, speed 2, damage 15, armor 5, attackDelay 5.5; it also has an `NPC` component that makes it face the player) and `Spiketrap.prefab`. **There is no boss enemy yet.**
- `EnemyGenerator` does a weighted random pick from `enemies[]` and spawns the enemy plus a single `PatrolPoint` at `spawnPoint`. The `enemyRoomLR` prefab overrides the list with Bandit (20), Spiketrap (5) and others.

### Items (data-driven)
- **Source of truth: `Assets/StreamingAssets/items.json`**. It is loaded once by `ItemDatabase` (a singleton prefab in Level0) through `ItemLoader`, which uses Newtonsoft to parse `ItemData` and then `RuntimeItem.SetData`.
- `Item` (abstract ScriptableObject) holds id, name, description template, sprite, `ItemType` (`Consumable, Weapon, Shield, Armor, Accessory`), price and isSellable, plus four effect lists:
  - `characterStatsEffects` / `healthEffects`: applied on **equip**, removed on **unequip** (`ApplyEffects` / `RemoveEffects`).
  - `onActivateCharacterStatsEffects` / `onActivateHealthEffects`: applied on **use** (consumables are then removed from the inventory).
- The JSON `effectType` strings are mapped in `RuntimeItem.Convert*Effects`:
  - CharacterStats: `Armor`, `Block`, `Damage`, `BleedDamage`, `BleedDuration`.
  - Health: `Health` (max HP), `Heal`.
  - **Unknown types are silently dropped**, for example `Buff` and `Shield` in items 6 and 14.
- Description placeholders are replaced by each effect's `AdjustDescription`: `{bonusArmor}`, `{bonusDamage}`, `{bonusHealth}`, `{healAmount}`, `{bleedDamage}`, `{bleedDuration}`. `Block` appends a paragraph. Other placeholders in the JSON (`{poisonDamage}`, `{bonusShield}`, `{burnDamage}`, `{bonusMagicPower}`…) aren't implemented and show up raw.
- Sprites: `spriteName` is loaded from `Resources/Sprites/<name>`. Names containing `armor`, `clothing` or `potion_red` are looked up as sub-sprites of the `basic_armor`, `basic_clothing` or `potion_red` sprite sheets (for example `basic_clothing_10`). Many JSON items reference sprites that don't exist, which gives them a null icon.
- `ItemSystem.AddToPlayerInventory(int[])` and `AddAndEquipOnPlayer(int[])` are the helpers for granting items by id.
- The old ScriptableObject item workflow (`ItemVarients/*` with `CreateAssetMenu`, `ScriptedItems/HealCollectable`, the `Collectable` pickup) still exists alongside the JSON system. New items should go into JSON.
- Economy: shop sell price is `floor(price * 0.6)`. The player starts with 50 gold (`InventorySystem.gold`).

### Inventory, equipment, shop, loot
- `InventorySystem` (list of `Item` + gold, event `onInventoryChanged`) and `EquipmentSystem` (four slots: weapon, shield, armor, accessory; event `OnEquipmentChanged`) both live on `Prefabs/InterfaceGraphics/InventoryGraphics/InventoryManager.prefab`.
- Clicking an item in the inventory equips it (swapping the old item back into the inventory) or uses it if it's a consumable. Clicking an equipment slot unequips.
- **Shop**: an NPC's `OpenShopBehavior` hands a `ShopInventory` asset (item ids + quantity, where quantity is currently ignored) to `ShopUI`. Selecting an item enables Buy or Sell, which call `ShopSystem.BuyItem` / `SellItem`.
- **Loot**: `LootChest` (on `Prefabs/Environment/LootChest.prefab`, wrapping Cainos' `Chest`) rolls `LootInventory.GetLoot()` once in `Start`. Interacting opens the chest animation and then `LootUI`, where **Take** moves the selected item into the inventory and **Take All** empties the chest and closes the window. Emptied chests stay open. Loot tables are `LootInventory` assets (lists of item-id groups with weights), for example `ScriptedItems/LootInventories/TestLootInventory/LootInventory_0.asset`.

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
  - Attention is `MerchantIntro_Composite`: intro dialog, then give item 21 (Merchants Amulet, +5 damage).
  - Trade is `MerchentTrade_Composite`: "Let's trade." dialog, then open a shop that sells potions 18, 19 and 20.
- `DialogSystem` splits text into **pages on `\n`**, uses a typewriter effect, and advances on E or left click. `ShowDialog(name, text, onClose)`.
- `Interactable` is added at runtime by `NPC`, `LootChest` and `RespawnCheckpoint`. It needs a trigger collider on the object and a player tagged `Player`. It spawns the `Resources/InteractKey` "E" prompt.
- `WorldStateManager` is a set of string-keyed bool, int and string dictionaries saved as JSON to `Application.persistentDataPath/worldstate.json`. It saves on scene load, checkpoint use and dungeon spawn. Only the two merchant bools are used so far. **Inventory, gold, equipment and dungeon level are not persisted** (see "Needs a design decision" in the Audit backlog).

### Scene transitions and checkpoints
- `LevelTransition` (on `Prefabs/Scening/ExitPoint.prefab`) triggers on the player, fades through `FadeTransition.FadeAndExecute`, loads `nextSceneName`, then moves the player to the GameObject named **`EntryPoint`** in the new scene. The prefab default `"Level2"` no longer exists; every instance overrides it.
- `RespawnCheckpoint` (`Prefabs/Environment/Respawn Stone.prefab`) sets `GameRespawn`'s respawn transform on interaction, saves world state, and shows a dialog.
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
  3. `ExpandToMaxDungeon()` repeatedly takes `activeNodes[0]` and calls `SpawnTile`. That computes the neighbour grid cell and world offset (map width and height), skips occupied cells, rolls a weighted room from the rules, reassigns node directions by position relative to the tile's bounds center, and searches for an entrance node pair that aligns with the exit pair (up to 10 attempts). On success it links the nodes, marks the cell occupied, grows the camera bounds, and queues the new room's other exits (sorted by direction).
  4. **Fill phase**: while exits remain open, the first open exit facing **Right** gets the `bossRoom` and every other exit gets an `empty` cap.
  5. It moves the player to the `EntryPoint` inside `startRoom`.
- `bossRoom` contains an `ExitPoint` back to `Level0`. It **contains no boss**.
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
| `Prefabs/Environment/LootChest.prefab`, `Respawn Stone.prefab` | Chest (Cainos) + LootChest, RespawnCheckpoint |
| `Prefabs/LevelGeneration/EnemyGenerator.prefab`, `Node.prefab`, `Rooms/*` | EnemyGenerator, Node, Room + Nodes |

Tags in use: `Player`, `Enemy`, `NPC`, `Sensor`. Layers: `Ground`, `Player` (must be layer 6 for i-frames), `Enemy`, `NPC`, `Sensor`, `Weapon`.

## How to extend

- **Add an item**: append to `items.json` with a unique `id`, a `type` that matches the `ItemType` name (for example `"Weapon"`), a `spriteName` that exists in `Resources/Sprites` (or a sheet sub-sprite), `price`, `isSellable`, and effects. Use only supported `effectType`s, and put the matching `{placeholder}` in the description.
- **Add an effect type**: subclass `ItemEffect<CharacterStats>` or `ItemEffect<Health>` in `Items/Effects/` (implement `AdjustDescription`, `ApplyEffect`, `RemoveEffect`, `UseItem`), then add a `case` in `RuntimeItem.ConvertCharacterStatsEffects` / `ConvertHealthEffects`. New stats need fields and `Total*` properties in `CharacterStats`.
- **Add an NPC behavior**: subclass `NPCInteractionBehavior` with `[CreateAssetMenu(menuName = "NPC/Behaviors/...")]`, create the asset under `ScriptedItems/NPCBehaviors/<NPC>/`, and assign it to an NPC slot. Use `WorldStateManager` bools with unique, descriptive keys (for example `Merchant_Amulet_Given`) for one-time actions.
- **Add a room**: create a 12x12 `.tmx` in `Assets/Sprites/Tilesets/` using `2D-Platformer-Tileset.tsx`. Make a prefab in `Prefabs/LevelGeneration/Rooms/` with the imported map, a `Room` component, and paired `Node`s at each opening, placed exactly where neighbouring rooms' nodes will sit. Then add it to `RoomGeneration.ruleEntries` in `RoomGenerator.unity`, both as an option under existing room types and as a source type with its own directions.
- **Add enum values at the end only.** `RoomType`, `ItemType`, `NPCAction`, `NodeShouldGoTo` and `HeroStates` are serialized as integers in scenes, prefabs and assets, so inserting a value in the middle silently remaps existing data.

## Conventions and gotchas

- Code style mostly follows Unity conventions, with some inconsistency: `m_` fields inherited from the HeroKnight demo in `Hero.cs`, camelCase method names in places (`handleInput`, `isGrounded`, `startState`), PascalCase elsewhere. Public fields and `[SerializeField] private` fields are used for Inspector wiring. There are no namespaces except `Assets.Scripts.IEntity`. Some files start with a UTF-8 BOM, and git warns about LF/CRLF. Match the surrounding file.
- **Misspellings are load-bearing**: `ItemVarients/`, `numberOffFlashes`, `Invunerability`, `Merchent*` asset names, "Lether Armor", "Merchents Amulet". Serialized field names are the keys Unity uses in YAML, so renaming a serialized field loses its Inspector values unless you add `[FormerlySerializedAs("oldName")]`. Renaming or moving scripts is safe only if the `.meta` file (GUID) moves with it.
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
- Loot chests: open, browse, Take or Take All (finished 2026-09-29)
- Procedural dungeon with enemy, loot and parkour rooms that grows each run

**Unfinished, where work stopped in May 2025:**
- No bosses, no boss AI, and the boss room is just an exit.
- Only Bandit and Spiketrap enemies exist. The monster art packs are imported but unused.
- No status effects: bleed, poison and burn are only description text. `BleedDamage` actually adds flat damage, and `BleedDuration` does nothing.
- No meta-progression, no win condition, no story beyond the "Dark Lord" line, and no save/load of the player's progress.
- Unity Behavior and NavMesh packages are installed but unused. The commit history shows they were tried and dropped in favour of the transform-based `Enemy` AI.
- `DungeonManager.EnemyRoomBaseCount` / `LootRoomBaseCount` are declared but unused.

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
- Note: enemy bars now actually draw their breakpoints (every 5 HP, per `Bandit.prefab`). Raise `breakpointEveryX` if that looks too dense.

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

### Needs a design decision (not scheduled)
- **Save/load**:
  - `WorldStateManager.Load()` is never called, and `Awake()` overwrites `worldstate.json` with empty state on every launch, so flags only last one session.
  - Just turning loading on would be *worse*: the merchant flag would persist but the inventory wouldn't, so the amulet would be lost forever.
  - It needs a real save of inventory, equipment, gold and dungeon level, or an explicit "no persistence" choice.
- Status effects (bleed, poison, burn) are description text only. `BleedDamage` adds flat damage, and `BleedDuration` does nothing.
- Many `items.json` entries reference sprites that don't exist, use unsupported effect types (`Buff`, `Shield`), show unfilled `{placeholders}`, or have no price (so they're unsellable).
- `ShopSystem.BuyItem` ignores `ShopItemData.quantity` (infinite stock).
- `ShowDialogBehavior` and `GiveItemBehavior` write runtime state into ScriptableObject fields, which persists into the asset in the Editor.
- Legacy Input Manager: migrate to the Input System package.
- No boss enemy, and the boss room is only an exit. `DungeonManager.EnemyRoomBaseCount` / `LootRoomBaseCount` are unused.
- `LevelTransition` runs its fade coroutine on an object destroyed by the scene load, so `FadeTransition.FadeBack` exists as a workaround. The transition flow could live on the persistent `FadeTransition` instead.
- Consider Git LFS for binary art before committing more vendor packs.

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

- **Policy: only commit vendor assets the game actually uses.** Vendor packs are imported whole into `Assets/`, but only the files that tracked scenes, prefabs and UI reference (directly or through other referenced assets) get committed, together with their `.meta` and parent-folder `.meta` files. Mostly or fully untracked packs: `Assets/RPG Icons Pixel Art/` (73 MB, unused), `Assets/Violet Theme Ui/` (a few icons and the red progress bar used), `Assets/Imported Assets/` (one icon sheet used), `Assets/JohnFarmer/Keyboard Keys & Mouse Sprites/` (a few key sprites used), and `Assets/NaughtyAttributes/` (unused, carries a local 6000.6 patch).
- **Before committing, check that new references resolve.** A fresh clone only has tracked files, so a scene or prefab pointing at an untracked asset breaks. Resolve the referenced GUIDs (`[0-9a-f]{32}` in YAML and UXML) against `.meta` files, and commit any untracked dependency. The 2026-09-29 baseline did this with a small script that walks references transitively.
- Never commit `Assets/JohnFarmer/Keyboard Keys & Mouse Sprites/PSB File~/` (~335 MB of unused Photoshop source). It is in `.gitignore`.
- `.gitignore` excludes `Library/`, `Temp/`, `Logs/`, `obj/`, `.vs/`, `.idea/`, `UserSettings/` (per-user, untracked since 2026-09-29), build output, `*.csproj`, `*.sln`, and TextMesh Pro Examples. There is no Git LFS, and binary art is committed directly.
