using UnityEngine;
using UnityEngine.XR;
using YueUltimateDronePhysics;

/// <summary>
/// XR thumbsticks (American / Mode 4): left Y throttle, left X roll, right Y pitch, right X yaw
/// (see <see cref="YueTransmitterMode.Mode4"/> in <see cref="YueInputModule"/>).
/// When <see cref="flightBoundary"/> is set, rigidbody motion is constrained to that collider volume.
/// </summary>
[DefaultExecutionOrder(-50)]
public class LimitedDroneEmulator : MonoBehaviour
{
    private const float ArmStickYThreshold = -0.9f;
    private const float MinStickValue = -1f;
    private const float MaxStickValue = 1f;

    [Header("References (auto-filled on this object if empty)")]
    [SerializeField] private YueDronePhysics dronePhysics;
    [SerializeField] private YueInputModule inputModule;
    [SerializeField] private Rigidbody rb;

    private const float BoundaryProbePadding = 0.02f;
    private const float InsideEpsilon = 0.0001f;
    private const int BoundaryBinarySearchSteps = 16;
    private const float OutsideRecoverySpeed = 6f;

    [Header("Flight boundary")]
    [Tooltip("Assign a closed boundary Collider (for example Route/Collider MeshCollider).")]
    [SerializeField] private Collider flightBoundary;

    public Vector2 CurrentLeftStick { get; private set; }
    public Vector2 CurrentRightStick { get; private set; }

    private bool hasArmedFromThrottleGate;
    private bool hasLastValidBoundaryPosition;
    private Vector3 lastValidBoundaryPosition;
    private readonly ProbeDebugData[] probeDebugData = new ProbeDebugData[3];

    private void Awake()
    {
        ResolveDroneComponents();
        ResetArmingState();
    }

    private void OnValidate() => ResolveDroneComponents();

    private void ResolveDroneComponents()
    {
        dronePhysics ??= GetComponent<YueDronePhysics>();
        inputModule ??= GetComponent<YueInputModule>();
        rb ??= GetComponent<Rigidbody>();
    }

    private void ResetArmingState()
    {
        hasArmedFromThrottleGate = false;

        if (dronePhysics != null)
            dronePhysics.armed = false;
    }

    private void Update()
    {
        if (inputModule == null)
            return;

        TryReadPrimary2DAxis(XRNode.LeftHand, out Vector2 left);
        TryReadPrimary2DAxis(XRNode.RightHand, out Vector2 right);
        CurrentLeftStick = left;
        CurrentRightStick = right;

        inputModule.rawLeftVertical = StickToThrottle01(left.y);
        inputModule.rawLeftHorizontal = left.x;
        inputModule.rawRightVertical = right.y;
        inputModule.rawRightHorizontal = right.x;

        TryArmFromThrottleGate(left.y);
    }

    private void TryArmFromThrottleGate(float leftStickY)
    {
        if (hasArmedFromThrottleGate || leftStickY > ArmStickYThreshold || dronePhysics == null)
            return;

        dronePhysics.armed = true;
        hasArmedFromThrottleGate = true;
    }

