// Written by Marcel Remmers for Yuetility-Studios 2022
using UnityEngine;

namespace YueUltimateDronePhysics
{
    [System.Serializable]
    public class YueDronePhysicsConfiguration
    {
        public YueDronePhysicsConfiguration(YueDronePhysicsConfiguration physicsConfig)
        {
            thrust = physicsConfig.thrust;

            mass = physicsConfig.mass;
            drag = physicsConfig.drag;
            angularDrag = physicsConfig.angularDrag;

            p = physicsConfig.p;
            i = physicsConfig.i;
            d = physicsConfig.d;

            pAltitude = physicsConfig.pAltitude;
            iAltitude = physicsConfig.iAltitude;
            dAltitude = physicsConfig.dAltitude;    
        }

        [Header("Maximum Thrust [N]")]
        public float thrust = 100f;

        [Header("Physics")]
        public float mass = 1f;
        public float drag = 0.1f;
        public float angularDrag = 0.1f;

        [Header("PID Rotation [Nm/Deg]")]
        public float p = 0f;
        public float i = 0f;
        public float d = 0f;

        [Header("PID Altitude [N/m]")]
        public float pAltitude = 0f;
        public float iAltitude = 0f;
        public float dAltitude = 0f;
    }
}