using UnityEngine;
using UnityEngine.XR;

namespace YueUltimateDronePhysics
{
    /// <summary>
    /// XR thumbsticks (American / Mode 4): left Y throttle, left X roll, right Y pitch, right X yaw
    /// (see <see cref="YueTransmitterMode.Mode4"/> in <see cref="YueInputModule"/>).
    /// When <see cref="flightVolume"/> is set, the rigidbody is clamped to that collider's world-space AABB.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class LimitedDroneEmulator : MonoBehaviour
    {
        [Header("References (auto-filled on this object if empty)")]
        [SerializeField] private YueDronePhysics dronePhysics;
        [SerializeField] private YueInputModule inputModule;
        [SerializeField] private Rigidbody rb;

        [Header("Flight volume")]
        [Tooltip("Assign a BoxCollider (e.g. on a glass cage). Bounds use BoxCollider.bounds each physics step.")]
        [SerializeField] private BoxCollider flightVolume;

        private void Awake()
        {
            ResolveDroneComponents();
        }

        private void OnValidate()
        {
            ResolveDroneComponents();
        }

        private void ResolveDroneComponents()
        {
            dronePhysics ??= GetComponent<YueDronePhysics>();
            inputModule ??= GetComponent<YueInputModule>();
            rb ??= GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (inputModule == null)
                return;

            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            Vector2 leftStick = Vector2.zero;
            Vector2 rightStick = Vector2.zero;
            bool hasLeftStick = leftDevice.isValid && leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftStick);
            bool hasRightStick = rightDevice.isValid && rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick);

            // Mode4: thrust=L vertical, yaw=R horizontal, pitch=R vertical, roll=L horizontal
            inputModule.ratesConfig.mode = YueTransmitterMode.Mode4;
            inputModule.rawLeftVertical = hasLeftStick ? leftStick.y : 0f;
            inputModule.rawLeftHorizontal = hasLeftStick ? leftStick.x : 0f;
            inputModule.rawRightVertical = hasRightStick ? rightStick.y : 0f;
            inputModule.rawRightHorizontal = hasRightStick ? rightStick.x : 0f;
        }

        private void FixedUpdate()
        {
            if (rb == null || flightVolume == null)
                return;

            Bounds worldBounds = flightVolume.bounds;

            Vector3 p = rb.position;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            p.x = Mathf.Clamp(p.x, min.x, max.x);
            p.y = Mathf.Clamp(p.y, min.y, max.y);
            p.z = Mathf.Clamp(p.z, min.z, max.z);

            Vector3 v = rb.linearVelocity;
            if (p.x <= min.x && v.x < 0f)
                v.x = 0f;
            if (p.x >= max.x && v.x > 0f)
                v.x = 0f;
            if (p.y <= min.y && v.y < 0f)
                v.y = 0f;
            if (p.y >= max.y && v.y > 0f)
                v.y = 0f;
            if (p.z <= min.z && v.z < 0f)
                v.z = 0f;
            if (p.z >= max.z && v.z > 0f)
                v.z = 0f;

            rb.position = p;
            rb.linearVelocity = v;
        }

        private void OnDrawGizmosSelected()
        {
            if (flightVolume == null)
                return;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(flightVolume.bounds.center, flightVolume.bounds.size);
        }
    }
}
