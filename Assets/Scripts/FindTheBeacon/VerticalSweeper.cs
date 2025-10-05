using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

public class VerticalSweeper : MonoBehaviour
{
    [Header("Endpoints (world space Y)")]
    [SerializeField] private float startY;
    [SerializeField] private float endY;
    [SerializeField] private bool snapToStartOnPlay = true;

    [Header("Motion")]
    [SerializeField] private float legTime = 2.5f;     // seconds for one leg
    [SerializeField] private float pauseAtEnds = 0.35f;
    [SerializeField] private float telegraphLead = 0.25f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Events (SFX/VFX hooks)")]
    [SerializeField] private UnityEvent onTelegraph;
    [SerializeField] private UnityEvent onMoveStart;
    [SerializeField] private UnityEvent onMoveEnd;

    // -------- Hazard / Death --------
    [Header("Hazard (kill player on contact)")]
    [SerializeField] private bool killOnContact = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask killLayers = ~0;
    [SerializeField] private bool killOnlyWhileMoving = true;

    [Tooltip("Seconds to wait before reload. Final wait will be max(reloadDelay, blinkDuration).")]
    [SerializeField] private float reloadDelay = 0.35f;
    [SerializeField] private UnityEvent onPlayerKilled;

    // -------- Death UI Blink (integrated) --------
    [Header("Death UI Blink (optional)")]
    [Tooltip("Assign the UI Image (or any GO) you want to blink. Leave null to skip blinking.")]
    [SerializeField] private GameObject blinkTarget;
    [Tooltip("Blink using CanvasGroup alpha instead of SetActive.")]
    [SerializeField] private bool useCanvasGroup = true;
    [SerializeField] private CanvasGroup blinkCanvasGroup;  // auto-added if missing and useCanvasGroup=true
    [SerializeField] private float blinkDuration = 0.6f;    // total blink time
    [SerializeField] private float blinkInterval = 0.1f;    // on/off cadence
    [SerializeField] private float blinkOnAlpha = 1f;
    [SerializeField] private float blinkOffAlpha = 0f;

    bool _moving;
    bool _hasKilled;
    Coroutine _loop;

    // cache for restore (mostly irrelevant because we reload, but safe)
    bool _initialActive;
    float _initialAlpha = 1f;

    void OnEnable()
    {
        if (snapToStartOnPlay) SetY(startY);
        if (_loop == null) _loop = StartCoroutine(Loop());

        if (blinkTarget == null && useCanvasGroup)
            blinkTarget = gameObject; // fallback to self if you really want
        if (useCanvasGroup && blinkTarget != null && blinkCanvasGroup == null)
        {
            blinkCanvasGroup = blinkTarget.GetComponent<CanvasGroup>();
            if (blinkCanvasGroup == null) blinkCanvasGroup = blinkTarget.AddComponent<CanvasGroup>();
        }

        if (blinkTarget != null)
        {
            _initialActive = blinkTarget.activeSelf;
            if (blinkCanvasGroup) _initialAlpha = blinkCanvasGroup.alpha;
        }
    }

    void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;

        // restore if not reloading (scene reload will destroy anyway)
        if (blinkTarget != null)
        {
            if (blinkCanvasGroup) blinkCanvasGroup.alpha = _initialAlpha;
            else blinkTarget.SetActive(_initialActive);
        }
    }

    IEnumerator Loop()
    {
        if (Mathf.Approximately(startY, endY)) yield break;

        while (true)
        {
            yield return DoLeg(startY, endY);
            if (pauseAtEnds > 0f) yield return new WaitForSeconds(pauseAtEnds);

            yield return DoLeg(endY, startY);
            if (pauseAtEnds > 0f) yield return new WaitForSeconds(pauseAtEnds);
        }
    }

    IEnumerator DoLeg(float fromY, float toY)
    {
        onMoveEnd?.Invoke(); // end of previous leg (or initial)

        if (telegraphLead > 0f)
        {
            onTelegraph?.Invoke();
            yield return new WaitForSeconds(telegraphLead);
        }

        onMoveStart?.Invoke();

        _moving = true;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, legTime);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = curve.Evaluate(Mathf.Clamp01(t / dur));
            SetY(Mathf.Lerp(fromY, toY, k));
            yield return null;
        }
        SetY(toY);
        _moving = false;
    }

    void SetY(float y)
    {
        var p = transform.position;
        p.y = y;
        transform.position = p;
    }

    // ---------- Hazard detection ----------
    void OnTriggerEnter(Collider other)
    {
        if (!killOnContact || _hasKilled) return;

        // layer+tag filters
        if (((1 << other.gameObject.layer) & killLayers) == 0) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        if (killOnlyWhileMoving && !_moving) return;

        // Kill sequence (once)
        _hasKilled = true;
        onPlayerKilled?.Invoke();
        StartCoroutine(DeathSequence());
    }

    // ---------- Death sequence with integrated UI blink ----------
    IEnumerator DeathSequence()
    {
        float wait = Mathf.Max(reloadDelay, blinkDuration);

        // Kick off blink
        if (blinkTarget != null)
            StartCoroutine(BlinkRoutine(blinkDuration, blinkInterval));

        // wait (allows blink/SFX to finish)
        if (wait > 0f) yield return new WaitForSeconds(wait);

        // reload
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    IEnumerator BlinkRoutine(float duration, float interval)
    {
        // no target? nothing to do
        if (blinkTarget == null) yield break;

        float t = 0f;
        bool state = true;

        // ensure visible at start
        if (useCanvasGroup && blinkCanvasGroup != null)
        {
            blinkCanvasGroup.alpha = blinkOnAlpha;
        }
        else
        {
            blinkTarget.SetActive(true);
        }

        while (t < duration)
        {
            yield return new WaitForSeconds(interval);
            t += interval;
            state = !state;

            if (useCanvasGroup && blinkCanvasGroup != null)
                blinkCanvasGroup.alpha = state ? blinkOnAlpha : blinkOffAlpha;
            else
                blinkTarget.SetActive(state);
        }

        // (We don't restore here because we reload the scene; OnDisable covers editor stops.)
    }

    // -------- Editor helpers --------
    [ContextMenu("Set START from current")]
    void SetStartFromCurrent() => startY = transform.position.y;

    [ContextMenu("Set END from current")]
    void SetEndFromCurrent() => endY = transform.position.y;

    void OnDrawGizmosSelected()
    {
        Vector3 a = new Vector3(transform.position.x, startY, transform.position.z);
        Vector3 b = new Vector3(transform.position.x, endY,   transform.position.z);
        Gizmos.color = new Color(1f, 0.55f, 0f, 1f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.06f);
        Gizmos.DrawSphere(b, 0.06f);
    }
}
