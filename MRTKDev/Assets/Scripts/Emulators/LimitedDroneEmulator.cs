using UnityEngine;

namespace YueUltimateDronePhysics
{
    /// <summary>
    /// Keyboard emulator: Space disarm, W throttle, A/D roll, Left/Right arrow yaw, Up/Down arrow pitch.
    /// When <see cref="flightVolume"/> is set, the rigidbody is clamped to that collider's world-space AABB.
    /// </summary>
    [DefaultExecutionOrder(50)]
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
            if (Input.GetKeyDown(KeyCode.Space) && dronePhysics != null)
                dronePhysics.armed = false;

            if (inputModule == null)
                return;

            float roll = -Input.GetAxisRaw("Horizontal");
            float yaw = 0f;
            if (Input.GetKey(KeyCode.LeftArrow))
                yaw -= 1f;
            if (Input.GetKey(KeyCode.RightArrow))
                yaw += 1f;

            float pitch = 0f;
            if (Input.GetKey(KeyCode.UpArrow))
                pitch += 1f;
            if (Input.GetKey(KeyCode.DownArrow))
                pitch -= 1f;

            float throttleStick = Input.GetKey(KeyCode.W) ? 1f : 0f;

            switch (dronePhysics != null ? dronePhysics.flightConfig : YueDronePhysicsFlightConfiguration.SelfLeveling)
            {
                case YueDronePhysicsFlightConfiguration.AcroMode:
                case YueDronePhysicsFlightConfiguration.SelfLeveling:
                    inputModule.rawLeftHorizontal = yaw;
                    inputModule.rawLeftVertical = throttleStick;
                    inputModule.rawRightHorizontal = roll;
                    inputModule.rawRightVertical = pitch;
                    break;

                case YueDronePhysicsFlightConfiguration.AltitudeHold:
                    inputModule.rawLeftHorizontal = yaw;
                    inputModule.rawLeftVertical = Input.GetAxis("Mouse ScrollWheel") * 100f + (Input.GetKey(KeyCode.W) ? 1f : 0f);
                    inputModule.rawRightHorizontal = roll;
                    inputModule.rawRightVertical = pitch;
                    break;
            }
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
