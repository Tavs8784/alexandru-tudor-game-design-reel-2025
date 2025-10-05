using UnityEngine;

[ExecuteAlways]
public class FOVGizmo3D : MonoBehaviour
{
    public Camera sourceCamera;              // auto-fills if left null
    [Header("Range")]
    public float minDistance = 0.05f;
    public float maxDistance = 25f;

    [Header("Style")]
    public Color wireColor = new Color(1f, 0.8f, 0f, 1f);
    public Color rayColor  = new Color(1f, 0.6f, 0f, 0.8f);
    public Color hitColor  = new Color(1f, 0.2f, 0f, 0.9f);

    [Header("Viewport Rays (optional)")]
    public bool drawViewportRays = true;
    [Range(0, 32)] public int raysX = 8;    // columns across the view
    [Range(0, 18)] public int raysY = 4;    // rows across the view
    public bool raycastAgainstWorld = true;
    public LayerMask occlusionMask = ~0;    // what blocks sight
    public float hitMarkerSize = 0.06f;

    Camera Cam => sourceCamera ? sourceCamera : (sourceCamera = GetComponent<Camera>());

    void OnDrawGizmos()
    {
        if (!Cam) return;

        // draw wire frustum aligned with the camera
        Gizmos.color  = wireColor;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, Cam.fieldOfView, maxDistance, minDistance, Cam.aspect);
        Gizmos.matrix = Matrix4x4.identity;

        if (!drawViewportRays) return;

        // draw a simple grid of viewport rays
        for (int iy = 0; iy <= raysY; iy++)
        {
            float v = (raysY == 0) ? 0.5f : iy / (float)raysY;
            for (int ix = 0; ix <= raysX; ix++)
            {
                float u = (raysX == 0) ? 0.5f : ix / (float)raysX;

                Ray r = Cam.ViewportPointToRay(new Vector3(u, v, 0f));
                Vector3 start = r.origin;
                Vector3 end = r.origin + r.direction * maxDistance;

                if (raycastAgainstWorld && Physics.Raycast(r, out RaycastHit hit, maxDistance, occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    // draw to hit point, then mark impact
                    Gizmos.color = rayColor;
                    Gizmos.DrawLine(start, hit.point);
                    Gizmos.color = hitColor;
                    Gizmos.DrawSphere(hit.point, hitMarkerSize);
                }
                else
                {
                    Gizmos.color = rayColor;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
