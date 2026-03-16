using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YueUltimateDronePhysics
{
    public class YueDroneAnimation : MonoBehaviour
    {
        [Header("Propellers")]
        public Transform FrontLeft;
        public Transform FrontRight;

        public Transform RearLeft;
        public Transform RearRight;

        private float motorFL;
        private float motorFR;
        private float motorRL;
        private float motorRR;

        [SerializeField]
        private YueDronePhysics dronePhysics;

        private void Start()
        {
            if (!dronePhysics)
                dronePhysics = GetComponent<YueDronePhysics>();
        }
        private void Update()
        {
            float thrust = dronePhysics.appliedForce.magnitude * 25f;

            motorFL = thrust + Mathf.Clamp((-dronePhysics.appliedTorque.x - dronePhysics.appliedTorque.z - dronePhysics.appliedTorque.y) * 100f, 0, 100f);
            motorFR = thrust + Mathf.Clamp((-dronePhysics.appliedTorque.x + dronePhysics.appliedTorque.z + dronePhysics.appliedTorque.y) * 100f, 0, 100f);
            motorRL = thrust + Mathf.Clamp((dronePhysics.appliedTorque.x - dronePhysics.appliedTorque.z + dronePhysics.appliedTorque.y) * 100f, 0, 100f);
            motorRR = thrust + Mathf.Clamp((dronePhysics.appliedTorque.x + dronePhysics.appliedTorque.z - dronePhysics.appliedTorque.y) * 100f, 0, 100f);

            if(motorFL != 0)
                FrontLeft.localRotation *=  Quaternion.Euler(0, motorFL, 0);

            if (motorFR != 0)
                FrontRight.localRotation *= Quaternion.Euler(0, motorFR, 0);

            if (motorRL != 0)
                RearLeft.localRotation *= Quaternion.Euler(0, motorRL, 0);

            if (motorRR != 0)
                RearRight.localRotation *= Quaternion.Euler(0, motorRR, 0);
        }
    }
}
