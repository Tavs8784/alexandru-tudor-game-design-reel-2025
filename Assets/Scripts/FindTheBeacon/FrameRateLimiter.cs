using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    [Range(15,240)] public int target = 60;
    public bool clampToMonitor = false; // usually off for a hard 60 cap

    void Awake()
    {
        // Desktop: let us control FPS via targetFrameRate
        QualitySettings.vSyncCount = 0;

        // Optional: never exceed monitor refresh
        if (clampToMonitor)
            target = Mathf.Min(target, Screen.currentResolution.refreshRate);

        Application.targetFrameRate = target;
        DontDestroyOnLoad(gameObject);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Some platforms reset this; re-apply on focus.
        if (hasFocus) Application.targetFrameRate = target;
    }
}