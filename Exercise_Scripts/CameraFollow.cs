using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STARTER — Camera follow for the "Orb Collector" lab goal.
/// Attach to the Main Camera. Drag the Player into the target field
/// (or let the script find it — compare both approaches live).
///
/// ALREADY WORKS: nothing yet — this one is entirely yours.
///
/// YOUR LAB TASKS:
///   TODO 3 support: move the follow logic here if you prefer, or do it in
///   PlayerController.LateUpdate(). Both are valid; LateUpdate is the key idea.
///   TODO 8 (stretch): use Input.GetAxis("Mouse ScrollWheel") to zoom the
///   offset length between minZoom and maxZoom.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;     // drag the Player here

    [Header("Framing")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
    [SerializeField] private float followSharpness = 5f;

    [Header("Zoom (stretch)")]
    [SerializeField] private float minZoom = 6f;
    [SerializeField] private float maxZoom = 18f;

    void LateUpdate()
    {
        if (target == null) return;

        // Follow AFTER the player moved — this is why we use LateUpdate.
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followSharpness * Time.deltaTime);
    }
}
