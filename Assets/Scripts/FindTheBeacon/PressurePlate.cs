using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PressurePlate : MonoBehaviour
{
    [Header("Activator Filter")]
    [Tooltip("Layers allowed to trigger the plate.")]
    [SerializeField] private LayerMask activatorLayers = ~0; // everything by default

    [Tooltip("Any of these tags will press the plate. Leave empty to accept any tag (see toggle below).")]
    [SerializeField] private List<string> allowedTags = new List<string> { "Player", "Crate" };

    [Tooltip("If true AND allowedTags is empty, accept any tag. If false AND list is empty, reject all.")]
    [SerializeField] private bool acceptAnyWhenListEmpty = true;

    [Header("Visual/Movement")]
    [Tooltip("The transform that visually moves (can be this object).")]
    [SerializeField] private Transform plateVisual;
    [Tooltip("How far the plate sinks when pressed (in local units, usually meters).")]
    public float pressDepth = 0.06f;
    public float pressTime = 0.12f;
    public float releaseTime = 0.15f;
    public AnimationCurve pressCurve   = AnimationCurve.EaseInOut(0,0, 1,1);
    public AnimationCurve releaseCurve = AnimationCurve.EaseInOut(0,0, 1,1);

    [Header("Door control (optional)")]
    [SerializeField] private SoftDoor door;  // assign your sliding door
    [Tooltip("If true, door opens on first press and stays open (latch). If false, door stays open only while pressed.")]
    [SerializeField] private bool latchDoorOpen = true;
    [SerializeField] private bool closeDoorOnRelease = false; // only used if latchDoorOpen == false
    [Tooltip("Delay before closing after release (used only if latchDoorOpen == false and closeDoorOnRelease == true).")]
    [SerializeField] private float doorCloseDelay = 0.25f;

    [Header("Events")]
    [SerializeField] private UnityEvent onPressed;   // SFX/VFX hooks
    [SerializeField] private UnityEvent onReleased;

    // --- internals ---
    Vector3 _restLocalPos;
    int _insideCount;
    bool _isAnimating;
    bool _isPressed;
    Coroutine _doorCloseRoutine;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // this component expects to run on a trigger "sensor"
        if (!plateVisual) plateVisual = transform;
    }

    void Awake()
    {
        if (!plateVisual) plateVisual = transform;
        _restLocalPos = plateVisual.localPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValidActivator(other)) return;
        _insideCount++;
        if (!_isPressed) SetPressed(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsValidActivator(other)) return;
        _insideCount = Mathf.Max(0, _insideCount - 1);
        if (_insideCount == 0 && _isPressed) SetPressed(false);
    }

    bool IsValidActivator(Collider other)
    {
        // Layer filter
        if ((activatorLayers.value & (1 << other.gameObject.layer)) == 0)
            return false;

        // Tag filter (any-of). If list is empty, obey acceptAnyWhenListEmpty.
        if (allowedTags != null && allowedTags.Count > 0)
        {
            for (int i = 0; i < allowedTags.Count; i++)
            {
                var tag = allowedTags[i];
                if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag))
                    return true;
            }
            return false;
        }
        else
        {
            return acceptAnyWhenListEmpty;
        }
    }

    void SetPressed(bool pressed)
    {
        _isPressed = pressed;

        // Cancel any pending door-close when pressed again
        if (pressed && _doorCloseRoutine != null)
        {
            StopCoroutine(_doorCloseRoutine);
            _doorCloseRoutine = null;
        }

        StopAllCoroutines();
        StartCoroutine(AnimatePlate(pressed));

        if (pressed)
        {
            onPressed?.Invoke();
            if (door) door.Open();
        }
        else
        {
            onReleased?.Invoke();

            // Only close if NOT latched and configured to close on release
            if (door && !latchDoorOpen && closeDoorOnRelease)
            {
                if (_doorCloseRoutine != null) StopCoroutine(_doorCloseRoutine);
                _doorCloseRoutine = StartCoroutine(CloseDoorAfterDelay());
            }
        }
    }

    IEnumerator CloseDoorAfterDelay()
    {
        float t = Mathf.Max(0f, doorCloseDelay);
        while (t > 0f)
        {
            // If pressed again during the wait, abort closing
            if (_isPressed)
            {
                _doorCloseRoutine = null;
                yield break;
            }
            t -= Time.deltaTime;
            yield return null;
        }
        // Still released? Close.
        if (!_isPressed) door.Close();
        _doorCloseRoutine = null;
    }

    IEnumerator AnimatePlate(bool pressed)
    {
        _isAnimating = true;
        float dur = Mathf.Max(0.0001f, pressed ? pressTime : releaseTime);
        var curve = pressed ? pressCurve : releaseCurve;

        Vector3 from = plateVisual.localPosition;
        Vector3 to   = _restLocalPos + Vector3.down * (pressed ? pressDepth : 0f);

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = curve.Evaluate(Mathf.Clamp01(t / dur));
            plateVisual.localPosition = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        plateVisual.localPosition = to;
        _isAnimating = false;
    }

    void OnDrawGizmosSelected()
    {
        var vis = plateVisual ? plateVisual : transform;
        Vector3 rest = Application.isPlaying ? _restLocalPos : vis.localPosition;
        Vector3 worldRest = vis.TransformPoint(rest);
        Vector3 worldDown = vis.TransformVector(Vector3.down * pressDepth);
        Gizmos.color = new Color(1f, 0.7f, 0f, 1f);
        Gizmos.DrawLine(worldRest, worldRest + worldDown);
        Gizmos.DrawSphere(worldRest, 0.01f);
        Gizmos.DrawSphere(worldRest + worldDown, 0.01f);
    }
}