    private static bool TryReadPrimary2DAxis(XRNode hand, out Vector2 axis)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
        axis = default;
        return device.isValid && device.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);
    }

    private static float StickToThrottle01(float stickY)
    {
        float clampedStick = Mathf.Clamp(stickY, MinStickValue, MaxStickValue);
        return (clampedStick + 1f) * 0.5f;
    }

    private void FixedUpdate()
    {
        if (rb == null || flightBoundary == null)
            return;

        Vector3 v = rb.linearVelocity;
        Vector3 p = rb.position;
        TryRecoverOutsideBoundary(ref p, ref v);

        float dt = Time.fixedDeltaTime;
        Vector3 target = p;
        Vector3 desiredDelta = v * dt;

        target = ConstrainAxis(target, desiredDelta.x, Axis.X, ref v);
        target = ConstrainAxis(target, desiredDelta.y, Axis.Y, ref v);
        target = ConstrainAxis(target, desiredDelta.z, Axis.Z, ref v);
        EnforceBoundaryAlongSegment(p, ref target, ref v);

        if (IsInsideBoundary(target))
        {
            hasLastValidBoundaryPosition = true;
            lastValidBoundaryPosition = target;
        }

        rb.position = target;
        rb.linearVelocity = v;
    }

    private void EnforceBoundaryAlongSegment(Vector3 start, ref Vector3 target, ref Vector3 velocity)
    {
        if (IsInsideBoundary(target))
            return;

        Vector3 attemptedTarget = target;

        // Find the furthest point still inside the boundary along this physics step.
        float low = 0f;
        float high = 1f;
        for (int i = 0; i < BoundaryBinarySearchSteps; i++)
        {
            float mid = (low + high) * 0.5f;
            Vector3 sample = Vector3.Lerp(start, target, mid);
            if (IsInsideBoundary(sample))
                low = mid;
            else
                high = mid;
        }

        Vector3 clamped = Vector3.Lerp(start, attemptedTarget, low);
        if (!IsInsideBoundary(clamped))
        {
            if (hasLastValidBoundaryPosition)
                clamped = lastValidBoundaryPosition;
            else
                clamped = flightBoundary.ClosestPoint(start);
        }

        Vector3 blockedDelta = attemptedTarget - clamped;
        if (blockedDelta.sqrMagnitude > InsideEpsilon * InsideEpsilon)
        {
            Vector3 blockedDirection = blockedDelta.normalized;
            float blockedSpeed = Vector3.Dot(velocity, blockedDirection);
            if (blockedSpeed > 0f)
                velocity -= blockedDirection * blockedSpeed;
        }

        target = clamped;
        hasLastValidBoundaryPosition = true;
        lastValidBoundaryPosition = clamped;
    }

    private void OnDrawGizmosSelected()
    {
        if (flightBoundary == null)
            return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Bounds bounds = flightBoundary.bounds;
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 0.2f;
        Gizmos.DrawWireSphere(center, radius);
        Gizmos.DrawLine(center, center + Vector3.right * bounds.extents.x);
        Gizmos.DrawLine(center, center + Vector3.up * bounds.extents.y);
        Gizmos.DrawLine(center, center + Vector3.forward * bounds.extents.z);

        DrawProbeGizmos();
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }

    private struct ProbeDebugData
    {
        public bool valid;
        public bool hit;
        public Vector3 origin;
        public Vector3 direction;
        public float distance;
        public Vector3 hitPoint;
    }

    private Vector3 ConstrainAxis(Vector3 current, float delta, Axis axis, ref Vector3 velocity)
    {
        int axisIndex = (int)axis;
        probeDebugData[axisIndex] = default;

        if (Mathf.Abs(delta) <= Mathf.Epsilon)
            return current;

        Vector3 candidate = current;
        SetAxis(ref candidate, axis, GetAxis(current, axis) + delta);

        // If we're already outside, allow motion that moves us back toward the last valid interior point.
        if (!IsInsideBoundary(current))
        {
            if (!hasLastValidBoundaryPosition)
                return candidate;

            float currentDist = (current - lastValidBoundaryPosition).sqrMagnitude;
            float candidateDist = (candidate - lastValidBoundaryPosition).sqrMagnitude;
            if (candidateDist <= currentDist + InsideEpsilon * InsideEpsilon)
                return candidate;

            float outsideAxisVelocity = GetAxis(velocity, axis);
            float toInteriorAxis = Mathf.Sign(GetAxis(lastValidBoundaryPosition - current, axis));
            if (toInteriorAxis != 0f && Mathf.Sign(outsideAxisVelocity) != toInteriorAxis)
                outsideAxisVelocity = 0f;
            SetAxis(ref velocity, axis, outsideAxisVelocity);
            return current;
        }

        if (IsInsideBoundary(candidate))
            return candidate;

        float directionSign = Mathf.Sign(delta);
        Vector3 direction = AxisToVector(axis) * directionSign;
        float rayDistance = Mathf.Abs(delta) + BoundaryProbePadding;
        float safeAxisValue = GetAxis(current, axis);
        bool hitBoundary = flightBoundary.Raycast(new Ray(current, direction), out RaycastHit hit, rayDistance);

        probeDebugData[axisIndex] = new ProbeDebugData
        {
            valid = true,
            hit = hitBoundary,
            origin = current,
            direction = direction,
            distance = rayDistance,
            hitPoint = hit.point
        };

        if (hitBoundary)
            safeAxisValue = GetAxis(hit.point, axis) - directionSign * BoundaryProbePadding;

        SetAxis(ref current, axis, safeAxisValue);
        if (!IsInsideBoundary(current) && hasLastValidBoundaryPosition)
            SetAxis(ref current, axis, GetAxis(lastValidBoundaryPosition, axis));

        float axisVelocity = GetAxis(velocity, axis);
        if (directionSign > 0f)
            axisVelocity = Mathf.Min(0f, axisVelocity);
        else
            axisVelocity = Mathf.Max(0f, axisVelocity);
        SetAxis(ref velocity, axis, axisVelocity);
        return current;
    }

    private void DrawProbeGizmos()
    {
        for (int i = 0; i < probeDebugData.Length; i++)
        {
            ProbeDebugData probe = probeDebugData[i];
            if (!probe.valid || probe.direction.sqrMagnitude <= Mathf.Epsilon || probe.distance <= 0f)
                continue;

            Gizmos.color = probe.hit ? Color.red : Color.green;
            Vector3 end = probe.origin + probe.direction.normalized * probe.distance;
            Gizmos.DrawLine(probe.origin, end);

            if (probe.hit)
            {
                Gizmos.DrawWireSphere(probe.hitPoint, 0.12f);
                Gizmos.DrawLine(probe.hitPoint, probe.hitPoint + Vector3.up * 0.6f);
            }
        }
    }

    private bool TryRecoverOutsideBoundary(ref Vector3 position, ref Vector3 velocity)
    {
        if (IsInsideBoundary(position))
        {
            hasLastValidBoundaryPosition = true;
            lastValidBoundaryPosition = position;
            return false;
        }

        if (!hasLastValidBoundaryPosition)
        {
            // No known interior point yet: do not guess with bounds center, just stop pushing.
            velocity = Vector3.zero;
            return true;
        }

        Vector3 toInterior = lastValidBoundaryPosition - position;
        float distance = toInterior.magnitude;
        if (distance <= InsideEpsilon)
        {
            position = lastValidBoundaryPosition;
            velocity = Vector3.zero;
            return true;
        }

        Vector3 direction = toInterior / distance;
        // Soft recovery: only cancel outward velocity so pilot can still fly back in.
        float outwardSpeed = Vector3.Dot(velocity, -direction);
        if (outwardSpeed > 0f)
            velocity += direction * outwardSpeed;

        return true;
    }

    private bool IsInsideBoundary(Vector3 point)
    {
        Vector3 closestPoint = flightBoundary.ClosestPoint(point);
        return (closestPoint - point).sqrMagnitude <= InsideEpsilon * InsideEpsilon;
    }

    private static Vector3 AxisToVector(Axis axis)
    {
        switch (axis)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            default: return Vector3.forward;
        }
    }

    private static float GetAxis(Vector3 value, Axis axis)
    {
        switch (axis)
        {
            case Axis.X: return value.x;
            case Axis.Y: return value.y;
            default: return value.z;
        }
    }

    private static void SetAxis(ref Vector3 value, Axis axis, float axisValue)
    {
        switch (axis)
        {
            case Axis.X:
                value.x = axisValue;
                break;
            case Axis.Y:
                value.y = axisValue;
                break;
            default:
                value.z = axisValue;
                break;
        }
    }
}
