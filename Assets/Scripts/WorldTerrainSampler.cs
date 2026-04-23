using UnityEngine;

// ============================================================
// WorldTerrainSampler
// Deterministic terrain and biome sampling based on world seed. (Этот скрипт отвечает за: deterministic terrain and biome sampling based on world seed.)
// ============================================================
public sealed class WorldTerrainSampler
{
    public enum TerrainBiome
    {
        Plains,
        Hills,
        Forest
    }

    private readonly WorldGenerator settings;

    public WorldTerrainSampler(WorldGenerator settings)
    {
        this.settings = settings;
    }

    // Sample final terrain height. (Sample final рельеф height)
    public float GetTerrainHeight(int worldX, int worldZ)
    {
        float smoothed = SampleSmoothedHeight(worldX, worldZ);
        int center = Mathf.RoundToInt(smoothed);

        int hPX = Mathf.RoundToInt(SampleSmoothedHeight(worldX + 1, worldZ));
        int hNX = Mathf.RoundToInt(SampleSmoothedHeight(worldX - 1, worldZ));
        int hPZ = Mathf.RoundToInt(SampleSmoothedHeight(worldX, worldZ + 1));
        int hNZ = Mathf.RoundToInt(SampleSmoothedHeight(worldX, worldZ - 1));

        int minNeighbour = Mathf.Min(hPX, hNX, hPZ, hNZ);
        int maxNeighbour = Mathf.Max(hPX, hNX, hPZ, hNZ);

        int maxStep = 2;

        if (center > maxNeighbour + maxStep)
            center = maxNeighbour + maxStep;

        if (center < minNeighbour - maxStep)
            center = minNeighbour - maxStep;

        return Mathf.Clamp(center, 6, settings.worldHeight - 6);
    }

    // Blend nearby samples for smoother terrain. (Blend nearby samples for smoother рельеф)
    private float SampleSmoothedHeight(int worldX, int worldZ)
    {
        float center =
            SampleRawHeight(worldX, worldZ) * 0.40f +
            SampleRawHeight(worldX + 1, worldZ) * 0.12f +
            SampleRawHeight(worldX - 1, worldZ) * 0.12f +
            SampleRawHeight(worldX, worldZ + 1) * 0.12f +
            SampleRawHeight(worldX, worldZ - 1) * 0.12f +
            SampleRawHeight(worldX + 1, worldZ + 1) * 0.03f +
            SampleRawHeight(worldX - 1, worldZ + 1) * 0.03f +
            SampleRawHeight(worldX + 1, worldZ - 1) * 0.03f +
            SampleRawHeight(worldX - 1, worldZ - 1) * 0.03f;

        return Mathf.Clamp(center, 6f, settings.worldHeight - 6f);
    }

