using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================================================
// StructureSpawner
// Spawns environment structures like rocks, trees, or other prefabs. (Этот скрипт отвечает за: spawns environment structures like rocks, trees, or other prefabs.)
// ============================================================
public class StructureSpawner
{
    private readonly WorldGenerator world;
    private readonly WorldTerrainSampler terrain;

    public StructureSpawner(WorldGenerator world, WorldTerrainSampler terrain)
    {
        this.world = world;
        this.terrain = terrain;
    }

    // Spawn Chunk Structures Async. (Spawn Chunk Structures Async)
    public IEnumerator SpawnChunkStructuresAsync(Chunk chunk, Vector2Int coord, int res)
    {
        if (chunk == null)
            yield break;

        if (world.treePrefab == null &&
            world.rockStructurePrefab == null &&
            world.ironOrePrefab == null)
            yield break;

        System.Random rng = new System.Random(world.seed ^ (coord.x * 92821) ^ (coord.y * 68917));
        int spawnedCount = 0;

        for (int x = 1; x < res - 1; x++)
        {
            for (int z = 1; z < res - 1; z++)
            {
                int y = chunk.GetTopSolidY(x, z);
                if (y <= 0)
                    continue;

                int topBlockType = chunk.GetLocalBlockType(x, y, z);
                if (topBlockType != 1)
                    continue;

                int worldX = WorldChunkUtility.ToWorldX(coord, x, res);
                int worldZ = WorldChunkUtility.ToWorldZ(coord, z, res);

                bool isForest = terrain.IsForest(worldX, worldZ);
                bool isHills = terrain.IsHills(worldX, worldZ);
                bool isPlains = terrain.IsPlains(worldX, worldZ);

                int flatnessTolerance = isHills ? 2 : 1;
                if (!terrain.IsAreaFlatEnoughForStructure(worldX, worldZ, flatnessTolerance))
                    continue;

                float density = terrain.GetStructureDensityValue(worldX, worldZ);
                if (density < (isPlains ? 0.34f : 0.40f))
                    continue;

                Vector3 pos = chunk.transform.position + new Vector3(x + 0.5f, y + 1f, z + 0.5f);
                pos = WorldChunkUtility.SnapToBlockCenter(pos);

                bool spawned = false;
                float roll = (float)rng.NextDouble();

                float treeChance = terrain.GetTreeSpawnChance(worldX, worldZ);
                float rockChance = terrain.GetRockSpawnChance(worldX, worldZ);
                float ironChance = terrain.GetIronOreSpawnChance(worldX, worldZ);

                if (isForest)
                {
                    treeChance *= Mathf.Lerp(0.95f, 1.30f, density);
                    rockChance *= Mathf.Lerp(0.65f, 1.00f, density);
                    ironChance *= 0.90f;
                }
                else if (isHills)
                {
                    treeChance *= Mathf.Lerp(0.55f, 0.85f, density);
                    rockChance *= Mathf.Lerp(1.15f, 1.65f, density);
                    ironChance *= Mathf.Lerp(1.05f, 1.35f, density);
                }
                else if (isPlains)
                {
                    treeChance *= Mathf.Lerp(0.35f, 0.65f, density);
                    rockChance *= Mathf.Lerp(0.55f, 0.90f, density);
                    ironChance *= 0.90f;
                }

                float treeCluster = terrain.GetStructureDensityValue(worldX + 213, worldZ + 213);
                float rockCluster = terrain.GetStructureDensityValue(worldX + 700, worldZ + 700);
                float ironCluster = terrain.GetStructureDensityValue(worldX + 1337, worldZ + 1337);

                if (isForest)
                {
                    if (treeCluster < 0.40f)
                        treeChance *= 0.35f;
                    else
                        treeChance *= Mathf.Lerp(0.85f, 1.20f, treeCluster);
                }
                else if (isPlains)
                {
                    if (treeCluster < 0.55f)
                        treeChance = 0f;
                    else
                        treeChance *= Mathf.Lerp(0.45f, 0.80f, treeCluster);
                }
                else
                {
                    if (treeCluster < 0.50f)
                        treeChance *= 0.25f;
                    else
                        treeChance *= Mathf.Lerp(0.55f, 0.90f, treeCluster);
                }

                if (isHills)
                {
                    if (rockCluster < 0.48f)
                        rockChance *= 0.45f;
                    else
                        rockChance *= Mathf.Lerp(0.90f, 1.45f, rockCluster);
                }
                else
                {
                    if (rockCluster < 0.64f)
                        rockChance = 0f;
                    else
                        rockChance *= Mathf.Lerp(0.45f, 0.95f, rockCluster);
                }

                if (ironCluster < (isHills ? 0.40f : 0.58f))
                    ironChance *= 0.35f;
                else
                    ironChance *= Mathf.Lerp(0.80f, 1.15f, ironCluster);

                treeChance = Mathf.Clamp01(treeChance);
                rockChance = Mathf.Clamp01(rockChance);
                ironChance = Mathf.Clamp01(ironChance);

                if (roll < treeChance && world.treePrefab != null)
                {
                    float treeRadius = isForest ? 1.6f : 1.9f;

                    if (!HasNearbyStructure(pos, treeRadius, true, false, false, false))
                    {
                        SpawnSaveableObject(world.treePrefab, pos, $"tree_{coord.x}_{coord.y}_{x}_{y}_{z}", chunk.transform);
                        spawned = true;
                    }
                }
                else if (roll < treeChance + rockChance && world.rockStructurePrefab != null)
                {
                    float rockRadius = isHills ? 2.8f : 2.35f;

                    if (!HasNearbyStructure(pos, rockRadius, false, true, false, false))
                    {
                        SpawnSaveableObject(world.rockStructurePrefab, pos, $"rock_{coord.x}_{coord.y}_{x}_{y}_{z}", chunk.transform);
                        spawned = true;
                    }
                }
                else if (CanSpawnIronOre(y) &&
                         roll < treeChance + rockChance + ironChance &&
                         world.ironOrePrefab != null)
                {
                    float ironRadius = isHills ? 1.6f : 1.3f;

                    if (!HasNearbyStructure(pos, ironRadius, false, false, false, true))
                    {
                        SpawnSaveableObject(world.ironOrePrefab, pos, $"iron_{coord.x}_{coord.y}_{x}_{y}_{z}", chunk.transform);
                        spawned = true;
                    }
                }

                if (spawned)
                {
                    spawnedCount++;
                    if (spawnedCount % 3 == 0)
                        yield return null;
                }
            }

            if (x % 4 == 3)
                yield return null;
        }
    }

