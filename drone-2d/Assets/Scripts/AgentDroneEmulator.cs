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
            return Input.GetAxis("Throttle"); // original
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

        float GetLeftVerticalRaw()
        {
            if (dronePhysics == null)
                dronePhysics = GetComponent<YueDronePhysics>();
            if (dronePhysics == null)
                return 0f;

            switch (dronePhysics.flightConfig)
            {
                case YueDronePhysicsFlightConfiguration.AcroMode:
                case YueDronePhysicsFlightConfiguration.SelfLeveling:
                    return GetThrottle();
                case YueDronePhysicsFlightConfiguration.AltitudeHold:
                    return useAgentInput ? agentThrottle * 100f : Input.GetAxis("Mouse ScrollWheel") * 100f;
                default:
                    return GetThrottle();
            }
        }

        /// <summary>Left vertical for HUD: 0 = bottom, 1 = top. Altitude-hold maps trimmed axis to 0–1.</summary>
        static float NormalizeLeftVerticalZeroToOne(float raw, YueDronePhysicsFlightConfiguration mode)
        {
            if (mode == YueDronePhysicsFlightConfiguration.AltitudeHold)
            {
                float n = Mathf.Clamp(raw / 100f, -1f, 1f);
                return (n + 1f) * 0.5f;
            }

            if (raw >= 0f && raw <= 1f)
                return Mathf.Clamp01(raw);
            return Mathf.Clamp01((raw + 1f) * 0.5f);
        }

        /// <summary>
        /// Mode2-style sticks for UI: left = yaw / throttle or altitude trim, right = roll / pitch.
        /// Left Y is in [0, 1] (throttle-style: 0 bottom, 1 top). All other axes are [-1, 1].
        /// </summary>
        public void GetStickVisualization(out Vector2 leftStick, out Vector2 rightStick)
        {
            if (dronePhysics == null)
                dronePhysics = GetComponent<YueDronePhysics>();
            if (dronePhysics == null)
            {
                leftStick = rightStick = Vector2.zero;
                return;
            }

            float lx = Mathf.Clamp(GetYaw(), -1f, 1f);
            float ly = NormalizeLeftVerticalZeroToOne(GetLeftVerticalRaw(), dronePhysics.flightConfig);
            float rx = Mathf.Clamp(GetRoll(), -1f, 1f);
            float ry = Mathf.Clamp(GetPitch(), -1f, 1f);
            leftStick = new Vector2(lx, ly);
            rightStick = new Vector2(rx, ry);
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
