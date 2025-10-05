using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class FOVConeGroundGizmo : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;        // assign your FPS camera
    [SerializeField] private float radius = 20f;         // how far to draw the cone
    [SerializeField] private float groundY = 0f;         // y height of the ground plane
    [SerializeField] private Color fillColor  = new Color(1f, 0.8f, 0f, 0.12f);
    [SerializeField] private Color lineColor  = new Color(1f, 0.7f, 0f, 0.9f);
    [SerializeField] private float lineThickness = 2f;

    Camera Cam => sourceCamera ? sourceCamera : (sourceCamera = Camera.main);

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Cam) return;

        // compute horizontal FOV from vertical FOV & aspect
        float vFovRad = Cam.fieldOfView * Mathf.Deg2Rad;
        float hFovRad = 2f * Mathf.Atan(Mathf.Tan(vFovRad * 0.5f) * Cam.aspect);
        float hFovDeg = hFovRad * Mathf.Rad2Deg;

        // center at player position projected on ground
        Vector3 pos = transform.position;
        pos.y = groundY;

        // forward direction projected on ground
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        if (fwd.sqrMagnitude < 1e-5f) fwd = Vector3.forward;

        // left boundary direction
        Vector3 leftDir = Quaternion.Euler(0f, -hFovDeg * 0.5f, 0f) * fwd;

        using (new Handles.DrawingScope(Matrix4x4.identity))
        {
            // Fill wedge
            Handles.color = fillColor;
            Handles.DrawSolidArc(pos, Vector3.up, leftDir, hFovDeg, radius);

            // Outline
            Handles.color = lineColor;
            Handles.DrawWireArc(pos, Vector3.up, leftDir, hFovDeg, radius);

            // Center ray
            Handles.DrawAAPolyLine(lineThickness, new Vector3[] { pos, pos + fwd * radius });
        }
    }
#endif
}