using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 24f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private float effectLifetime = 1.5f;

    [Header("Spawn and Checkpoints")]
    [Tooltip("Primary initial spawn point at course start.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Section 3 Part 1 checkpoint spawn point (start of spike platform).")]
    [SerializeField] private Transform checkpointSection3Part1;
    [Tooltip("Section 3 Part 2 checkpoint spawn point (before Start_Line).")]
    [SerializeField] private Transform checkpointSection3;
    [Tooltip("Current active spawn point used for respawns.")]
    [SerializeField] private Transform activeSpawnPoint;

    [Header("Model and Ground Alignment")]
    [SerializeField] private Transform visualModel;
    [Tooltip("Offset applied to visualModel. Defaults to -0.4 on Y so feet firmly touch the platform surface.")]
    [SerializeField] private Vector3 modelOffset = Vector3.zero;
    [SerializeField] private float modelRotationOffset = 0f;

    [Header("Walk and Limb Animation")]
    [SerializeField] private bool enableWalkAnimation = true;
    [SerializeField] private float walkAnimationSpeed = 12f;
    [SerializeField] private float legSwingAngle = 30f;
    [SerializeField] private float armSwingAngle = 22f;
    [SerializeField] private float bodyBobHeight = 0.25f;
    [SerializeField] private float bodyWaddleAngle = 4.5f;

    [Header("Camera and View")]
    [SerializeField] private Camera playerCamera;

    [Header("Jump and Physics (Realistic Gravity)")]
    [SerializeField] private float jumpVelocity = 18f;
    [SerializeField] private float upwardGravityMultiplier = 1f;
    [SerializeField] private float fallGravityMultiplier = 5f;
    [SerializeField] private float lowJumpMultiplier = 1.2f;
    [SerializeField] private bool variableJumpHeight = false;
    [SerializeField] private float maxFallSpeed = 70f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.25f;

    [Header("Fall and Hazard")]
    [SerializeField] private float fallBoundaryY = -5f;
    [SerializeField] private float hazardCooldown = 0.4f;

    // Limb bones (Kenney character skeleton)
    private Transform leftLeg;
    private Transform rightLeg;
    private Transform leftArm;
    private Transform rightArm;
    private Quaternion initialLeftLegRot;
    private Quaternion initialRightLegRot;
    private Quaternion initialLeftArmRot;
    private Quaternion initialRightArmRot;

    private Rigidbody body;
    private BoxCollider boxCollider;
    private Vector3 moveInput;
    private float walkTimer;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float groundedUntil;
    private float nextHazardTime;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private GameObject fallbackEffectPrefab;
    private Vector3 externalVelocity;
    private MovingPlatform supportPlatform;

    public Vector3 EffectiveModelOffset
    {
        get { return modelOffset; }
        set { modelOffset = value; }
    }

    public Transform ActiveSpawnPoint => activeSpawnPoint != null ? activeSpawnPoint : spawnPoint;

    public void Bounce(float upwardVelocity, float forwardVelocity = 0f, Vector3 direction = default)
    {
        if (body != null)
        {
            Vector3 vel = body.linearVelocity;
            vel.y = upwardVelocity;
            body.linearVelocity = vel;
        }

        if (forwardVelocity > 0f)
        {
            Vector3 dir = direction != default ? direction : Vector3.left;
            dir.y = 0f;
            dir = dir.normalized;
            externalVelocity = dir * forwardVelocity;
        }

        groundedUntil = 0f;
        coyoteCounter = 0f;
        supportPlatform = null;
        Debug.Log($"[PlayerController] Bounced! Upward: {upwardVelocity}, Forward: {forwardVelocity}");
    }

    public void SetActiveSpawnPoint(Transform newSpawnPoint)
    {
        if (newSpawnPoint != null)
        {
            activeSpawnPoint = newSpawnPoint;
            Debug.Log($"[PlayerController] Active spawn point updated to: {newSpawnPoint.name}");
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        initialPosition = transform.position;

        if (activeSpawnPoint == null)
        {
            activeSpawnPoint = spawnPoint;
        }

        // Auto-assign camera if needed
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Align child model (Oozi) so its feet touch the platform and face matches player forward
        if (visualModel == null)
        {
            Transform found = transform.Find("Oozi");
            visualModel = found != null ? found : (transform.childCount > 0 ? transform.GetChild(0) : null);
        }

        if (visualModel != null)
        {
            // Lower model slightly to align feet with platform and reset rotation
            visualModel.localPosition = EffectiveModelOffset;
            visualModel.localRotation = Quaternion.Euler(0f, modelRotationOffset, 0f);
            FindLimbs();
        }

        // Start facing down the track (-X direction)
        initialRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        transform.rotation = initialRotation;
        if (body != null) body.rotation = initialRotation;

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
        EnsureLadderColliders();
        EnsureSpikeColliders();
        EnsureSpringColliders();
        if (leftLeg == null || rightLeg == null)
        {
            FindLimbs();
        }
        Debug.Log("[PlayerController] Ready. WASD/arrows move, Space jumps, RMB drag orbits camera, R restarts after the game ends.");
    }

    private void EnsureSpikeColliders()
    {
        // Disable dummy 1x1x1 colliders on parent containers if present
        GameObject bars = GameObject.Find("bars");
        if (bars != null) { Collider c = bars.GetComponent<Collider>(); if (c != null) c.enabled = false; }
        GameObject blocks = GameObject.Find("blocks");
        if (blocks != null) { Collider c = blocks.GetComponent<Collider>(); if (c != null) c.enabled = false; }

        // Use the visible mesh bounds, including spikes outside the main Spike group.
        // Keep the damage volume low and inset so clearing a tip is safe.
        foreach (MeshFilter mf in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null || !mf.sharedMesh.name.ToLowerInvariant().Contains("spike")) continue;
            GameObject go = mf.gameObject;
            go.tag = "Hazard";

            BoxCollider box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();
            foreach (Collider other in go.GetComponents<Collider>())
                if (other != box) other.enabled = false;
            box.isTrigger = true;
            box.enabled = true;

            Bounds bounds = mf.sharedMesh.bounds;
            Vector3 size = Vector3.Scale(bounds.size, new Vector3(0.85f, 0.55f, 0.85f));
            box.size = size;
            box.center = new Vector3(bounds.center.x, bounds.min.y + size.y * 0.5f, bounds.center.z);
        }
    }

    private void EnsureSpringColliders()
    {
        GameObject springParent = GameObject.Find("Spring");
        if (springParent != null)
        {
            // Ensure parent broad trigger collider across the 3 springs
            BoxCollider parentBox = springParent.GetComponent<BoxCollider>();
            if (parentBox == null) parentBox = springParent.AddComponent<BoxCollider>();
            parentBox.isTrigger = true;
            parentBox.enabled = true;
            parentBox.center = new Vector3(-72.59f, 10.3f, -4.0f);
            parentBox.size = new Vector3(5.0f, 1.8f, 18.0f);

            SpringPad parentPad = springParent.GetComponent<SpringPad>();
            if (parentPad == null) parentPad = springParent.AddComponent<SpringPad>();
            parentPad.SetParameters(24f, 10f, Vector3.left);

            // Ensure each child spring model has its own trigger collider and SpringPad
            MeshFilter[] filters = springParent.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in filters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                GameObject go = mf.gameObject;

                BoxCollider box = go.GetComponent<BoxCollider>();
                if (box == null) box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.enabled = true;
                box.size = new Vector3(0.7f, 0.4f, 0.7f);
                box.center = new Vector3(0f, 0.15f, 0f);

                SpringPad pad = go.GetComponent<SpringPad>();
                if (pad == null) pad = go.AddComponent<SpringPad>();
                pad.SetParameters(24f, 10f, Vector3.left);
            }
        }
    }

    private void EnsureLadderColliders()
    {
        // Disable dummy BoxCollider on parent bridge containers so they don't block walking
        string[] bridgeParentNames = new string[] { "Start-bridge", "End-bridge" };
        foreach (string bName in bridgeParentNames)
        {
            GameObject bGo = GameObject.Find(bName);
            if (bGo != null)
            {
                Collider bc = bGo.GetComponent<Collider>();
                if (bc != null) bc.enabled = false;
            }
        }

        // Find all ladder GameObjects across the scene and ensure they have a smooth, walkable BoxCollider
        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            string meshName = mf.sharedMesh.name.ToLower();
            string goName = mf.gameObject.name.ToLower();

            if (meshName.Contains("ladder") || goName.Contains("ladder") || goName.Contains("bridge"))
            {
                // Disable bumpy MeshCollider if present
                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if (mc != null) mc.enabled = false;

                // Ensure a smooth BoxCollider exists and is enabled
                BoxCollider box = mf.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = mf.gameObject.AddComponent<BoxCollider>();
                }
                box.enabled = true;
                if (meshName.Contains("long"))
                {
                    box.size = new Vector3(0.55f, 2.05f, 0.2f);
                    box.center = new Vector3(0f, 1.0f, 0f);
                }
                else
                {
                    box.size = new Vector3(0.55f, 1.05f, 0.2f);
                    box.center = new Vector3(0f, 0.5f, 0f);
                }
            }
        }
    }

    private void OnValidate()
    {
        if (visualModel == null)
        {
            Transform found = transform.Find("Oozi");
            visualModel = found != null ? found : (transform.childCount > 0 ? transform.GetChild(0) : null);
        }
    }

    private void FindLimbs()
    {
        if (visualModel == null) return;

        Transform[] allChildren = visualModel.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            string n = t.name.ToLower();
            if (n.Contains("leg") && (n.Contains("left") || n.EndsWith("-l") || n.Contains(".l")))
            {
                leftLeg = t;
                initialLeftLegRot = t.localRotation;
            }
            else if (n.Contains("leg") && (n.Contains("right") || n.EndsWith("-r") || n.Contains(".r")))
            {
                rightLeg = t;
                initialRightLegRot = t.localRotation;
            }
            else if (n.Contains("arm") && (n.Contains("left") || n.EndsWith("-l") || n.Contains(".l")))
            {
                leftArm = t;
                initialLeftArmRot = t.localRotation;
            }
            else if (n.Contains("arm") && (n.Contains("right") || n.EndsWith("-r") || n.Contains(".r")))
            {
                rightArm = t;
                initialRightArmRot = t.localRotation;
            }
        }
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

        // Void fall check
        if (transform.position.y < fallBoundaryY)
        {
            RespawnAtSpawn("Fell off the course - returned to spawn.");
            return;
        }

        // Section 3 Part 1 checkpoint detection (auto-activate when reaching Part 1)
        if (activeSpawnPoint != checkpointSection3Part1 && activeSpawnPoint != checkpointSection3 && checkpointSection3Part1 != null)
        {
            float dx = transform.position.x - checkpointSection3Part1.position.x;
            float dy = Mathf.Abs(transform.position.y - checkpointSection3Part1.position.y);
            float dz = Mathf.Abs(transform.position.z - checkpointSection3Part1.position.z);
            if (dx <= 15f && dx >= -180f && dy < 15f && dz < 30f)
            {
                SetActiveSpawnPoint(checkpointSection3Part1);
            }
        }

        // Section 3 Part 2 checkpoint detection (auto-activate when reaching Part 2 before start line)
        if (activeSpawnPoint != checkpointSection3 && checkpointSection3 != null)
        {
            float dx = transform.position.x - checkpointSection3.position.x;
            float dy = Mathf.Abs(transform.position.y - checkpointSection3.position.y);
            float dz = Mathf.Abs(transform.position.z - checkpointSection3.position.z);
            if (dx <= 10f && dx >= -140f && dy < 15f && dz < 25f)
            {
                SetActiveSpawnPoint(checkpointSection3);
            }
        }

        // Camera-relative movement inputs
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Transform view = playerCamera != null ? playerCamera.transform : (Camera.main != null ? Camera.main.transform : null);
        Vector3 forward = Vector3.left;   // Track forward (-X)
        Vector3 right = Vector3.forward;  // Track right (+Z)

        if (view != null)
        {
            Vector3 camFwd = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            Vector3 camRight = Vector3.ProjectOnPlane(view.right, Vector3.up);

            if (camFwd.sqrMagnitude > 0.001f) forward = camFwd.normalized;
            if (camRight.sqrMagnitude > 0.001f) right = camRight.normalized;
        }

        // W moves in forward direction (into the screen), S backward, D right, A left
        moveInput = Vector3.ClampMagnitude(forward * vertical + right * horizontal, 1f);

        // Ground check & coyote time
        if (CheckGrounded())
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // Jump buffer
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Animate character limbs and body movement
        if (visualModel != null && enableWalkAnimation)
        {
            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            bool isGrounded = CheckGrounded();

            if (isGrounded && isMoving)
            {
                walkTimer += Time.deltaTime * walkAnimationSpeed;
                float legCycle = Mathf.Sin(walkTimer);
                float bobCycle = Mathf.Abs(Mathf.Sin(walkTimer));
                float waddleCycle = Mathf.Sin(walkTimer * 0.5f);

                // 1. Leg and Arm swinging (natural walk cycle)
                if (leftLeg != null)
                    leftLeg.localRotation = initialLeftLegRot * Quaternion.Euler(legCycle * legSwingAngle, 0f, 0f);
                if (rightLeg != null)
                    rightLeg.localRotation = initialRightLegRot * Quaternion.Euler(-legCycle * legSwingAngle, 0f, 0f);
                if (leftArm != null)
                    leftArm.localRotation = initialLeftArmRot * Quaternion.Euler(-legCycle * armSwingAngle, 0f, 0f);
                if (rightArm != null)
                    rightArm.localRotation = initialRightArmRot * Quaternion.Euler(legCycle * armSwingAngle, 0f, 0f);

                // 2. Body bobbing & waddle tilt
                Vector3 targetOffset = EffectiveModelOffset + new Vector3(0f, bobCycle * bodyBobHeight, 0f);
                visualModel.localPosition = targetOffset;
                visualModel.localRotation = Quaternion.Euler(0f, modelRotationOffset, waddleCycle * bodyWaddleAngle);
            }
            else
            {
                // Smoothly return limbs to rest pose
                if (leftLeg != null)
                    leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, initialLeftLegRot, 12f * Time.deltaTime);
                if (rightLeg != null)
                    rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, initialRightLegRot, 12f * Time.deltaTime);
                if (leftArm != null)
                    leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, initialLeftArmRot, 12f * Time.deltaTime);
                if (rightArm != null)
                    rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, initialRightArmRot, 12f * Time.deltaTime);

                // Smoothly return body to grounded resting position
                visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, EffectiveModelOffset, 14f * Time.deltaTime);
                visualModel.localRotation = Quaternion.Lerp(visualModel.localRotation, Quaternion.Euler(0f, modelRotationOffset, 0f), 14f * Time.deltaTime);
            }
        }
    }

    private void FixedUpdate()
    {
        if (transform.position.y < fallBoundaryY)
        {
            RespawnAtSpawn("Fell off the course - returned to spawn.");
            return;
        }

        // 1. Horizontal movement
        Vector3 currentVel = body.linearVelocity;
        Vector3 targetHVel = new Vector3(moveInput.x * walkSpeed, 0f, moveInput.z * walkSpeed);
        if (supportPlatform != null && Time.time <= groundedUntil && currentVel.y <= 0.1f)
            targetHVel += supportPlatform.Velocity;
        if (externalVelocity.sqrMagnitude > 0.01f)
        {
            targetHVel += externalVelocity;
            externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, 1.0f * Time.fixedDeltaTime);
            if (CheckGrounded() && body.linearVelocity.y <= 0.1f)
            {
                externalVelocity = Vector3.zero;
            }
        }
        body.linearVelocity = new Vector3(targetHVel.x, currentVel.y, targetHVel.z);

        // 2. Character rotation: face always points forward in movement direction
        if (moveInput.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveInput, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            body.rotation = transform.rotation;
        }

        // 3. Jump execution
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            body.linearVelocity = new Vector3(body.linearVelocity.x, jumpVelocity, body.linearVelocity.z);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            groundedUntil = 0f;
            supportPlatform = null;
        }

        // 4. Realistic Physics: Gravity & Drop Acceleration
        float baseGravityY = Physics.gravity.y; // -9.81 m/s^2
        if (body.linearVelocity.y < 0f)
        {
            // Falling down: heavier gravity multiplier so the character drops with real physical weight
            float extraFallGravity = baseGravityY * (fallGravityMultiplier - 1f);
            body.AddForce(Vector3.up * extraFallGravity, ForceMode.Acceleration);
        }
        else if (body.linearVelocity.y > 0f)
        {
            if (variableJumpHeight && !Input.GetKey(KeyCode.Space))
            {
                // Released jump key early: short-jump gravity for responsive hops
                float extraLowJumpGravity = baseGravityY * (lowJumpMultiplier - 1f);
                body.AddForce(Vector3.up * extraLowJumpGravity, ForceMode.Acceleration);
            }
            else if (upwardGravityMultiplier > 1f)
            {
                // Upward jump arc gravity
                float extraUpwardGravity = baseGravityY * (upwardGravityMultiplier - 1f);
                body.AddForce(Vector3.up * extraUpwardGravity, ForceMode.Acceleration);
            }
        }

        // 5. Terminal velocity clamp
        if (body.linearVelocity.y < -maxFallSpeed)
        {
            body.linearVelocity = new Vector3(body.linearVelocity.x, -maxFallSpeed, body.linearVelocity.z);
        }
    }

    private bool CheckGrounded()
    {
        if (Time.time <= groundedUntil) return true;

        if (boxCollider != null)
        {
            float halfY = boxCollider.size.y * 0.5f * transform.localScale.y;
            float halfX = boxCollider.size.x * 0.35f * transform.localScale.x;
            float halfZ = boxCollider.size.z * 0.35f * transform.localScale.z;

            Vector3 centerBottom = transform.position + boxCollider.center - Vector3.up * (halfY - 0.2f);
            float checkDist = 0.6f;

            Vector3[] rayOrigins = new Vector3[]
            {
                centerBottom,
                centerBottom + new Vector3(halfX, 0f, halfZ),
                centerBottom + new Vector3(-halfX, 0f, halfZ),
                centerBottom + new Vector3(halfX, 0f, -halfZ),
                centerBottom + new Vector3(-halfX, 0f, -halfZ)
            };

            for (int i = 0; i < rayOrigins.Length; i++)
            {
                if (Physics.Raycast(rayOrigins[i], Vector3.down, out RaycastHit hit, checkDist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject && hit.normal.y > 0.35f && !IsHazard(hit.collider.gameObject))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryReachExit(collision.collider);
        if (IsHazard(collision.gameObject))
        {
            RespawnAtSpawn("Obstacle hit - returned to spawn.");
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.35f)
            {
                groundedUntil = Time.time + coyoteTime;
                supportPlatform = collision.rigidbody != null ? collision.rigidbody.GetComponent<MovingPlatform>() : null;
                break;
            }
        }

        Debug.Log("[OnCollisionEnter] Touched " + collision.gameObject.name);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (GameManager.Instance != null && GameManager.Instance.HasKey)
            TryReachExit(collision.collider);
        if (IsHazard(collision.gameObject))
        {
            RespawnAtSpawn("Obstacle hit - returned to spawn.");
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.35f)
            {
                groundedUntil = Time.time + 0.12f;
                supportPlatform = collision.rigidbody != null ? collision.rigidbody.GetComponent<MovingPlatform>() : null;
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsHazard(other.gameObject))
        {
            RespawnAtSpawn("Obstacle hit - returned to spawn.");
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
        else TryReachExit(other);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (supportPlatform != null && collision.rigidbody == supportPlatform.GetComponent<Rigidbody>())
            supportPlatform = null;
    }

    private void OnTriggerStay(Collider other)
    {
        if (GameManager.Instance != null && GameManager.Instance.HasKey)
            TryReachExit(other);
        if (IsHazard(other.gameObject))
        {
            RespawnAtSpawn("Obstacle hit - returned to spawn.");
        }
    }

    private void TryReachExit(Collider other)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        for (Transform current = other.transform; current != null; current = current.parent)
        {
            if (!current.CompareTag("Exit")) continue;
            GameManager.Instance.ReachExit();
            return;
        }
    }

    private bool IsHazard(GameObject obj)
    {
        if (obj == null) return false;

        Transform current = obj.transform;
        while (current != null)
        {
            if (current.CompareTag("Hazard"))
                return true;
            current = current.parent;
        }

        current = obj.transform;
        while (current != null)
        {
            string n = current.name.ToLower();
            if (n == "spike" || n == "spikes" || n == "saws" || n == "saw" ||
                n == "bars" || n == "obstacles" || n == "obstacle" ||
                n == "red_line" || n.Contains("spike-block") || n.Contains("hazard"))
            {
                return true;
            }
            current = current.parent;
        }

        return false;
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

    public void RespawnAtSpawn(string reason = "Hazard hit - returned to spawn.")
    {
        if (Time.time < nextHazardTime) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        nextHazardTime = Time.time + hazardCooldown;
        moveInput = Vector3.zero;
        walkTimer = 0f;
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        groundedUntil = 0f;
        externalVelocity = Vector3.zero;
        supportPlatform = null;

        Transform targetSpawn = activeSpawnPoint != null ? activeSpawnPoint : spawnPoint;
        Vector3 spawnPos = targetSpawn != null ? targetSpawn.position : initialPosition;
        Quaternion spawnRot = initialRotation;
        if (targetSpawn != null && targetSpawn.rotation != Quaternion.identity)
        {
            spawnRot = targetSpawn.rotation;
        }
        
        Debug.Log("RESPAWN TRIGGERED! Spawn position: " + spawnPos);
        transform.position = spawnPos;
        transform.rotation = spawnRot;

        if (visualModel != null)
        {
            visualModel.localPosition = EffectiveModelOffset;
            visualModel.localRotation = Quaternion.Euler(0f, modelRotationOffset, 0f);
            if (leftLeg != null) leftLeg.localRotation = initialLeftLegRot;
            if (rightLeg != null) rightLeg.localRotation = initialRightLegRot;
            if (leftArm != null) leftArm.localRotation = initialLeftArmRot;
            if (rightArm != null) rightArm.localRotation = initialRightArmRot;
        }

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = spawnPos;
            body.rotation = spawnRot;
        }

        Physics.SyncTransforms();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordRespawn(reason);
        }

        CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (follow != null)
        {
            follow.SnapToTarget();
        }

        Debug.Log($"[Respawn] Returned to start section. Reason: {reason}");
    }

    private void OnDisable()
    {
        Debug.Log("[OnDisable] Player controller stopped.");
    }
}
