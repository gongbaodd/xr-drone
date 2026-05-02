using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;
using YueUltimateDronePhysics;

[RequireComponent(typeof(HoverScorer))]
public class DroneHoverAgent : Agent
{
    [Header("References")]
    public Rigidbody droneRb;
    public AgentDroneEmulator droneInput;

    [Header("UI")]
    [Tooltip("If enabled and Reward Text is unassigned, a screen-space Canvas is created at runtime.")]
    [SerializeField] bool showRewardOnCanvas = true;
    [SerializeField] Text rewardText;

    [Header("Observations")]
    [Tooltip("Clamp (y - TargetH) / Radius to [-k, k] before dividing by k for observation 1.")]
    [SerializeField] float errorClampK = 2f;
    [SerializeField] float verticalVelocityRef = 8f;

    [Header("Rewards")]
    [Tooltip("One-time bonus the first time the drone enters the acceptable height band each episode.")]
    [SerializeField] float firstArrivalReward = 10f;
    [Tooltip("Per-second reward while |height error| ≤ acceptable radius.")]
    [SerializeField] float livingRewardPerSecond = 0.5f;
    [Tooltip("Scales living reward when outside the band. Use 0 to cut sustain outside the band.")]
    [SerializeField] float outsideBandRewardScale = 0f;
    [Tooltip("Optional extra penalty per second when outside the band, scaled by excess beyond radius.")]
    [SerializeField] float outsideBandShapingPenaltyPerSecond = 0f;
    [Tooltip("Optional: multiply |vy| by this and dt, subtracted only while in band.")]
    [SerializeField] float verticalVelocityDampingInBand = 0f;

    [Header("Episode")]
    [Tooltip(
        "If ≤ 0, no timeout. Otherwise episode ends after this many simulated seconds. " +
        "Default rewards (10 + 0.5/s in band) make a 60s cap land near cumulative reward ~40; that is timeout, not a crash. " +
        "Use 0 for endless inference.")]
    public float maxEpisodeTime = 0f;
    [Tooltip("Episode ends if height goes below this value.")]
    public float failHeightLow = 0.2f;
    [Tooltip("Episode ends if height goes above this value.")]
    public float failHeight = 50f;
    [Tooltip("Seconds after reset before ground contact counts as crash.")]
    [SerializeField] float crashGraceSeconds = 0.5f;

    [Header("Throttle telemetry CSV")]
    [SerializeField] bool enableThrottleCsvLog = true;
    [SerializeField] string throttleCsvFileName = "hover_throttle_telemetry.csv";

    Vector3 startPosition;
    Quaternion startRotation;
    float episodeTimer;
    bool firstArrivalBonusClaimed;

    // Physics steps per decision (from DecisionRequester); scales dt for reward and episode timer.
    int decisionPeriodCached = 1;

    HoverScorer hoverScorer;

    string throttleCsvPath;
    bool throttleCsvHeaderWritten;
    static readonly object ThrottleCsvLock = new object();

    float TargetH => hoverScorer.targetHeight;
    float Radius => Mathf.Max(hoverScorer.acceptableRadius, 1e-6f);

    /// <summary>World-space target height (m) from <see cref="HoverScorer"/>.</summary>
    public float ResolvedTargetHeight => hoverScorer.targetHeight;

    public bool useAgentInput = true;

    /// <summary>
    /// Sets normalized throttle on <see cref="droneInput"/> and optionally appends one CSV row:
    /// UTC timestamp, raw stick values (same semantics as <see cref="YueInputModule"/> raw fields), height (m).
    /// </summary>
    public void SetDroneThrottle(float throttleNormalized)
    {
        if (droneInput == null)
        {
            Debug.LogError(
                $"{nameof(DroneHoverAgent)}: missing reference to {nameof(AgentDroneEmulator)} ({nameof(droneInput)}).");
            return;
        }

        throttleNormalized = Mathf.Clamp01(throttleNormalized);
        droneInput.agentThrottle = throttleNormalized;

        if (!enableThrottleCsvLog)
            return;

        droneInput.GetCommandedRawInputs(out float rawLeftH, out float rawLeftV, out float rawRightH, out float rawRightV);
        float height = droneRb != null ? droneRb.position.y : transform.position.y;
        AppendThrottleTelemetryRow(rawLeftH, rawLeftV, rawRightH, rawRightV, height);
    }

    void AppendThrottleTelemetryRow(
        float rawLeftHorizontal,
        float rawLeftVertical,
        float rawRightHorizontal,
        float rawRightVertical,
        float heightMeters)
    {
        string fileName = string.IsNullOrWhiteSpace(throttleCsvFileName)
            ? "hover_throttle_telemetry.csv"
            : throttleCsvFileName;
        if (string.IsNullOrEmpty(throttleCsvPath))
            throttleCsvPath = Path.Combine(Application.persistentDataPath, fileName);

        string utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        var line = new StringBuilder(128)
            .Append(utc).Append(',')
            .Append(heightMeters.ToString("R", CultureInfo.InvariantCulture)).Append(',')
            .Append(rawLeftHorizontal.ToString("R", CultureInfo.InvariantCulture)).Append(',')
            .Append(rawLeftVertical.ToString("R", CultureInfo.InvariantCulture)).Append(',')
            .Append(rawRightHorizontal.ToString("R", CultureInfo.InvariantCulture)).Append(',')
            .Append(rawRightVertical.ToString("R", CultureInfo.InvariantCulture))
            .AppendLine();

        lock (ThrottleCsvLock)
        {
            if (!throttleCsvHeaderWritten)
            {
                if (!File.Exists(throttleCsvPath))
                {
                    const string header =
                        "TimestampUtc,HeightM,RawLeftHorizontal,RawLeftVertical,RawRightHorizontal,RawRightVertical";
                    File.WriteAllText(throttleCsvPath, header + Environment.NewLine, Encoding.UTF8);
                }

                throttleCsvHeaderWritten = true;
            }

            File.AppendAllText(throttleCsvPath, line.ToString(), Encoding.UTF8);
        }
    }

