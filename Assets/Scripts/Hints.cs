using System.Collections.Generic;
using UnityEngine;

// ============================================================
// Hints
// Shows contextual hint text to the player. (Этот скрипт отвечает за: shows contextual hint text to the player.)
// ============================================================
public class Hints : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private List<GameObject> hintCanvases = new List<GameObject>();

    private bool isVisible = false;

    void Start()
    {
        ApplyState();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleHints();
        }
    }

    void ToggleHints()
    {
        isVisible = !isVisible;
        ApplyState();
    }

    void ApplyState()
    {
        foreach (var canvas in hintCanvases)
        {
            if (canvas != null)
                canvas.SetActive(isVisible);
        }
    }
}
