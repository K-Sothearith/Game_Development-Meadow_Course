using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour
{
    [Header("Click palette")]
    [SerializeField] private Color[] clickColors = { Color.cyan, Color.magenta, Color.yellow };

    [Header("Bob & spin")]
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float spinSpeed = 90f;

    private Vector3 basePosition;
    private Vector3 baseScale;
    private int clickIndex;
    private float hideTime = -1f;

    void Awake()
    {
        basePosition = transform.position;
        baseScale = transform.localScale;
    }

    void Update()
    {
        float bob = Mathf.Sin(Time.time * 3f) * bobHeight;
        transform.position = basePosition + Vector3.up * bob;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnMouseDown()
    {
        clickIndex = (clickIndex + 1) % clickColors.Length;
        GetComponent<Renderer>().material.color = clickColors[clickIndex];
        Debug.Log($"[Orb] Clicked → {clickColors[clickIndex]}");
    }

}