    // Sample raw noise-based terrain height. (Sample raw noise-based рельеф height)
    private float SampleRawHeight(int worldX, int worldZ)
    {
        TerrainBiome biome = GetBiome(worldX, worldZ);

        float macro = Mathf.PerlinNoise(
            (worldX + settings.seed * 0.09f) / Mathf.Max(1f, settings.continentScale * 1.9f),
            (worldZ + settings.seed * 0.09f) / Mathf.Max(1f, settings.continentScale * 1.9f)
        );

        float continent = Mathf.PerlinNoise(
            (worldX + settings.seed * 0.17f) / Mathf.Max(1f, settings.continentScale),
            (worldZ + settings.seed * 0.17f) / Mathf.Max(1f, settings.continentScale)
        );

        float mountain = Mathf.PerlinNoise(
            (worldX + settings.seed * 0.31f) / Mathf.Max(1f, settings.mountainScale),
            (worldZ + settings.seed * 0.31f) / Mathf.Max(1f, settings.mountainScale)
        );

        float detail = Mathf.PerlinNoise(
            (worldX + settings.seed * 0.61f) / Mathf.Max(1f, settings.detailScale),
            (worldZ + settings.seed * 0.61f) / Mathf.Max(1f, settings.detailScale)
        );

        float ridge = Mathf.PerlinNoise(
            (worldX + settings.seed * 0.77f) / Mathf.Max(1f, settings.mountainScale * 0.72f),
            (worldZ + settings.seed * 0.77f) / Mathf.Max(1f, settings.mountainScale * 0.72f)
        );

        float plainsNoise = Mathf.PerlinNoise(
            (worldX + settings.seed * 1.03f) / 180f,
            (worldZ + settings.seed * 1.03f) / 180f
        );

        macro = Mathf.SmoothStep(0f, 1f, macro);
        continent = Mathf.SmoothStep(0f, 1f, continent);
        mountain = Mathf.Pow(mountain, 1.28f);
        ridge = Mathf.Pow(ridge, 1.7f);

        float biomeHeightBonus;
        float mountainMultiplier;
        float detailMultiplier;
        float flattenStrength;

        switch (biome)
        {
            case TerrainBiome.Plains:
                biomeHeightBonus = -2.0f;
                mountainMultiplier = 0.22f;
                detailMultiplier = 0.10f;
                flattenStrength = 0.78f;
                break;

            case TerrainBiome.Hills:
                biomeHeightBonus = 2.0f;
                mountainMultiplier = 1.05f;
                detailMultiplier = 0.42f;
                flattenStrength = 0.32f;
                break;

            default: // Forest
                biomeHeightBonus = 0.8f;
                mountainMultiplier = 0.48f;
                detailMultiplier = 0.24f;
                flattenStrength = 0.50f;
                break;
        }

        float biomeBlend = GetBiomeBlendValue(worldX, worldZ);
        flattenStrength = Mathf.Lerp(flattenStrength, 0.45f, biomeBlend * 0.35f);

        float height =
            settings.baseGroundHeight +
            biomeHeightBonus +
            macro * (settings.continentStrength * 0.32f) +
            continent * settings.continentStrength +
            mountain * (settings.mountainStrength * mountainMultiplier) +
            ridge * (settings.mountainStrength * 0.18f * mountainMultiplier) +
            (detail - 0.5f) * (settings.detailStrength * detailMultiplier) +
            (plainsNoise - 0.5f) * 3.0f * (1f - flattenStrength * 0.55f);

        height = ApplyBlockyTerracing(height, flattenStrength);
        return Mathf.Clamp(height, 6f, settings.worldHeight - 6f);
    }

    // Apply Blocky Terracing. (Apply Blocky Terracing)
    private float ApplyBlockyTerracing(float height, float flattenStrength)
    {
        float floor = Mathf.Floor(height);
        float fractional = height - floor;

        float terraceStrength = flattenStrength * 0.35f;

        float smooth = Mathf.Lerp(
            height,
            floor + 0.5f,
            terraceStrength
        );

        return smooth;
    }

    // Sample biome for a world position. (Sample biome for a мира position)
    public TerrainBiome GetBiome(int worldX, int worldZ)
    {
        float biomeNoise = GetBiomeBlendValue(worldX, worldZ);
        float humidity = Mathf.PerlinNoise(
            (worldX + settings.seed * 1.41f) / Mathf.Max(1f, settings.forestBiomeScale * 0.95f),
            (worldZ + settings.seed * 1.41f) / Mathf.Max(1f, settings.forestBiomeScale * 0.95f)
        );

        float ridgeMask = Mathf.PerlinNoise(
            (worldX + settings.seed * 2.17f) / 140f,
            (worldZ + settings.seed * 2.17f) / 140f
        );

        if (biomeNoise < 0.34f)
            return TerrainBiome.Plains;

        if (biomeNoise > 0.67f && ridgeMask > 0.46f)
            return TerrainBiome.Hills;

        if (humidity > 0.54f)
            return TerrainBiome.Forest;

        return biomeNoise > 0.58f ? TerrainBiome.Hills : TerrainBiome.Forest;
    }

    // Get Biome Blend Value. (Get Biome Blend Value)
    public float GetBiomeBlendValue(int worldX, int worldZ)
    {
        return Mathf.PerlinNoise(
            (worldX + settings.seed * 0.83f) / 220f,
            (worldZ + settings.seed * 0.83f) / 220f
        );
    }

