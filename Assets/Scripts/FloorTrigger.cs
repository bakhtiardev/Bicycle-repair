using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this component to PhysicsCollider_Floor.
///
/// • Forces the BoxCollider to <c>isTrigger = true</c> in Awake so it never
///   physically blocks the player / headset height.
/// • Filters trigger events so only relevant objects react:
///     – Allen wrenches (any object with <see cref="WrenchProximityGrab"/>): their
///       Rigidbody is frozen at the floor surface, simulating a solid floor for them.
///     – The back wheel, but ONLY after it has been detached from the bike
///       (<see cref="WheelTwoHandGrab.IsDetached"/> must be true).
///     – Pedals (<see cref="PedalGrab"/>), but ONLY after detached
///       (<see cref="PedalGrab.IsDetached"/> must be true).
/// • When the detached wheel/pedal enters the trigger it is smoothly rotated to the
///   horizontal orientation defined by their respective euler fields.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class FloorTrigger : MonoBehaviour
{
    [Tooltip("Seconds the wheel takes to rotate to horizontal after touching the floor.")]
    public float wheelSnapDuration = 0.35f;

    // Track wheels already being snapped to avoid double-triggering.
    private readonly HashSet<WheelTwoHandGrab> _snapping = new HashSet<WheelTwoHandGrab>();
    private readonly HashSet<PedalGrab>        _snappingPedals = new HashSet<PedalGrab>();

    // World-space Y of the top surface of this flat collider, computed once in Awake.
    private float _floorTopY;

    private void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        // Top surface = object Y + collider local centre Y + half height
        _floorTopY = transform.position.y + col.center.y + col.size.y * 0.5f;
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckWrench(other);
        CheckWheel(other);
        CheckPedal(other);
    }

    // OnTriggerStay catches objects dropped while already inside the volume
    // (OnTriggerEnter won't fire again until they exit and re-enter).
    private void OnTriggerStay(Collider other)
    {
        CheckWrench(other);
        CheckWheel(other);
        CheckPedal(other);
    }

    // When the wheel is re-grabbed and released again it may re-enter.
    // Remove it from the set when it exits so it can snap again if needed.
    private void OnTriggerExit(Collider other)
    {
        var wheel = other.GetComponentInParent<WheelTwoHandGrab>();
        if (wheel != null)
            _snapping.Remove(wheel);

        var pedal = other.GetComponentInParent<PedalGrab>();
        if (pedal != null)
            _snappingPedals.Remove(pedal);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void CheckWrench(Collider other)
    {
        var wrench = other.GetComponentInParent<WrenchProximityGrab>();
        if (wrench == null) return;

        var rb = wrench.GetComponent<Rigidbody>();
        // Only act when the rigidbody is actively falling (non-kinematic with gravity).
        if (rb == null || rb.isKinematic) return;

        // Freeze the wrench and park it just above the floor surface.
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;
        rb.useGravity      = false;

        // Sit the wrench root at floor level + small visual margin.
        Vector3 p = wrench.transform.position;
        p.y = _floorTopY + 0.02f;
        wrench.transform.position = p;

        Debug.Log($"[FloorTrigger] Allen wrench '{wrench.name}' stopped on floor.");
    }

    private void CheckWheel(Collider other)
    {
        var wheel = other.GetComponentInParent<WheelTwoHandGrab>();
        if (wheel == null) return;
        if (!wheel.IsDetached) return;      // Still on the bike — ignore.

        // Freeze physics — same treatment as the wrench so the wheel doesn't fall through.
        var rb = wheel.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
            rb.useGravity      = false;
            // Park the wheel just above the floor surface.
            Vector3 p = wheel.transform.position;
            p.y = _floorTopY + 0.05f;
            wheel.transform.position = p;
            Debug.Log("[FloorTrigger] Detached wheel stopped on floor.");
        }

        // Only kick off the rotation snap once per landing.
        if (_snapping.Contains(wheel)) return;
        _snapping.Add(wheel);
        StartCoroutine(SnapWheelToHorizontal(wheel));
    }

    private IEnumerator SnapWheelToHorizontal(WheelTwoHandGrab wheel)
    {
        Transform  target   = wheel.transform;
        Quaternion from     = target.rotation;
        Quaternion to       = Quaternion.Euler(wheel.horizontalEulers);
        float      elapsed  = 0f;

        while (elapsed < wheelSnapDuration)
        {
            // Stop if the wheel was destroyed or the WheelTwoHandGrab was disabled
            // mid-snap (e.g. FinalizeOnCarpet fired first).
            if (wheel == null || target == null)
                yield break;

            elapsed         += Time.deltaTime;
            target.rotation  = Quaternion.Slerp(from, to, elapsed / wheelSnapDuration);
            yield return null;
        }

        if (target != null)
            target.rotation = to;
    }

    private void CheckPedal(Collider other)
    {
        var pedal = other.GetComponentInParent<PedalGrab>();
        if (pedal == null) return;
        if (!pedal.IsDetached) return;      // Still on the bike — ignore.

        var rb = pedal.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
            rb.useGravity      = false;
            Vector3 p = pedal.transform.position;
            p.y = _floorTopY + 0.02f;
            pedal.transform.position = p;
            Debug.Log("[FloorTrigger] Detached pedal stopped on floor.");
        }

        if (_snappingPedals.Contains(pedal)) return;
        _snappingPedals.Add(pedal);
        StartCoroutine(SnapPedalToFlat(pedal));
    }

    private IEnumerator SnapPedalToFlat(PedalGrab pedal)
    {
        Transform  target  = pedal.transform;
        Quaternion from    = target.rotation;
        Quaternion to      = Quaternion.Euler(pedal.flatEulers);
        float      elapsed = 0f;

        while (elapsed < wheelSnapDuration)
        {
            if (pedal == null || target == null)
                yield break;

            elapsed         += Time.deltaTime;
            target.rotation  = Quaternion.Slerp(from, to, elapsed / wheelSnapDuration);
            yield return null;
        }

        if (target != null)
            target.rotation = to;
    }
}
