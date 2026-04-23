using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================================================
// Chunk
// Voxel chunk generation, block storage, mesh rebuilds, breaking logic, drops, and saved changes. (Этот скрипт отвечает за: voxel chunk generation, block storage, mesh rebuilds, breaking logic, drops, and saved changes.)
// ============================================================
public class Chunk : MonoBehaviour
{
    // Block IDs:
    // 0 = air
    // 1 = grass
    // 2 = dirt
    // 3 = stone
    // 4 = iron ore

    private int[,,] map;
    private int res;
    private int worldHeight;
    private WorldGenerator gen;
    private Vector2Int coord;

    private readonly Dictionary<Vector3Int, float> damageMap = new Dictionary<Vector3Int, float>();

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Break Settings")]
    public float groundBreakTime = 0.45f;
    public float stoneBreakTime = 1.0f;
    public float wrongToolStonePenalty = 2.5f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Optional Drops")]
    public GameObject stoneDropPrefab;
    public GameObject groundDropPrefab;
    public GameObject ironOreDropPrefab;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Ore Generation")]
    [Range(0f, 1f)] public float ironOreVeinChance = 0.11f;
    public int ironOreMinVeinSize = 3;
    public int ironOreMaxVeinSize = 8;

    private bool isGenerated = false;

    // Generate this system asynchronously. (Generate эту систему asynchronously)
    public IEnumerator GenerateAsync(Vector2Int coord, int res, WorldGenerator gen)
    {
        this.coord = coord;
        this.res = res;
        this.gen = gen;
        this.worldHeight = gen.worldHeight;

        map = new int[res, worldHeight, res];

        EnsureRenderComponents();
        yield return null;

        yield return StartCoroutine(GenerateTerrainAsync());
        yield return StartCoroutine(GenerateIronOreAsync());
        yield return StartCoroutine(ApplySavedBlockChangesAsync());

        RebuildMesh(false);
        yield return null;

        ApplyColliderFromCurrentMesh();
        yield return null;

        yield return gen.SpawnChunkEnvironmentAsync(this, coord, res);
        isGenerated = true;
    }

    // Prepare objects before chunk unload. (Prepare objects before чанка unзагрузка)
    public void PrepareForChunkUnload()
    {
        SaveableWorldObject[] saveables = GetComponentsInChildren<SaveableWorldObject>(true);
        for (int i = 0; i < saveables.Length; i++)
            saveables[i].PrepareForChunkUnload();
    }

    void EnsureRenderComponents()
    {
        if (GetComponent<MeshFilter>() == null)
            gameObject.AddComponent<MeshFilter>();

        if (GetComponent<MeshRenderer>() == null)
            gameObject.AddComponent<MeshRenderer>();

        if (GetComponent<MeshCollider>() == null)
            gameObject.AddComponent<MeshCollider>();
    }

    IEnumerator GenerateTerrainAsync()
    {
        for (int x = 0; x < res; x++)
        {
            for (int z = 0; z < res; z++)
            {
                FillColumnFromTerrain(x, z);
            }

            if (x % 4 == 3)
                yield return null;
        }
    }

