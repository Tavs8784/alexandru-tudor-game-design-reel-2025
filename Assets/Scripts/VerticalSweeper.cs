using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // <-- reload

public class VerticalSweeper : MonoBehaviour
{
    [Header("Endpoints (world space Y)")]
    [Tooltip("Captured from scene placement via the context menu")]
    [SerializeField] private float startY;
    [SerializeField] private float endY;
    [Tooltip("Snap to startY when play begins")]
    [SerializeField] private bool snapToStartOnPlay = true;

    [Header("Motion")]
    [Tooltip("Seconds for one leg (start->end or end->start)")]
    [SerializeField] private float legTime = 2.5f;
    [Tooltip("Pause at each end (seconds)")]
    [SerializeField] private float pauseAtEnds = 0.35f;
    [Tooltip("Warning time before moving (seconds)")]
    [SerializeField] private float telegraphLead = 0.25f;

    [Header("Easing")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Events (hook SFX/VFX)")]
    [SerializeField] private UnityEvent onTelegraph;  // called before moving each leg
    [SerializeField] private UnityEvent onMoveStart;
    [SerializeField] private UnityEvent onMoveEnd;

    // --- HAZARD ADD-ON ---
    [Header("Hazard (kill player on contact)")]
    [SerializeField] private bool killOnContact = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask killLayers = ~0;  // which layers can be killed
    [SerializeField] private bool killOnlyWhileMoving = true;
    [SerializeField] private float reloadDelay = 0.35f;  // seconds before reloading
    [SerializeField] private UnityEvent onPlayerKilled;  // optional SFX/VFX

    bool _moving;         // true while the bar is actually moving
    bool _hasKilled;      // prevent double-kill
    Coroutine _loop;

    void OnEnable()
    {
        if (snapToStartOnPlay) SetY(startY);
        if (_loop == null) _loop = StartCoroutine(Loop());
    }

    void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
    }

    System.Collections.IEnumerator Loop()
    {
        if (Mathf.Approximately(startY, endY)) yield break;

        while (true)
        {
            // start -> end
            yield return DoLeg(startY, endY);
            if (pauseAtEnds > 0f) yield return new WaitForSeconds(pauseAtEnds);

            // end -> start
            yield return DoLeg(endY, startY);
            if (pauseAtEnds > 0f) yield return new WaitForSeconds(pauseAtEnds);
        }
    }

    System.Collections.IEnumerator DoLeg(float fromY, float toY)
    {
        onMoveEnd?.Invoke(); // end of previous leg (or initial state)

        if (telegraphLead > 0f)
        {
            onTelegraph?.Invoke();
            yield return new WaitForSeconds(telegraphLead);
        }

        onMoveStart?.Invoke();

        _moving = true; // <-- lethal window (if killOnlyWhileMoving)
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
        _moving = false; // <-- not lethal if killOnlyWhileMoving is true
    }

    void SetY(float y)
    {
        var p = transform.position;
        p.y = y;                    // world-space Y only
        transform.position = p;
    }

    // -------- Hazard detection (trigger-based) --------
    void OnTriggerEnter(Collider other)
    {
        if (!killOnContact || _hasKilled) return;

        // layer + tag filters
        if (((1 << other.gameObject.layer) & killLayers) == 0) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        // only lethal while moving? (optional)
        if (killOnlyWhileMoving && !_moving) return;

        // OK, kill the player
        _hasKilled = true;
        onPlayerKilled?.Invoke();
        StartCoroutine(ReloadAfterDelay());
    }

    System.Collections.IEnumerator ReloadAfterDelay()
    {
        if (reloadDelay > 0f) yield return new WaitForSeconds(reloadDelay);
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
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