    public override void Initialize()
    {
        if (droneRb == null)
            droneRb = GetComponent<Rigidbody>();
        if (droneInput == null)
            droneInput = GetComponent<AgentDroneEmulator>();

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Awake()
    {
        hoverScorer = GetComponent<HoverScorer>();
        var decisionRequester = GetComponent<DecisionRequester>();
        if (decisionRequester != null && decisionRequester.DecisionPeriod > 0)
            decisionPeriodCached = decisionRequester.DecisionPeriod;
        if (showRewardOnCanvas && rewardText == null)
            rewardText = CreateOrFindRewardText();
    }

    void LateUpdate()
    {
        if (showRewardOnCanvas && rewardText != null)
            rewardText.text = $"Reward: {GetCumulativeReward():F2}";
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
        droneRb.linearVelocity = Vector3.zero;
        droneRb.angularVelocity = Vector3.zero;
        episodeTimer = 0f;
        firstArrivalBonusClaimed = false;

        var physics = GetComponent<YueDronePhysics>();
        if (physics != null)
            physics.ResetInternals();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float y = transform.position.y;
        float k = Mathf.Max(errorClampK, 1e-6f);
        float signedNorm = Mathf.Clamp((y - TargetH) / Radius, -k, k) / k;
        sensor.AddObservation(signedNorm);

        float vRef = Mathf.Max(verticalVelocityRef, 1e-6f);
        sensor.AddObservation(Mathf.Clamp(droneRb.linearVelocity.y / vRef, -1f, 1f));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttleNormalized = Mathf.Clamp01(actions.ContinuousActions[0]);

        if (droneInput == null)
        {
            Debug.LogError(
                $"{nameof(DroneHoverAgent)}: missing reference to {nameof(AgentDroneEmulator)} ({nameof(droneInput)}).");
            return;
        }

        droneInput.SetUseAgentInput(useAgentInput);

        // Always mirror actions so UI (e.g. yellow agent dots) can show policy output when not driving the drone.
        droneInput.SetDroneThrottle(throttleNormalized);

        if (useAgentInput)
        {
            float y = transform.position.y;
            float heightError = Mathf.Abs(y - TargetH);
            bool inBand = heightError <= Radius;
            float dt = Time.fixedDeltaTime * decisionPeriodCached;

            if (!firstArrivalBonusClaimed && inBand)
            {
                AddReward(firstArrivalReward);
                firstArrivalBonusClaimed = true;
            }

            float sustain = livingRewardPerSecond * dt;
            if (inBand)
            {
                AddReward(sustain);
                if (verticalVelocityDampingInBand > 0f)
                    AddReward(-Mathf.Abs(droneRb.linearVelocity.y) * verticalVelocityDampingInBand * dt);
            }
            else
            {
                AddReward(sustain * outsideBandRewardScale);
                if (outsideBandShapingPenaltyPerSecond > 0f)
                {
                    float excess = Mathf.Max(0f, heightError - Radius);
                    float norm = excess / Radius;
                    AddReward(-outsideBandShapingPenaltyPerSecond * norm * dt);
                }
            }

            episodeTimer += dt;

            bool crashed = y < failHeightLow && episodeTimer > crashGraceSeconds;
            bool tooHigh = y > failHeight;
            bool timeout = maxEpisodeTime > 0f && episodeTimer >= maxEpisodeTime;
            bool flipped = Vector3.Dot(transform.up, Vector3.up) < 0f;

            if (crashed || flipped)
            {
                AddReward(-1f);
                EndEpisode();
            }
            else if (tooHigh)
            {
                AddReward(-0.5f);
                EndEpisode();
            }
            else if (timeout)
            {
                EndEpisode();
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        float throttle = Mathf.Clamp(Input.GetAxis("Throttle"), 0f, 1f);
        float roll = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        float pitch = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        float yaw = Mathf.Clamp(Input.GetAxis("Yaw"), -1f, 1f);


        continuousActions[0] = throttle;
    }

    static Text CreateOrFindRewardText()
    {
        const string canvasName = "HoverRewardCanvas";
        GameObject existing = GameObject.Find(canvasName);
        if (existing != null)
        {
            Text found = existing.GetComponentInChildren<Text>();
            if (found != null)
                return found;
        }

        var canvasGo = new GameObject(canvasName);
        DontDestroyOnLoad(canvasGo);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var textGo = new GameObject("RewardText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var text = textGo.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.font = font;
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;

        var rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -24f);
        rt.sizeDelta = new Vector2(480f, 56f);

        return text;
    }
}
