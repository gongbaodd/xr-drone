using UnityEngine;

namespace YueUltimateDronePhysics
{
    /// <summary>
    /// Bridges manual input or <see cref="PIDHoverController"/> stick outputs into <see cref="YueInputModule"/>.
    /// Physics and pose updates are handled by <see cref="YueDronePhysics"/>; this script only writes raw stick axes and optional respawn.
    /// </summary>
    public class PIDHoverEmulator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private YueDronePhysics dronePhysics;
        [SerializeField]
        private YueInputModule inputModule;
        [SerializeField]
        private PIDHoverController pidFlightController;

        [Header("Input source")]
        [Tooltip("When off, keyboard axes drive the drone. When on and the mission is active, sticks come from PIDHoverController.")]
        [SerializeField]
        private bool preferPidMissionInputs = true;

        private Vector3 startPos;
        private Quaternion startRot;

        void Start()
        {
            dronePhysics = GetComponent<YueDronePhysics>();
            inputModule = GetComponent<YueInputModule>();
            if (pidFlightController == null)
                pidFlightController = GetComponent<PIDHoverController>();

            startPos = transform.position;
            startRot = transform.rotation;
        }

        void Update()
        {
            if (preferPidMissionInputs && pidFlightController != null)
            {
                inputModule.rawLeftHorizontal = pidFlightController.OutRawLeftHorizontal;
                // Controller exposes throttle as 0..1; YueInputModule expects -1..1 for rawThrust.
                inputModule.rawLeftVertical = pidFlightController.OutRawLeftVertical * 2f - 1f;
                inputModule.rawRightHorizontal = pidFlightController.OutRawRightHorizontal;
                inputModule.rawRightVertical = pidFlightController.OutRawRightVertical;
            }
            else
                ApplyKeyboardSticks();

            if (Input.GetButton("Fire1"))
                Respawn();
        }

        private void ApplyKeyboardSticks()
        {
            inputModule.rawLeftHorizontal = Input.GetAxis("Yaw");

            switch (dronePhysics.flightConfig)
            {
                case YueDronePhysicsFlightConfiguration.AcroMode:
                    inputModule.rawRightHorizontal = -Input.GetAxis("Horizontal");
                    inputModule.rawRightVertical = Input.GetAxis("Vertical");
                    inputModule.rawLeftVertical = Input.GetAxis("Throttle");
                    break;

                case YueDronePhysicsFlightConfiguration.SelfLeveling:
                    inputModule.rawRightHorizontal = -Input.GetAxis("Horizontal");
                    inputModule.rawRightVertical = Input.GetAxis("Vertical");
                    inputModule.rawLeftVertical = Input.GetAxis("Throttle");
                    break;

                case YueDronePhysicsFlightConfiguration.AltitudeHold:
                    inputModule.rawRightHorizontal = -Input.GetAxis("Horizontal");
                    inputModule.rawRightVertical = Input.GetAxis("Vertical");
                    inputModule.rawLeftVertical = Input.GetAxis("Mouse ScrollWheel") * 100f;
                    break;
            }
        }

        private void Respawn()
        {
            transform.position = startPos;
            transform.rotation = startRot;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            dronePhysics.ResetInternals();
        }
    }
}
