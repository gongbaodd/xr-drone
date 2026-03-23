using UnityEngine;

namespace YueUltimateDronePhysics
{
    public class AgentDroneEmulator : MonoBehaviour
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

        [Header("Optional Agent Input Injection")]
        [SerializeField]
        private bool useAgentInput = false;

        // These values are expected to be written by whatever is producing agent actions.
        // When `useAgentInput` is false, we fall back to reading Unity Input axes.
        public float agentThrottle;
        public float agentYaw;
        public float agentPitch;
        public float agentRoll;

        // Allow external systems (like an ML-Agents Agent) to enable agent-driven input.
        public void SetUseAgentInput(bool enabled)
        {
            useAgentInput = enabled;
        }

        void Start()
        {
            dronePhysics = GetComponent<YueDronePhysics>();
            inputModule = GetComponent<YueInputModule>();

            startPos = transform.position;
            startRot = transform.rotation;
        }

        float GetThrottle()
        {
            if (useAgentInput)
            {
                // Map from [-1, 1] to [0, 1] since the original "Jump" thrust axis is effectively non-negative.
                return agentThrottle;
            }
            return Input.GetAxis("Jump"); // original
        }

        float GetYaw()
        {
            if (useAgentInput) return agentYaw; // return the value from the agent
            return Input.GetAxis("Yaw"); // original
        }

        float GetPitch()
        {
            if (useAgentInput) return agentPitch; // return the value from the agent
            return Input.GetAxis("Vertical");
        }

        float GetRoll()
        {
            if (useAgentInput)
                // Match manual input inversion: original code uses `-Input.GetAxis("Horizontal")`
                return -agentRoll; // return the value from the agent
            return -Input.GetAxis("Horizontal");
        }

        void Update()
        {
            inputModule.rawLeftHorizontal = GetYaw();

            // Example Population of InputModule
            switch (dronePhysics.flightConfig)
            {
                case (YueDronePhysicsFlightConfiguration.AcroMode):
                    inputModule.rawRightHorizontal = GetRoll();
                    inputModule.rawRightVertical = GetPitch();

                    inputModule.rawLeftVertical = GetThrottle();
                    break;

                case (YueDronePhysicsFlightConfiguration.SelfLeveling):
                    inputModule.rawRightHorizontal = GetRoll();
                    inputModule.rawRightVertical = GetPitch();

                    inputModule.rawLeftVertical = GetThrottle();
                    break;

                case (YueDronePhysicsFlightConfiguration.AltitudeHold):
                    inputModule.rawRightHorizontal = GetRoll();
                    inputModule.rawRightVertical = GetPitch();

                    // Altitude hold uses mouse wheel sign/direction in manual play.
                    // When driven by an agent, preserve action sign for up/down.
                    inputModule.rawLeftVertical = useAgentInput
                        ? agentThrottle * 100f
                        : Input.GetAxis("Mouse ScrollWheel") * 100f;
                    break;
            }

            // Respawn on Fire 1
            if (Input.GetButton("Fire1") && !useAgentInput)
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
