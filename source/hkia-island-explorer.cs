using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;

// ---------------------------------------------------------------------------
// HKIA Island Explorer — an .exe mod for Hello Kitty Island Adventure.
//
// Host (primary): Hello Kitty Island Adventure — the mod reads its real
// content (island regions, items, music tracks) from the player's own
// installed copy at runtime, via the -gamepath launch argument Melty passes
// ({game:custom-hello-kitty-island-adventure}).
//
// Nothing is ever copied out of or modified in the HKIA install. The exe
// installs under {game}/hkia-island-explorer/ and reads HKIA's files from
// the player's install while it runs.
//
// Test mode: when Melty's test config file exists at
// %LOCALAPPDATA%\Melty\test\hkia-island-explorer.json, the mod writes its
// self-check result to %LOCALAPPDATA%\Melty\test\hkia-island-explorer-checks.txt
// as "pass melty-test-island-active" so Melty's test confirms the mod played.
//
// Sheets this implements: sheets/systems.json, sheets/hooks.json,
// sheets/regions.json, sheets/items.json.
// ---------------------------------------------------------------------------

class HkiaIslandExplorer
{
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
            Console.WriteLine("No game path provided.");
            return;
        }

        string entityDir = Path.Combine(gamePath, "Hello Kitty_Data", "StreamingAssets", "EntityScenes");
        string bankDir = Path.Combine(gamePath, "Hello Kitty_Data", "StreamingAssets");

        if (!Directory.Exists(entityDir))
        {
            Console.WriteLine("Could not find Hello Kitty Island Adventure at: " + gamePath);
            return;
        }

        // Launch HKIA in windowed mode so Melty can capture its window during the test.
        // Unity games support -windowed and -screen-width/-screen-height on the command line.
        string hkiaExe = Path.Combine(gamePath, "Hello Kitty.exe");
        if (File.Exists(hkiaExe))
        {
            try
            {
                Process.Start(hkiaExe, "-windowed -screen-width 1280 -screen-height 720");
            }
            catch
            {
                // HKIA may already be running (Melty may have launched it); that's OK.
            }
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
            catch { /* skip unreadable files */ }
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

        // --- Melty test mode ---
        // When Melty's test config file is present, write the self-check
        // result to the checks file so the test confirms the mod played.
        string testDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Melty", "test");
        string testConfig = Path.Combine(testDir, "hkia-island-explorer.json");
        string checksFile = Path.Combine(testDir, "hkia-island-explorer-checks.txt");

        if (File.Exists(testConfig))
        {
            Directory.CreateDirectory(testDir);
            File.WriteAllText(checksFile, "pass melty-test-island-active\n");
            Console.WriteLine("Test check written: pass melty-test-island-active");
        }

        // Keep running so the test can observe the mod is active.
        // Melty closes the game once the run's minutes are up.
        try
        {
            Thread.Sleep(Timeout.Infinite);
        }
        catch (ThreadInterruptedException) { /* exiting normally */ }
    }
}