    IEnumerator GenerateIronOreAsync()
    {
        if (gen == null)
            yield break;

        System.Random rng = new System.Random(gen.seed ^ (coord.x * 73856093) ^ (coord.y * 19349663) ^ 9157);
        int attempts = Mathf.Max(1, Mathf.RoundToInt(res * res * ironOreVeinChance * 0.22f));

        for (int i = 0; i < attempts; i++)
        {
            int startX = rng.Next(1, Mathf.Max(2, res - 1));
            int startZ = rng.Next(1, Mathf.Max(2, res - 1));

            int worldX = WorldChunkUtility.ToWorldX(coord, startX, res);
            int worldZ = WorldChunkUtility.ToWorldZ(coord, startZ, res);

            int terrainHeight = Mathf.RoundToInt(gen.GetTerrainHeight(worldX, worldZ));
            int maxY = Mathf.Min(
                Mathf.Min(gen.ironOreMaxHeight, terrainHeight - 3),
                worldHeight - 2
            );

            int minY = Mathf.Clamp(gen.ironOreMinHeight, 2, worldHeight - 2);

            if (maxY <= minY)
                continue;

            float biomeChance = gen.TerrainSampler != null
                ? gen.TerrainSampler.GetIronOreSpawnChance(worldX, worldZ)
                : gen.ironOreSpawnChancePlains;

            bool isHills = gen.TerrainSampler != null && gen.TerrainSampler.IsHills(worldX, worldZ);
            bool isForest = gen.TerrainSampler != null && gen.TerrainSampler.IsForest(worldX, worldZ);

            float roll = (float)rng.NextDouble();
            float spawnThreshold = Mathf.Clamp01(biomeChance * (isHills ? 5.2f : (isForest ? 3.8f : 3.1f)));

            if (roll > spawnThreshold)
                continue;

            int startY = rng.Next(minY, maxY + 1);
            int veinSize = rng.Next(
                Mathf.Max(1, ironOreMinVeinSize),
                Mathf.Max(ironOreMinVeinSize + 1, ironOreMaxVeinSize + 1)
            );

            CarveIronOreVein(startX, startY, startZ, veinSize, rng);

            if (i % 3 == 2)
                yield return null;
        }
    }

    void CarveIronOreVein(int startX, int startY, int startZ, int veinSize, System.Random rng)
    {
        Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        Vector3Int start = new Vector3Int(startX, startY, startZ);
        frontier.Enqueue(start);
        visited.Add(start);

        int placed = 0;

        while (frontier.Count > 0 && placed < veinSize)
        {
            Vector3Int current = frontier.Dequeue();

            if (!Inside(current.x, current.y, current.z))
                continue;

            if (map[current.x, current.y, current.z] == 3)
            {
                map[current.x, current.y, current.z] = 4;
                placed++;
            }

            TryQueueVeinNeighbor(current + Vector3Int.right, rng, frontier, visited);
            TryQueueVeinNeighbor(current + Vector3Int.left, rng, frontier, visited);
            TryQueueVeinNeighbor(current + new Vector3Int(0, 1, 0), rng, frontier, visited);
            TryQueueVeinNeighbor(current + new Vector3Int(0, -1, 0), rng, frontier, visited);
            TryQueueVeinNeighbor(current + new Vector3Int(0, 0, 1), rng, frontier, visited);
            TryQueueVeinNeighbor(current + new Vector3Int(0, 0, -1), rng, frontier, visited);
        }
    }

    void TryQueueVeinNeighbor(Vector3Int pos, System.Random rng, Queue<Vector3Int> frontier, HashSet<Vector3Int> visited)
    {
        if (!Inside(pos.x, pos.y, pos.z))
            return;

        if (!visited.Add(pos))
            return;

        if (map[pos.x, pos.y, pos.z] != 3)
            return;

        if ((float)rng.NextDouble() > 0.72f)
            return;

        frontier.Enqueue(pos);
    }

    void FillColumnFromTerrain(int x, int z)
    {
        int worldX = WorldChunkUtility.ToWorldX(coord, x, res);
        int worldZ = WorldChunkUtility.ToWorldZ(coord, z, res);
        int height = Mathf.RoundToInt(gen.GetTerrainHeight(worldX, worldZ));

        if (height < 0)
            return;

        for (int y = 0; y <= height && y < worldHeight; y++)
        {
            int depthFromTop = height - y;

            if (depthFromTop == 0)
                map[x, y, z] = 1; // grass
            else if (depthFromTop <= 3)
                map[x, y, z] = 2; // dirt
            else
                map[x, y, z] = 3; // stone
        }
    }

