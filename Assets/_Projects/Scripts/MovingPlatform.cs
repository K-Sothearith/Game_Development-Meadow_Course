using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
[DefaultExecutionOrder(-100)]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;

    private Rigidbody body;
    private Transform destination;
    public Vector3 Velocity { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        destination = pointB;
    }

    private void FixedUpdate()
    {
        Velocity = Vector3.zero;
        if (pointA == null || pointB == null || speed <= 0f) return;
        if (destination == null) destination = pointB;
        Vector3 next = Vector3.MoveTowards(body.position, destination.position, speed * Time.fixedDeltaTime);
        Velocity = (next - body.position) / Time.fixedDeltaTime;
        body.MovePosition(next);
        if (Vector3.Distance(next, destination.position) < 0.01f)
            destination = destination == pointB ? pointA : pointB;
    }
}
