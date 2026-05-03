using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
namespace YueUltimateDronePhysics
{
/// <summary>
/// Tag-driven PID autopilot for SimplifyiedMapPID.
/// Discovers waypoint triggers and finish trigger, then computes stick outputs for visualization.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class PidFollowController : MonoBehaviour
{
    [Header("Drone binding")]
    [SerializeField] private Transform droneRoot;
    [SerializeField] private Rigidbody droneRigidbody;

    [Header("Mission")]
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private float takeoffHeight = 2f;
    [SerializeField] private float minimumTakeoffRise = 1f;
    [SerializeField] private float finishReachDistance = 2.1f;
    [SerializeField] private float maxSegmentSpeed = 7f;
    [SerializeField] private float lineLookAheadDistance = 3f;
    [SerializeField, Min(1)] private int controlPeriodFrames = 20;

    [Header("Mission targets (assign in Inspector)")]
    [SerializeField] private List<Transform> waypointTargets = new List<Transform>();
    [SerializeField] private Transform finishTargetReference;
    [SerializeField] private bool sortWaypointsByNearestFromDrone = true;

    [Header("Bezier path generation")]
    [SerializeField, Range(0f, 1f)] private float bezierTension = 0.25f;
    [SerializeField, Min(2)] private int samplesPerSegment = 20;

    [Header("Path tracking")]
    [SerializeField] private PIDController pidX = new PIDController { Kp = 1.9f, Ki = 0.04f, Kd = 0.8f, maxOutput = 10f };
    [SerializeField] private PIDController pidY = new PIDController { Kp = 3.2f, Ki = 0.08f, Kd = 1.1f, maxOutput = 12f };
    [SerializeField] private PIDController pidZ = new PIDController { Kp = 1.9f, Ki = 0.04f, Kd = 0.8f, maxOutput = 10f };
    [SerializeField] private float altitudeIntegralDeadband = 0.18f;
    [SerializeField, Min(0f)] private float pathTangentSpeed = 5f;

    [Header("Velocity PID (inner loop)")]
    [SerializeField] private PIDController pidVx = new PIDController { Kp = 1.0f, Ki = 0.03f, Kd = 0.22f, maxOutput = 7f };
    [SerializeField] private PIDController pidVy = new PIDController { Kp = 1.4f, Ki = 0.08f, Kd = 0.35f, maxOutput = 10f };
    [SerializeField] private PIDController pidVz = new PIDController { Kp = 1.0f, Ki = 0.03f, Kd = 0.22f, maxOutput = 7f };
    [SerializeField] private PIDController pidYaw = new PIDController { Kp = 2.4f, Ki = 0f, Kd = 0.5f, maxOutput = 6f };
    [SerializeField] private bool invertYawControl = false;
    [SerializeField, Range(0f, 30f)] private float yawDeadbandDegrees = 3f;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;
    [SerializeField] private float debugAxisLength = 3f;
    [SerializeField] private string csvFileName = "pid_follow_control_log.csv";

    public float OutRawLeftVertical01 { get; private set; } = 0.5f;
    public float OutRawLeftHorizontal { get; private set; }
    public float OutRawRightVertical { get; private set; }
    public float OutRawRightHorizontal { get; private set; }
    public bool MissionRunning => phase == FlightPhase.Takeoff || phase == FlightPhase.FollowPath;

    private enum FlightPhase
    {
        Idle,
        Takeoff,
        FollowPath,
        Complete
    }

    private FlightPhase phase = FlightPhase.Idle;
    private readonly List<Transform> orderedWaypoints = new List<Transform>();
    private readonly List<Vector3> missionPath = new List<Vector3>();
    private readonly HashSet<Transform> reachedByTrigger = new HashSet<Transform>();
    private Transform finishTarget;
    private float cruiseAltitude;
    private bool warnedMissingTargets;
    private PidMapDroneTriggerRelay triggerRelay;
    private int currentPathIndex;
    private Vector3 debugClosestPathPoint;
    private bool hasDebugClosestPathPoint;
    private Vector3 debugPathDirection;
    private bool hasDebugPathDirection;
    private int framesAccumulated;
    private float accumulatedDt;
    private string csvFilePath;

    private void Awake()
    {
        ResolveReferences();
        InstallTriggerRelay();
        NeutralizeOutputs();
        InitializeCsvLogging();
    }

    private void Start()
    {
        if (autoStartOnPlay)
            StartMission();
    }

    [ContextMenu("Start PID Map Mission")]
    public void StartMission()
    {
        ResolveReferences();
        InstallTriggerRelay();
        if (!TryBuildMissionTargets())
        {
            SetPhase(FlightPhase.Idle);
            NeutralizeOutputs();
            return;
        }

        currentPathIndex = 0;
        cruiseAltitude = ResolveTakeoffAltitude();
        SetPhase(FlightPhase.Takeoff);
        ResetAllPid();
        framesAccumulated = controlPeriodFrames - 1;
        accumulatedDt = 0f;
    }

    private void Update()
    {
        if (droneRoot == null)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        bool shouldRunControl = phase == FlightPhase.Takeoff || phase == FlightPhase.FollowPath;
        if (!shouldRunControl)
        {
            framesAccumulated = 0;
            accumulatedDt = 0f;
            NeutralizeOutputs();
            return;
        }

        accumulatedDt += dt;
        framesAccumulated++;
        if (framesAccumulated < controlPeriodFrames)
            return;

        float controlDt = accumulatedDt;
        framesAccumulated = 0;
        accumulatedDt = 0f;

        switch (phase)
        {
            case FlightPhase.Takeoff:
                HandleTakeoff(controlDt);
                break;
            case FlightPhase.FollowPath:
                HandleFollowPath(controlDt);
                break;
        }

        AppendControlLog();

    }

    private void HandleTakeoff(float dt)
    {
        Vector3 target = new Vector3(droneRoot.position.x, cruiseAltitude, droneRoot.position.z);
        hasDebugClosestPathPoint = false;
        hasDebugPathDirection = false;
        ApplyAltitudeHoldOnly(target.y, dt);

        if (Mathf.Abs(droneRoot.position.y - cruiseAltitude) <= 0.35f)
        {
            SetPhase(FlightPhase.FollowPath);
            ResetAllPid();
        }
    }

    private void HandleFollowPath(float dt)
    {
        if (missionPath.Count < 2)
        {
            CompleteMission();
            return;
        }

        GetPathGuidance(droneRoot.position, out Vector3 closestOnPath, out Vector3 pathDirection, out Vector3 guidancePoint);
        hasDebugClosestPathPoint = true;
        debugClosestPathPoint = closestOnPath;
        hasDebugPathDirection = pathDirection.sqrMagnitude > 0.0001f;
        if (hasDebugPathDirection)
            debugPathDirection = pathDirection.normalized;
        UpdateYawToPathDirection(pathDirection, dt);
        ApplyPathTracking(closestOnPath, pathDirection, guidancePoint.y, dt);

        Vector3 pathEnd = missionPath[missionPath.Count - 1];
        bool reachedByDistance = Vector3.Distance(droneRoot.position, pathEnd) <= finishReachDistance;
        bool reachedByTriggerHit = finishTarget != null && reachedByTrigger.Contains(finishTarget);
        if (reachedByDistance || reachedByTriggerHit)
            CompleteMission();
    }

    private void CompleteMission()
    {
        SetPhase(FlightPhase.Complete);
        NeutralizeOutputs();
    }

    private void ApplyPathTracking(Vector3 closestPoint, Vector3 pathDirection, float desiredPathY, float dt)
    {
        Vector3 pos = droneRoot.position;
        Vector3 vel = droneRigidbody != null ? droneRigidbody.linearVelocity : Vector3.zero;

        Vector3 tangent = pathDirection;
        tangent.y = 0f;
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = droneRoot.forward;
        tangent.y = 0f;
        tangent.Normalize();

        Vector3 crossTrack = closestPoint - pos;
        crossTrack.y = 0f;

        float corrX = pidX.Update(crossTrack.x, dt);
        float corrZ = pidZ.Update(crossTrack.z, dt);
        Vector3 crossTrackCorrection = new Vector3(corrX, 0f, corrZ);

        float commandedTangentSpeed = Mathf.Clamp(pathTangentSpeed, 0f, maxSegmentSpeed);
        Vector3 desiredH = tangent * commandedTangentSpeed + crossTrackCorrection;
        if (desiredH.sqrMagnitude > maxSegmentSpeed * maxSegmentSpeed)
            desiredH = desiredH.normalized * maxSegmentSpeed;
        float errY = desiredPathY - pos.y;
        float desiredVy = Mathf.Clamp(UpdateAltitudeOuterPid(errY, dt), -maxSegmentSpeed, maxSegmentSpeed);

        float yawDeg = droneRoot.eulerAngles.y;
        Quaternion yawOnly = Quaternion.AngleAxis(yawDeg, Vector3.up);
        Vector3 forward = yawOnly * Vector3.forward;
        Vector3 right = yawOnly * Vector3.right;

        Vector3 velH = new Vector3(vel.x, 0f, vel.z);
        float desiredAlong = Vector3.Dot(desiredH, forward);
        float desiredSide = Vector3.Dot(desiredH, right);
        float velAlong = Vector3.Dot(velH, forward);
        float velSide = Vector3.Dot(velH, right);

        float errAlong = desiredAlong - velAlong;
        float errSide = desiredSide - velSide;
        float errVy = desiredVy - vel.y;

        float forcePitch = pidVz.Update(errAlong, dt);
        float forceRoll = pidVx.Update(errSide, dt);
        float forceY = pidVy.Update(errVy, dt);

        OutRawRightHorizontal = -Mathf.Clamp(forceRoll / Mathf.Max(0.001f, pidVx.maxOutput), -1f, 1f);
        OutRawRightVertical = Mathf.Clamp(forcePitch / Mathf.Max(0.001f, pidVz.maxOutput), -1f, 1f);
        OutRawLeftVertical01 = SignedThrottleTo01(forceY / Mathf.Max(0.001f, pidVy.maxOutput));
    }

    private void ApplyAltitudeHoldOnly(float targetY, float dt)
    {
        Vector3 vel = droneRigidbody != null ? droneRigidbody.linearVelocity : Vector3.zero;
        float errY = targetY - droneRoot.position.y;
        float desiredVy = Mathf.Clamp(UpdateAltitudeOuterPid(errY, dt), -maxSegmentSpeed, maxSegmentSpeed);
        float forceY = pidVy.Update(desiredVy - vel.y, dt);
        OutRawRightHorizontal = 0f;
        OutRawRightVertical = 0f;
        OutRawLeftVertical01 = SignedThrottleTo01(forceY / Mathf.Max(0.001f, pidVy.maxOutput));
    }

    private float UpdateAltitudeOuterPid(float errorY, float dt)
    {
        if (Mathf.Abs(errorY) < altitudeIntegralDeadband)
            pidY.ClearIntegral();

        float outY = pidY.Update(errorY, dt);
        if (errorY > 0f)
            outY = Mathf.Max(0f, outY);
        return outY;
    }

    private void NeutralizeOutputs()
    {
        OutRawLeftVertical01 = 0.5f;
        OutRawLeftHorizontal = 0f;
        OutRawRightVertical = 0f;
        OutRawRightHorizontal = 0f;
        hasDebugClosestPathPoint = false;
        hasDebugPathDirection = false;
    }

    private bool TryBuildMissionTargets()
    {
        reachedByTrigger.Clear();
        orderedWaypoints.Clear();
        missionPath.Clear();
        warnedMissingTargets = false;
        finishTarget = finishTargetReference;

        for (int i = 0; i < waypointTargets.Count; i++)
        {
            Transform waypoint = waypointTargets[i];
            if (waypoint != null)
                orderedWaypoints.Add(waypoint);
        }

        if (orderedWaypoints.Count == 0)
        {
            WarnMissingTargets("No waypoint targets assigned.");
            return false;
        }

        if (finishTarget == null)
        {
            WarnMissingTargets("No finish target assigned.");
            return false;
        }

        if (sortWaypointsByNearestFromDrone && droneRoot != null)
            SortWaypointsNearestNeighbor(orderedWaypoints, droneRoot.position);

        BuildMissionBezierPath();
        if (missionPath.Count < 2)
        {
            WarnMissingTargets("Failed to build mission path.");
            return false;
        }

        return true;
    }

    private void BuildMissionBezierPath()
    {
        List<Vector3> anchors = new List<Vector3>(orderedWaypoints.Count + 1);
        for (int i = 0; i < orderedWaypoints.Count; i++)
            anchors.Add(orderedWaypoints[i].position);
        anchors.Add(finishTarget.position);

        if (anchors.Count < 2)
            return;

        missionPath.Add(anchors[0]);
        int segmentSamples = Mathf.Max(2, samplesPerSegment);
        float tension = Mathf.Clamp01(bezierTension);

        for (int i = 0; i < anchors.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? anchors[i - 1] : anchors[i];
            Vector3 p1 = anchors[i];
            Vector3 p2 = anchors[i + 1];
            Vector3 p3 = i + 2 < anchors.Count ? anchors[i + 2] : anchors[i + 1];

            Vector3 c1 = p1 + (p2 - p0) * tension;
            Vector3 c2 = p2 - (p3 - p1) * tension;

            for (int s = 1; s <= segmentSamples; s++)
            {
                float t = (float)s / segmentSamples;
                missionPath.Add(CubicBezier(p1, c1, c2, p2, t));
            }
        }
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0
             + 3f * u * u * t * p1
             + 3f * u * t * t * p2
             + t * t * t * p3;
    }

    private void GetPathGuidance(Vector3 currentPosition, out Vector3 closestPoint, out Vector3 pathDirection, out Vector3 guidancePoint)
    {
        int searchStart = Mathf.Max(0, currentPathIndex - 40);
        int closestIndex = searchStart;
        float minDist = float.MaxValue;
        for (int i = searchStart; i < missionPath.Count; i++)
        {
            float dist = Vector3.Distance(currentPosition, missionPath[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }

        currentPathIndex = closestIndex;
        closestPoint = missionPath[closestIndex];
        float lookAhead = Mathf.Max(0.1f, lineLookAheadDistance);
        float accumulated = 0f;
        int targetIndex = closestIndex;
        for (int i = closestIndex; i < missionPath.Count - 1; i++)
        {
            accumulated += Vector3.Distance(missionPath[i], missionPath[i + 1]);
            targetIndex = i + 1;
            if (accumulated >= lookAhead)
                break;
        }

        guidancePoint = missionPath[targetIndex];
        pathDirection = guidancePoint - closestPoint;
        pathDirection.y = 0f;
        if (pathDirection.sqrMagnitude < 0.0001f && targetIndex < missionPath.Count - 1)
        {
            pathDirection = missionPath[targetIndex + 1] - guidancePoint;
            pathDirection.y = 0f;
        }
    }

    private void UpdateYawToPathDirection(Vector3 pathDirection, float dt)
    {
        if (dt <= 0f || pathDirection.sqrMagnitude < 0.0001f)
        {
            OutRawLeftHorizontal = 0f;
            return;
        }

        float desiredYaw = Mathf.Atan2(pathDirection.x, pathDirection.z) * Mathf.Rad2Deg;
        float currentYaw = droneRoot.eulerAngles.y;
        float yawError = Mathf.DeltaAngle(currentYaw, desiredYaw);
        if (Mathf.Abs(yawError) <= yawDeadbandDegrees)
        {
            pidYaw.Reset();
            OutRawLeftHorizontal = 0f;
            return;
        }

        float yawCommand = pidYaw.Update(yawError, dt);
        if (invertYawControl)
            yawCommand = -yawCommand;
        OutRawLeftHorizontal = Mathf.Clamp(yawCommand / Mathf.Max(0.001f, pidYaw.maxOutput), -1f, 1f);
    }

    private void WarnMissingTargets(string reason)
    {
        if (warnedMissingTargets)
            return;
        warnedMissingTargets = true;
        Debug.LogWarning($"[PidMapAutoFlightController] {reason} Mission stays idle.");
    }

    private static void SortWaypointsNearestNeighbor(List<Transform> waypoints, Vector3 startPos)
    {
        List<Transform> remaining = new List<Transform>(waypoints.Count);
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null)
                remaining.Add(waypoint);
        }

        waypoints.Clear();
        Vector3 cursor = startPos;
        while (remaining.Count > 0)
        {
            int nearestIndex = 0;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < remaining.Count; i++)
            {
                float sqr = (remaining[i].position - cursor).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearestIndex = i;
                }
            }

            Transform next = remaining[nearestIndex];
            remaining.RemoveAt(nearestIndex);
            waypoints.Add(next);
            cursor = next.position;
        }
    }

    private static float SignedThrottleTo01(float signedUnit)
    {
        return Mathf.Clamp01(0.5f + 0.5f * Mathf.Clamp(signedUnit, -1f, 1f));
    }

    private void ResolveReferences()
    {
        if (droneRoot == null)
            droneRoot = transform;

        if (droneRigidbody == null && droneRoot != null)
            droneRigidbody = droneRoot.GetComponent<Rigidbody>();
    }

    private void InstallTriggerRelay()
    {
        if (droneRoot == null)
            return;

        triggerRelay = droneRoot.GetComponent<PidMapDroneTriggerRelay>();
        if (triggerRelay == null)
            triggerRelay = droneRoot.gameObject.AddComponent<PidMapDroneTriggerRelay>();

        triggerRelay.Initialize(this);
    }

    internal void NotifyTriggerEnter(Collider other)
    {
        if (other == null || other.transform == null)
            return;

        if (IsMissionTarget(other.transform))
            reachedByTrigger.Add(other.transform);
    }

    private bool IsMissionTarget(Transform candidate)
    {
        if (candidate == null)
            return false;

        if (finishTarget == candidate)
            return true;
        return false;
    }

    private void ResetAllPid()
    {
        pidX.Reset(); pidY.Reset(); pidZ.Reset();
        pidVx.Reset(); pidVy.Reset(); pidVz.Reset();
        pidYaw.Reset();
    }

    private float ResolveTakeoffAltitude()
    {
        float minimumAltitude = droneRoot != null
            ? droneRoot.position.y + Mathf.Max(0.1f, minimumTakeoffRise)
            : Mathf.Max(0.1f, minimumTakeoffRise);

        if (missionPath.Count > 0)
            return Mathf.Max(missionPath[0].y + takeoffHeight, minimumAltitude);

        if (finishTarget != null)
            return Mathf.Max(finishTarget.position.y + takeoffHeight, minimumAltitude);

        float fallback = droneRoot != null ? droneRoot.position.y + takeoffHeight : takeoffHeight;
        return Mathf.Max(fallback, minimumAltitude);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebug || droneRoot == null)
            return;

        Vector3 origin = droneRoot.position;
        float len = Mathf.Max(0.2f, debugAxisLength);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + droneRoot.right * len);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + droneRoot.up * len);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + droneRoot.forward * len);

        if (missionPath.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < missionPath.Count - 1; i++)
                Gizmos.DrawLine(missionPath[i], missionPath[i + 1]);
        }

        if (hasDebugClosestPathPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(debugClosestPathPoint, 0.25f);
            Gizmos.DrawLine(origin, debugClosestPathPoint);
        }

        if (hasDebugPathDirection)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(debugClosestPathPoint, debugClosestPathPoint + debugPathDirection * 2f);
        }
    }

    private void SetPhase(FlightPhase nextPhase)
    {
        if (phase == nextPhase)
            return;

        FlightPhase previous = phase;
        phase = nextPhase;
        Debug.Log($"[PidMapAutoFlightController] Phase: {previous} -> {nextPhase}");
        if (nextPhase == FlightPhase.FollowPath)
            Debug.Log($"[PidMapAutoFlightController] Altitude hold enabled at Y={cruiseAltitude:F2} during target tracking.");
    }

    private void InitializeCsvLogging()
    {
        string fileName = string.IsNullOrWhiteSpace(csvFileName) ? "pid_follow_control_log.csv" : csvFileName.Trim();
        csvFilePath = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(csvFilePath))
            return;

        File.WriteAllText(
            csvFilePath,
            "timestamp,left_vertical,left_horizontal,right_vertical,right_horizontal,drone_pos_x,drone_pos_y,drone_pos_z,drone_rot_x,drone_rot_y,drone_rot_z,path_end_dx,path_end_dy,path_end_dz\n");
    }

    private void AppendControlLog()
    {
        if (string.IsNullOrWhiteSpace(csvFilePath))
            InitializeCsvLogging();

        Vector3 position = droneRoot != null ? droneRoot.position : Vector3.zero;
        Vector3 rotation = droneRoot != null ? droneRoot.eulerAngles : Vector3.zero;
        Vector3 pathEndDelta = Vector3.zero;
        if (droneRoot != null && missionPath.Count > 0)
        {
            Vector3 pathEnd = missionPath[missionPath.Count - 1];
            pathEndDelta = pathEnd - droneRoot.position;
        }

        string row = string.Format(
            CultureInfo.InvariantCulture,
            "{0},{1:F6},{2:F6},{3:F6},{4:F6},{5:F6},{6:F6},{7:F6},{8:F6},{9:F6},{10:F6},{11:F6},{12:F6},{13:F6}\n",
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            OutRawLeftVertical01,
            OutRawLeftHorizontal,
            OutRawRightVertical,
            OutRawRightHorizontal,
            position.x,
            position.y,
            position.z,
            rotation.x,
            rotation.y,
            rotation.z,
            pathEndDelta.x,
            pathEndDelta.y,
            pathEndDelta.z);
        File.AppendAllText(csvFilePath, row);
    }
}

/// <summary>
/// Trigger forwarding helper attached at runtime to the drone root.
/// </summary>
public sealed class PidMapDroneTriggerRelay : MonoBehaviour
{
    private PidFollowController owner;

    internal void Initialize(PidFollowController controller)
    {
        owner = controller;
    }

    private void OnTriggerEnter(Collider other)
    {
        owner?.NotifyTriggerEnter(other);
    }
}
}