    IEnumerator ApplySavedBlockChangesAsync()
    {
        for (int x = 0; x < res; x++)
        {
            for (int z = 0; z < res; z++)
            {
                int worldX = WorldChunkUtility.ToWorldX(coord, x, res);
                int worldZ = WorldChunkUtility.ToWorldZ(coord, z, res);

                for (int y = 0; y < worldHeight; y++)
                {
                    if (WorldSaveSystem.TryGetBlockType(worldX, y, worldZ, out int savedType))
                        map[x, y, z] = savedType;
                }
            }

            if (x % 2 == 1)
                yield return null;
        }
    }

    // Get Top Solid Y. (Get Top Solid Y)
    public int GetTopSolidY(int x, int z)
    {
        for (int y = worldHeight - 1; y >= 0; y--)
        {
            if (map[x, y, z] != 0)
                return y;
        }

        return -1;
    }

    // Get Local Block Type. (Get Local Block Type)
    public int GetLocalBlockType(int x, int y, int z)
    {
        if (!Inside(x, y, z))
            return 0;

        return map[x, y, z];
    }

    bool Inside(int x, int y, int z)
    {
        return x >= 0 && x < res &&
               y >= 0 && y < worldHeight &&
               z >= 0 && z < res;
    }

    bool IsAir(int x, int y, int z)
    {
        if (!Inside(x, y, z))
            return true;

        return map[x, y, z] == 0;
    }

    // World To Local Block. (World To Local Block)
    public bool WorldToLocalBlock(Vector3 worldPos, out int x, out int y, out int z)
    {
        Vector3 local = worldPos - transform.position;
        x = Mathf.FloorToInt(local.x);
        y = Mathf.FloorToInt(local.y);
        z = Mathf.FloorToInt(local.z);
        return Inside(x, y, z);
    }

    // Try to Get Block At World. (Try to Get Block At World)
    public bool TryGetBlockAtWorld(Vector3 worldPos, out int blockType, out Vector3 worldCenter)
    {
        blockType = 0;
        worldCenter = Vector3.zero;

        if (!WorldToLocalBlock(worldPos, out int x, out int y, out int z))
            return false;

        blockType = map[x, y, z];
        if (blockType == 0)
            return false;

        worldCenter = transform.position + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
        return true;
    }

    // Get Block Type. (Get Block Type)
    public int GetBlockType(Vector3 worldPos)
    {
        if (!WorldToLocalBlock(worldPos, out int x, out int y, out int z))
            return 0;

        return map[x, y, z];
    }

    // Can Break Block At World. (Can Break Block At World)
    public bool CanBreakBlockAtWorld(Vector3 worldPos, ResourceType toolType)
    {
        int blockType = GetBlockType(worldPos);
        return blockType != 0;
    }

    // Get Break Multiplier For Block. (Get Break Multiplier For Block)
    public float GetBreakMultiplierForBlock(Vector3 worldPos, ResourceType toolType)
    {
        int blockType = GetBlockType(worldPos);

        if (blockType == 0)
            return 0f;

        if (blockType == 1 || blockType == 2)
            return 1f;

        if (blockType == 3 || blockType == 4)
        {
            bool properTool = toolType == ResourceType.Stone || toolType == ResourceType.Universal;
            float multiplier = properTool ? 1f : (1f / wrongToolStonePenalty);

            if (blockType == 4)
                multiplier *= 0.9f;

            return multiplier;
        }

        return 1f;
    }

