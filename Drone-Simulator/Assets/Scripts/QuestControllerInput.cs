using UnityEngine;

namespace YueUltimateDronePhysics
{
    public class MetaQuestControllerInput : MonoBehaviour
    {
        [Header("This Component injects into the InputModule and uses Inputs from Meta Quest 3 Controllers\n\n\n")]

        [SerializeField]
        private YueDronePhysics dronePhysics;
        [SerializeField]
        private YueInputModule inputModule;

        [Header("Button Mappings")]
        [Tooltip("Button used to respawn the drone")]
        [SerializeField]
        private OVRInput.Button respawnButton = OVRInput.Button.One; // A button

        private Vector3 startPos = Vector3.zero;
        private Quaternion startRot;
        private Rigidbody rb;

        void Start()
        {
            dronePhysics = GetComponent<YueDronePhysics>();
            inputModule = GetComponent<YueInputModule>();
            rb = GetComponent<Rigidbody>();

            startPos = transform.position;
            startRot = transform.rotation;
        }

        void Update()
        {
            // Ensure OVRInput is updated (safe to call even if OVRManager handles it)
            OVRInput.Update();

            // --- Read Quest 3 thumbsticks ---
            // Left thumbstick (PrimaryThumbstick = left controller)
            Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

            // Right thumbstick (SecondaryThumbstick = right controller)
            Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

            // Inject into InputModule (matching the original mapping logic)
            inputModule.rawLeftHorizontal = leftStick.x;   // Yaw / strafe
            inputModule.rawLeftVertical   = leftStick.y;    // Throttle

            inputModule.rawRightHorizontal = -rightStick.x; // Pitch (inverted, same as original)
            inputModule.rawRightVertical   = rightStick.y; // Roll  (inverted, same as original)

            // --- Respawn on button press ---
            if (OVRInput.Get(respawnButton))
            {
                // Reset Position & Rotation
                transform.position = startPos;
                transform.rotation = startRot;

                // Reset Rigidbody
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Reset Target Rotation
                dronePhysics.ResetInternals();
            }
        }

        void FixedUpdate()
        {
            OVRInput.FixedUpdate();
        }
    }
}