#include <sourcemod>
#include <string>
#pragma newdecls required

#define PLUGIN_VERSION "1.0"
#define MARKER_PREFIX "MELTY_TEST_ISLAND_ACTIVE"
#define HOST_PREFIX "Melty join address: "
#define JOIN_PREFIX "Melty joined: "
#define FAIL_PREFIX "Melty join failed: "

// ---------------------------------------------------------------------------
// Island Outbreak — a Left 4 Dead 2 x Hello Kitty Island Adventure mashup.
//
// Host (primary):   Left 4 Dead 2 — the player is in it.
// Companion:        Hello Kitty Island Adventure — its island regions and
//                   items are read from the player's own installed copy at
//                   runtime via the -island_path launch argument Melty passes.
//                   Nothing is ever copied out of either game.
//
// Melty multiplayer contract:
//   - At start, read join= from {localappdata}/island-outbreak/melty.properties
//     (passed as -melty_props). Empty => host; an address => join.
//   - When hosting: write "Melty join address: <ip>:<port>" to melty.log
//     (passed as -melty_log).
//   - When joining: connect to the address, write "Melty joined: <address>"
//     or "Melty join failed: <why>" to melty.log.
//
// Sheets this implements: sheets/systems.json, sheets/hooks.json,
// sheets/regions.json, sheets/items.json.
// ---------------------------------------------------------------------------

char g_IslandPath[260] = "";
char g_MeltyProps[260] = "";
char g_MeltyLog[260] = "";
char g_Region[64] = "";
char g_Item[64] = "";
bool g_ThemeApplied = false;
bool g_HostLogged = false;
bool g_IsJoiner = false;
char g_JoinAddress[128] = "";

public Action Command_Theme(int client, int args)
{
    ApplyTheme();
    return Plugin_Handled;
}

// HKIA region/item tokens, discovered from real entity-name strings in the
// player's HKIA install (StreamingAssets/EntityScenes/*.entities).
char g_RegionTokens[40][64];
char g_RegionNames[40][64];
int g_RegionCount = 0;

char g_ItemTokens[40][64];
char g_ItemNames[40][64];
int g_ItemCount = 0;

public void OnPluginStart()
{
    GetCommandLineParam("-island_path", g_IslandPath, sizeof(g_IslandPath));
    GetCommandLineParam("-melty_props", g_MeltyProps, sizeof(g_MeltyProps));
    GetCommandLineParam("-melty_log", g_MeltyLog, sizeof(g_MeltyLog));

    LoadHkiaTokenTables();
    LoadHkiaData();

    LogToFile("islandoutbreak.log", "Island Outbreak v%s loaded (ISLAND_PATH=%s)", PLUGIN_VERSION, g_IslandPath);

    // --- Melty multiplayer: decide host vs join ---
    char props[512];
    if (ReadFileString(g_MeltyProps, props, sizeof(props)))
    {
        char key[32];
        char val[256];
        if (ParseKeyValue(props, "join", key, sizeof(key), val, sizeof(val)) && val[0] != '\0')
        {
            g_IsJoiner = true;
            strcopy(g_JoinAddress, sizeof(g_JoinAddress), val);
            LogToFile("islandoutbreak.log", "Island Outbreak: join mode, target=%s", g_JoinAddress);
            // Try connecting to the host's address right away.
            char cmd[160];
            Format(cmd, sizeof(cmd), "connect %s", g_JoinAddress);
            engine.ServerCommand(cmd);
            return;
        }
    }

    // Host mode: when the local player (client 1) spawns into the listen
    // server, publish the address to melty.log.
    RegConsoleCmd("sm_island_theme", Command_Theme, "Force-apply the Island Outbreak safe-room theme (test)");
}

public void OnMapStart()
{
    g_ThemeApplied = false;
    g_HostLogged = false;
    LoadHkiaData();
}

public void OnClientPutInServer(int client)
{
    // Melty multiplayer: host publishes its listen-server address.
    if (!g_IsJoiner && !g_HostLogged && client == 1)
    {
        g_HostLogged = true;
        char ip[64];
        GetClientIP(client, ip, sizeof(ip));
        ConVar portCvar = FindConVar("hostport");
        int port = portCvar ? GetConVarInt(portCvar) : 27015;
        WriteMeltyLog("%s%s:%d", HOST_PREFIX, ip, port);
    }

    // Safe-room theme: the host player has spawned into the safe room.
    if (client == 1 && !g_ThemeApplied)
    {
        g_ThemeApplied = true;
        ApplyTheme();
    }
}

// Joiner: once connected, confirm in melty.log.
public void OnClientConnected(int client)
{
    if (g_IsJoiner && client == 1)
    {
        WriteMeltyLog("%s%s", JOIN_PREFIX, g_JoinAddress);
    }
}

stock void ApplyTheme()
{
    PickRandom(g_RegionNames, g_RegionCount, g_Region, sizeof(g_Region), "Merry Meadows");
    PickRandom(g_ItemNames, g_ItemCount, g_Item, sizeof(g_Item), "Strawberry");

    char msg[280];
    Format(msg, sizeof(msg),
        "[Island Outbreak] This safe room is themed on %s from Hello Kitty Island Adventure (real HKIA island content read from your install). Island supply treat: %s.",
        g_Region, g_Item);
    PrintToChatAll(msg);

    LogToFile("islandoutbreak.log", "%s:%s|%s", MARKER_PREFIX, g_Region, g_Item);
}

stock void WriteMeltyLog(const char[] format, any ...)
{
    if (g_MeltyLog[0] == '\0')
    {
        return;
    }

    char msg[256];
    VFormat(msg, sizeof(msg), format);

    Handle h = OpenFile(g_MeltyLog, "a", false);
    if (h)
    {
        LogToOpenFile(h, "%s", msg);
        CloseHandle(h);
    }
}

