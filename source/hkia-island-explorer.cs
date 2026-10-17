using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

// ---------------------------------------------------------------------------
// HKIA Island Explorer — an .exe mod for Hello Kitty Island Adventure.
//
// Host (primary): Hello Kitty Island Adventure — the mod reads its real
// content (island regions, items, music tracks) from the player's own
// installed copy at runtime, via the -gamepath launch argument Melty passes
// ({game:custom-hello-kitty-island-adventure}).
//
// Nothing is ever copied out of or modified in the HKIA install. The exe
// installs to the mashup's own folder ({managed}/hkia-island-explorer/) and
// reads HKIA's files from there while it runs.
//
// Sheets this implements: sheets/systems.json, sheets/hooks.json,
// sheets/regions.json, sheets/items.json.
// ---------------------------------------------------------------------------

class HkiaIslandExplorer
{
    // HKIA region tokens, discovered from real entity-name strings in the
    // player's HKIA install (StreamingAssets/EntityScenes/*.entities).
    static readonly string[][] RegionTokens = {
        new string[] { "MerryMeadows", "Merry Meadows" },
        new string[] { "Meadows", "Merry Meadows" },
        new string[] { "GemstoneMountain", "Gemstone Mountain" },
        new string[] { "IcyPeak", "Icy Peak" },
        new string[] { "RainbowReef", "Rainbow Reef" },
        new string[] { "CloudIsland", "Cloud Island" },
        new string[] { "HeartOfTheIsland", "Heart of the Island" },
        new string[] { "BlizzardPeak", "Blizzard Peak" },
        new string[] { "FalsePeak", "False Peak" },
        new string[] { "HopscotchIslands", "Hopscotch Islands" },
        new string[] { "MoonIsland", "Moon Island" },
        new string[] { "GemstoneTown", "Gemstone Town" },
        new string[] { "GemBeach", "Gem Beach" },
        new string[] { "CitySkyline", "City Skyline" },
        new string[] { "CozyIslands", "Cozy Islands" },
        new string[] { "HomeIsland", "Home Island" },
        new string[] { "SeasideSanctuary", "Seaside Sanctuary" },
        new string[] { "SunPeak", "Sun Peak" }
    };

    // HKIA item tokens, discovered from real entity-name strings in the
    // player's HKIA install (StreamingAssets/EntityScenes/*.entities).
    static readonly string[][] ItemTokens = {
        new string[] { "Strawberry", "Strawberry" },
        new string[] { "CactusCream", "Cactus Cream" },
        new string[] { "Almond", "Almond" },
        new string[] { "ChocolateCoin", "Chocolate Coin" },
        new string[] { "HeartPie", "Heart Pie" },
        new string[] { "FruityPizza", "Fruity Pizza" },
        new string[] { "Shortcake", "Shortcake" },
        new string[] { "Cupcake", "Giant Cupcake" },
        new string[] { "Cake", "Cake" }
    };

    static void Main(string[] args)
    {
        string gamePath = "";
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-gamepath")
            {
                gamePath = args[i + 1];
            }
        }

        Console.WriteLine("=== HKIA Island Explorer ===");
        Console.WriteLine("Hello Kitty Island Adventure mod — reads real HKIA content from your install.");
        Console.WriteLine();

        if (string.IsNullOrEmpty(gamePath))
        {
            Console.WriteLine("No game path provided. Melty should launch this with -gamepath {game}.");
            try { Console.WriteLine("Press any key to exit."); Console.ReadKey(); } catch { /* no console; exit quietly */ }
            return;
        }

        string entityDir = Path.Combine(gamePath, "Hello Kitty_Data", "StreamingAssets", "EntityScenes");
        string bankDir = Path.Combine(gamePath, "Hello Kitty_Data", "StreamingAssets");

        // Verify HKIA is really installed at the given path.
        if (!Directory.Exists(entityDir))
        {
            Console.WriteLine("Could not find Hello Kitty Island Adventure at: " + gamePath);
            Console.WriteLine("Expected: " + entityDir);
            return;
        }

        // Scan entity files for regions and items (real HKIA content).
        HashSet<string> regionsFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> itemsFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(entityDir, "*.entities"))
        {
            try
            {
                string content = File.ReadAllText(file);
                foreach (var pair in RegionTokens)
                {
                    if (content.IndexOf(pair[0], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        regionsFound.Add(pair[1]);
                    }
                }
                foreach (var pair in ItemTokens)
                {
                    if (content.IndexOf(pair[0], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        itemsFound.Add(pair[1]);
                    }
                }
            }
            catch
            {
                // Skip unreadable files.
            }
        }

        // Scan music banks (real HKIA content).
        List<string> musicTracks = new List<string>();
        foreach (var file in Directory.GetFiles(bankDir, "MX_Score_*.bank"))
        {
            musicTracks.Add(Path.GetFileNameWithoutExtension(file));
        }
        musicTracks.Sort();

        // Display discovered content.
        Console.WriteLine("Reading from: " + gamePath);
        Console.WriteLine();
        Console.WriteLine("Island regions found in your HKIA install:");
        foreach (var r in regionsFound) Console.WriteLine("  • " + r);
        Console.WriteLine();
        Console.WriteLine("Items found in your HKIA install:");
        foreach (var i in itemsFound) Console.WriteLine("  • " + i);
        Console.WriteLine();
        Console.WriteLine("Music tracks found in your HKIA install:");
        foreach (var m in musicTracks) Console.WriteLine("  • " + m);
        Console.WriteLine();
        Console.WriteLine("All content read straight from your own Hello Kitty Island Adventure install.");
        Console.WriteLine("Nothing copied out, nothing modified.");
        Console.WriteLine();

        // Write test marker so Melty's test confirms the mod played.
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "island-outbreak");
        Directory.CreateDirectory(logDir);
        File.WriteAllText(Path.Combine(logDir, "melty.log"), "MELTY_TEST_ISLAND_ACTIVE:1\n");

            }
}
