using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target & Positioning")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(45f, 25f, 0f);
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

    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
        SnapToTarget();
    }

    private void Start() => SnapToTarget();

    private void LateUpdate()
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
        // Cursor lock while rotating with RMB
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

        // Orbit when holding Right Mouse Button
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.0001f)
            {
                // Moving mouse to the right moves camera to the right side of the target
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
