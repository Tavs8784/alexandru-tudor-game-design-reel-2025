using UnityEngine;
using UnityEngine.Events;

public class VerticalSweeper : MonoBehaviour
{
    [Header("Endpoints (world space Y)")]
    [Tooltip("Captured from scene placement via the context menu")]
    public float startY;
    public float endY;
    [Tooltip("Snap to startY when play begins")]
    public bool snapToStartOnPlay = true;

    [Header("Motion")]
    [Tooltip("Seconds for one leg (start->end or end->start)")]
    public float legTime = 2.5f;
    [Tooltip("Pause at each end (seconds)")]
    public float pauseAtEnds = 0.35f;
    [Tooltip("Warning time before moving (seconds)")]
    public float telegraphLead = 0.25f;

    [Header("Easing")]
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Events (hook SFX/VFX)")]
    public UnityEvent onTelegraph;  // called before moving each leg
    public UnityEvent onMoveStart;
    public UnityEvent onMoveEnd;

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
        // If endpoints are equal, nothing to do.
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
        // ReSharper disable once IteratorNeverReturns
    }

    System.Collections.IEnumerator DoLeg(float fromY, float toY)
    {
        // End-of-previous-leg notification (or initial state)
        onMoveEnd?.Invoke();

        if (telegraphLead > 0f)
        {
            onTelegraph?.Invoke();
            yield return new WaitForSeconds(telegraphLead);
        }

        onMoveStart?.Invoke();

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
    }

    void SetY(float y)
    {
        var p = transform.position;
        p.y = y;                    // world-space Y only
        transform.position = p;
    }

    // -------- Editor helpers --------

    [ContextMenu("Set START from current")]
    void SetStartFromCurrent()
    {
        startY = transform.position.y;
    }

    [ContextMenu("Set END from current")]
    void SetEndFromCurrent()
    {
        endY = transform.position.y;
    }

    void OnDrawGizmosSelected()
    {
        // Draw a vertical guide from startY to endY at current XZ
        Vector3 a = new Vector3(transform.position.x, startY, transform.position.z);
        Vector3 b = new Vector3(transform.position.x, endY,   transform.position.z);

        Gizmos.color = new Color(1f, 0.55f, 0f, 1f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.06f);
        Gizmos.DrawSphere(b, 0.06f);
    }
}
