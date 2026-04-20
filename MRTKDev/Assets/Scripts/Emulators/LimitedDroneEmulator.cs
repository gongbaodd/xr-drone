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

    [Header("Flight boundary")]
    [Tooltip("Assign a closed boundary Collider (for example Route/Collider MeshCollider).")]
    [SerializeField] private Collider flightBoundary;

    public Vector2 CurrentLeftStick { get; private set; }
    public Vector2 CurrentRightStick { get; private set; }

    private bool hasArmedFromThrottleGate;
    private bool hasLastValidBoundaryPosition;
    private Vector3 lastValidBoundaryPosition;

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
        if (TryRecoverToLastValidPosition(ref p, ref v))
            rb.position = p;

        float dt = Time.fixedDeltaTime;
        Vector3 target = p;
        Vector3 desiredDelta = v * dt;

        target = ConstrainAxis(target, desiredDelta.x, Axis.X, ref v);
        target = ConstrainAxis(target, desiredDelta.y, Axis.Y, ref v);
        target = ConstrainAxis(target, desiredDelta.z, Axis.Z, ref v);

        if (IsInsideBoundary(target))
        {
            hasLastValidBoundaryPosition = true;
            lastValidBoundaryPosition = target;
        }

        rb.position = target;
        rb.linearVelocity = v;
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
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }

    private Vector3 ConstrainAxis(Vector3 current, float delta, Axis axis, ref Vector3 velocity)
    {
        if (Mathf.Abs(delta) <= Mathf.Epsilon)
            return current;

        Vector3 candidate = current;
        SetAxis(ref candidate, axis, GetAxis(current, axis) + delta);
        if (IsInsideBoundary(candidate))
            return candidate;

        float directionSign = Mathf.Sign(delta);
        Vector3 direction = AxisToVector(axis) * directionSign;
        float rayDistance = Mathf.Abs(delta) + BoundaryProbePadding;
        float safeAxisValue = GetAxis(current, axis);
        if (flightBoundary.Raycast(new Ray(current, direction), out RaycastHit hit, rayDistance))
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

    private bool TryRecoverToLastValidPosition(ref Vector3 position, ref Vector3 velocity)
    {
        if (IsInsideBoundary(position))
        {
            hasLastValidBoundaryPosition = true;
            lastValidBoundaryPosition = position;
            return false;
        }

        if (!hasLastValidBoundaryPosition)
            return false;

        Vector3 outsideDelta = position - lastValidBoundaryPosition;
        position = lastValidBoundaryPosition;

        if (Mathf.Abs(outsideDelta.x) > InsideEpsilon && Mathf.Sign(velocity.x) == Mathf.Sign(outsideDelta.x))
            velocity.x = 0f;
        if (Mathf.Abs(outsideDelta.y) > InsideEpsilon && Mathf.Sign(velocity.y) == Mathf.Sign(outsideDelta.y))
            velocity.y = 0f;
        if (Mathf.Abs(outsideDelta.z) > InsideEpsilon && Mathf.Sign(velocity.z) == Mathf.Sign(outsideDelta.z))
            velocity.z = 0f;

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