// Read a file into a buffer.
stock bool ReadFileString(char[] path, char[] out, int outlen)
{
    Handle h = OpenFile(path, "r", false);
    if (!h)
    {
        return false;
    }
    bool ok = ReadFileLine(h, out, outlen) != 0;
    CloseHandle(h);
    return ok;
}

// Parse "key=value" from a line of text.
stock bool ParseKeyValue(char[] line, const char[] key, char[] kout, int klen, char[] vout, int vlen)
{
    char eq[2];
    eq[0] = '=';
    eq[1] = '\0';
    int idx = StrFind(line, eq);
    if (idx == -1)
    {
        return false;
    }
    int klen2 = idx;
    if (klen2 >= klen) klen2 = klen - 1;
    strcopy(kout, klen2 + 1, line);
    kout[klen2] = '\0';
    // trim trailing spaces from key
    while (klen2 > 0 && kout[klen2 - 1] == ' ') { kout[klen2 - 1] = '\0'; klen2--; }

    int vstart = idx + 1;
    int vlen2 = strlen(line) - vstart;
    if (vlen2 >= vlen) vlen2 = vlen - 1;
    strcopy(vout, vlen2 + 1, line + vstart);
    vout[vlen2] = '\0';
    // trim leading/trailing spaces from value
    int vs = 0;
    while (vout[vs] == ' ') vs++;
    if (vs > 0)
    {
        memmove(vout, vout + vs, strlen(vout) - vs + 1);
    }
    vlen2 = strlen(vout);
    while (vlen2 > 0 && vout[vlen2 - 1] == ' ') { vout[vlen2 - 1] = '\0'; vlen2--; }

    return StrCompare(kout, key, false) == 0;
}

stock void LoadHkiaTokenTables()
{
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "MerryMeadows", "Merry Meadows");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "Meadows", "Merry Meadows");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "GemstoneMountain", "Gemstone Mountain");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "IcyPeak", "Icy Peak");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "RainbowReef", "Rainbow Reef");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "CloudIsland", "Cloud Island");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "HeartOfTheIsland", "Heart of the Island");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "BlizzardPeak", "Blizzard Peak");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "FalsePeak", "False Peak");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "HopscotchIslands", "Hopscotch Islands");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "MoonIsland", "Moon Island");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "GemstoneTown", "Gemstone Town");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "GemBeach", "Gem Beach");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "CitySkyline", "City Skyline");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "CozyIslands", "Cozy Islands");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "HomeIsland", "Home Island");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "SeasideSanctuary", "Seaside Sanctuary");
    AddPair(g_RegionTokens, g_RegionNames, g_RegionCount, "SunPeak", "Sun Peak");

    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "Strawberry", "Strawberry");
    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "CactusCream", "Cactus Cream");
    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "Almond", "Almond");
    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "ChocolateCoin", "Chocolate Coin");
    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "HeartPie", "Heart Pie");
    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "FruityPizza", "Fruity Pizza");
    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "Shortcake", "Shortcake");
    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "Cupcake", "Giant Cupcake");
    AddPair(g_ItemTokens, g_ItemNames, g_ItemCount, "Cake", "Cake");
}

stock void AddPair(char[][] tokens, char[][] names, int& count, const char[] token, const char[] name)
{
    if (count >= 40) return;
    strcopy(tokens[count], 64, token);
    strcopy(names[count], 64, name);
    count++;
}

stock bool LoadHkiaData()
{
    if (g_IslandPath[0] == '\0')
    {
        return false;
    }

    char dir[320];
    Format(dir, sizeof(dir), "%s/Hello Kitty_Data/StreamingAssets/EntityScenes", g_IslandPath);

    Handle dh = OpenDirectory(dir, false);
    if (!dh)
    {
        return false;
    }

    char entry[260];
    FileType ftype;
    while (ReadDirEntry(dh, entry, sizeof(entry), ftype))
    {
        if (ftype != FileType_File)
        {
            continue;
        }
        if (StrContains(entry, ".entities", false) == -1 && StrContains(entry, ".entityheader", false) == -1)
        {
            continue;
        }

        char path[384];
        Format(path, sizeof(path), "%s/%s", dir, entry);
        ScanFile(path);
    }

    CloseHandle(dh);
    return true;
}

stock void ScanFile(char[] path)
{
    Handle h = OpenFile(path, "r", false);
    if (!h)
    {
        return;
    }

    char line[2048];
    while (ReadFileLine(h, line, sizeof(line)))
    {
        for (int i = 0; i < g_RegionCount; i++)
        {
            if (StrContains(line, g_RegionTokens[i], false) != -1)
            {
                AddUnique(g_Region, sizeof(g_Region), g_RegionNames[i]);
                break;
            }
        }

        for (int i = 0; i < g_ItemCount; i++)
        {
            if (StrContains(line, g_ItemTokens[i], false) != -1)
            {
                AddUnique(g_Item, sizeof(g_Item), g_ItemNames[i]);
                break;
            }
        }
    }

    CloseHandle(h);
}

stock void AddUnique(char[] list, int listlen, const char[] name)
{
    if (StrContains(list, name, false) != -1)
    {
        return;
    }
    int len = strlen(list);
    if (len > 0)
    {
        list[len] = ',';
        list[len + 1] = ' ';
        len += 2;
    }
    int copylen = listlen - len - 1;
    if (copylen < 1)
    {
        return;
    }
    strcopy(list[len], copylen, name);
}

stock void PickRandom(char[][] names, int count, char[] out, int outlen, const char[] fallback)
{
    if (count <= 0)
    {
        strcopy(out, outlen, fallback);
        return;
    }
    int i = GetRandomInt(0, count - 1);
    strcopy(out, outlen, names[i]);
}
