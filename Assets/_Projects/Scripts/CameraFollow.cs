using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
    [SerializeField] private float followSharpness = 8f;
    [SerializeField] private float lookHeight = 1f;
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float minZoom = 6f;
    [SerializeField] private float maxZoom = 18f;

    private void Start() => SnapToTarget();

    private void LateUpdate()
    {
        if (target == null) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float distance = Mathf.Clamp(offset.magnitude - scroll * zoomSpeed, minZoom, maxZoom);
            offset = offset.normalized * distance;
        }

        Vector3 desired = target.position + offset;
        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, blend);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
