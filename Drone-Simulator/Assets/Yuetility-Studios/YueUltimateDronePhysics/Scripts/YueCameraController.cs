using UnityEngine;

namespace YueUltimateDronePhysics
{
    public class YueCameraController : MonoBehaviour
    {
        [SerializeField]
        private Transform target;
        [SerializeField]
        private float maxDistance = 2.5f;
        [SerializeField]
        private float followSpeed = 1f;
        [SerializeField]
        private float yOffset = 1f;

        void Update()
        {
            Vector3 delta = target.position - transform.position;

            if(delta.magnitude > maxDistance)
            {
                transform.position += delta.normalized * followSpeed * Time.deltaTime * (delta.magnitude - maxDistance);
            }
            transform.LookAt(target.position);          
        }
    }
}