using UnityEngine;

public class ThumbstickLocomotion : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float gravity = 0f;

    public Transform headTransform;      // CenterEyeAnchor
    public Transform cameraRigTransform; // OVRCameraRig

    private CharacterController controller;
    private float verticalVelocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("CharacterController missing on Player.", this);
            enabled = false;
            return;
        }

        if (headTransform == null)
        {
            Debug.LogError("Head Transform not assigned.", this);
            enabled = false;
            return;
        }

        if (cameraRigTransform == null)
        {
            Debug.LogError("Camera Rig Transform not assigned.", this);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        SyncBodyToHead();

        //Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        //Vector3 forward = headTransform.forward;
        //forward.y = 0f;
        //forward.Normalize();

        //Vector3 right = headTransform.right;
        //right.y = 0f;
        //right.Normalize();

        //Vector3 horizontalMove = (forward * axis.y + right * axis.x) * moveSpeed;

        //if (controller.isGrounded && verticalVelocity < 0f)
        //    verticalVelocity = -2f;
        //else
        //    verticalVelocity += gravity * Time.deltaTime;

        //Vector3 motion = horizontalMove;
        //motion.y = verticalVelocity;

        //CollisionFlags flags = controller.Move(motion * Time.deltaTime);

        //if ((flags & CollisionFlags.Sides) != 0)
        //    Debug.Log("Hit wall");

        Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = headTransform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 horizontalMove = (forward * axis.y + right * axis.x) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = horizontalMove;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);
    }

    private void SyncBodyToHead()
    {
        //Vector3 headLocal = cameraRigTransform.InverseTransformPoint(headTransform.position);

        //Vector3 horizontalOffset = new Vector3(headLocal.x, 0f, headLocal.z);

        //// Move player root so capsule follows the head horizontally
        //transform.position += transform.TransformVector(horizontalOffset);

        //// Recenter the rig under the player root
        //cameraRigTransform.localPosition -= horizontalOffset;

        //// Keep controller dimensions sensible
        //float headHeight = Mathf.Clamp(headLocal.y, 1.0f, 2.0f);
        //controller.height = headHeight;
        //controller.center = new Vector3(0f, headHeight * 0.5f, 0f);
        Vector3 headLocal = transform.InverseTransformPoint(headTransform.position);

        float headHeight = Mathf.Clamp(headLocal.y, 1.0f, 2.0f);
        controller.height = headHeight;

        controller.center = new Vector3(
            headLocal.x,
            headHeight * 0.5f,
            headLocal.z
        );
    }
}