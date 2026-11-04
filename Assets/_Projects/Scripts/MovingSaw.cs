using System.Collections.Generic;
using UnityEngine;

public class MovingSaw : MonoBehaviour
{
    [Header("Oscillation Settings")]
    [Tooltip("Movement axis in local space. Default Vector3.forward (Z axis) moves side-to-side across the track.")]
    [SerializeField] private Vector3 moveAxis = Vector3.forward;
    [Tooltip("Half-distance of oscillation across the platform from center.")]
    [SerializeField] private float distance = 11f;
    [Tooltip("Speed of side-to-side oscillation.")]
    [SerializeField] private float speed = 2.4f;
    [Tooltip("Time offset so saws oscillate at different times.")]
    [SerializeField] private float timeOffset = 0f;

    [Header("Blade Spin")]
    [SerializeField] private bool spinBlade = true;
    [SerializeField] private float spinSpeed = 360f;
    [SerializeField] private Vector3 spinAxis = Vector3.forward;

    [Header("Group Control (Parent of Saws)")]
    [Tooltip("If true and this object has children, automatically manages and moves all child saws with staggered timing.")]
    [SerializeField] private bool autoManageChildren = true;
    [Tooltip("Time delay offset between consecutive child saws so each moves at a different time.")]
    [SerializeField] private float staggerStep = 0.65f;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private bool isParentController = false;

    private class ChildSawData
    {
        public Transform transform;
        public Vector3 baseLocalPos;
        public float offset;
    }
    private readonly List<ChildSawData> childSaws = new List<ChildSawData>();

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;

        if (autoManageChildren && transform.childCount > 0)
        {
            isParentController = true;
            childSaws.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                // Ensure each saw has a trigger collider so it can hit the player
                Collider col = child.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider box = child.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    // Kenney saw blade model bounds
                    box.size = new Vector3(0.85f, 0.85f, 0.35f);
                }

                childSaws.Add(new ChildSawData
                {
                    transform = child,
                    baseLocalPos = child.localPosition,
                    offset = i * staggerStep
                });
            }
        }
        else
        {
            // Individual saw mode
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(0.85f, 0.85f, 0.35f);
            }
        }
    }

    private void Update()
    {
        float t = Time.time;

        if (isParentController)
        {
            // Move each child saw horizontally across the platform with staggered timing
            for (int i = 0; i < childSaws.Count; i++)
            {
                ChildSawData saw = childSaws[i];
                if (saw.transform == null) continue;

                float wave = Mathf.Sin((t + saw.offset) * speed);
                Vector3 newPos = saw.baseLocalPos + moveAxis.normalized * (wave * distance);
                saw.transform.localPosition = newPos;

                if (spinBlade)
                {
                    saw.transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
                }
            }
        }
        else
        {
            // Move this individual saw
            float wave = Mathf.Sin((t + timeOffset) * speed);
            transform.localPosition = initialLocalPos + moveAxis.normalized * (wave * distance);

            if (spinBlade)
            {
                transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (isParentController && childSaws.Count > 0)
        {
            foreach (var saw in childSaws)
            {
                if (saw.transform == null) continue;
                Vector3 center = transform.TransformPoint(saw.baseLocalPos);
                Vector3 dir = transform.TransformDirection(moveAxis.normalized);
                Gizmos.DrawLine(center - dir * distance, center + dir * distance);
            }
        }
        else
        {
            Vector3 origin = Application.isPlaying ? initialLocalPos : transform.localPosition;
            if (transform.parent != null) origin = transform.parent.TransformPoint(origin);
            Vector3 dir = transform.parent != null ? transform.parent.TransformDirection(moveAxis.normalized) : moveAxis.normalized;
            Gizmos.DrawLine(origin - dir * distance, origin + dir * distance);
        }
    }
}

