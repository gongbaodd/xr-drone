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

        private void Awake() => ResolveDroneComponents();

        private void OnValidate() => ResolveDroneComponents();

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

            TryReadPrimary2DAxis(XRNode.LeftHand, out Vector2 left);
            TryReadPrimary2DAxis(XRNode.RightHand, out Vector2 right);

            inputModule.ratesConfig.mode = YueTransmitterMode.Mode4;
            float throttle01 = (left.y + 1f) * 0.5f;
            inputModule.rawLeftVertical = throttle01;
            inputModule.rawLeftHorizontal = left.x;
            inputModule.rawRightVertical = right.y;
            inputModule.rawRightHorizontal = right.x;
        }

        private static bool TryReadPrimary2DAxis(XRNode hand, out Vector2 axis)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            axis = default;
            return device.isValid && device.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);
        }

        private void FixedUpdate()
        {
            if (rb == null || flightVolume == null)
                return;

            Bounds b = flightVolume.bounds;
            Vector3 p = rb.position;
            Vector3 min = b.min;
            Vector3 max = b.max;

            p.x = Mathf.Clamp(p.x, min.x, max.x);
            p.y = Mathf.Clamp(p.y, min.y, max.y);
            p.z = Mathf.Clamp(p.z, min.z, max.z);

            Vector3 v = rb.linearVelocity;
            if (p.x <= min.x) v.x = Mathf.Max(0f, v.x);
            if (p.x >= max.x) v.x = Mathf.Min(0f, v.x);
            if (p.y <= min.y) v.y = Mathf.Max(0f, v.y);
            if (p.y >= max.y) v.y = Mathf.Min(0f, v.y);
            if (p.z <= min.z) v.z = Mathf.Max(0f, v.z);
            if (p.z >= max.z) v.z = Mathf.Min(0f, v.z);

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
