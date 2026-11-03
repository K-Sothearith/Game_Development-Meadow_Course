using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;

    private Rigidbody body;
    private Transform destination;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        destination = pointB;
    }

    private void FixedUpdate()
    {
        if (pointA == null || pointB == null || speed <= 0f) return;
        if (destination == null) destination = pointB;
        Vector3 next = Vector3.MoveTowards(body.position, destination.position, speed * Time.fixedDeltaTime);
        body.MovePosition(next);
        if (Vector3.Distance(next, destination.position) < 0.01f)
            destination = destination == pointB ? pointA : pointB;
    }
}
