using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using YueUltimateDronePhysics;

public class DroneHoverAgent : Agent
{
    [Header("References")]
    public Rigidbody droneRb;
    public AgentDroneEmulator droneInput;
    [Tooltip("If set, target height and acceptable radius are taken from HoverScorer (same GameObject as this agent in the Hover scene).")]
    [SerializeField] HoverScorer hoverScorer;

    [Header("Target")]
    [Tooltip("Fallback when HoverScorer is not assigned; keep in sync with HoverScorer.targetHeight.")]
    public float targetHeight = 10f;
    [Tooltip("Fallback when HoverScorer is not assigned; keep in sync with HoverScorer.acceptableRadius.")]
    public float acceptableRadius = 2f;

    [Header("Episode")]
    public float maxEpisodeTime = 10f;
    [Tooltip("Episode ends if height goes below this value.")]
    public float failHeightLow = 0.2f;
    [Tooltip("Episode ends if height goes above this value.")]
    public float failHeight = 50f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float episodeTimer;

    float TargetH => hoverScorer != null ? hoverScorer.targetHeight : targetHeight;
    float Radius => Mathf.Max(hoverScorer != null ? hoverScorer.acceptableRadius : acceptableRadius, 1e-6f);
    /// <summary>World-space target height (m); matches HoverScorer when present.</summary>
    public float ResolvedTargetHeight => TargetH;

    public override void Initialize()
    {
        if (droneRb == null)
            droneRb = GetComponent<Rigidbody>();
        if (droneInput == null)
            droneInput = GetComponent<AgentDroneEmulator>();
        if (hoverScorer == null)
            hoverScorer = GetComponent<HoverScorer>();

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    public override void OnEpisodeBegin()
    {
        // Reset drone
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
        droneRb.linearVelocity = Vector3.zero;
        droneRb.angularVelocity = Vector3.zero;
        episodeTimer = 0f;

        var physics = GetComponent<YueDronePhysics>();
        if (physics != null)
            physics.ResetInternals();

        if (hoverScorer == null)
            hoverScorer = GetComponent<HoverScorer>();
        hoverScorer?.StartScoring();
    }

    // --- OBSERVATIONS (4 floats) ---
    public override void CollectObservations(VectorSensor sensor)
    {
        float y = transform.position.y;
        // 1. Current height (normalized rough estimate)
        sensor.AddObservation(y / failHeight);

        // 2. Signed height error vs target, in units of acceptable radius (clamped for stability)
        float signedError = y - TargetH;
        sensor.AddObservation(Mathf.Clamp(signedError / Radius, -3f, 3f) / 3f);

        // 3. Vertical velocity
        sensor.AddObservation(droneRb.linearVelocity.y / 20f);

        // 4. Uprightness (1 = upright, 0 = sideways, -1 = flipped)
        sensor.AddObservation(Vector3.Dot(transform.up, Vector3.up));
    }

    // --- ACTIONS (1 continuous: throttle) ---
    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttleNormalized = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);

        if (droneInput == null)
        {
            Debug.LogError(
                $"{nameof(DroneHoverAgent)}: missing reference to {nameof(AgentDroneEmulator)} ({nameof(droneInput)}).");
            return;
        }

        droneInput.SetUseAgentInput(true);
        droneInput.agentThrottle = throttleNormalized;

        float y = transform.position.y;
        // --- REWARDS (same height space & band as HoverScorer) ---
        float heightError = Mathf.Abs(y - TargetH);
        float proximityReward = 1f / (1f + (heightError / Radius) * (heightError / Radius));
        AddReward(proximityReward * 0.01f);

        // Penalty for large velocity (encourage stillness)
        float velPenalty = Mathf.Abs(droneRb.linearVelocity.y) * 0.001f;
        AddReward(-velPenalty);

        // --- EPISODE END CONDITIONS ---
        episodeTimer += Time.fixedDeltaTime;

        bool crashed = y < failHeightLow && episodeTimer > 0.5f;
        bool tooHigh = y > failHeight;
        bool timeout = episodeTimer >= maxEpisodeTime;
        bool flipped = Vector3.Dot(transform.up, Vector3.up) < 0f;

        if (crashed || flipped)
        {
            AddReward(-1f);
            StopScorerAndEndEpisode();
        }
        else if (tooHigh)
        {
            AddReward(-0.5f);
            StopScorerAndEndEpisode();
        }
        else if (timeout)
        {
            StopScorerAndEndEpisode();
        }
    }

    void StopScorerAndEndEpisode()
    {
        hoverScorer?.StopAndGetScore();
        EndEpisode();
    }

    // --- MANUAL TESTING (lets you fly with keyboard) ---
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        float throttle = Mathf.Clamp(Input.GetAxis("Throttle"), 0f, 1f);
        float roll = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        float pitch = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        float yaw = Mathf.Clamp(Input.GetAxis("Yaw"), -1f, 1f);


        continuousActions[0] = throttle;
    }
}