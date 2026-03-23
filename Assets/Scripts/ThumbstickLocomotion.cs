using UnityEngine;

/// <summary>
/// Smooth locomotion driven by the LEFT controller thumbstick.
/// - Left stick: move forward/back/strafe (relative to head yaw)
/// - Right stick: snap/smooth turn (already handled separately)
///
/// Attach this to the OVRCameraRig GameObject.
/// </summary>
public class ThumbstickLocomotion : MonoBehaviour
{
    [Tooltip("Movement speed in metres per second.")]
    public float moveSpeed = 2.5f;

    private Transform _headTransform;

    private void Start()
    {
        // CenterEyeAnchor is the head camera under OVRCameraRig > TrackingSpace
        var cameraRig = GetComponent<OVRCameraRig>();
        if (cameraRig != null)
            _headTransform = cameraRig.centerEyeAnchor;

        if (_headTransform == null)
            _headTransform = Camera.main != null ? Camera.main.transform : transform;
    }

    private void Update()
    {
        Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        if (axis.sqrMagnitude < 0.01f) return;

        // Project head forward onto the XZ plane so we don't fly up/down
        Vector3 forward = _headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = _headTransform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 move = (forward * axis.y + right * axis.x) * moveSpeed * Time.deltaTime;
        transform.position += move;
    }
}