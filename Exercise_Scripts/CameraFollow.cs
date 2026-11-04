using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camera follow and orbit for the Meadow Course / Orb Collector lab.
/// Attach to the Main Camera. Supports smooth follow, scroll zoom, and RMB drag orbit.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Framing")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
    [SerializeField] private float followSharpness = 8f;
    [SerializeField] private float lookHeight = 1f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float minZoom = 6f;
    [SerializeField] private float maxZoom = 50f;

    [Header("Orbit (RMB Drag)")]
    [SerializeField] private float orbitSpeed = 4f;
    [SerializeField] private bool allowVerticalOrbit = true;
    [SerializeField] private float verticalOrbitSpeed = 2f;
    [SerializeField] private float minHeight = 3f;
    [SerializeField] private float maxHeight = 40f;
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool lockCursorOnRMB = true;

    private void Start() => SnapToTarget();

    void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        HandleOrbit();

        Vector3 desired = target.position + offset;
        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, blend);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float distance = Mathf.Clamp(offset.magnitude - scroll * zoomSpeed, minZoom, maxZoom);
            offset = offset.normalized * distance;
        }
    }

    private void HandleOrbit()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (lockCursorOnRMB)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (lockCursorOnRMB)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.0001f)
            {
                float sign = invertX ? 1f : -1f;
                offset = Quaternion.AngleAxis(mouseX * orbitSpeed * sign, Vector3.up) * offset;
            }

            if (allowVerticalOrbit && Mathf.Abs(mouseY) > 0.0001f)
            {
                float newY = Mathf.Clamp(offset.y - mouseY * verticalOrbitSpeed, minHeight, maxHeight);
                offset.y = newY;
            }
        }
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
