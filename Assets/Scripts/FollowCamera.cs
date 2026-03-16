using UnityEngine;

/// <summary>
/// Creates a Google Glass-style HUD that stays in a fixed position relative to the camera.
/// Reparents the object to the camera transform so it moves with the user's head.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Header("HUD Positioning")]
    [Tooltip("Horizontal offset from center (positive = right, negative = left)")]
    public float horizontalOffset = 0.6f;

    [Tooltip("Vertical offset from center (positive = up, negative = down)")]
    public float verticalOffset = 0.15f;

    [Tooltip("Distance from camera (further = smaller in view)")]
    public float distance = 1.2f;

    [Header("Scale")]
    [Tooltip("Scale multiplier for the UI (1 = original size, 0.5 = half size)")]
    public float scaleMultiplier = 0.35f;

    [Header("Transition")]
    [Tooltip("Smooth transition when reparenting (0 = instant)")]
    public float reparentSmoothTime = 0.3f;

    private Transform _cameraTransform;
    private bool _isReparented = false;
    private Vector3 _targetLocalPosition;
    private Vector3 _velocity = Vector3.zero;
    private Vector3 _originalScale;

    void Start()
    {
        // Store original scale
        _originalScale = transform.localScale;

        // Find the camera
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            _cameraTransform = rig.centerEyeAnchor;
            Debug.Log("FollowCamera: Found OVRCameraRig centerEyeAnchor");
        }
        else if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
            Debug.Log("FollowCamera: Using Camera.main");
        }

        if (_cameraTransform == null)
        {
            Debug.LogError("FollowCamera: Could not find camera. Make sure OVRCameraRig or Camera.main exists.");
            enabled = false;
            return;
        }

        // Calculate target local position (top-right corner)
        _targetLocalPosition = new Vector3(horizontalOffset, verticalOffset, distance);

        // Reparent to camera
        if (transform.parent != _cameraTransform)
        {
            // Convert current world position to local space of camera
            Vector3 startLocalPos = _cameraTransform.InverseTransformPoint(transform.position);
            
            // Set parent
            transform.SetParent(_cameraTransform, true);
            
            // Apply scale
            transform.localScale = _originalScale * scaleMultiplier;
            
            // Smooth transition to target position
            if (reparentSmoothTime > 0)
            {
                StartCoroutine(SmoothTransitionToPosition(startLocalPos, _targetLocalPosition));
            }
            else
            {
                transform.localPosition = _targetLocalPosition;
                _isReparented = true;
            }
        }
        else
        {
            transform.localScale = _originalScale * scaleMultiplier;
            transform.localPosition = _targetLocalPosition;
            _isReparented = true;
        }
    }

    System.Collections.IEnumerator SmoothTransitionToPosition(Vector3 start, Vector3 target)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < reparentSmoothTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / reparentSmoothTime;
            // Smoothstep for smooth acceleration/deceleration
            t = t * t * (3f - 2f * t);
            
            transform.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }
        
        transform.localPosition = target;
        _isReparented = true;
    }

    void LateUpdate()
    {
        if (!_isReparented) return;

        // Update position if parameters change during runtime
        Vector3 newTargetPos = new Vector3(horizontalOffset, verticalOffset, distance);
        if (transform.localPosition != newTargetPos)
        {
            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition, 
                newTargetPos, 
                ref _velocity, 
                0.1f
            );
        }

        // Ensure scale is maintained
        Vector3 targetScale = _originalScale * scaleMultiplier;
        if (transform.localScale != targetScale)
        {
            transform.localScale = targetScale;
        }

        // Reset rotation to identity (face same direction as camera)
        transform.localRotation = Quaternion.identity;
    }

    void OnDestroy()
    {
        // Restore original scale and unparent when destroyed
        if (transform != null)
        {
            transform.localScale = _originalScale;
            if (transform.parent != null && _cameraTransform != null && transform.parent == _cameraTransform)
            {
                transform.SetParent(null, true);
            }
        }
    }
}
