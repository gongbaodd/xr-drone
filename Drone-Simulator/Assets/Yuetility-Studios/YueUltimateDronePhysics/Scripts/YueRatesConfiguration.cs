// Written by Marcel Remmers for Yuetility-Studios 2022
using UnityEngine;

namespace YueUltimateDronePhysics
{
    public enum YueTransmitterMode
    {
        Mode1, Mode2, Mode3, Mode4
    };

    [System.Serializable]
    public class YueRatesConfiguration
    {
        public YueRatesConfiguration(YueRatesConfiguration ratesConfig)
        {
            proportionalGain = ratesConfig.proportionalGain;
            exponentialGain = ratesConfig.exponentialGain;
            mode = ratesConfig.mode;
            maxAngle = ratesConfig.maxAngle;
        }

        [Header("Gains [Deg/s]")]
        public float proportionalGain = 45f;
        public float exponentialGain = 0f;

        [Header("Flight Mode")]
        public YueTransmitterMode mode = YueTransmitterMode.Mode2;

        [Header("Self Leveling [Deg]")]
        public float maxAngle = 15f;
    }
}