    // Can Spawn Iron Ore. (Can Spawn Iron Ore)
    private bool CanSpawnIronOre(int terrainY)
    {
        return terrainY >= world.ironOreMinHeight && terrainY <= world.ironOreMaxHeight;
    }

    // Try to Spawn World Tool. (Try to Spawn World Tool)
    public void TrySpawnWorldTool(Vector2Int coord, Transform parent)
    {
        System.Random rng = new System.Random(world.seed ^ (coord.x * 92821) ^ (coord.y * 68917) ^ 12345);
        if ((float)rng.NextDouble() > world.toolSpawnChancePerChunk)
            return;

        List<GameObject> available = new List<GameObject>();
        CollectIfExists(available, world.axePrefab);
        CollectIfExists(available, world.hammerPrefab);
        CollectIfExists(available, world.crowbarPrefab);

        if (available.Count == 0)
            return;

        int localX = rng.Next(2, world.chunkRes - 2);
        int localZ = rng.Next(2, world.chunkRes - 2);
        string uniqueId = $"tool_{coord.x}_{coord.y}_{localX}_{localZ}";

        if (WorldSaveSystem.IsWorldObjectRemoved(uniqueId))
            return;

        int worldX = WorldChunkUtility.ToWorldX(coord, localX, world.chunkRes);
        int worldZ = WorldChunkUtility.ToWorldZ(coord, localZ, world.chunkRes);
        bool isHills = terrain.IsHills(worldX, worldZ);

        if (!terrain.IsAreaFlatEnoughForStructure(worldX, worldZ, isHills ? 2 : 1))
            return;

        Vector3 spawnPos = new Vector3(worldX + 0.5f, terrain.GetTerrainHeight(worldX, worldZ) + 1.5f, worldZ + 0.5f);
        spawnPos = WorldChunkUtility.SnapToBlockCenter(spawnPos);

        GameObject prefab = available[rng.Next(0, available.Count)];
        GameObject obj = Object.Instantiate(prefab, spawnPos, Quaternion.identity, parent);
        world.AttachSaveableObject(obj, uniqueId);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(Vector3.up * 1.2f, ForceMode.Impulse);
        }

