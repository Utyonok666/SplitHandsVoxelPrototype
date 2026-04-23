using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
// ============================================================
// WorldSaveSystem
// Static JSON save/load system for world, player, hotbar, drops, and time state. (Этот скрипт отвечает за: static json save/load system for world, player, hotbar, drops, and time state.)
// ============================================================
public class WorldBlockSaveRecord
{
    public int x;
    public int y;
    public int z;
    public int type;
}

[Serializable]
public class HotbarSlotSaveRecord
{
    public string itemName;
    public int count;
}

[Serializable]
public class HeldItemSaveRecord
{
    public string resourceName;
}

[Serializable]
public class WorldDropSaveRecord
{
    public string itemName;
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class WorldSaveData
{
    public int seed;
    public int viewDistance;
    public Vector3Serializable playerPosition;
    public float playerYaw;
    public float playerPitch;
    public List<WorldBlockSaveRecord> blockRecords = new List<WorldBlockSaveRecord>();
    public List<string> removedWorldObjectIds = new List<string>();
    public List<HotbarSlotSaveRecord> hotbarSlots = new List<HotbarSlotSaveRecord>();
    public HeldItemSaveRecord leftHand;
    public HeldItemSaveRecord rightHand;
    public List<WorldDropSaveRecord> worldDrops = new List<WorldDropSaveRecord>();
    public float timeOfDay = 0.25f;
    public bool cycleEnabled = true;
}

[Serializable]
public struct Vector3Serializable
{
    public float x;
    public float y;
    public float z;

    public Vector3Serializable(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    // To Vector3. (To Vector3)
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

// ============================================================
// WorldSaveSystem
// Static JSON save/load system for world, player, hotbar, drops, and time state. (Этот скрипт отвечает за: static json save/load system for world, player, hotbar, drops, and time state.)
// ============================================================
public static class WorldSaveSystem
{
    private const string SaveFileName = "last_world_save.json";

    private static WorldSaveData cache;
    private static bool cacheLoaded = false;
    private static bool isQuitting = false;

    private static readonly Dictionary<string, int> blockTypeLookup = new Dictionary<string, int>();
    private static readonly HashSet<string> removedObjectLookup = new HashSet<string>();
    private static bool lookupsBuilt = false;

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeState()
    {
        cache = null;
        cacheLoaded = false;
        isQuitting = false;
        lookupsBuilt = false;
        blockTypeLookup.Clear();
        removedObjectLookup.Clear();
    }

    public static bool IsQuitting => isQuitting;
    // Notify Application Quit. (Notify Application Quit)
    public static void NotifyApplicationQuit() => isQuitting = true;
    // Check whether a save file exists. (Check whether a сохранение file exists)
    public static bool HasSave() => File.Exists(SavePath);

    // Load Save. (Load Save)
    public static WorldSaveData LoadSave()
    {
        if (cacheLoaded)
            return cache;

        cacheLoaded = true;

        if (!File.Exists(SavePath))
        {
            cache = null;
            lookupsBuilt = false;
            return null;
        }

        try
        {
            cache = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(SavePath));
            if (cache == null)
                cache = new WorldSaveData();

            EnsureLists(cache);
            RebuildLookups(cache);
            return cache;
        }
        catch (Exception e)
        {
            Debug.LogError("[WorldSaveSystem] Load failed: " + e.Message);
            cache = null;
            lookupsBuilt = false;
            return null;
        }
    }

    // Write current world state to disk. (Write текущее мира state to disk)
    public static void Save(WorldSaveData data)
    {
        if (data == null)
            return;

        EnsureLists(data);
        cache = data;
        cacheLoaded = true;
        RebuildLookups(data);

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError("[WorldSaveSystem] Save failed: " + e.Message);
        }
    }

    // Set Block Type. (Set Block Type)
    public static void SetBlockType(int x, int y, int z, int type)
    {
        WorldSaveData data = GetOrCreateData();
        string key = BuildBlockKey(x, y, z);

        bool found = false;
        for (int i = 0; i < data.blockRecords.Count; i++)
        {
            WorldBlockSaveRecord record = data.blockRecords[i];
            if (record.x == x && record.y == y && record.z == z)
            {
                record.type = type;
                data.blockRecords[i] = record;
                found = true;
                break;
            }
        }

        if (!found)
        {
            data.blockRecords.Add(new WorldBlockSaveRecord { x = x, y = y, z = z, type = type });
        }

        blockTypeLookup[key] = type;
        cache = data;
        cacheLoaded = true;
    }

    // Try to Get Block Type. (Try to Get Block Type)
    public static bool TryGetBlockType(int x, int y, int z, out int type)
    {
        type = 0;
        WorldSaveData data = LoadSave();
        if (data == null)
            return false;

        EnsureLookupsBuilt(data);
        return blockTypeLookup.TryGetValue(BuildBlockKey(x, y, z), out type);
    }

    // Mark World Object Removed. (Mark World Object Removed)
    public static void MarkWorldObjectRemoved(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId))
            return;

        WorldSaveData data = GetOrCreateData();
        EnsureLookupsBuilt(data);

        if (removedObjectLookup.Add(uniqueId))
            data.removedWorldObjectIds.Add(uniqueId);

        cache = data;
        cacheLoaded = true;
    }

    // Is World Object Removed. (Is World Object Removed)
    public static bool IsWorldObjectRemoved(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId))
            return false;

        WorldSaveData data = LoadSave();
        if (data == null)
            return false;

        EnsureLookupsBuilt(data);
        return removedObjectLookup.Contains(uniqueId);
    }

    // Get Or Create Data. (Get Or Create Data)
    private static WorldSaveData GetOrCreateData()
    {
        WorldSaveData data = LoadSave();
        if (data == null)
            data = new WorldSaveData();

        EnsureLists(data);
        EnsureLookupsBuilt(data);
        return data;
    }

    // Ensure Lists. (Ensure Lists)
    private static void EnsureLists(WorldSaveData data)
    {
        if (data.blockRecords == null) data.blockRecords = new List<WorldBlockSaveRecord>();
        if (data.removedWorldObjectIds == null) data.removedWorldObjectIds = new List<string>();
        if (data.hotbarSlots == null) data.hotbarSlots = new List<HotbarSlotSaveRecord>();
        if (data.worldDrops == null) data.worldDrops = new List<WorldDropSaveRecord>();
    }

    // Ensure Lookups Built. (Ensure Lookups Built)
    private static void EnsureLookupsBuilt(WorldSaveData data)
    {
        if (!lookupsBuilt)
            RebuildLookups(data);
    }

    // Rebuild Lookups. (Rebuild Lookups)
    private static void RebuildLookups(WorldSaveData data)
    {
        blockTypeLookup.Clear();
        removedObjectLookup.Clear();

        if (data != null)
        {
            EnsureLists(data);

            for (int i = 0; i < data.blockRecords.Count; i++)
            {
                WorldBlockSaveRecord record = data.blockRecords[i];
                if (record == null)
                    continue;

                blockTypeLookup[BuildBlockKey(record.x, record.y, record.z)] = record.type;
            }

            for (int i = 0; i < data.removedWorldObjectIds.Count; i++)
            {
                string id = data.removedWorldObjectIds[i];
                if (!string.IsNullOrEmpty(id))
                    removedObjectLookup.Add(id);
            }
        }

        lookupsBuilt = true;
    }

    // Build Block Key. (Build Block Key)
    private static string BuildBlockKey(int x, int y, int z)
    {
        return x + "|" + y + "|" + z;
    }
}
