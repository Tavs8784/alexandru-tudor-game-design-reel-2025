using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class BeaconGoal : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] string playerTag = "Player";   // who can complete the level

    [Header("UI Blink (same Image you use for death)")]
    [SerializeField] GameObject blinkTarget;        // assign your Canvas→Image GO
    [SerializeField] bool useCanvasGroup = true;    // fade via alpha rather than SetActive
    [SerializeField] CanvasGroup blinkCanvasGroup;  // auto-added if missing
    [SerializeField] Image blinkImage;              // used to tint green
    [SerializeField] Color goalColor = Color.green; // blink color on success
    [SerializeField] float blinkDuration = 0.6f;    // total time to blink
    [SerializeField] float blinkInterval = 0.1f;    // on/off cadence
    [SerializeField] float blinkOnAlpha = 1f;
    [SerializeField] float blinkOffAlpha = 0f;

    [Header("Reload")]
    [SerializeField] float reloadDelay = 0.35f;     // waits max(reloadDelay, blinkDuration)

    [Header("Events")]
    [SerializeField] UnityEvent onGoalReached;      // optional SFX/VFX

    bool _triggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // goal should be a trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        _triggered = true;
        onGoalReached?.Invoke();
        StartCoroutine(GoalSequence());
    }

    IEnumerator GoalSequence()
    {
        // Prepare UI refs
        if (blinkTarget && useCanvasGroup && !blinkCanvasGroup)
        {
            blinkCanvasGroup = blinkTarget.GetComponent<CanvasGroup>();
            if (!blinkCanvasGroup) blinkCanvasGroup = blinkTarget.AddComponent<CanvasGroup>();
        }
        if (blinkTarget && !blinkImage) blinkImage = blinkTarget.GetComponent<Image>();

        // Tint to green (remember original for editor convenience)
        Color original = Color.white;
        if (blinkImage)
        {
            original = blinkImage.color;
            blinkImage.color = goalColor;
        }

        // Start blink
        if (blinkTarget) StartCoroutine(BlinkRoutine(blinkDuration, blinkInterval));

        // Wait long enough for blink and any extra delay
        float wait = Mathf.Max(reloadDelay, blinkDuration);
        if (wait > 0f) yield return new WaitForSeconds(wait);

        // Restore color if still in editor play (reload will wipe anyway)
        if (blinkImage) blinkImage.color = original;

        // Reload current scene
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    IEnumerator BlinkRoutine(float duration, float interval)
    {
        if (!blinkTarget) yield break;

        float t = 0f;
        bool state = true;

        // ensure visible at start
        if (useCanvasGroup && blinkCanvasGroup)
            blinkCanvasGroup.alpha = blinkOnAlpha;
        else
            blinkTarget.SetActive(true);

        while (t < duration)
        {
            yield return new WaitForSeconds(interval);
            t += interval;
            state = !state;

            if (useCanvasGroup && blinkCanvasGroup)
                blinkCanvasGroup.alpha = state ? blinkOnAlpha : blinkOffAlpha;
            else
                blinkTarget.SetActive(state);
        }
    }
}
