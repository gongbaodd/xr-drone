using UnityEngine;

/// <summary>
/// Perpetual altitude hold: same cascade as <see cref="PidMapAutoFlightController"/>
/// <c>ApplyAltitudeHoldOnly</c>. After <see cref="StartMission"/>, keeps driving sticks at the
/// resolved cruise height until disabled (never idles out at the setpoint).
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class PIDHoverController : MonoBehaviour
{
    [Header("Drone binding")]
    [SerializeField] private Transform droneRoot;
    [SerializeField] private Rigidbody droneRigidbody;

    [Header("Takeoff")]
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private float takeoffHeight = 5f;
    [SerializeField] private float minimumTakeoffRise = 1f;
    [SerializeField, Min(1)] private int controlPeriodFrames = 20;

    [Header("Altitude PID (PidMapAutoFlightController takeoff)")]
    [SerializeField] private PIDController pidY = new PIDController { Kp = 3.2f, Ki = 0.08f, Kd = 1.1f, maxOutput = 12f };
    [SerializeField] private PIDController pidVy = new PIDController { Kp = 1.4f, Ki = 0.08f, Kd = 0.35f, maxOutput = 10f };
    [SerializeField] private float altitudeIntegralDeadband = 0.18f;
    [SerializeField, Min(0f)] private float maxSegmentSpeed = 7f;

    public float OutRawLeftVertical { get; private set; } = 0.5f;
    public float OutRawLeftHorizontal { get; private set; }
    public float OutRawRightVertical { get; private set; }
    public float OutRawRightHorizontal { get; private set; }

    public bool IsPidDrivingInputs => phase == FlightPhase.HoldAltitude;

    private enum FlightPhase
    {
        Idle,
        HoldAltitude
    }

    private FlightPhase phase = FlightPhase.Idle;
    private float cruiseAltitude;
    private int framesAccumulated;
    private float accumulatedDt;

    private void Awake()
    {
        ResolveReferences();
        NeutralizeOutputs();
    }

    private void Start()
    {
        if (autoStartOnPlay)
            StartMission();
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

        accumulatedDt += dt;
        framesAccumulated++;
        if (framesAccumulated < controlPeriodFrames)
            return;

        framesAccumulated = 0;
        ApplyAltitudeHoldOnly(cruiseAltitude, accumulatedDt);
        accumulatedDt = 0f;
    }

    [ContextMenu("Start Hover Takeoff")]
    public void StartMission()
    {
        ResolveReferences();
        if (droneRoot == null)
            return;

        cruiseAltitude = ResolveTakeoffAltitude();
        phase = FlightPhase.HoldAltitude;
        ResetAltitudePids();
        framesAccumulated = controlPeriodFrames - 1;
        accumulatedDt = 0f;
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
