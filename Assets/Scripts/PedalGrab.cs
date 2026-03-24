using UnityEngine;

/// <summary>
/// Single-hand pedal removal — enabled by <see cref="PedalLooseningInteraction"/> once the
/// pedal bolt is loose.
///
/// INTERACTION FLOW
///   1. Controller must be within <see cref="grabRadius"/> of the pedal centre.
///   2. Player presses the GRIP (side) trigger to grab.
///   3. The pedal follows the holding hand.
///   4. Once pulled more than <see cref="detachDistance"/> from its start the pedal fully
///      detaches from the bike.
///   5. Releasing GRIP before detach snaps the pedal back to its mount so the player can retry.
///   6. After detach the player can release, re-grab, and re-release as many times as needed.
///   7. The step completes when the detached pedal is resting within
///      <see cref="carpetPlaceRadius"/> of <see cref="carpetTarget"/>.
///
/// SCENE SETUP
///   1. Attach this script to the pedal root GameObject.
///   2. Leave the component DISABLED — PedalLooseningInteraction enables it at the right time.
///   3. Assign the Carpet_Interact Transform to <see cref="carpetTarget"/>.
///   4. Assign the <see cref="PedalLevelController"/> to <see cref="levelController"/>.
///   5. Set <see cref="isLeftPedal"/> correctly (true for left, false for right).
///   6. Optionally assign the <see cref="counterCanvas"/> so it is hidden on detach.
/// </summary>
[DisallowMultipleComponent]
public class PedalGrab : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Grab Settings")]
    [Tooltip("How close (metres) a controller must be to the pedal centre to grab it.")]
    public float grabRadius = 0.25f;

    [Tooltip("How far (metres) the pedal must move from its start position before it fully detaches.")]
    public float detachDistance = 0.2f;

    [Header("Placement")]
    [Tooltip("The Carpet_Interact Transform — the detached pedal must be resting here to complete the step.")]
    public Transform carpetTarget;

    [Tooltip("How close (metres) the pedal centre must be to carpetTarget to count as placed.")]
    public float carpetPlaceRadius = 0.8f;

    [Tooltip("World-space Euler angles the pedal snaps to when placed on the carpet (lying flat).")]
    public Vector3 flatEulers = new Vector3(0f, 0f, 0f);

    [Tooltip("How far (metres) left/right each pedal is offset from the carpet target centre so both are visible.")]
    public float carpetSideOffset = 0.15f;

    [Header("UI")]
    [Tooltip("The CounterCanvas from PedalLooseningInteraction — hidden once the pedal detaches.")]
    public GameObject counterCanvas;

    [Header("Sequence")]
    [Tooltip("True for the left pedal, False for the right pedal.")]
    public bool isLeftPedal = true;

    [Tooltip("The PedalLevelController in the scene — notified when this pedal is placed on the carpet.")]
    public PedalLevelController levelController;

    // ── State ─────────────────────────────────────────────────────────────────
    private Transform           _leftController;
    private Transform           _rightController;
    private Rigidbody           _rb;

    private Vector3             _startWorldPos;
    private Transform           _originalParent;
    private Vector3             _startLocalPos;
    private Quaternion          _startLocalRot;

    private Transform           _heldBy      = null;
    private OVRInput.Controller _holdingCtrl = OVRInput.Controller.None;
    private bool                _detached    = false;
    private bool                _finalized   = false;

    /// <summary>True once the pedal has been fully pulled off the bike.</summary>
    public bool IsDetached => _detached;

    // ── Unity ─────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.useGravity  = false;
            _rb.isKinematic = true;
        }

        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            _leftController  = rig.leftControllerAnchor;
            _rightController = rig.rightControllerAnchor;
        }

        _startWorldPos  = transform.position;
        _originalParent = transform.parent;
        _startLocalPos  = transform.localPosition;
        _startLocalRot  = transform.localRotation;

        _heldBy      = null;
        _holdingCtrl = OVRInput.Controller.None;
        _detached    = false;
        _finalized   = false;
    }

    private void Update()
    {
        if (_finalized) return;

        if (_heldBy == null)
        {
            // Try to grab with either controller.
            TryGrab(OVRInput.Controller.LTouch, _leftController);
            TryGrab(OVRInput.Controller.RTouch, _rightController);
        }
        else
        {
            // Follow the holding hand.
            transform.position = _heldBy.position;
            transform.rotation = _heldBy.rotation;

            // Check detach BEFORE release so reaching the threshold while releasing
            // in the same frame still counts as a detach.
            if (!_detached && Vector3.Distance(transform.position, _startWorldPos) >= detachDistance)
                Detach();

            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, _holdingCtrl))
                Drop();
        }

        // Carpet proximity check — only when detached and not being held.
        if (_detached && _heldBy == null && carpetTarget != null &&
            Vector3.Distance(transform.position, carpetTarget.position) <= carpetPlaceRadius)
        {
            FinalizeOnCarpet();
        }
    }

    // ── Internal ──────────────────────────────────────────────────────────────
    private void TryGrab(OVRInput.Controller controller, Transform hand)
    {
        if (hand == null) return;
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller)) return;
        if (Vector3.Distance(hand.position, transform.position) > grabRadius) return;

        _heldBy      = hand;
        _holdingCtrl = controller;

        // Un-parent from bike so the pedal can be moved freely.
        transform.SetParent(null, worldPositionStays: true);

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity  = false;
        }
    }

    private void Drop()
    {
        _heldBy      = null;
        _holdingCtrl = OVRInput.Controller.None;

        if (!_detached)
        {
            // Not yet detached — snap back to the mount position so the player can retry.
            transform.SetParent(_originalParent, worldPositionStays: false);
            transform.localPosition = _startLocalPos;
            transform.localRotation = _startLocalRot;
            _startWorldPos = transform.position; // recalibrate after snap-back

            if (_rb != null)
            {
                _rb.isKinematic     = true;
                _rb.linearVelocity  = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            // Already detached — enable gravity so the pedal falls to the floor.
            // ContinuousDynamic prevents tunnelling through the thin floor trigger.
            if (_rb != null)
            {
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                _rb.isKinematic = false;
                _rb.useGravity  = true;
            }
        }
    }

    private void Detach()
    {
        _detached = true;

        // Ensure the pedal is fully un-parented (may already be from TryGrab).
        transform.SetParent(null, worldPositionStays: true);

        if (counterCanvas != null)
            counterCanvas.SetActive(false);

        Debug.Log($"[PedalGrab] {(isLeftPedal ? "Left" : "Right")} pedal detached — carry it to the carpet.");
    }

    private void FinalizeOnCarpet()
    {
        if (_finalized) return;
        _finalized = true;

        // Freeze the pedal over the carpet.
        if (_rb != null)
        {
            _rb.isKinematic     = true;
            _rb.useGravity      = false;
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        // Snap pedal to carpet centre so it sits cleanly, offset left/right so both are visible.
        float side = isLeftPedal ? -carpetSideOffset : carpetSideOffset;
        transform.position = carpetTarget.position + new Vector3(side, 0.02f, 0f);
        transform.rotation = Quaternion.Euler(flatEulers);

        // Disable all interactive behaviours on this pedal.
        foreach (var b in GetComponentsInChildren<Behaviour>(includeInactive: true))
        {
            if (b != this)
                b.enabled = false;
        }

        Debug.Log($"[PedalGrab] {(isLeftPedal ? "Left" : "Right")} pedal placed on carpet.");

        if (levelController != null)
            levelController.OnPedalPlaced(isLeftPedal);

        enabled = false;
    }

    // ── Editor Gizmos ─────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, grabRadius);

        if (carpetTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(carpetTarget.position, carpetPlaceRadius);
        }
    }
}
