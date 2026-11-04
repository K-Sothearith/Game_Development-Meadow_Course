using System.Collections;
using UnityEngine;

public class SpringPad : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Upward velocity applied to player.")]
    [SerializeField] private float bounceVelocity = 24f;

    [Tooltip("Forward velocity applied to player in the track forward direction (-X).")]
    [SerializeField] private float forwardForce = 10f;

    [Tooltip("Direction to launch horizontally. Default Vector3.left (-X).")]
    [SerializeField] private Vector3 launchDirection = Vector3.left;

    [Header("Juice & Visuals")]
    [Tooltip("Animate spring compression and release when stepped on.")]
    [SerializeField] private bool squishAnimation = true;
    [SerializeField] private float squishDuration = 0.35f;
    [SerializeField] private float cooldownTime = 0.4f;

    private float nextBounceTime;
    private Vector3 originalScale;
    private Coroutine squishRoutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBounce(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryBounce(collision.gameObject);
    }

    public void SetParameters(float upwardVel, float forwardVel, Vector3 direction)
    {
        bounceVelocity = upwardVel;
        forwardForce = forwardVel;
        launchDirection = direction;
    }

    private void TryBounce(GameObject target)
    {
        if (Time.time < nextBounceTime) return;

        PlayerController player = target.GetComponent<PlayerController>();
        if (player == null) player = target.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            nextBounceTime = Time.time + cooldownTime;
            player.Bounce(bounceVelocity, forwardForce, launchDirection);

            if (squishAnimation)
            {
                if (squishRoutine != null) StopCoroutine(squishRoutine);
                squishRoutine = StartCoroutine(AnimateSquish());
            }

            Debug.Log($"[SpringPad] Player launched by '{gameObject.name}' with bounceVelocity={bounceVelocity}, forwardForce={forwardForce}");
        }
    }

    private IEnumerator AnimateSquish()
    {
        float elapsed = 0f;
        // Phase 1: compress along Y
        Vector3 squishedScale = new Vector3(originalScale.x * 1.25f, originalScale.y * 0.35f, originalScale.z * 1.25f);
        float compressTime = squishDuration * 0.25f;
        while (elapsed < compressTime)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, squishedScale, elapsed / compressTime);
            yield return null;
        }

        // Phase 2: spring overshoot stretch along Y
        elapsed = 0f;
        Vector3 stretchedScale = new Vector3(originalScale.x * 0.9f, originalScale.y * 1.4f, originalScale.z * 0.9f);
        float stretchTime = squishDuration * 0.35f;
        while (elapsed < stretchTime)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squishedScale, stretchedScale, elapsed / stretchTime);
            yield return null;
        }

        // Phase 3: return smoothly to original scale
        elapsed = 0f;
        float returnTime = squishDuration * 0.4f;
        while (elapsed < returnTime)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(stretchedScale, originalScale, elapsed / returnTime);
            yield return null;
        }
        transform.localScale = originalScale;
        squishRoutine = null;
    }
}

