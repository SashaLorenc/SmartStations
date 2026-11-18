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

## Auto fill

Each station has an **Auto fill: ON / OFF** button (off by default). When on, it pulls ore and fuel from chests within 10 m every 5 seconds. Cooking stations only refill their fuel, so food never burns unattended.

Auto fill only uses chests built by a player, that you can access, that nobody has open, and that your game currently owns on the network. Combined with owner-only station updates, items can never be duplicated in multiplayer.

## Configuration

`BepInEx/config/sashalorenc.SmartStations.cfg`: enable each station family, max distance, keep products and capacity, auto fill radius, interval and default.

## Notes

- Every player needs the mod (it adds network calls).
- If you uninstall the mod, products still stored inside a smelter are lost: empty them first.
- Gamepad is not supported in the window yet.

Requires BepInExPack Valheim and Jötunn. Works alone, no other mod needed.