    // Get Forest Value. (Get Forest Value)
    public float GetForestValue(int worldX, int worldZ)
    {
        float baseForest = Mathf.PerlinNoise(
            (worldX + settings.seed) / Mathf.Max(1f, settings.forestBiomeScale),
            (worldZ + settings.seed) / Mathf.Max(1f, settings.forestBiomeScale)
        );

        TerrainBiome biome = GetBiome(worldX, worldZ);

        if (biome == TerrainBiome.Forest)
            return Mathf.Clamp01(0.68f + baseForest * 0.32f);

        if (biome == TerrainBiome.Hills)
            return Mathf.Clamp01(baseForest * 0.55f);

        return Mathf.Clamp01(baseForest * 0.38f);
    }

    // Is Forest. (Is Forest)
    public bool IsForest(int worldX, int worldZ)
    {
        return GetBiome(worldX, worldZ) == TerrainBiome.Forest || GetForestValue(worldX, worldZ) > settings.forestThreshold;
    }

    // Is Hills. (Is Hills)
    public bool IsHills(int worldX, int worldZ)
    {
        return GetBiome(worldX, worldZ) == TerrainBiome.Hills;
    }

    // Is Plains. (Is Plains)
    public bool IsPlains(int worldX, int worldZ)
    {
        return GetBiome(worldX, worldZ) == TerrainBiome.Plains;
    }

    // Get Tree Spawn Chance. (Get Tree Spawn Chance)
    public float GetTreeSpawnChance(int worldX, int worldZ)
    {
        TerrainBiome biome = GetBiome(worldX, worldZ);

        switch (biome)
        {
            case TerrainBiome.Forest:
                return settings.treeSpawnChanceForest;
            case TerrainBiome.Hills:
                return Mathf.Lerp(settings.treeSpawnChancePlains, settings.treeSpawnChanceForest, 0.45f);
            default:
                return settings.treeSpawnChancePlains * 0.85f;
        }
    }

    // Get Rock Spawn Chance. (Get Rock Spawn Chance)
    public float GetRockSpawnChance(int worldX, int worldZ)
    {
        TerrainBiome biome = GetBiome(worldX, worldZ);

        switch (biome)
        {
            case TerrainBiome.Hills:
                return Mathf.Max(settings.rockSpawnChanceForest, settings.rockSpawnChancePlains) * 1.35f;
            case TerrainBiome.Forest:
                return settings.rockSpawnChanceForest;
            default:
                return settings.rockSpawnChancePlains * 0.75f;
        }
    }

    // Get Iron Ore Spawn Chance. (Get Iron Ore Spawn Chance)
    public float GetIronOreSpawnChance(int worldX, int worldZ)
    {
        TerrainBiome biome = GetBiome(worldX, worldZ);

        switch (biome)
        {
            case TerrainBiome.Hills:
                return Mathf.Max(settings.ironOreSpawnChanceForest, settings.ironOreSpawnChancePlains) * 1.20f;
            case TerrainBiome.Forest:
                return settings.ironOreSpawnChanceForest;
            default:
                return settings.ironOreSpawnChancePlains;
        }
    }

    // Is Area Flat Enough For Structure. (Is Area Flat Enough For Structure)
    public bool IsAreaFlatEnoughForStructure(int worldX, int worldZ, int maxHeightDifference = 1)
    {
        int h0 = Mathf.RoundToInt(GetTerrainHeight(worldX, worldZ));
        int h1 = Mathf.RoundToInt(GetTerrainHeight(worldX + 1, worldZ));
        int h2 = Mathf.RoundToInt(GetTerrainHeight(worldX - 1, worldZ));
        int h3 = Mathf.RoundToInt(GetTerrainHeight(worldX, worldZ + 1));
        int h4 = Mathf.RoundToInt(GetTerrainHeight(worldX, worldZ - 1));

        int min = Mathf.Min(h0, h1, h2, h3, h4);
        int max = Mathf.Max(h0, h1, h2, h3, h4);

        return (max - min) <= maxHeightDifference;
    }

    // Get Structure Density Value. (Get Structure Density Value)
    public float GetStructureDensityValue(int worldX, int worldZ)
    {
        return Mathf.PerlinNoise(
            (worldX + settings.seed * 1.13f) / 56f,
            (worldZ + settings.seed * 1.13f) / 56f
        );
    }
}
