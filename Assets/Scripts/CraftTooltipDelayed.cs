using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// ============================================================
// CraftTooltipDelayed
// Simple crafting tooltip display for UI hover events. (Этот скрипт отвечает за: simple crafting tooltip display for ui hover events.)
// ============================================================
public class CraftTooltipDelayed : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI tooltipText;

    [TextArea(3, 6)]
    public string recipeDescription;

    // On Pointer Enter. (On Pointer Enter)
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("[CraftTooltipDelayed] Pointer enter on " + gameObject.name);

        if (tooltipText == null)
        {
            Debug.LogError("[CraftTooltipDelayed] tooltipText is NULL on " + gameObject.name);
            return;
        }

        tooltipText.text = recipeDescription;
        Debug.Log("[CraftTooltipDelayed] Show tooltip: " + recipeDescription);
    }

    // On Pointer Exit. (On Pointer Exit)
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[CraftTooltipDelayed] Pointer exit on " + gameObject.name);

        if (tooltipText != null)
            tooltipText.text = "";
    }

    // Initialize runtime state. (Initialize runtime state)
    private void Start()
    {
        if (tooltipText != null)
            tooltipText.text = "";
    }
}
