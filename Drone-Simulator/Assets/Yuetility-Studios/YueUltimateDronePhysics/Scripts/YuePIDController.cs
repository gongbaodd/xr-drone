using UnityEngine;

namespace YueUltimateDronePhysics
{
    public class YuePIDController
    {
        private Vector3 pd;

        private Vector3 p;
        private Vector3 d;
        private Vector3 lastE;

        public YuePIDController()
        {

        }

        public Vector3 CalculatePD(Vector3 e, float pk, float dk)
        {
            p = e * pk;
            d = ((e - lastE) / Time.fixedDeltaTime) * dk;
            pd = p + d;

            lastE = e;
            return pd;
        }
    }
}
