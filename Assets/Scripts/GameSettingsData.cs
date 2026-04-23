using UnityEngine;

// ============================================================
// GameSettingsData
// Serializable container for game settings values. (Этот скрипт отвечает за: serializable container for game settings values.)
// ============================================================
public static class GameSettingsData
{
    public static int Seed;
    public static int ViewDistance = 5;
    public static bool HasPendingLaunchSettings;
    public static bool ContinueRequested;

    // Set New Game. (Set New Game)
    public static void SetNewGame(int seed, int viewDistance)
    {
        Seed = seed;
        ViewDistance = viewDistance;
        HasPendingLaunchSettings = true;
        ContinueRequested = false;
    }

    // Set Continue Game. (Set Continue Game)
    public static void SetContinueGame()
    {
        HasPendingLaunchSettings = false;
        ContinueRequested = true;
    }

    // Clear Pending Launch. (Clear Pending Launch)
    public static void ClearPendingLaunch()
    {
        HasPendingLaunchSettings = false;
        ContinueRequested = false;
    }

    // Stable String To Seed. (Stable String To Seed)
    public static int StableStringToSeed(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Random.Range(1, int.MaxValue);

        unchecked
        {
            int hash = 23;
            for (int i = 0; i < text.Length; i++)
                hash = hash * 31 + text[i];

            if (hash == 0)
                hash = 1;

            return hash;
        }
    }
}
