using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// ============================================================
// MainMenu
// Main menu flow, new game/continue/settings logic. (Этот скрипт отвечает за: main menu flow, new game/continue/settings logic.)
// ============================================================
public class MainMenu : MonoBehaviour
{
    public TMP_InputField seedInput;
    public Slider distanceSlider;
    public TextMeshProUGUI distanceText;
    public Button continueButton;

    private const string PendingViewDistanceKey = "pending_view_distance";

    void Start()
    {
        RefreshContinueButton();

        WorldSaveData save = WorldSaveSystem.LoadSave();

        if (save != null)
        {
            if (seedInput != null && string.IsNullOrWhiteSpace(seedInput.text))
                seedInput.text = save.seed.ToString();
        }

        if (distanceSlider != null)
        {
            if (PlayerPrefs.HasKey(PendingViewDistanceKey))
                distanceSlider.value = PlayerPrefs.GetInt(PendingViewDistanceKey);
            else if (save != null)
                distanceSlider.value = save.viewDistance;
        }

        UpdateUI();
    }

    // Play Game. (Play Game)
    public void PlayGame()
    {
        int seed;
        if (seedInput == null || string.IsNullOrWhiteSpace(seedInput.text))
            seed = Random.Range(1, int.MaxValue);
        else if (!int.TryParse(seedInput.text, out seed))
            seed = GameSettingsData.StableStringToSeed(seedInput.text);

        int distance = distanceSlider != null ? Mathf.RoundToInt(distanceSlider.value) : 5;

        PlayerPrefs.SetInt(PendingViewDistanceKey, distance);
        PlayerPrefs.Save();

        GameSettingsData.SetNewGame(seed, distance);
        SceneManager.LoadScene(1);
    }

    // Continue Game. (Continue Game)
    public void ContinueGame()
    {
        if (!WorldSaveSystem.HasSave())
            return;

        int distance = distanceSlider != null ? Mathf.RoundToInt(distanceSlider.value) : 5;

        PlayerPrefs.SetInt(PendingViewDistanceKey, distance);
        PlayerPrefs.Save();

        GameSettingsData.SetContinueGame();
        SceneManager.LoadScene(1);
    }

    // Refresh Continue Button. (Refresh Continue Button)
    public void RefreshContinueButton()
    {
        if (continueButton != null)
            continueButton.interactable = WorldSaveSystem.HasSave();
    }

    // Refresh visible UI state. (Refresh visible UI state)
    public void UpdateUI()
    {
        if (distanceText != null && distanceSlider != null)
            distanceText.text = "Distance: " + Mathf.RoundToInt(distanceSlider.value);
    }

    // Quit. (Quit)
    public void Quit()
    {
        Application.Quit();
    }
}
