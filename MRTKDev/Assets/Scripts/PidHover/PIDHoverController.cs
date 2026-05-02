using UnityEngine;

/// <summary>
/// Altitude-only takeoff PID: mirrors <see cref="PidMapAutoFlightController"/> takeoff phase
/// (<c>ApplyAltitudeHoldOnly</c> / altitude cascade) and exposes the same stick outputs as
/// <see cref="DronePIDFlightController"/> for <see cref="YueUltimateDronePhysics.PIDHoverEmulator"/>.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class PIDHoverController : MonoBehaviour
{
    [Header("Drone binding")]
    [SerializeField] private Transform droneRoot;
    [SerializeField] private Rigidbody droneRigidbody;

    [Header("Takeoff")]
    [SerializeField] private float takeoffHeight = 2f;
    [SerializeField] private float minimumTakeoffRise = 1f;
    [SerializeField] private float takeoffReachBand = 0.35f;

    [Header("Altitude PID (PidMapAutoFlightController takeoff)")]
    [SerializeField] private PIDController pidY = new PIDController { Kp = 3.2f, Ki = 0.08f, Kd = 1.1f, maxOutput = 12f };
    [SerializeField] private PIDController pidVy = new PIDController { Kp = 1.4f, Ki = 0.08f, Kd = 0.35f, maxOutput = 10f };
    [SerializeField] private float altitudeIntegralDeadband = 0.18f;
    [SerializeField, Min(0f)] private float maxSegmentSpeed = 7f;

    public float OutRawLeftVertical { get; private set; } = 0.5f;
    public float OutRawLeftHorizontal { get; private set; }
    public float OutRawRightVertical { get; private set; }
    public float OutRawRightHorizontal { get; private set; }

    public bool IsPidDrivingInputs => phase == FlightPhase.Takeoff;

    private enum FlightPhase
    {
        Idle,
        Takeoff,
        Complete
    }

    private FlightPhase phase = FlightPhase.Idle;
    private float cruiseAltitude;

    private void Awake()
    {
        ResolveReferences();
        NeutralizeOutputs();
    }

    private void Update()
    {
        if (droneRoot == null)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        if (!IsPidDrivingInputs)
        {
            NeutralizeOutputs();
            return;
        }

        HandleTakeoff(dt);
    }

    [ContextMenu("Start Hover Takeoff")]
    public void StartMission()
    {
        ResolveReferences();
        if (droneRoot == null)
            return;

        cruiseAltitude = ResolveTakeoffAltitude();
        phase = FlightPhase.Takeoff;
        ResetAltitudePids();
    }

    private void HandleTakeoff(float dt)
    {
        ApplyAltitudeHoldOnly(cruiseAltitude, dt);

        if (Mathf.Abs(droneRoot.position.y - cruiseAltitude) <= takeoffReachBand)
        {
            phase = FlightPhase.Complete;
            ResetAltitudePids();
            NeutralizeOutputs();
        }
    }

    private void ApplyAltitudeHoldOnly(float targetY, float dt)
    {
        Vector3 vel = droneRigidbody != null ? droneRigidbody.linearVelocity : Vector3.zero;
        float errY = targetY - droneRoot.position.y;
        float desiredVy = Mathf.Clamp(UpdateAltitudeOuterPid(errY, dt), -maxSegmentSpeed, maxSegmentSpeed);
        float forceY = pidVy.Update(desiredVy - vel.y, dt);
        OutRawLeftHorizontal = 0f;
        OutRawRightHorizontal = 0f;
        OutRawRightVertical = 0f;
        OutRawLeftVertical = SignedThrottleTo01(forceY / Mathf.Max(0.001f, pidVy.maxOutput));
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
        OutRawLeftVertical = 0.5f;
        OutRawLeftHorizontal = 0f;
        OutRawRightVertical = 0f;
        OutRawRightHorizontal = 0f;
    }

    private static float SignedThrottleTo01(float signedUnit)
    {
        return Mathf.Clamp01(0.5f + 0.5f * Mathf.Clamp(signedUnit, -1f, 1f));
    }

    private float ResolveTakeoffAltitude()
    {
        float minimumAltitude = droneRoot.position.y + Mathf.Max(0.1f, minimumTakeoffRise);
        float fallback = droneRoot.position.y + takeoffHeight;
        return Mathf.Max(fallback, minimumAltitude);
    }

    private void ResetAltitudePids()
    {
        pidY.Reset();
        pidVy.Reset();
    }

    private void ResolveReferences()
    {
        if (droneRoot == null)
            droneRoot = transform;

        if (droneRigidbody == null && droneRoot != null)
            droneRigidbody = droneRoot.GetComponent<Rigidbody>();
    }
}
