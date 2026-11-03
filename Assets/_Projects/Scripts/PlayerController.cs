using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 24f;
    [SerializeField] private float jumpVelocity = 14f;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private float effectLifetime = 1.5f;

    private Rigidbody body;
    private Vector3 moveInput;
    private bool jumpQueued;
    private float groundedUntil;
    private float nextHazardTime;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private GameObject fallbackEffectPrefab;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        if (pickupEffectPrefab == null)
        {
            fallbackEffectPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallbackEffectPrefab.name = "Pickup Effect Template";
            fallbackEffectPrefab.transform.localScale = Vector3.one * 0.35f;
            Collider effectCollider = fallbackEffectPrefab.GetComponent<Collider>();
            if (effectCollider != null) Destroy(effectCollider);
            Renderer effectRenderer = fallbackEffectPrefab.GetComponent<Renderer>();
            if (effectRenderer != null) effectRenderer.material.color = Color.yellow;
            fallbackEffectPrefab.SetActive(false);
        }
    }

    private void Start()
    {
        Debug.Log("[PlayerController] Oozi ready. WASD/arrow keys move, Space jumps, R restarts after the game ends.");
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            moveInput = Vector3.zero;
            if (Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Transform view = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = view != null ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = view != null ? Vector3.ProjectOnPlane(view.right, Vector3.up).normalized : Vector3.right;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        if (right.sqrMagnitude < 0.01f) right = Vector3.right;
        moveInput = Vector3.ClampMagnitude(forward * vertical + right * horizontal, 1f);
        if (Input.GetKeyDown(KeyCode.Space)) jumpQueued = true;
    }

    private void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        body.linearVelocity = new Vector3(moveInput.x * walkSpeed, velocity.y, moveInput.z * walkSpeed);
        if (jumpQueued && Time.time <= groundedUntil)
        {
            body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            body.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
        }
        jumpQueued = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Hazard"))
        {
            RespawnAtSpawn();
            return;
        }
        Debug.Log("[OnCollisionEnter] Oozi touched " + collision.gameObject.name);
    }

    private void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.55f)
            {
                groundedUntil = Time.time + 0.12f;
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hazard"))
        {
            RespawnAtSpawn();
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (other.CompareTag("coin"))
        {
            GameManager.Instance.CollectCoin();
            Collect(other);
        }
        else if (other.CompareTag("diamond"))
        {
            GameManager.Instance.CollectDiamond();
            Collect(other);
        }
        else if (other.CompareTag("key"))
        {
            GameManager.Instance.CollectKey();
            Collect(other);
        }
        else if (other.CompareTag("Exit"))
        {
            GameManager.Instance.ReachExit();
        }
    }

    private void Collect(Collider pickup)
    {
        Debug.Log("[OnTriggerEnter] Collected " + pickup.gameObject.name);
        GameObject effectSource = pickupEffectPrefab != null ? pickupEffectPrefab : fallbackEffectPrefab;
        if (effectSource != null)
        {
            GameObject effect = Instantiate(effectSource, pickup.transform.position, Quaternion.identity);
            effect.SetActive(true);
            Destroy(effect, effectLifetime);
        }
        pickup.enabled = false;
        Destroy(pickup.gameObject);
    }

    public void RespawnAtSpawn()
    {
        if (Time.time < nextHazardTime) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        nextHazardTime = Time.time + 0.75f;
        moveInput = Vector3.zero;
        jumpQueued = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = spawnPoint != null ? spawnPoint.position : initialPosition;
        body.rotation = spawnPoint != null ? spawnPoint.rotation : initialRotation;
        if (GameManager.Instance != null) GameManager.Instance.RecordRespawn();
        CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.SnapToTarget();
        Debug.Log("[Respawn] Oozi returned to the spawn point.");
    }

    private void OnDisable()
    {
        Debug.Log("[OnDisable] Player controller stopped.");
    }
}