    // Break Block At World. (Break Block At World)
    public void BreakBlockAtWorld(Vector3 worldPos, float damagePerSecond)
    {
        if (!WorldToLocalBlock(worldPos, out int x, out int y, out int z))
            return;

        int blockType = map[x, y, z];
        if (blockType == 0)
            return;

        Vector3Int key = new Vector3Int(x, y, z);
        if (!damageMap.ContainsKey(key))
            damageMap[key] = 0f;

        damageMap[key] += damagePerSecond * Time.deltaTime;
        float targetBreakTime = (blockType == 3 || blockType == 4) ? stoneBreakTime : groundBreakTime;

        if (damageMap[key] < targetBreakTime)
            return;

        Vector3 worldDropPos = transform.position + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
        SpawnDropForBlock(blockType, worldDropPos);

        map[x, y, z] = 0;
        damageMap.Remove(key);

        int worldX = WorldChunkUtility.ToWorldX(coord, x, res);
        int worldZ = WorldChunkUtility.ToWorldZ(coord, z, res);
        WorldSaveSystem.SetBlockType(worldX, y, worldZ, 0);

        RebuildMesh(true);
    }

    void SpawnDropForBlock(int blockType, Vector3 pos)
    {
        GameObject prefab = null;

        if (blockType == 4)
            prefab = ironOreDropPrefab;
        else if (blockType == 3)
            prefab = stoneDropPrefab;
        else if (blockType == 1 || blockType == 2)
            prefab = groundDropPrefab;

        if (prefab == null)
            return;

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 force = new Vector3(
                Random.Range(-1.0f, 1.0f),
                Random.Range(2.0f, 3.5f),
                Random.Range(-1.0f, 1.0f)
            );
            rb.AddForce(force, ForceMode.Impulse);
        }
    }

    void RebuildMesh(bool applyColliderToo)
    {
        EnsureRenderComponents();

        MeshFilter grassFilter = gen.groundPrefab != null ? gen.groundPrefab.GetComponent<MeshFilter>() : null;
        MeshFilter dirtFilter = gen.dirtPrefab != null ? gen.dirtPrefab.GetComponent<MeshFilter>() : null;
        MeshFilter stoneFilter = gen.stonePrefab != null ? gen.stonePrefab.GetComponent<MeshFilter>() : null;
        GameObject ironVisualPrefab = gen.ironOreBlockPrefab != null ? gen.ironOreBlockPrefab : gen.ironOrePrefab;
        MeshFilter ironOreFilter = ironVisualPrefab != null ? ironVisualPrefab.GetComponent<MeshFilter>() : null;

        if (grassFilter == null || grassFilter.sharedMesh == null)
        {
            Debug.LogError("[Chunk] У groundPrefab нет MeshFilter/sharedMesh");
            return;
        }

        if (dirtFilter == null || dirtFilter.sharedMesh == null)
        {
            Debug.LogError("[Chunk] У dirtPrefab нет MeshFilter/sharedMesh");
            return;
        }

        if (stoneFilter == null || stoneFilter.sharedMesh == null)
        {
            Debug.LogError("[Chunk] У stonePrefab нет MeshFilter/sharedMesh");
            return;
        }

        if (ironOreFilter == null || ironOreFilter.sharedMesh == null)
        {
            Debug.LogError("[Chunk] Назначь ironOreBlockPrefab (или ironOrePrefab с MeshFilter/sharedMesh)");
            return;
        }

        List<CombineInstance> grass = new List<CombineInstance>();
        List<CombineInstance> dirt = new List<CombineInstance>();
        List<CombineInstance> stone = new List<CombineInstance>();
        List<CombineInstance> ironOre = new List<CombineInstance>();

        CollectVisibleBlocks(
            grassFilter.sharedMesh,
            dirtFilter.sharedMesh,
            stoneFilter.sharedMesh,
            ironOreFilter.sharedMesh,
            grass,
            dirt,
            stone,
            ironOre
        );

        Mesh grassPart = BuildSubMesh(grass);
        Mesh dirtPart = BuildSubMesh(dirt);
        Mesh stonePart = BuildSubMesh(stone);
        Mesh ironOrePart = BuildSubMesh(ironOre);
        Mesh chunkMesh = BuildChunkMesh(grassPart, dirtPart, stonePart, ironOrePart);

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        MeshCollider mc = GetComponent<MeshCollider>();

        if (mf.sharedMesh != null)
            Destroy(mf.sharedMesh);

        mf.sharedMesh = chunkMesh;

        Material grassMat = gen.groundPrefab.GetComponent<MeshRenderer>()?.sharedMaterial;
        Material dirtMat = gen.dirtPrefab.GetComponent<MeshRenderer>()?.sharedMaterial;
        Material stoneMat = gen.stonePrefab.GetComponent<MeshRenderer>()?.sharedMaterial;
        Material ironOreMat = ironVisualPrefab != null ? ironVisualPrefab.GetComponent<MeshRenderer>()?.sharedMaterial : null;
        mr.sharedMaterials = new Material[] { grassMat, dirtMat, stoneMat, ironOreMat };

        if (applyColliderToo)
        {
            mc.sharedMesh = null;
            mc.sharedMesh = chunkMesh;
        }
    }

    void CollectVisibleBlocks(
        Mesh grassMesh,
        Mesh dirtMesh,
        Mesh stoneMesh,
        Mesh ironOreMesh,
        List<CombineInstance> grass,
        List<CombineInstance> dirt,
        List<CombineInstance> stone,
        List<CombineInstance> ironOre)
    {
        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                for (int z = 0; z < res; z++)
                {
                    int type = map[x, y, z];
                    if (type == 0 || IsFullyHidden(x, y, z))
                        continue;

                    Mesh sourceMesh = null;

                    if (type == 1) sourceMesh = grassMesh;
                    else if (type == 2) sourceMesh = dirtMesh;
                    else if (type == 3) sourceMesh = stoneMesh;
                    else if (type == 4) sourceMesh = ironOreMesh;

                    if (sourceMesh == null)
                        continue;

                    CombineInstance instance = new CombineInstance
                    {
                        mesh = sourceMesh,
                        transform = Matrix4x4.TRS(
                            new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
                            Quaternion.identity,
                            Vector3.one
                        )
                    };

                    if (type == 1) grass.Add(instance);
                    else if (type == 2) dirt.Add(instance);
                    else if (type == 3) stone.Add(instance);
                    else if (type == 4) ironOre.Add(instance);
                }
            }
        }
    }

    bool IsFullyHidden(int x, int y, int z)
    {
        return !IsAir(x + 1, y, z) &&
               !IsAir(x - 1, y, z) &&
               !IsAir(x, y + 1, z) &&
               !IsAir(x, y - 1, z) &&
               !IsAir(x, y, z + 1) &&
               !IsAir(x, y, z - 1);
    }

    Mesh BuildSubMesh(List<CombineInstance> combine)
    {
        Mesh part = new Mesh();
        part.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        if (combine.Count > 0)
            part.CombineMeshes(combine.ToArray(), true, true);

        return part;
    }

    Mesh BuildChunkMesh(Mesh grassPart, Mesh dirtPart, Mesh stonePart, Mesh ironOrePart)
    {
        Mesh chunkMesh = new Mesh();
        chunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        chunkMesh.subMeshCount = 4;

        CombineInstance[] final = new CombineInstance[4];
        final[0] = new CombineInstance { mesh = grassPart, transform = Matrix4x4.identity };
        final[1] = new CombineInstance { mesh = dirtPart, transform = Matrix4x4.identity };
        final[2] = new CombineInstance { mesh = stonePart, transform = Matrix4x4.identity };
        final[3] = new CombineInstance { mesh = ironOrePart, transform = Matrix4x4.identity };

        chunkMesh.CombineMeshes(final, false, false);
        chunkMesh.RecalculateBounds();
        chunkMesh.RecalculateNormals();
        return chunkMesh;
    }

    void ApplyColliderFromCurrentMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider mc = GetComponent<MeshCollider>();

        if (mf == null || mc == null)
            return;

        mc.sharedMesh = null;
        mc.sharedMesh = mf.sharedMesh;
    }
}
