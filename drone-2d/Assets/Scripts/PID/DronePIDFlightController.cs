// DronePIDFlightController.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mission / path PID: reads drone state, outputs transmitter-style sticks: left vertical throttle 0..1
/// (0.5 neutral), other axes -1..1. Physics and position come from Yue drone + <see cref="YueUltimateDronePhysics.PIDDroneEmulator"/>.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DronePIDFlightController : MonoBehaviour
{
    [Header("引用")]
    public TeardropPathGenerator pathGenerator;
    [Tooltip("Optional: used only to read linear velocity for cascade PID.")]
    public Rigidbody droneRigidbody;

    [Header("飞行阶段")]
    public float takeoffHeight = 2f;
    public float cruiseSpeed = 5f;

    [Header("位置PID（外环）")]
    public PIDController pidX = new PIDController { Kp = 2f, Ki = 0.1f, Kd = 1.5f, maxOutput = 10f };
    public PIDController pidY = new PIDController { Kp = 3f, Ki = 0.2f, Kd = 2.0f, maxOutput = 15f };
    public PIDController pidZ = new PIDController { Kp = 2f, Ki = 0.1f, Kd = 1.5f, maxOutput = 10f };

    [Header("速度PID（内环）- 串级控制")]
    public PIDController pidVx = new PIDController { Kp = 1f, Ki = 0.05f, Kd = 0.3f, maxOutput = 8f };
    public PIDController pidVy = new PIDController { Kp = 1.5f, Ki = 0.1f, Kd = 0.5f, maxOutput = 12f };
    public PIDController pidVz = new PIDController { Kp = 1f, Ki = 0.05f, Kd = 0.3f, maxOutput = 8f };

    [Header("偏航PID")]
    public PIDController pidYaw = new PIDController { Kp = 3f, Ki = 0.0f, Kd = 1.0f, maxOutput = 5f };

    /// <summary>Mode2: left vertical = throttle stick (0..1, 0.5 = neutral).</summary>
    public float OutRawLeftVertical { get; private set; }
    /// <summary>Mode2: rawLeftHorizontal = yaw.</summary>
    public float OutRawLeftHorizontal { get; private set; }
    /// <summary>Mode2: rawRightVertical = pitch.</summary>
    public float OutRawRightVertical { get; private set; }
    /// <summary>Mode2: rawRightHorizontal = roll (match keyboard: negate vs. logical roll if needed).</summary>
    public float OutRawRightHorizontal { get; private set; }

    public bool IsPidDrivingInputs =>
        currentPhase == FlightPhase.TakeOff
        || currentPhase == FlightPhase.FollowPath
        || currentPhase == FlightPhase.Landing;

    private enum FlightPhase { Idle, TakeOff, FollowPath, Landing, Complete }
    private FlightPhase currentPhase = FlightPhase.Idle;

    private PathFollower pathFollower;
    private List<Vector3> flightPath;
    private Vector3 targetPosition;
    private Vector3 homePosition;

    void Awake()
    {
        if (droneRigidbody == null)
            droneRigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        if (pathFollower == null)
            pathFollower = gameObject.AddComponent<PathFollower>();
        homePosition = transform.position;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (!IsPidDrivingInputs)
        {
            ClearStickOutputs();
            return;
        }

        switch (currentPhase)
        {
            case FlightPhase.TakeOff:
                HandleTakeOff(dt);
                break;
            case FlightPhase.FollowPath:
                HandleFollowPath(dt);
                break;
            case FlightPhase.Landing:
                HandleLanding(dt);
                break;
        }
    }

    public void StartMission()
    {
        Debug.Log("📍 任务开始：起飞");
        homePosition = transform.position;
        targetPosition = new Vector3(homePosition.x, homePosition.y + takeoffHeight, homePosition.z);
        currentPhase = FlightPhase.TakeOff;
        ResetAllPIDs();
    }

    private void HandleTakeOff(float dt)
    {
        ApplyPositionPidSticks(targetPosition, dt);

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            Debug.Log("✅ 起飞完成，开始跟踪路径");
            flightPath = pathGenerator.GeneratePath();
            AdjustPathAltitude(flightPath, transform.position.y);
            pathFollower.SetPath(flightPath);
            currentPhase = FlightPhase.FollowPath;
            ResetAllPIDs();
        }
    }

    private void HandleFollowPath(float dt)
    {
        OutRawLeftHorizontal = 0f;

        if (pathFollower.IsComplete)
        {
            Debug.Log("✅ 路径跟踪完成，开始降落");
            targetPosition = homePosition;
            currentPhase = FlightPhase.Landing;
            ResetAllPIDs();
            return;
        }

        Vector3 target = pathFollower.GetTargetPoint(transform.position);
        ApplyCascadePidSticks(target, dt);

        Vector3 velocity = droneRigidbody != null ? droneRigidbody.linearVelocity : Vector3.zero;
        if (velocity.magnitude > 0.5f)
        {
            Vector3 flatVelocity = new Vector3(velocity.x, 0, velocity.z);
            if (flatVelocity.magnitude > 0.1f)
            {
                float targetYaw = Mathf.Atan2(flatVelocity.x, flatVelocity.z) * Mathf.Rad2Deg;
                ApplyYawStick(targetYaw, dt);
            }
        }
    }

    private void HandleLanding(float dt)
    {
        ApplyPositionPidSticks(targetPosition, dt);

        if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
        {
            Debug.Log("🏠 降落完成！任务结束");
            currentPhase = FlightPhase.Complete;
            ClearStickOutputs();
        }
    }

    private void ApplyPositionPidSticks(Vector3 target, float dt)
    {
        OutRawLeftHorizontal = 0f;

        Vector3 pos = transform.position;
        float errorX = target.x - pos.x;
        float errorY = target.y - pos.y;
        float errorZ = target.z - pos.z;

        float fx = pidX.Update(errorX, dt);
        float fy = pidY.Update(errorY, dt);
        float fz = pidZ.Update(errorZ, dt);

        // Match Yue manual input: rawRoll = -Horizontal (see PIDDroneEmulator / AgentDroneEmulator).
        OutRawRightHorizontal = -Mathf.Clamp(fx / pidX.maxOutput, -1f, 1f);
        OutRawRightVertical = Mathf.Clamp(fz / pidZ.maxOutput, -1f, 1f);
        OutRawLeftVertical = SignedThrottleToLeftVertical01(fy / pidY.maxOutput);
    }

    private void ApplyCascadePidSticks(Vector3 target, float dt)
    {
        Vector3 pos = transform.position;
        Vector3 vel = droneRigidbody != null ? droneRigidbody.linearVelocity : Vector3.zero;

        float errorX = target.x - pos.x;
        float errorY = target.y - pos.y;
        float errorZ = target.z - pos.z;

        float desiredVx = pidX.Update(errorX, dt);
        float desiredVy = pidY.Update(errorY, dt);
        float desiredVz = pidZ.Update(errorZ, dt);

        Vector3 desiredVel = new Vector3(desiredVx, desiredVy, desiredVz);
        if (desiredVel.magnitude > cruiseSpeed)
            desiredVel = desiredVel.normalized * cruiseSpeed;

        float velErrorX = desiredVel.x - vel.x;
        float velErrorY = desiredVel.y - vel.y;
        float velErrorZ = desiredVel.z - vel.z;

        float forceX = pidVx.Update(velErrorX, dt);
        float forceY = pidVy.Update(velErrorY, dt);
        float forceZ = pidVz.Update(velErrorZ, dt);

        OutRawRightHorizontal = -Mathf.Clamp(forceX / pidVx.maxOutput, -1f, 1f);
        OutRawRightVertical = Mathf.Clamp(forceZ / pidVz.maxOutput, -1f, 1f);
        OutRawLeftVertical = SignedThrottleToLeftVertical01(forceY / pidVy.maxOutput);
    }

    private void ApplyYawStick(float targetYawDeg, float dt)
    {
        float currentYaw = transform.eulerAngles.y;
        float yawError = Mathf.DeltaAngle(currentYaw, targetYawDeg);
        float yawOut = pidYaw.Update(yawError, dt);
        OutRawLeftHorizontal = Mathf.Clamp(yawOut / pidYaw.maxOutput, -1f, 1f);
    }

    private void ClearStickOutputs()
    {
        OutRawLeftVertical = 0.5f;
        OutRawLeftHorizontal = 0f;
        OutRawRightVertical = 0f;
        OutRawRightHorizontal = 0f;
    }

    private void AdjustPathAltitude(List<Vector3> path, float altitude)
    {
        for (int i = 0; i < path.Count; i++)
            path[i] = new Vector3(path[i].x, altitude, path[i].z);
    }

    /// <summary>Maps signed PID output (-1..1) to throttle stick 0..1 (0.5 = hover).</summary>
    static float SignedThrottleToLeftVertical01(float signedUnit)
    {
        return Mathf.Clamp01(0.5f + 0.5f * Mathf.Clamp(signedUnit, -1f, 1f));
    }

    private void ResetAllPIDs()
    {
        pidX.Reset(); pidY.Reset(); pidZ.Reset();
        pidVx.Reset(); pidVy.Reset(); pidVz.Reset();
        pidYaw.Reset();
    }

    private void OnDrawGizmos()
    {
        if (pathFollower != null && pathFollower.IsFollowing)
        {
            Vector3 target = pathFollower.GetTargetPoint(transform.position);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(target, 0.3f);
            Gizmos.DrawLine(transform.position, target);
        }
    }
}
