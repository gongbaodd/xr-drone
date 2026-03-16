using UnityEngine;


namespace YueUltimateDronePhysics
{
    public class XBOXControllerInput : MonoBehaviour
    {
        [Header("This Component injects into the InputModule and uses Inputs from XBOX Gamepad\n\n\n")]

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
            // Inject Inputs from Joystick into InputModule
            inputModule.rawLeftHorizontal = Input.GetAxis("Horizontal");
            inputModule.rawLeftVertical = Input.GetAxis("Vertical");

            inputModule.rawRightHorizontal = -Input.GetAxis("Mouse Y");
            inputModule.rawRightVertical = -Input.GetAxis("Mouse X"); 

            // Respawn on Fire 1
            if(Input.GetButton("Fire1"))
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
