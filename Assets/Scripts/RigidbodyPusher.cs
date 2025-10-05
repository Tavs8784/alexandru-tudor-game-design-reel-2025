// RigidbodyPusher.cs
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RigidbodyPusher : MonoBehaviour
{
    [Tooltip("Units/second of push velocity applied to crates on contact.")]
    public float pushSpeed = 2.2f;
    [Tooltip("Only push when the collision is mostly horizontal.")]
    [Range(0f,1f)] public float minHorizontalDot = 0.5f;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var rb = hit.rigidbody;
        if (!rb || rb.isKinematic) return;

        // Don’t push if we mostly hit from above/below
        if (Vector3.Dot(Vector3.up, hit.normal) > minHorizontalDot) return;

        // Horizontal push in the direction we’re moving
        var dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        if (dir.sqrMagnitude < 0.0001f) return;

        // Give it a target horizontal velocity (preserve Y)
        var v = rb.linearVelocity;
        v.x = dir.normalized.x * pushSpeed;
        v.z = dir.normalized.z * pushSpeed;
        rb.linearVelocity = v;
    }
}