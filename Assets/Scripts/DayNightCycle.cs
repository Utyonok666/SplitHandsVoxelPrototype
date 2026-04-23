using UnityEngine;

// ============================================================
// DayNightCycle
// Time-of-day simulation and sun/light visual updates. (Этот скрипт отвечает за: time-of-day simulation and sun/light visual updates.)
// ============================================================
public class DayNightCycle : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("References")]
    public Light sunLight;
    public Transform sunPivot;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Cycle")]
    public bool cycleEnabled = true;
    public float fullDayLengthSeconds = 600f;
    [Range(0f, 1f)] public float timeOfDay = 0.25f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Sun")]
    public float sunBaseAngleOffset = -90f;
    public Gradient lightColorOverDay;
    public AnimationCurve lightIntensityOverDay = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    void Update()
    {
        if (cycleEnabled)
        {
            if (fullDayLengthSeconds <= 0.01f)
                fullDayLengthSeconds = 0.01f;

            timeOfDay += Time.deltaTime / fullDayLengthSeconds;
            if (timeOfDay >= 1f)
                timeOfDay -= 1f;
        }

        ApplyTimeVisuals();
    }

    void ApplyTimeVisuals()
    {
        float sunAngle = (timeOfDay * 360f) + sunBaseAngleOffset;

        if (sunPivot != null)
            sunPivot.rotation = Quaternion.Euler(sunAngle, 0f, 0f);
        else if (sunLight != null)
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 0f, 0f);

        if (sunLight != null)
        {
            if (lightColorOverDay != null)
                sunLight.color = lightColorOverDay.Evaluate(timeOfDay);

            if (lightIntensityOverDay != null)
                sunLight.intensity = lightIntensityOverDay.Evaluate(timeOfDay);
        }
    }

    // Set time of day using normalized value. (Set time of day using normalized value)
    public void SetTimeNormalized(float value)
    {
        timeOfDay = Mathf.Repeat(value, 1f);
        ApplyTimeVisuals();
    }

    // Set preset daytime value. (Set preset daytime value)
    public void SetDay() => SetTimeNormalized(0.25f);
    // Set preset night value. (Set preset night value)
    public void SetNight() => SetTimeNormalized(0.75f);
    // Set preset morning value. (Set preset morning value)
    public void SetMorning() => SetTimeNormalized(0.15f);
    // Set preset evening value. (Set preset evening value)
    public void SetEvening() => SetTimeNormalized(0.60f);
    // Enable or disable day/night cycle. (Enable or disable день/ночь cycle)
    public void SetCycleEnabled(bool enabled) => cycleEnabled = enabled;
    // Toggle Cycle. (Toggle Cycle)
    public void ToggleCycle() => cycleEnabled = !cycleEnabled;

    // Get Time String. (Get Time String)
    public string GetTimeString()
    {
        int totalMinutes = Mathf.RoundToInt(timeOfDay * 24f * 60f);
        int hours = (totalMinutes / 60) % 24;
        int minutes = totalMinutes % 60;
        return hours.ToString("00") + ":" + minutes.ToString("00");
    }
}
