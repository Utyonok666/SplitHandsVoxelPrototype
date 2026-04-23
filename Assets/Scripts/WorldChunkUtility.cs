using UnityEngine;
using System.Collections.Generic;

// ============================================================
// WorldChunkUtility
// Shared chunk/grid coordinate conversion helpers. (Этот скрипт отвечает за: shared chunk/grid coordinate conversion helpers.)
// ============================================================
public static class WorldChunkUtility
{
    // Convert world position to chunk coordinate. (Convert мира position to чанка coordinate)
    public static Vector2Int WorldToChunkCoord(Vector3 worldPosition, int chunkRes)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkRes),
            Mathf.FloorToInt(worldPosition.z / chunkRes)
        );
    }

    // Convert chunk coordinate to world origin. (Convert чанка coordinate to мира origin)
    public static Vector3 ChunkToWorldPosition(Vector2Int coord, int chunkRes)
    {
        return new Vector3(coord.x * chunkRes, 0f, coord.y * chunkRes);
    }

    // Convert local chunk X to world X. (Convert local чанка X to мира X)
    public static int ToWorldX(Vector2Int chunkCoord, int localX, int chunkRes)
    {
        return chunkCoord.x * chunkRes + localX;
    }

    // Convert local chunk Z to world Z. (Convert local чанка Z to мира Z)
    public static int ToWorldZ(Vector2Int chunkCoord, int localZ, int chunkRes)
    {
        return chunkCoord.y * chunkRes + localZ;
    }

    // Check whether a chunk is inside the load radius. (Check whether a чанка is inside the загрузка radius)
    public static bool IsInsideChunkRadius(Vector2Int center, Vector2Int coord, int radius)
    {
        int dx = Mathf.Abs(coord.x - center.x);
        int dz = Mathf.Abs(coord.y - center.y);
        return Mathf.Max(dx, dz) <= radius;
    }

    // Iterate coordinates on the outer shell of a chunk radius. (Iterate coordinates on the outer shell of a чанка radius)
    public static IEnumerable<Vector2Int> EnumerateShell(Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != radius)
                    continue;

                yield return new Vector2Int(center.x + x, center.y + z);
            }
        }
    }

    // Snap a position to whole-number grid coordinates. (Snap a position to whole-number grid coordinates)
    public static Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x),
            Mathf.Round(pos.y),
            Mathf.Round(pos.z)
        );
    }

    // Snap a position to the visual center of a voxel block. (Snap a position to the visual center of a voxel block)
    public static Vector3 SnapToBlockCenter(Vector3 pos)
    {
        return new Vector3(
            Mathf.Floor(pos.x) + 0.5f,
            Mathf.Round(pos.y),
            Mathf.Floor(pos.z) + 0.5f
        );
    }
}
