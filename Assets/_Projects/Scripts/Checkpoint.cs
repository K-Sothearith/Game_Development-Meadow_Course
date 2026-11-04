using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("The spawn point transform. If not set, uses this GameObject's transform.")]
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private GameObject activationEffect;

    private bool isActivated;

    private void Awake()
    {
        if (spawnTransform == null) spawnTransform = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && isActivated) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            isActivated = true;
            player.SetActiveSpawnPoint(spawnTransform != null ? spawnTransform : transform);
            Debug.Log($"[Checkpoint] Checkpoint activated: '{gameObject.name}' at {(spawnTransform != null ? spawnTransform.position : transform.position)}");

            if (activationEffect != null)
            {
                activationEffect.SetActive(true);
            }
        }
    }
}

