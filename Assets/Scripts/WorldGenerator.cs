using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================================================
// WorldGenerator
// Main world streaming manager: seed, chunk loading, terrain sampling, player spawn, and environment setup. (Этот скрипт отвечает за: main world streaming manager: seed, chunk loading, terrain sampling, player spawn, and environment setup.)
// ============================================================
public class WorldGenerator : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Seed")]
    public int seed = 0;
    public bool randomSeed = true;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Chunks")]
    public int chunkRes = 16;
    [Range(2, 12)] public int viewDistance = 5;
    public float worldUpdateInterval = 0.15f;
    public int initialChunksPerFrame = 1;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Fog")]
    public bool autoFogFromViewDistance = true;
    public Camera targetCamera;
    [Range(0.1f, 1f)] public float fogStartMultiplier = 0.65f;
    [Range(0.2f, 1.2f)] public float fogEndMultiplier = 0.95f;
    public float extraFogOffset = 0f;
    public float farClipPadding = 16f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("World Height")]
    public int worldHeight = 96;
    public int baseGroundHeight = 24;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Terrain")]
    public float continentScale = 260f;
    public float mountainScale = 110f;
    public float detailScale = 36f;
    public float continentStrength = 14f;
    public float mountainStrength = 26f;
    public float detailStrength = 4f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Biome / Forest")]
    public float forestBiomeScale = 120f;
    [Range(0f, 1f)] public float forestThreshold = 0.55f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Trees")]
    [Range(0f, 1f)] public float treeSpawnChanceForest = 0.10f;
    [Range(0f, 1f)] public float treeSpawnChancePlains = 0.015f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Rock Structures")]
    [Range(0f, 1f)] public float rockSpawnChanceForest = 0.06f;
    [Range(0f, 1f)] public float rockSpawnChancePlains = 0.04f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Block Prefabs")]
    public GameObject groundPrefab; // grass block
    public GameObject dirtPrefab;   // dirt block
    public GameObject stonePrefab;  // stone block
    public GameObject chunkPrefab;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Environment")]
    public GameObject treePrefab;
    public GameObject ironOrePrefab;
    public GameObject ironOreBlockPrefab;
    public GameObject rockStructurePrefab;
    public GameObject chestPrefab;
    [Range(0f, 0.2f)] public float chestSpawnChance = 0.02f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Iron Ore")]
    [Range(0f, 0.5f)] public float ironOreSpawnChanceForest = 0.015f;
    [Range(0f, 0.5f)] public float ironOreSpawnChancePlains = 0.025f;
    public int ironOreMinHeight = 18;
    public int ironOreMaxHeight = 42;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Tool Prefabs")]
    public GameObject axePrefab;
    public GameObject hammerPrefab;
    public GameObject crowbarPrefab;
    [Range(0f, 0.25f)] public float toolSpawnChancePerChunk = 0.08f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Player")]
    public Transform player;
    public float spawnExtraHeight = 2.5f;
    public HandGamePlayer playerController;
    public HotbarManager hotbarManager;
    public DayNightCycle dayNightCycle;
    public float autoSaveInterval = 8f;

    private readonly Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();
    private readonly HashSet<Vector2Int> chunksBeingGenerated = new HashSet<Vector2Int>();

    private WorldTerrainSampler terrainSampler;
    private StructureSpawner structureSpawner;

    private WorldSaveData loadedSave;
    private bool continueFromSave = false;

    private const string PendingViewDistanceKey = "pending_view_distance";

    public int ActiveChunkCount => activeChunks.Count;
    public WorldTerrainSampler TerrainSampler => terrainSampler;

    void Awake()
    {
        ResolveLaunchSettings();
        Random.InitState(seed);

        terrainSampler = new WorldTerrainSampler(this);
        structureSpawner = new StructureSpawner(this, terrainSampler);

        Debug.Log("SEED: " + seed);
        Debug.Log("[WorldGenerator] Final viewDistance = " + viewDistance);
        GameSettingsData.ClearPendingLaunch();
    }

    void Start()
    {
        ApplyViewDistanceVisuals();
        StartCoroutine(GenerateAroundPlayerInitialAsync());
        StartCoroutine(FixPlayerSpawnNextFrame());
        StartCoroutine(UpdateWorld());
        StartCoroutine(AutoSaveLoop());
    }

    void OnApplicationQuit()
    {
        SaveWorldNow();
        WorldSaveSystem.NotifyApplicationQuit();
    }

    void ResolveLaunchSettings()
    {
        int sliderDistanceOverride = PlayerPrefs.GetInt(PendingViewDistanceKey, -1);

        if (GameSettingsData.ContinueRequested)
        {
            loadedSave = WorldSaveSystem.LoadSave();
            if (loadedSave != null)
            {
                seed = loadedSave.seed;
                viewDistance = sliderDistanceOverride >= 2 ? sliderDistanceOverride : loadedSave.viewDistance;
                randomSeed = false;
                continueFromSave = true;
                return;
            }
        }

        if (GameSettingsData.HasPendingLaunchSettings)
        {
            seed = GameSettingsData.Seed;
            viewDistance = GameSettingsData.ViewDistance;
            randomSeed = false;
            continueFromSave = false;
            return;
        }

        if (randomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);

        if (sliderDistanceOverride >= 2)
            viewDistance = sliderDistanceOverride;

        continueFromSave = false;
    }

    IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            SaveWorldNow();
        }
    }

    // Save World Now. (Save World Now)
    public void SaveWorldNow()
    {
        WorldSaveData save = loadedSave ?? new WorldSaveData();
        save.seed = seed;
        save.viewDistance = viewDistance;

        if (player != null)
            save.playerPosition = new Vector3Serializable(player.position);

        save.playerYaw = player != null ? player.eulerAngles.y : 0f;
        save.playerPitch = playerController != null ? playerController.GetCameraPitch() : 0f;
        save.hotbarSlots = hotbarManager != null ? hotbarManager.CaptureSaveData() : new List<HotbarSlotSaveRecord>();
        save.leftHand = playerController != null ? playerController.CaptureLeftHandSave() : null;
        save.rightHand = playerController != null ? playerController.CaptureRightHandSave() : null;

        if (dayNightCycle != null)
        {
            save.timeOfDay = dayNightCycle.timeOfDay;
            save.cycleEnabled = dayNightCycle.cycleEnabled;
        }

        save.worldDrops = CaptureWorldDrops();
        loadedSave = save;
        WorldSaveSystem.Save(save);
    }

    List<WorldDropSaveRecord> CaptureWorldDrops()
    {
        List<WorldDropSaveRecord> drops = new List<WorldDropSaveRecord>();
        ItemDrop[] allDrops = FindObjectsOfType<ItemDrop>();

        for (int i = 0; i < allDrops.Length; i++)
        {
            if (allDrops[i] != null)
                drops.Add(allDrops[i].CaptureSaveData());
        }

        return drops;
    }

    void RestoreWorldDrops()
    {
        if (loadedSave == null || loadedSave.worldDrops == null)
            return;

        ItemDrop[] existingDrops = FindObjectsOfType<ItemDrop>();
        for (int i = 0; i < existingDrops.Length; i++)
            Destroy(existingDrops[i].gameObject);

        for (int i = 0; i < loadedSave.worldDrops.Count; i++)
        {
            WorldDropSaveRecord rec = loadedSave.worldDrops[i];
            if (rec == null || string.IsNullOrEmpty(rec.itemName))
                continue;

            GameObject prefab = Resources.Load<GameObject>(rec.itemName);
            if (prefab == null)
                continue;

            GameObject obj = Instantiate(prefab, new Vector3(rec.x, rec.y, rec.z), Quaternion.identity);
            ItemDrop drop = obj.GetComponent<ItemDrop>();
            if (drop != null)
            {
                drop.itemName = rec.itemName;
                drop.myPrefab = prefab;
            }
        }
    }

    // Attach Saveable Object. (Attach Saveable Object)
    public void AttachSaveableObject(GameObject obj, string uniqueId)
    {
        if (obj == null || string.IsNullOrEmpty(uniqueId))
            return;

        SaveableWorldObject saveable = obj.GetComponent<SaveableWorldObject>();
        if (saveable == null)
            saveable = obj.AddComponent<SaveableWorldObject>();

        saveable.Initialize(uniqueId);
    }

    IEnumerator FixPlayerSpawnNextFrame()
    {
        if (player == null)
            yield break;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        if (playerController == null)
            playerController = player.GetComponent<HandGamePlayer>();

        bool loadExistingPlayer = continueFromSave && loadedSave != null;

        Vector3 targetXZ;

        if (loadExistingPlayer)
        {
            Vector3 loadedPos = loadedSave.playerPosition.ToVector3();

            if (IsFiniteVector3(loadedPos))
                targetXZ = new Vector3(loadedPos.x, 0f, loadedPos.z);
            else
                targetXZ = Vector3.zero;
        }
        else
        {
            targetXZ = new Vector3(
                Mathf.Floor(player.position.x) + 0.5f,
                0f,
                Mathf.Floor(player.position.z) + 0.5f
            );
        }

        Vector2Int startCoord = WorldChunkUtility.WorldToChunkCoord(targetXZ, chunkRes);

        float waitTimer = 0f;
        while (waitTimer < 6f)
        {
            if (activeChunks.TryGetValue(startCoord, out GameObject chunkObj) && chunkObj != null)
            {
                MeshCollider mc = chunkObj.GetComponent<MeshCollider>();
                MeshFilter mf = chunkObj.GetComponent<MeshFilter>();

                if (mc != null && mc.sharedMesh != null && mf != null && mf.sharedMesh != null)
                    break;
            }

            waitTimer += Time.deltaTime;
            yield return null;
        }

        int spawnX = Mathf.FloorToInt(targetXZ.x);
        int spawnZ = Mathf.FloorToInt(targetXZ.z);

        float terrainY = GetTerrainHeight(spawnX, spawnZ);

        float upwardOffset = Mathf.Max(spawnExtraHeight, 4f);
        if (cc != null)
            upwardOffset = Mathf.Max(upwardOffset, cc.height * 0.75f + 0.5f);

        Vector3 safePos = new Vector3(
            spawnX + 0.5f,
            terrainY + upwardOffset,
            spawnZ + 0.5f
        );

        Vector3 rayStart = new Vector3(spawnX + 0.5f, worldHeight + 30f, spawnZ + 0.5f);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, worldHeight + 80f, ~0, QueryTriggerInteraction.Ignore))
        {
            float finalOffset = upwardOffset;
            if (cc != null)
                finalOffset = Mathf.Max(finalOffset, cc.height * 0.5f + 0.2f);

            safePos = new Vector3(
                Mathf.Floor(hit.point.x) + 0.5f,
                hit.point.y + finalOffset,
                Mathf.Floor(hit.point.z) + 0.5f
            );
        }

        if (!IsFiniteVector3(safePos))
        {
            safePos = new Vector3(0.5f, GetTerrainHeight(0, 0) + 5f, 0.5f);
        }

        player.position = safePos;

        yield return null;

        if (continueFromSave && loadedSave != null && playerController != null)
            playerController.ApplyLoadedRotation(loadedSave.playerYaw, loadedSave.playerPitch);

        if (cc != null)
            cc.enabled = true;

        if (hotbarManager != null)
        {
            if (continueFromSave && loadedSave != null)
                hotbarManager.LoadFromSave(loadedSave);
            else
                hotbarManager.ResetInventory();
        }

        if (dayNightCycle != null && continueFromSave && loadedSave != null)
        {
            dayNightCycle.SetTimeNormalized(loadedSave.timeOfDay);
            dayNightCycle.SetCycleEnabled(loadedSave.cycleEnabled);
        }

        if (!continueFromSave)
            SpawnStarterTools(safePos);

        if (continueFromSave && playerController != null)
            playerController.LoadHeldItems(loadedSave);

        if (continueFromSave)
            RestoreWorldDrops();

        SaveWorldNow();
    }

    bool IsFiniteFloat(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    bool IsFiniteVector3(Vector3 value)
    {
        return IsFiniteFloat(value.x) && IsFiniteFloat(value.y) && IsFiniteFloat(value.z);
    }

    void SpawnStarterTools(Vector3 playerSpawnPos)
    {
        GameObject[] tools = { axePrefab, hammerPrefab, crowbarPrefab };

        for (int i = 0; i < tools.Length; i++)
        {
            if (tools[i] == null)
                continue;

            Vector3 spawnPos =
                playerSpawnPos +
                player.forward * 2.4f +
                player.right * ((i - 1) * 1.3f) +
                Vector3.up * 1.2f;

            spawnPos = WorldChunkUtility.SnapToBlockCenter(spawnPos);
            GameObject obj = Instantiate(tools[i], spawnPos, Quaternion.identity);

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(Vector3.up * 1.8f + player.forward * 0.8f, ForceMode.Impulse);
            }

            if (obj.GetComponent<ItemPhysics>() == null)
                obj.AddComponent<ItemPhysics>();
        }
    }

    IEnumerator GenerateAroundPlayerInitialAsync()
    {
        if (player == null)
            yield break;

        Vector2Int center = GetPlayerChunkCoord();
        int spawnedThisFrame = 0;

        for (int r = 0; r <= viewDistance; r++)
        {
            foreach (Vector2Int coord in WorldChunkUtility.EnumerateShell(center, r))
            {
                if (activeChunks.ContainsKey(coord) || chunksBeingGenerated.Contains(coord))
                    continue;

                StartCoroutine(SpawnChunkAsync(coord));
                spawnedThisFrame++;

                if (spawnedThisFrame >= initialChunksPerFrame)
                {
                    spawnedThisFrame = 0;
                    yield return null;
                }
            }
        }
    }

    IEnumerator UpdateWorld()
    {
        while (true)
        {
            if (player == null)
            {
                yield return null;
                continue;
            }

            Vector2Int center = GetPlayerChunkCoord();
            yield return StartCoroutine(SpawnNextMissingChunkAsync(center));
            UnloadFarChunks(center);

            yield return new WaitForSeconds(worldUpdateInterval);
        }
    }

    IEnumerator SpawnNextMissingChunkAsync(Vector2Int center)
    {
        for (int r = 0; r <= viewDistance; r++)
        {
            foreach (Vector2Int coord in WorldChunkUtility.EnumerateShell(center, r))
            {
                if (activeChunks.ContainsKey(coord) || chunksBeingGenerated.Contains(coord))
                    continue;

                yield return StartCoroutine(SpawnChunkAsync(coord));
                yield break;
            }
        }
    }

    void UnloadFarChunks(Vector2Int center)
    {
        List<Vector2Int> toRemove = new List<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, GameObject> pair in activeChunks)
        {
            if (!WorldChunkUtility.IsInsideChunkRadius(center, pair.Key, viewDistance + 1))
                toRemove.Add(pair.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            Vector2Int coord = toRemove[i];
            if (!activeChunks.TryGetValue(coord, out GameObject chunkObj))
                continue;

            if (chunkObj != null)
            {
                Chunk chunk = chunkObj.GetComponent<Chunk>();
                if (chunk != null)
                    chunk.PrepareForChunkUnload();

                Destroy(chunkObj);
            }

            activeChunks.Remove(coord);
        }
    }

    Vector2Int GetPlayerChunkCoord()
    {
        return WorldChunkUtility.WorldToChunkCoord(player.position, chunkRes);
    }

    IEnumerator SpawnChunkAsync(Vector2Int coord)
    {
        if (chunkPrefab == null)
            yield break;

        if (activeChunks.ContainsKey(coord) || chunksBeingGenerated.Contains(coord))
            yield break;

        chunksBeingGenerated.Add(coord);

        Vector3 chunkWorldPos = WorldChunkUtility.SnapToGrid(WorldChunkUtility.ChunkToWorldPosition(coord, chunkRes));
        GameObject chunkObj = Instantiate(chunkPrefab, chunkWorldPos, Quaternion.identity, transform);
        chunkObj.name = $"Chunk_{coord.x}_{coord.y}";

        Chunk chunk = chunkObj.GetComponent<Chunk>();
        if (chunk == null)
        {
            Destroy(chunkObj);
            chunksBeingGenerated.Remove(coord);
            yield break;
        }

        activeChunks.Add(coord, chunkObj);
        yield return StartCoroutine(chunk.GenerateAsync(coord, chunkRes, this));

        yield return null;
        structureSpawner.TrySpawnWorldTool(coord, chunkObj.transform);

        yield return null;
        structureSpawner.TrySpawnChest(coord, chunkObj.transform);

        chunksBeingGenerated.Remove(coord);
    }

    // Spawn Chunk Environment Async. (Spawn Chunk Environment Async)
    public IEnumerator SpawnChunkEnvironmentAsync(Chunk chunk, Vector2Int coord, int res)
    {
        yield return structureSpawner.SpawnChunkStructuresAsync(chunk, coord, res);
    }

    // Sample final terrain height. (Sample final рельеф height)
    public float GetTerrainHeight(int worldX, int worldZ)
    {
        return terrainSampler.GetTerrainHeight(worldX, worldZ);
    }

    void ApplyViewDistanceVisuals()
    {
        if (!autoFogFromViewDistance)
            return;

        float visibleDistance = viewDistance * chunkRes;
        float fogStart = visibleDistance * fogStartMultiplier;
        float fogEnd = visibleDistance * fogEndMultiplier + extraFogOffset;

        if (fogEnd <= fogStart)
            fogEnd = fogStart + 1f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam != null)
            cam.farClipPlane = fogEnd + farClipPadding;

        Debug.Log($"[WorldGenerator] ViewDistance={viewDistance}, FogStart={fogStart}, FogEnd={fogEnd}");
    }

    // Set Seed. (Set Seed)
    public void SetSeed(string input)
    {
        if (int.TryParse(input, out int parsed))
        {
            seed = parsed;
            randomSeed = false;
        }
    }

    // Set View Distance. (Set View Distance)
    public void SetViewDistance(float value)
    {
        viewDistance = Mathf.RoundToInt(value);
        ApplyViewDistanceVisuals();
    }
}
