using System.Collections;
using UnityEngine;

public class SoftDoor : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform panel;           // the mesh that slides
    [SerializeField] private Vector3 openOffset = new Vector3(0, 0, -2f); // how far to move from closed
    [SerializeField] private float moveTime = 0.6f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0,0, 1,1);
    [SerializeField] private bool startClosed = true;

    Vector3 _closedPos;
    Vector3 _openPos;
    Coroutine _anim;
    bool _isOpen;

    void Awake()
    {
        if (!panel) panel = transform;
        _closedPos = panel.position;
        _openPos = _closedPos + openOffset;
        if (startClosed) panel.position = _closedPos; else { panel.position = _openPos; _isOpen = true; }
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        StartMove(_openPos);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        StartMove(_closedPos);
    }

    void StartMove(Vector3 target)
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(MoveTo(target));
    }

    IEnumerator MoveTo(Vector3 target)
    {
        Vector3 from = panel.position;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, moveTime);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / dur));
            panel.position = Vector3.LerpUnclamped(from, target, k);
            yield return null;
        }
        panel.position = target;
    }

    // optional gizmo
    void OnDrawGizmosSelected()
    {
        var p = panel ? panel : transform;
        Vector3 closed = Application.isPlaying ? _closedPos : p.position;
        Vector3 open = closed + openOffset;
        Gizmos.color = new Color(0f, 0.7f, 1f, 1f);
        Gizmos.DrawLine(closed, open);
        Gizmos.DrawSphere(closed, 0.03f);
        Gizmos.DrawSphere(open, 0.03f);
    }
}