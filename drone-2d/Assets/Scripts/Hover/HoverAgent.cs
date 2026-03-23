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

    [Header("Target")]
    public float targetHeight = 10f;

    [Header("Episode")]
    public float maxEpisodeTime = 10f;
    [Tooltip("Episode ends if height goes below this value.")]
    public float failHeightLow = 0.2f;
    [Tooltip("Episode ends if height goes above this value.")]
    public float failHeight = 50f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float episodeTimer;

    public override void Initialize()
    {
        if (droneRb == null)
            droneRb = GetComponent<Rigidbody>();
        if (droneInput == null)
            droneInput = GetComponent<AgentDroneEmulator>();

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
    }

    // --- OBSERVATIONS (4 floats) ---
    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Current height (normalized rough estimate)
        sensor.AddObservation(transform.localPosition.y / failHeight);

        // 2. Height error (signed)
        float error = transform.localPosition.y - targetHeight;
        sensor.AddObservation(error / failHeight);

        // 3. Vertical velocity
        sensor.AddObservation(droneRb.linearVelocity.y / 20f);

        // 4. Uprightness (1 = upright, 0 = sideways, -1 = flipped)
        sensor.AddObservation(Vector3.Dot(transform.up, Vector3.up));
    }

     // --- ACTIONS (1 continuous: throttle) ---
     public override void OnActionReceived(ActionBuffers actions)
     {
        print($"Actions: {actions.ContinuousActions[0]}");
         float throttleNormalized = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);

         if (droneInput == null)
         {
             Debug.LogError(
                 $"{nameof(DroneHoverAgent)}: missing reference to {nameof(AgentDroneEmulator)} ({nameof(droneInput)}).");
             return;
         }

         droneInput.SetUseAgentInput(true);
         droneInput.agentThrottle = throttleNormalized;

        // --- REWARDS ---
        float heightError = Mathf.Abs(transform.localPosition.y - targetHeight);

        // Small reward every step for being close to target
        // At error=0 → reward = +1/step, at error=5 → reward ≈ +0.14/step
        float proximityReward = 1f / (1f + heightError * heightError);
        AddReward(proximityReward * 0.01f);

        // Penalty for large velocity (encourage stillness)
        float velPenalty = Mathf.Abs(droneRb.linearVelocity.y) * 0.001f;
        AddReward(-velPenalty);

        // --- EPISODE END CONDITIONS ---
        episodeTimer += Time.fixedDeltaTime;

        bool crashed = transform.localPosition.y < failHeightLow && episodeTimer > 0.5f;
        bool tooHigh = transform.localPosition.y > failHeight;
        bool timeout = episodeTimer >= maxEpisodeTime;
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
            // No extra penalty — cumulative rewards speak for themselves
            EndEpisode();
        }
    }

     // --- MANUAL TESTING (lets you fly with keyboard) ---
     public override void Heuristic(in ActionBuffers actionsOut)
     {

        print($"Heuristic");
        var continuousActions = actionsOut.ContinuousActions;

        float throttle = Mathf.Clamp(Input.GetAxis("Throttle"), 0f, 1f);
        float roll = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        float pitch = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        float yaw = Mathf.Clamp(Input.GetAxis("Yaw"), -1f, 1f);

        print($"Throttle: {throttle}, Roll: {roll}, Pitch: {pitch}, Yaw: {yaw}");

        continuousActions[0] = throttle;
     }
}