        if (obj.GetComponent<ItemPhysics>() == null)
            obj.AddComponent<ItemPhysics>();
    }

    // Try to Spawn Chest. (Try to Spawn Chest)
    public void TrySpawnChest(Vector2Int coord, Transform parent)
    {
        if (world.chestPrefab == null)
            return;

        System.Random rng = new System.Random(world.seed ^ (coord.x * 73856093) ^ (coord.y * 19349663));
        if ((float)rng.NextDouble() > world.chestSpawnChance)
            return;

        int localX = rng.Next(2, world.chunkRes - 2);
        int localZ = rng.Next(2, world.chunkRes - 2);
        string uniqueId = $"chest_{coord.x}_{coord.y}_{localX}_{localZ}";

        if (WorldSaveSystem.IsWorldObjectRemoved(uniqueId))
            return;

        int worldX = WorldChunkUtility.ToWorldX(coord, localX, world.chunkRes);
        int worldZ = WorldChunkUtility.ToWorldZ(coord, localZ, world.chunkRes);
        bool isHills = terrain.IsHills(worldX, worldZ);

        if (!terrain.IsAreaFlatEnoughForStructure(worldX, worldZ, isHills ? 2 : 1))
            return;

        float y = terrain.GetTerrainHeight(worldX, worldZ) + 1f;
        Vector3 spawnPos = new Vector3(worldX + 0.5f, y + 0.25f, worldZ + 0.5f);
        spawnPos = WorldChunkUtility.SnapToBlockCenter(spawnPos);

        if (HasNearbyStructure(spawnPos, 1.25f, false, false, true, false))
            return;

        Quaternion chestRotation = Quaternion.Euler(0f, 0f, 90f);

        GameObject obj = Object.Instantiate(world.chestPrefab, spawnPos, chestRotation, parent);
        world.AttachSaveableObject(obj, uniqueId);
    }

    // Spawn Saveable Object. (Spawn Saveable Object)
    private void SpawnSaveableObject(GameObject prefab, Vector3 pos, string uniqueId, Transform parent)
    {
        if (prefab == null || WorldSaveSystem.IsWorldObjectRemoved(uniqueId))
            return;

        GameObject obj = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
        obj.transform.position = WorldChunkUtility.SnapToBlockCenter(obj.transform.position);

        if (prefab == world.treePrefab)
        {
            Vector3 baseScale = obj.transform.localScale;

            float widthScale = Random.Range(0.85f, 1.20f);
            float heightScale = Random.Range(0.82f, 1.35f);

            obj.transform.localScale = new Vector3(
                baseScale.x * widthScale,
                baseScale.y * heightScale,
                baseScale.z * widthScale
            );

            obj.transform.Rotate(0f, Random.Range(0f, 360f), 0f);
        }

        world.AttachSaveableObject(obj, uniqueId);
    }

    // Has Nearby Structure. (Has Nearby Structure)
    private bool HasNearbyStructure(
        Vector3 pos,
        float radius,
        bool checkTrees,
        bool checkRocks,
        bool checkChests,
        bool checkIron)
    {
        Collider[] hits = Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            string lowerName = hits[i].transform.root.name.ToLowerInvariant();

            if (checkTrees && lowerName.Contains("tree"))
                return true;

            if (checkRocks && lowerName.Contains("rock"))
                return true;

            if (checkChests && lowerName.Contains("chest"))
                return true;

            if (checkIron && lowerName.Contains("iron"))
                return true;
        }

        return false;
    }

    // Collect If Exists. (Collect If Exists)
    private void CollectIfExists(List<GameObject> list, GameObject prefab)
    {
        if (prefab != null)
            list.Add(prefab);
    }
}
