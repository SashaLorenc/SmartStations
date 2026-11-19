# Smart Stations

Production stations open like a chest instead of "click to add one".

## Supported stations

| Station | Slots |
|---|---|
| Charcoal kiln, smelter, blast furnace, eitr refinery, windmill, spinning wheel (any Smelter-based station, modded ones too) | Input, fuel (when used), products |
| Fermenter | Input, products (shows progress and time left) |
| Cooking station, iron cooking station, stone oven | Raw food, fuel (oven), cooked or burnt food |
| Beehive, sap collector | Products |

## How to use

- **E** on a station opens your inventory with the station window above it. **Shift + E** keeps the vanilla interaction.
- **Add**: pick up an item in your inventory and click the window, or right-click the item. It goes to the right slot. **Add all** / **Fill** take everything that fits from your inventory.
- **Take back**: click a slot to take the whole stack, right-click to take one. **Take all** empties the products.
- Smelter products stay inside the station instead of dropping on the ground (configurable, capacity 100). If the station is destroyed, they drop as usual.
- Taking cooked food from the window gives Cooking skill like picking it by hand.

## Craft from nearby chests

- Crafting stations (workbench, forge, cauldron, artisan table...) also use materials stored in chests within 15 m of the station. A green **+N** next to each ingredient shows how many the chests hold.
- Building with the hammer, hoe or cultivator uses chests within 15 m of you.
- Only chests your game owns on the network and that nobody has open are used, so the count you see is exactly what can be taken: nothing is ever duplicated or lost.
- Turns itself off when another "craft from containers" mod is installed (Craft From Containers, AzuCraftyBoxes...).

## Crafting categories and search

Next to the crafting menu: a search field and one button per category found at the station (weapons, armor, tools, ammo, food and potions, other). With **Smart Chest** installed, its categories (including your own and your per-item changes) are used instead.

## Auto fill

Each station has an **Auto fill: ON / OFF** button (off by default). When on, it pulls ore and fuel from chests within 10 m every 5 seconds. Cooking stations only refill their fuel, so food never burns unattended.

Auto fill only uses chests built by a player, that you can access, that nobody has open, and that your game currently owns on the network. Combined with owner-only station updates, items can never be duplicated in multiplayer.

## Configuration

`BepInEx/config/sashalorenc.SmartStations.cfg`: enable each station family, max distance, keep products and capacity, auto fill radius, interval and default, craft/build from chests and radius, categories, Smart Chest categories, category bar position.

## Notes

- Every player needs the mod (it adds network calls).
- If you uninstall the mod, products still stored inside a smelter are lost: empty them first.
- Gamepad is not supported in the window yet.

Requires BepInExPack Valheim and Jötunn. Works alone, no other mod needed.
