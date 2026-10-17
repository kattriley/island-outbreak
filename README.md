# Island Outbreak

A Left 4 Dead 2 x Hello Kitty Island Adventure mashup for Melty.

## What it is

You play **Left 4 Dead 2**, and **Hello Kitty Island Adventure** is really in it: every safe room is themed on a real HKIA island region (Merry Meadows, Gemstone Mountain, Icy Peak, Rainbow Reef, Cloud Island, Heart of the Island, and more), read straight from your own installed copy of Hello Kitty Island Adventure while the game runs. An island supply treat drawn from HKIA's own items (strawberry, cactus cream, almond, chocolate coin, heart pie, fruity pizza, shortcake, cupcake) is announced for the team.

Nothing is copied out of either game: the mod reads HKIA's region/item identifiers from your HKIA install folder at runtime, and everything else is Left 4 Dead 2's own content.

## First minute

You spawn into an L4D2 campaign safe room. Chat announces which HKIA region the safe room is themed on and which island supply treat awaits. You heal up and open the safe room door to begin the campaign.

## Players

Solo or up to 4-player co-op on your own listen server. Friends join with one click through Melty (no port forwarding).

## Games you need

- Left 4 Dead 2 (host — you play in it)
- Hello Kitty Island Adventure (companion — its content is read from your install)

## Build

- `source/islandoutbreak.sp` — SourceMod plugin source
- `build/islandoutbreak.smx` — compiled plugin (install to `left4dead2/addons/sourcemod/plugins/`)
- `sheets/` — design sheets (source of truth)

## Melty

Host game: left-4-dead-2 (role primary). Companion: custom-hello-kitty-island-adventure (role companion). The mod receives the HKIA install path via the `-island_path` launch argument Melty provides, and publishes its listen-server address to `addons/sourcemod/logs/islandoutbreak.log` so Melty can connect friends.
