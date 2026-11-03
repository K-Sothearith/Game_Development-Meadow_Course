using UnityEngine;

// Optional visual script for a coin or diamond. PlayerController handles collection.
public class Orb : MonoBehaviour
{
    [SerializeField] private Color[] clickColors = { Color.cyan, Color.magenta, Color.yellow };
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float spinSpeed = 90f;

    private Vector3 basePosition;
    private int clickIndex;

    private void Awake() => basePosition = transform.position;

    private void Update()
    {
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnMouseDown()
    {
        if (clickColors == null || clickColors.Length == 0) return;
        Renderer itemRenderer = GetComponentInChildren<Renderer>();
        if (itemRenderer == null) return;
        clickIndex = (clickIndex + 1) % clickColors.Length;
        itemRenderer.material.color = clickColors[clickIndex];
        Debug.Log("[OnMouseDown] Pickup recolored.");
    }
}
