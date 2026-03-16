// Written by Marcel Remmers for Yuetility-Studios 2022
using UnityEngine;

namespace YueUltimateDronePhysics
{
    public class YueInputModule : MonoBehaviour
    {
        #region
        public YueRatesConfiguration ratesConfig;

        [Header("Raw Input")]
        [Tooltip("Raw Inputs should be between -1 to 1")]

        [Range(-1f, 1f)]
        public float rawLeftHorizontal = 0f;

        [Range(-1f, 1f)]
        public float rawLeftVertical = 0f;

        [Range(-1f, 1f)]
        public float rawRightHorizontal = 0f;

        [Range(-1f, 1f)]
        public float rawRightVertical = 0f;

        [HideInInspector]
        public float thrust = 0f;
        [HideInInspector]
        public float yaw = 0f;
        [HideInInspector]
        public float pitch = 0f;
        [HideInInspector]
        public float roll = 0f;

        [HideInInspector]
        public float rawThrust = 0f;
        [HideInInspector]
        public float rawYaw = 0f;
        [HideInInspector]
        public float rawPitch = 0f;
        [HideInInspector]
        public float rawRoll = 0f;
        #endregion

        public void Update()
        {
            CalculateInputWithRatesConfig();
        }

        private void CalculateInputWithRatesConfig()
        {
            // Implement according to FlightMode
            switch (ratesConfig.mode)
            {
                case (YueTransmitterMode.Mode1):
                    rawThrust = rawRightVertical;
                    rawYaw = rawLeftHorizontal;
                    rawPitch = rawLeftVertical;
                    rawRoll = rawRightHorizontal;
                    break;

                case (YueTransmitterMode.Mode2):
                    rawThrust = rawLeftVertical;
                    rawYaw = rawLeftHorizontal;
                    rawPitch = rawRightVertical;
                    rawRoll = rawRightHorizontal;
                    break;

                case (YueTransmitterMode.Mode3):
                    rawThrust = rawRightVertical;
                    rawYaw = rawRightHorizontal;
                    rawPitch = rawLeftVertical;
                    rawRoll = rawLeftHorizontal;
                    break;

                case (YueTransmitterMode.Mode4):
                    rawThrust = rawLeftVertical;
                    rawYaw = rawRightHorizontal;
                    rawPitch = rawRightVertical;
                    rawRoll = rawLeftHorizontal;
                    break;
            }

            // Scale with Rates
            thrust = (rawThrust + 1) * 0.5f;
            yaw = TransformInput(rawYaw, ratesConfig.proportionalGain, ratesConfig.exponentialGain);
            pitch = TransformInput(rawPitch, ratesConfig.proportionalGain, ratesConfig.exponentialGain);
            roll = TransformInput(rawRoll, ratesConfig.proportionalGain, ratesConfig.exponentialGain);
        }

        public float TransformInput(float input, float p, float e)
        {
            return (input * p) + (Mathf.Pow(input, 2) * Mathf.Sign(input) * e);
        }
    }
}
