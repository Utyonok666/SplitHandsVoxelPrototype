using UnityEngine;

// ============================================================
// SaveableWorldObject
// Tracks world objects that must persist across chunk unloads and saves. (Этот скрипт отвечает за: tracks world objects that must persist across chunk unloads and saves.)
// ============================================================
public class SaveableWorldObject : MonoBehaviour
{
    [SerializeField] private string uniqueId;
    private bool initialized = false;
    private bool chunkUnloading = false;
    private bool manuallyRemoved = false;

    public string UniqueId => uniqueId;

    // Initialize. (Initialize)
    public void Initialize(string id)
    {
        uniqueId = id;
        initialized = true;
    }

    // Prepare objects before chunk unload. (Prepare objects before чанка unзагрузка)
    public void PrepareForChunkUnload()
    {
        chunkUnloading = true;
    }

    // Mark Removed. (Mark Removed)
    public void MarkRemoved()
    {
        if (!initialized || string.IsNullOrEmpty(uniqueId))
            return;

        manuallyRemoved = true;
        WorldSaveSystem.MarkWorldObjectRemoved(uniqueId);
    }

    // Handle destroy cleanup. (Handle destroy cleanup)
    private void OnDestroy()
    {
        if (!Application.isPlaying)
            return;
        if (!initialized || string.IsNullOrEmpty(uniqueId))
            return;
        if (chunkUnloading || manuallyRemoved || WorldSaveSystem.IsQuitting)
            return;

        WorldSaveSystem.MarkWorldObjectRemoved(uniqueId);
    }
}
