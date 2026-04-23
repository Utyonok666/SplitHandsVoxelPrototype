using UnityEngine;
using TMPro;

// ============================================================
// PlayerCoordinatesUI
// HUD display for player position and seed. (Этот скрипт отвечает за: hud display for player position and seed.)
// ============================================================
public class PlayerCoordinatesUI : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("References")]
    public Transform playerTransform;
    public TextMeshProUGUI coordsText;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Optional (Seed Source)")]
    public WorldGenerator worldGenerator;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Display Settings")]
    public bool showDecimals = true;
    public int decimalPlaces = 1;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Seed")]
    public bool showSeed = true;

    void Update()
    {
        if (playerTransform == null || coordsText == null)
            return;

        Vector3 pos = playerTransform.position;

        string coords;

        if (showDecimals)
        {
            coords =
                $"X: {pos.x.ToString($"F{decimalPlaces}")}\n" +
                $"Y: {pos.y.ToString($"F{decimalPlaces}")}\n" +
                $"Z: {pos.z.ToString($"F{decimalPlaces}")}";
        }
        else
        {
            coords =
                $"X: {Mathf.RoundToInt(pos.x)}\n" +
                $"Y: {Mathf.RoundToInt(pos.y)}\n" +
                $"Z: {Mathf.RoundToInt(pos.z)}";
        }

        if (showSeed)
        {
            coords += "\nSeed: " + GetCurrentSeed();
        }

        coordsText.text = coords;
    }

    string GetCurrentSeed()
    {
        // вариант 1 — если seed хранится в WorldGenerator
        if (worldGenerator != null)
            return worldGenerator.seed.ToString();

        // вариант 2 — если seed хранится в GameSettingsData
        try
        {
            return GameSettingsData.Seed.ToString();
        }
        catch
        {
            return "Unknown";
        }
    }
}
