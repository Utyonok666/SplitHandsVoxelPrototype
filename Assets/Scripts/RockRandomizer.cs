using UnityEngine;

// ============================================================
// RockRandomizer
// Randomizes rock visuals/variants for environment props. (Этот скрипт отвечает за: randomizes rock visuals/variants for environment props.)
// ============================================================
public class RockRandomizer : MonoBehaviour
{
    void Start()
    {
        // Рандомный поворот и размер
        transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        transform.localScale = Vector3.one * Random.Range(0.7f, 1.8f);
        
        // Немного "утапливаем" в землю для естественности
        transform.position += Vector3.down * 0.2f;
    }
}
