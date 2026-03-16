using UnityEngine;
using UnityEditor;

namespace YueUltimateDronePhysics
{
    public class PCDroneEmulator : MonoBehaviour
    {
        [Header("This Component injects into the InputModule to emulate Controller Inputs with Arrows\n\n" +
                              " - 'Arrows' for Orientation \n - 'Space' for Thrust.\n\n" +
                              "In Altitude Hold Mode, Altitude is Controlled with Mouse Wheel!\n")]

        [SerializeField]
        private YueDronePhysics dronePhysics;
        [SerializeField]
        private YueInputModule inputModule;

        private Vector3 startPos = Vector3.zero;
        private Quaternion startRot;

        void Start()
        {
            dronePhysics = GetComponent<YueDronePhysics>();
            inputModule = GetComponent<YueInputModule>();

            startPos = transform.position;
            startRot = transform.rotation;
        }

        void Update()
        {
            // Example Population of InputModule
            switch (dronePhysics.flightConfig)
            {
                case (YueDronePhysicsFlightConfiguration.AcroMode):
                    inputModule.rawRightHorizontal = -Input.GetAxis("Horizontal");
                    inputModule.rawRightVertical = Input.GetAxis("Vertical");

                    inputModule.rawLeftVertical = Input.GetAxis("Jump");
                    break;

                case (YueDronePhysicsFlightConfiguration.SelfLeveling):
                    inputModule.rawRightHorizontal = -Input.GetAxis("Horizontal");
                    inputModule.rawRightVertical = Input.GetAxis("Vertical");

                    inputModule.rawLeftVertical = Input.GetAxis("Jump");
                    break;

                case (YueDronePhysicsFlightConfiguration.AltitudeHold):
                    inputModule.rawRightHorizontal = -Input.GetAxis("Horizontal");
                    inputModule.rawRightVertical = Input.GetAxis("Vertical");

                    inputModule.rawLeftVertical = Input.GetAxis("Mouse ScrollWheel") * 100f;
                    break;
            }

            // Respawn on Fire 1
            if (Input.GetButton("Fire1"))
            {
                //Reset Position & Rotation on Respawn
                transform.position = startPos;
                transform.rotation = startRot;

                // Reset Rigidbody on Respawn
                GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

                // Reset Target Rotation on Respawn
                GetComponent<YueDronePhysics>().ResetInternals();
            }
        }
    }
}
