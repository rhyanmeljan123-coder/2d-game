# Farming RPG: Project Analysis & Roadmap

_Written after inspecting the project at commit `e302c25` (28 scripts, ~3,200 lines)._

## 1. What exists today

| # | System | Status | What is there now |
|---|--------|--------|-------------------|
| 1 | Farming | Partial | 5 crops (pumpkin, potato, carrot, tomato, peas), 10 plots, plant/harvest with E, seeds 1-5, 5 growth stages (fixed ~20 s), seeds drop back. **Missing:** watering, per-crop durations, seasons, rare seeds, quality/rarity |
| 2 | Animals | Cosmetic only | Chickens, cows, pigs, sheep wander with idle/walk animation and collide with the world. **Missing:** pens, feeding, products, happiness/health, breeding, shelters |
| 3 | Gathering | None | Trees/rocks are solid decoration. Herbs/flowers are loose pickups. **Missing:** wood, ore, mushrooms, tools, respawn |
| 4 | Crafting | Partial | 1 station (table in farmhouse), 6 food/potion recipes, ingredients from farm + dungeon. **Missing:** other stations, gear, tools, unlocks, mana/stamina/speed potions |
| 5 | Dungeon & combat | Partial | 1 hand-designed floor, 5 rooms, slimes + skeletons (chase, contact damage, knockback, drops), spikes/flames/arrow traps, silver/gold key chain, 3 chests. **Missing:** floors, procedural encounters, mini/final bosses, rarity tiers, environments, scaling |
| 6 | NPCs & quests | None | |
| 7 | Economy | Partial | Coins, market stall (sell crops/loot/food, buy seeds/milk/flask), fixed prices. **Missing:** rotating stock, travelling merchant, selling animal goods/gear |
| 8 | House & building | Partial | Farmhouse interior with bed (heals, skips time), table, chest. **Missing:** upgrades, storage chests, placeable furniture, farm expansion |
| 9 | Seasons & weather | None | No clock, no calendar |
| 10 | Progression | None | Only HP; no XP/skills |
| 11 | Events & exploration | Light | Chests, crates, ground pickups, flasks. **Missing:** random events, invasions, treasure maps, secrets, hidden caves |
| 12 | Inventory | Basic | Per-item counts, HUD shows what you own. **Missing:** slots/stacks, categories, rarity, descriptions, equipment, hotbar, storage, sorting, tooltips |
| 13 | Loop connection | Partial | Farm -> craft -> eat -> dungeon -> loot -> sell/craft already works end to end in miniature |
| 14 | UI | Basic | IMGUI HUD (HP, backpack, prompts, toasts) and one shared list menu (crafting, market) |

Combat, health, traps and the key/chest progression are the most complete parts.

## 2. Architecture findings

**Works well, keep:**
- World is built from code (`SimpleMapBuilder`, `FarmWorld`, `DungeonWorld`) and shows in the Scene view; edit-mode previews work.
- `Interactable` + `PlayerInteractor` (E key) is a clean extension point: crops, chests, bed, crates, stalls and crafting all use it.
- `Health` is shared by player and enemies. `SimpleAnimator` + `SpriteSheet` need no Animator controllers.
- Menus use one generic `MenuEntry` list (crafting, shop).

**Needs to change before adding big features:**
1. **Items are an enum with parallel hard-coded arrays** (names, heal, sell, buy). Rarity, categories, equipment, descriptions and quality can't fit here. Needs a data-driven item database.
2. **Inventory is just counts.** No slots, stacks, equipment or hotbar.
3. **No game clock.** Seasons, weather, animal feeding, crop timing, merchants and events all need a shared calendar.
4. **No save/load.** All state is static and resets on Play. Needed once progression exists.
5. **IMGUI UI** is fine for prototyping but will get heavy with inventory, journal, map, skills and animal screens.
6. Dungeon is one fixed layout; floors/bosses need a floor-generator and monster/loot tables.
7. `Random` is unseeded everywhere (fine now; matters for generated dungeons and save/load).

**Also noted:**
- All code written so far has **not been run in the Unity editor by me**. Phase 0 is a playtest/bug pass.
- Unused art that fits the plan: `Player_Actions.png` (axe/pickaxe/hoe/watering can animations), enemy hit-flash and death rows, cliff/beach/farmland tiles, bridges, `Player` death row.
- The free Cute Fantasy license forbids redistribution and the repo is public; consider making the repo private.

## 3. Roadmap (one system at a time)

Each phase ends with: compile check, playtest, fix, commit. No phase deletes a working feature.

| Phase | Goal | Main work | Depends on |
|-------|------|-----------|------------|
| **0** | Stabilise | You play the current build; fix compile/runtime errors and balance issues I can't see | none |
| **1** | Foundation | `ItemDatabase` (data-driven: id, name, category, rarity, stack size, value, effects, icon), slot-based inventory with stacking, hotbar, equipment slots, tooltips, inventory screen, save/load | 0 |
| **2** | Time & weather | Game clock, day/night tint, 4 seasons, weather (sun/rain/storm/snow), sleep advances the day, events hook | 1 |
| **3** | Farming v2 | Data-driven crops (duration, season), watering (rain waters), crop quality/rarity, rare dungeon seeds, tilling/farm tools | 1, 2 |
| **4** | Skills & tools | XP + 6 skills, level bonuses, tool tiers, stamina | 1 |
| **5** | Gathering | Trees, rocks/ore, mushrooms/herbs, rarity, respawn timers, tool requirements | 3-4 |
| **6** | Animals | Pens, feeding, products (egg/milk/wool/meat), happiness/health, breeding, shelter upgrades | 2, 4 |
| **7** | Crafting v2 | Stations (alchemy, forge, workbench, kitchen), recipe unlocks, potions (health/mana/stamina/speed), weapons/armor/tools/furniture | 1, 4, 5 |
| **8** | Dungeon v2 | Floor generator, monster/loot tables, rarity tiers, mini-bosses and final boss with unique drops, environments, difficulty scaling | 1, 7 |
| **9** | NPCs, quests, economy | NPC schedules, quest journal and rewards, rotating shop stock, travelling merchant | 2, 7, 8 |
| **10** | Building & decoration | House upgrades, storage chests, furniture placement, farm expansion | 1, 7 |
| **11** | Events & exploration | Random chests, invasions, treasure maps, hidden caves, secrets | 2, 8 |
| **12** | UI polish | Map, journal, stats/skills screen, animal/farm panels, consistent style, audio, pause menu | all |

## 4. Recommended first step

Phase 1 (Foundation). Almost every later system depends on a data-driven item/inventory layer, so building it first avoids rewriting everything twice. It will keep all current items and recipes working (the existing `Item` values map onto the new database).
