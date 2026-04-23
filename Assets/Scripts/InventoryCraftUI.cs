using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
// InventoryCraftUI
// Inventory/crafting menu UI, recipe buttons, and tooltip integration. (Этот скрипт отвечает за: inventory/crafting menu ui, recipe buttons, and tooltip integration.)
// ============================================================
public class InventoryCraftUI : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("References")]
    public GameObject inventoryRoot;
    public HotbarManager inventory;
    public CraftingSystem craftingSystem;
    public HandGamePlayer player;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("UI Text")]
    public TMP_Text inventoryText;
    public TMP_Text craftInfoText;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Buttons")]
    public Button furnaceButton;
    public Button axeButton;
    public Button hammerButton;
    public Button crowbarButton;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.I;
    public bool lockCursorWhenOpen = true;
    public bool pauseGameWhenOpen = false;

    private bool isOpen;
    private string lastCraftMessage = "";

    // Initialize runtime state. (Initialize runtime state)
    private void Start()
    {
        Debug.Log("[InventoryCraftUI] Start called");

        if (inventoryRoot != null)
            inventoryRoot.SetActive(false);

        if (player == null)
            player = FindFirstObjectByType<HandGamePlayer>();

        if (craftInfoText != null)
        {
            craftInfoText.text = "TEST TEXT";
            Debug.Log("[InventoryCraftUI] craftInfoText assigned: " + craftInfoText.name);
        }
        else
        {
            Debug.LogError("[InventoryCraftUI] craftInfoText is NULL");
        }

        HookButtons();
        RefreshUI();
        ApplyOpenState(false);
    }

    // Run per-frame logic. (Run per-frame logic)
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleInventory();
    }

    // Hook Buttons. (Hook Buttons)
    private void HookButtons()
    {
        if (furnaceButton != null)
        {
            furnaceButton.onClick.RemoveAllListeners();
            furnaceButton.onClick.AddListener(() => TryCraft("craft_furnace"));
        }

        if (axeButton != null)
        {
            axeButton.onClick.RemoveAllListeners();
            axeButton.onClick.AddListener(() => TryCraft("craft_axe"));
        }

        if (hammerButton != null)
        {
            hammerButton.onClick.RemoveAllListeners();
            hammerButton.onClick.AddListener(() => TryCraft("craft_hammer"));
        }

        if (crowbarButton != null)
        {
            crowbarButton.onClick.RemoveAllListeners();
            crowbarButton.onClick.AddListener(() => TryCraft("craft_crowbar"));
        }
    }

    // Toggle Inventory. (Toggle Inventory)
    public void ToggleInventory()
    {
        ApplyOpenState(!isOpen);
        RefreshUI();
    }

    // Apply Open State. (Apply Open State)
    private void ApplyOpenState(bool opened)
    {
        isOpen = opened;

        if (inventoryRoot != null)
            inventoryRoot.SetActive(opened);

        if (player != null)
            player.SetChatInputBlocked(opened);

        ApplyCursorState(opened);

        if (pauseGameWhenOpen)
            Time.timeScale = opened ? 0f : 1f;
    }

    // Apply Cursor State. (Apply Cursor State)
    private void ApplyCursorState(bool opened)
    {
        if (!lockCursorWhenOpen)
            return;

        Cursor.visible = opened;
        Cursor.lockState = opened ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // Try to Craft. (Try to Craft)
    public void TryCraft(string recipeId)
    {
        if (inventory == null || craftingSystem == null)
        {
            SetCraftInfo("Missing inventory/crafting refs.");
            return;
        }

        CraftingSystem.Recipe recipe = craftingSystem.GetRecipe(recipeId);
        if (recipe == null)
        {
            SetCraftInfo("Recipe not found: " + recipeId);
            return;
        }

        if (!craftingSystem.CanCraft(recipeId, inventory))
        {
            SetCraftInfo("Not enough resources: " + recipe.displayName);
            RefreshUI();
            return;
        }

        bool success = craftingSystem.Craft(recipeId, inventory);

        if (success)
            SetCraftInfo("Crafted: " + recipe.displayName);
        else
            SetCraftInfo("Craft failed: " + recipe.displayName + " (check Furnace definition/prefab)");

        RefreshUI();
    }

    // Refresh visible UI state. (Refresh visible UI state)
    public void RefreshUI()
    {
        RefreshInventoryText();
        RefreshRecipeStateText();
        RefreshButtonStates();
    }

    // Refresh Inventory Text. (Refresh Inventory Text)
    private void RefreshInventoryText()
    {
        if (inventoryText == null || inventory == null)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("INVENTORY");
        sb.AppendLine("----------------");
        AppendItemLine(sb, "Plank");
        AppendItemLine(sb, "Stone");
        AppendItemLine(sb, "IronChunk");
        AppendItemLine(sb, "IronIngot");
        AppendItemLine(sb, "Furnace");
        AppendItemLine(sb, "Axe");
        AppendItemLine(sb, "PickHammer");
        AppendItemLine(sb, "Crowbar");

        inventoryText.text = sb.ToString();
    }

    // Append Item Line. (Append Item Line)
    private void AppendItemLine(StringBuilder sb, string itemId)
    {
        int count = inventory.GetItemCount(itemId);
        sb.AppendLine(itemId + ": " + count);
    }

    // Refresh Recipe State Text. (Refresh Recipe State Text)
    private void RefreshRecipeStateText()
    {
        if (craftInfoText == null)
            return;

        craftInfoText.text = lastCraftMessage;
    }

    // Refresh Button States. (Refresh Button States)
    private void RefreshButtonStates()
    {
        if (inventory == null || craftingSystem == null)
            return;

        if (furnaceButton != null)
            furnaceButton.interactable = craftingSystem.CanCraft("craft_furnace", inventory);

        if (axeButton != null)
            axeButton.interactable = craftingSystem.CanCraft("craft_axe", inventory);

        if (hammerButton != null)
            hammerButton.interactable = craftingSystem.CanCraft("craft_hammer", inventory);

        if (crowbarButton != null)
            crowbarButton.interactable = craftingSystem.CanCraft("craft_crowbar", inventory);
    }

    // Set Craft Info. (Set Craft Info)
    private void SetCraftInfo(string message)
    {
        lastCraftMessage = message;

        if (craftInfoText != null)
            craftInfoText.text = message;

        Debug.Log("[Crafting] " + message);
    }

    // Handle destroy cleanup. (Handle destroy cleanup)
    private void OnDestroy()
    {
        if (player != null)
            player.SetChatInputBlocked(false);

        if (pauseGameWhenOpen && Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}
