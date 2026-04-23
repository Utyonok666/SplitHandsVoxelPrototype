using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

// ============================================================
// ChatConsole
// In-game chat/console UI, command input, suggestions, history, and log fading. (Этот скрипт отвечает за: in-game chat/console ui, command input, suggestions, history, and log fading.)
// ============================================================
public class ChatConsole : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("UI")]
    public GameObject chatPanel;
    public Image chatBackground;
    public ScrollRect logScrollRect;
    public TextMeshProUGUI logText;
    public TMP_InputField inputField;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Suggestions UI")]
    public GameObject suggestionPanel;
    public Image suggestionBackground;
    public TextMeshProUGUI suggestionText;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Suggestion Style")]
    public Color normalSuggestionColor = new Color(0.86f, 0.86f, 0.86f, 1f);
    public Color selectedSuggestionColor = new Color(1f, 0.93f, 0.45f, 1f);
    public Color suggestionBackgroundColor = new Color(0f, 0f, 0f, 0.72f);

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Chat Look")]
    public Color openChatBackgroundColor = new Color(0f, 0f, 0f, 0.42f);
    public Color closedChatBackgroundColor = new Color(0f, 0f, 0f, 0f);
    public Color openLogColor = new Color(1f, 1f, 1f, 1f);
    public Color closedLogColor = new Color(1f, 1f, 1f, 0.55f);

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Chat Colors")]
    public string inputColorHex = "#F2F2F2";
    public string infoColorHex = "#D0D0D0";
    public string errorColorHex = "#FF6B6B";
    public string usageColorHex = "#FFD166";
    public string mutedColorHex = "#A8A8A8";

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Keys")]
    public KeyCode autocompleteKey = KeyCode.Tab;
    public KeyCode historyUpKey = KeyCode.UpArrow;
    public KeyCode historyDownKey = KeyCode.DownArrow;
    public KeyCode scrollUpKey = KeyCode.PageUp;
    public KeyCode scrollDownKey = KeyCode.PageDown;
    public KeyCode closeChatKey = KeyCode.Escape;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Systems")]
    public DayNightCycle dayNightCycle;
    public HandGamePlayer playerController;
    public HotbarManager hotbarManager;
    public Transform playerTransform;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Log")]
    public int maxLogLines = 120;
    public float closedFadeDelay = 7f;

    private readonly string[] baseCommands = new string[]
    {
        "/day",
        "/night",
        "/morning",
        "/evening",
        "/time",
        "/cycle on",
        "/cycle off",
        "/cycle toggle",
        "/tp",
        "/give",
        "/noclip"
    };

    private readonly List<string> renderedLines = new List<string>();
    private readonly List<string> commandHistory = new List<string>();

    private string[] currentSuggestions = new string[0];
    private int selectedSuggestionIndex = 0;
    private string lastInputValue = "";
    private int historyIndex = -1;
    private string unsentDraftBeforeHistory = "";

    private bool isOpen = false;
    private float lastMessageTime = -999f;
    private bool hotbarWasEnabledBeforeChat = true;
    private float reopenBlockUntil = -1f;

    public bool IsChatOpen => isOpen;

    void Start()
    {
        if (chatPanel != null)
            chatPanel.SetActive(true);

        HideSuggestions();
        ApplySuggestionBackgroundStyle();
        ApplyChatVisualState(false, true);
        AddMutedLine("Chat ready. Press Enter to open chat.");
    }

    void Update()
    {
        if (!isOpen)
        {
            HandleClosedChatFade();

            if (Time.unscaledTime >= reopenBlockUntil && Input.GetKeyDown(KeyCode.Return))
            {
                OpenChat("");
                return;
            }

            return;
        }

        if (Input.GetKeyDown(closeChatKey))
        {
            CloseChat();
            return;
        }

        if (inputField != null && inputField.text != lastInputValue)
        {
            lastInputValue = inputField.text;
            RefreshSuggestions(false);
        }

        if (Input.GetKeyDown(autocompleteKey))
        {
            HandleAutocomplete();
            return;
        }

        if (Input.GetKeyDown(historyUpKey))
        {
            if (HasCommandSuggestionsContext())
                MoveSuggestionSelection(-1);
            else
                BrowseHistory(-1);

            return;
        }

        if (Input.GetKeyDown(historyDownKey))
        {
            if (HasCommandSuggestionsContext())
                MoveSuggestionSelection(1);
            else
                BrowseHistory(1);

            return;
        }

        if (Input.GetKeyDown(scrollUpKey))
        {
            ScrollLog(0.18f);
            return;
        }

        if (Input.GetKeyDown(scrollDownKey))
        {
            ScrollLog(-0.18f);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
            SubmitInput();
    }

    void OpenChat(string initialText)
    {
        isOpen = true;

        if (playerController != null)
            playerController.SetChatInputBlocked(true);

        if (hotbarManager != null)
        {
            hotbarWasEnabledBeforeChat = hotbarManager.enabled;
            hotbarManager.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ApplyChatVisualState(true, true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (inputField != null)
        {
            inputField.text = initialText;
            inputField.caretPosition = inputField.text.Length;
            lastInputValue = inputField.text;
            historyIndex = -1;
            unsentDraftBeforeHistory = "";
            inputField.DeactivateInputField();
            inputField.ActivateInputField();
            inputField.Select();
            RefreshSuggestions(false);
        }
    }

    void CloseChat()
    {
        isOpen = false;
        reopenBlockUntil = Time.unscaledTime + 0.15f;

        if (playerController != null)
            playerController.SetChatInputBlocked(false);

        if (hotbarManager != null)
            hotbarManager.enabled = hotbarWasEnabledBeforeChat;

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ReleaseSelection();
            inputField.DeactivateInputField();
        }

        lastInputValue = "";
        historyIndex = -1;
        unsentDraftBeforeHistory = "";

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        HideSuggestions();
        ApplyChatVisualState(false, false);
    }

    void ApplyChatVisualState(bool open, bool forceScrollBottom)
    {
        if (chatBackground != null)
            chatBackground.color = open ? openChatBackgroundColor : closedChatBackgroundColor;

        if (logText != null)
            logText.color = open ? openLogColor : closedLogColor;

        if (forceScrollBottom)
            ScrollToBottomNextFrame();
    }

    void HandleClosedChatFade()
    {
        if (logText == null)
            return;

        float t = Mathf.Clamp01((Time.unscaledTime - lastMessageTime) / Mathf.Max(0.01f, closedFadeDelay));
        float alpha = Mathf.Lerp(closedLogColor.a, 0f, t);

        Color c = closedLogColor;
        c.a = alpha;
        logText.color = c;
    }

    void SubmitInput()
    {
        if (inputField == null)
            return;

        string text = inputField.text.Trim();

        if (string.IsNullOrEmpty(text))
        {
            CloseChat();
            return;
        }

        AddInputLine(text);

        if (text.StartsWith("/"))
        {
            commandHistory.Add(text);
            historyIndex = -1;
            unsentDraftBeforeHistory = "";
            ExecuteCommand(text);
        }

        inputField.text = "";
        lastInputValue = "";
        RefreshSuggestions(false);

        inputField.ActivateInputField();
        inputField.Select();
    }

    void HandleAutocomplete()
    {
        if (inputField == null)
            return;

        string text = inputField.text;

        if (string.IsNullOrWhiteSpace(text))
        {
            inputField.text = "/";
            inputField.caretPosition = inputField.text.Length;
            lastInputValue = inputField.text;
            RefreshSuggestions(false);
            inputField.ActivateInputField();
            inputField.Select();
            return;
        }

        if (!text.StartsWith("/"))
        {
            inputField.text = "/" + text;
            inputField.caretPosition = inputField.text.Length;
            lastInputValue = inputField.text;
            RefreshSuggestions(false);
            inputField.ActivateInputField();
            inputField.Select();
            return;
        }

        RefreshSuggestions(true);

        if (currentSuggestions.Length == 0)
            return;

        ApplySelectedSuggestion();
    }

    bool HasCommandSuggestionsContext()
    {
        if (inputField == null)
            return false;

        string text = inputField.text.Trim();
        return text.StartsWith("/") && currentSuggestions != null && currentSuggestions.Length > 0;
    }

    void MoveSuggestionSelection(int direction)
    {
        if (currentSuggestions == null || currentSuggestions.Length == 0)
            return;

        selectedSuggestionIndex += direction;

        if (selectedSuggestionIndex < 0)
            selectedSuggestionIndex = currentSuggestions.Length - 1;

        if (selectedSuggestionIndex >= currentSuggestions.Length)
            selectedSuggestionIndex = 0;

        UpdateSuggestionVisuals();
    }

    void ApplySelectedSuggestion()
    {
        if (currentSuggestions == null || currentSuggestions.Length == 0)
            return;

        ApplySuggestion(currentSuggestions[selectedSuggestionIndex]);
    }

    void ApplySuggestion(string suggestion)
    {
        if (inputField == null)
            return;

        string value = suggestion;

        if (value.Contains("<x>") || value.Contains("<item>") || value.Contains("<amount>"))
        {
            value = value.Replace("<x>", "")
                         .Replace("<y>", "")
                         .Replace("<z>", "")
                         .Replace("<item>", "")
                         .Replace("<amount>", "");

            while (value.Contains("  "))
                value = value.Replace("  ", " ");

            value = value.TrimEnd();
        }

        inputField.text = value;
        inputField.caretPosition = inputField.text.Length;
        lastInputValue = inputField.text;

        RefreshSuggestions(true);
        inputField.ActivateInputField();
        inputField.Select();
    }

    void BrowseHistory(int direction)
    {
        if (commandHistory.Count == 0 || inputField == null)
            return;

        if (historyIndex == -1)
            unsentDraftBeforeHistory = inputField.text;

        historyIndex += direction;

        if (historyIndex < -1)
            historyIndex = -1;

        if (historyIndex >= commandHistory.Count)
            historyIndex = commandHistory.Count - 1;

        if (historyIndex == -1)
        {
            inputField.text = unsentDraftBeforeHistory;
        }
        else
        {
            int reversedIndex = commandHistory.Count - 1 - historyIndex;
            reversedIndex = Mathf.Clamp(reversedIndex, 0, commandHistory.Count - 1);
            inputField.text = commandHistory[reversedIndex];
        }

        inputField.caretPosition = inputField.text.Length;
        lastInputValue = inputField.text;
        RefreshSuggestions(false);

        inputField.ActivateInputField();
        inputField.Select();
    }

    void ScrollLog(float delta)
    {
        if (logScrollRect == null)
            return;

        float v = logScrollRect.verticalNormalizedPosition;
        v = Mathf.Clamp01(v + delta);
        logScrollRect.verticalNormalizedPosition = v;
    }

    void ScrollToBottomNextFrame()
    {
        if (logScrollRect == null)
            return;

        StartCoroutine(ScrollToBottomRoutine());
    }

    System.Collections.IEnumerator ScrollToBottomRoutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (logScrollRect != null)
            logScrollRect.verticalNormalizedPosition = 0f;
    }

    void RefreshSuggestions(bool preserveSelection)
    {
        if (inputField == null)
        {
            HideSuggestions();
            return;
        }

        string text = inputField.text.Trim();

        if (string.IsNullOrEmpty(text) || !text.StartsWith("/"))
        {
            HideSuggestions();
            return;
        }

        string previousSelected = preserveSelection && currentSuggestions.Length > 0 &&
                                  selectedSuggestionIndex >= 0 &&
                                  selectedSuggestionIndex < currentSuggestions.Length
            ? currentSuggestions[selectedSuggestionIndex]
            : "";

        List<string> matches = BuildSuggestions(text);

        currentSuggestions = matches.ToArray();

        if (currentSuggestions.Length == 0)
        {
            HideSuggestions();
            return;
        }

        if (preserveSelection && !string.IsNullOrEmpty(previousSelected))
        {
            int found = System.Array.IndexOf(currentSuggestions, previousSelected);
            selectedSuggestionIndex = found >= 0 ? found : 0;
        }
        else
        {
            selectedSuggestionIndex = 0;
        }

        if (suggestionPanel != null)
            suggestionPanel.SetActive(true);

        ApplySuggestionBackgroundStyle();
        UpdateSuggestionVisuals();
    }

    List<string> BuildSuggestions(string text)
    {
        List<string> matches = new List<string>();
        string lowered = text.ToLower();

        if (lowered == "/")
        {
            for (int i = 0; i < baseCommands.Length; i++)
                matches.Add(baseCommands[i]);

            return matches;
        }

        if (lowered == "/tp")
        {
            matches.Add("/tp <x> <y> <z>");
            matches.Add("/tp 0 10 0");
            matches.Add("/tp 10 25 -5");
            return matches;
        }

        if (lowered.StartsWith("/tp "))
        {
            matches.Add("/tp <x> <y> <z>");
            matches.Add("/tp 0 10 0");
            matches.Add("/tp 10 25 -5");
            matches.Add("/tp 100 25 -20");
            return matches;
        }

        if (lowered == "/give")
        {
            matches.Add("/give <item> <amount>");
            AddGiveItemSuggestions(matches, "");
            return Deduplicate(matches);
        }

        if (lowered.StartsWith("/give "))
        {
            string afterGive = text.Length > 6 ? text.Substring(6).Trim() : "";

            if (string.IsNullOrEmpty(afterGive))
            {
                matches.Add("/give <item> <amount>");
                AddGiveItemSuggestions(matches, "");
                return Deduplicate(matches);
            }

            string[] parts = afterGive.Split(' ');

            if (parts.Length == 1)
            {
                matches.Add("/give <item> <amount>");
                AddGiveItemSuggestions(matches, parts[0]);
                return Deduplicate(matches);
            }

            matches.Add("/give " + parts[0] + " <amount>");
            matches.Add("/give " + parts[0] + " 1");
            matches.Add("/give " + parts[0] + " 10");
            matches.Add("/give " + parts[0] + " 64");
            return Deduplicate(matches);
        }

        for (int i = 0; i < baseCommands.Length; i++)
        {
            if (baseCommands[i].StartsWith(lowered))
                matches.Add(baseCommands[i]);
        }

        if (matches.Count == 0)
        {
            for (int i = 0; i < baseCommands.Length; i++)
            {
                if (baseCommands[i].Contains(lowered))
                    matches.Add(baseCommands[i]);
            }
        }

        return Deduplicate(matches);
    }

    List<string> Deduplicate(List<string> input)
    {
        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>();

        for (int i = 0; i < input.Count; i++)
        {
            if (seen.Add(input[i]))
                result.Add(input[i]);
        }

        return result;
    }

    void AddGiveItemSuggestions(List<string> matches, string itemFilter)
    {
        if (hotbarManager == null || hotbarManager.itemDefinitions == null)
            return;

        string loweredFilter = itemFilter.ToLower();
        int addedCount = 0;

        for (int i = 0; i < hotbarManager.itemDefinitions.Length; i++)
        {
            HotbarItemDefinition def = hotbarManager.itemDefinitions[i];
            if (def == null || string.IsNullOrEmpty(def.itemName))
                continue;

            string itemName = def.itemName;

            if (!string.IsNullOrEmpty(loweredFilter) && !itemName.ToLower().StartsWith(loweredFilter))
                continue;

            matches.Add("/give " + itemName + " <amount>");
            addedCount++;

            if (addedCount >= 8)
                break;
        }
    }

    void UpdateSuggestionVisuals()
    {
        if (suggestionText == null)
            return;

        StringBuilder sb = new StringBuilder();

        string normalHex = ColorUtility.ToHtmlStringRGB(normalSuggestionColor);
        string selectedHex = ColorUtility.ToHtmlStringRGB(selectedSuggestionColor);

        for (int i = 0; i < currentSuggestions.Length; i++)
        {
            bool selected = i == selectedSuggestionIndex;
            string prefix = selected ? "> " : "   ";
            string colorHex = selected ? selectedHex : normalHex;

            sb.Append("<color=#");
            sb.Append(colorHex);
            sb.Append(">");
            sb.Append(prefix);
            sb.Append(EscapeRichText(currentSuggestions[i]));
            sb.Append("</color>");

            if (i < currentSuggestions.Length - 1)
                sb.AppendLine();
        }

        suggestionText.text = sb.ToString();
    }

    void ApplySuggestionBackgroundStyle()
    {
        if (suggestionBackground != null)
            suggestionBackground.color = suggestionBackgroundColor;
    }

    void HideSuggestions()
    {
        currentSuggestions = new string[0];
        selectedSuggestionIndex = 0;

        if (suggestionPanel != null)
            suggestionPanel.SetActive(false);

        if (suggestionText != null)
            suggestionText.text = "";
    }

    void ExecuteCommand(string command)
    {
        string[] parts = command.Trim().Split(' ');

        if (parts.Length == 0)
            return;

        string root = parts[0].ToLower();

        switch (root)
        {
            case "/day":
                if (RequireDayNight())
                {
                    dayNightCycle.SetDay();
                    AddInfoLine("Set time to day");
                }
                return;

            case "/night":
                if (RequireDayNight())
                {
                    dayNightCycle.SetNight();
                    AddInfoLine("Set time to night");
                }
                return;

            case "/morning":
                if (RequireDayNight())
                {
                    dayNightCycle.SetMorning();
                    AddInfoLine("Set time to morning");
                }
                return;

            case "/evening":
                if (RequireDayNight())
                {
                    dayNightCycle.SetEvening();
                    AddInfoLine("Set time to evening");
                }
                return;

            case "/time":
                if (RequireDayNight())
                    AddInfoLine("Time: " + dayNightCycle.GetTimeString());
                return;

            case "/cycle":
                HandleCycleCommand(parts);
                return;

            case "/tp":
                HandleTeleportCommand(parts);
                return;

            case "/give":
                HandleGiveCommand(parts);
                return;

            case "/noclip":
                HandleNoClipCommand();
                return;

            default:
                AddErrorLine("Unknown command");
                return;
        }
    }

    bool RequireDayNight()
    {
        if (dayNightCycle == null)
        {
            AddErrorLine("DayNightCycle is not assigned");
            return false;
        }

        return true;
    }

    void HandleCycleCommand(string[] parts)
    {
        if (!RequireDayNight())
            return;

        if (parts.Length < 2)
        {
            AddUsageLine("/cycle on | off | toggle");
            return;
        }

        string mode = parts[1].ToLower();

        if (mode == "on")
        {
            dayNightCycle.SetCycleEnabled(true);
            AddInfoLine("Day-night cycle enabled");
        }
        else if (mode == "off")
        {
            dayNightCycle.SetCycleEnabled(false);
            AddInfoLine("Day-night cycle disabled");
        }
        else if (mode == "toggle")
        {
            dayNightCycle.ToggleCycle();
            AddInfoLine("Day-night cycle toggled");
        }
        else
        {
            AddUsageLine("/cycle on | off | toggle");
        }
    }

    void HandleTeleportCommand(string[] parts)
    {
        if (playerTransform == null)
        {
            AddErrorLine("Player transform is not assigned");
            return;
        }

        if (parts.Length < 4)
        {
            AddUsageLine("/tp <x> <y> <z>");
            return;
        }

        if (!TryParseFloat(parts[1], out float x) ||
            !TryParseFloat(parts[2], out float y) ||
            !TryParseFloat(parts[3], out float z))
        {
            AddErrorLine("Invalid coordinates");
            AddUsageLine("/tp 10 25 -5");
            return;
        }

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        bool hadController = cc != null && cc.enabled;

        if (hadController)
            cc.enabled = false;

        playerTransform.position = new Vector3(x, y, z);

        if (hadController)
            cc.enabled = true;

        AddInfoLine($"Teleported to ({x}, {y}, {z})");
    }

    void HandleGiveCommand(string[] parts)
    {
        if (hotbarManager == null)
        {
            AddErrorLine("HotbarManager is not assigned");
            return;
        }

        if (parts.Length < 3)
        {
            AddUsageLine("/give <item> <amount>");
            return;
        }

        string itemName = parts[1];

        if (!int.TryParse(parts[2], out int amount) || amount <= 0)
        {
            AddErrorLine("Invalid amount");
            AddUsageLine("/give wood 10");
            return;
        }

        HotbarItemDefinition def = FindItemDefinition(itemName);
        if (def == null)
        {
            AddErrorLine("Item not found");
            AddUsageLine("/give <item> <amount>");
            return;
        }

        int added = 0;

        for (int i = 0; i < amount; i++)
        {
            bool ok = hotbarManager.AddItem(def.itemName, def.icon, def.prefab);
            if (!ok)
                break;

            added++;
        }

        AddInfoLine($"Given {added} {def.itemName}");
    }

    HotbarItemDefinition FindItemDefinition(string searchName)
    {
        if (hotbarManager == null || hotbarManager.itemDefinitions == null)
            return null;

        string lowered = searchName.ToLower();

        for (int i = 0; i < hotbarManager.itemDefinitions.Length; i++)
        {
            HotbarItemDefinition def = hotbarManager.itemDefinitions[i];
            if (def == null || string.IsNullOrEmpty(def.itemName))
                continue;

            if (def.itemName.ToLower() == lowered)
                return def;
        }

        return null;
    }

    void HandleNoClipCommand()
    {
        if (playerController == null)
        {
            AddErrorLine("PlayerController is not assigned");
            return;
        }

        playerController.ToggleNoClip();
        AddInfoLine("Noclip: " + (playerController.IsNoClipEnabled() ? "ON" : "OFF"));
    }

    bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
               float.TryParse(value, out result);
    }

    void AddInputLine(string text)
    {
        AddLine($"<color={inputColorHex}>{EscapeRichText(text)}</color>");
    }

    void AddInfoLine(string text)
    {
        AddLine($"<color={infoColorHex}>{EscapeRichText(text)}</color>");
    }

    void AddErrorLine(string text)
    {
        AddLine($"<color={errorColorHex}>{EscapeRichText(text)}</color>");
    }

    void AddUsageLine(string text)
    {
        AddLine($"<color={usageColorHex}>{EscapeRichText(text)}</color>");
    }

    void AddMutedLine(string text)
    {
        AddLine($"<color={mutedColorHex}>{EscapeRichText(text)}</color>");
    }

    string EscapeRichText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        return text.Replace("<", "&lt;").Replace(">", "&gt;");
    }

    void AddLine(string line)
    {
        renderedLines.Add(line);

        if (renderedLines.Count > maxLogLines)
            renderedLines.RemoveAt(0);

        if (logText != null)
            logText.text = string.Join("\n", renderedLines);

        lastMessageTime = Time.unscaledTime;

        if (isOpen)
            ApplyChatVisualState(true, true);
        else
            ApplyChatVisualState(false, true);
    }
}
