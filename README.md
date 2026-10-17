# HKIA Island Explorer

An .exe mod for Hello Kitty Island Adventure, published on Melty.

## What it is

HKIA Island Explorer reads real Hello Kitty Island Adventure content — island regions, items, and music tracks — straight from your own installed copy of Hello Kitty Island Adventure while it runs, and lets you browse them. Nothing is ever copied out of or modified in your HKIA install. Single player.

## Games you need

- Hello Kitty Island Adventure (host — you play in it; its folder is passed to the exe at launch)

## First minute

Launch the explorer .exe. It scans your HKIA install folder and lists island regions (Merry Meadows, Gemstone Mountain, Icy Peak, Rainbow Reef, Cloud Island, Heart of the Island and more), items (strawberry, cactus cream, almond, chocolate coin, heart pie...), and music tracks (MX_Score_MerryMeadows.bank, etc.) — all from your own HKIA files.

## Build

- `source/hkia-island-explorer.cs` — C# source
- `build/hkia-island-explorer.exe` — compiled .exe (csc.exe, .NET Framework 4.0)
- `sheets/` — design sheets (source of truth)

## Melty

Host game: custom-hello-kitty-island-adventure (role primary). Melty passes `{game:custom-hello-kitty-island-adventure}` as `-gamepath` at launch. The exe reads HKIA's real content from that folder and writes `MELTY_TEST_ISLAND_ACTIVE:1` to its log so Melty's test can confirm it played.
