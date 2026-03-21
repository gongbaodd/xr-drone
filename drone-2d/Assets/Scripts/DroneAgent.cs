using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using YueUltimateDronePhysics;

public class DroneAgent : Agent
{
    [Header("References")]
    public Rigidbody droneRb;
    public DroneWaypointMission mission;
    public FlightScorer scorer;
    public Transform homePoint;
    public AgentDroneEmulator droneInput;

    // Cache
    private Vector3 startPosition;
    private Quaternion startRotation;

    public override void Initialize()
    {
        startPosition = droneRb.transform.position;
        startRotation = droneRb.transform.rotation;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Transform currentTarget = mission.GetCurrentTargetTransform();

        // 1. Relative position to current waypoint (local space) — 3 floats
        Vector3 localTargetDir = droneRb.transform.InverseTransformPoint(
            currentTarget.position);
        sensor.AddObservation(localTargetDir / 50f); // normalized

        // 2. Distance to current waypoint — 1 float
        sensor.AddObservation(Vector3.Distance(
            droneRb.position, currentTarget.position) / 100f);

        // 3. Drone velocity (local space) — 3 floats
        Vector3 localVelocity = droneRb.transform.InverseTransformDirection(
            droneRb.linearVelocity);
        sensor.AddObservation(localVelocity / 20f);

        // 4. Drone angular velocity (local space) — 3 floats
        Vector3 localAngVel = droneRb.transform.InverseTransformDirection(
            droneRb.angularVelocity);
        sensor.AddObservation(localAngVel / 10f);

        // 5. Drone orientation (forward + up vectors) — 6 floats
        sensor.AddObservation(droneRb.transform.forward);
        sensor.AddObservation(droneRb.transform.up);

        // 6. Mission phase (one-hot: GoToTarget, Orbit, ReturnHome) — 3 floats
        sensor.AddOneHotObservation((int)mission.CurrentPhase, 3);

        // 7. Waypoint progress — 1 float
        sensor.AddObservation(
            (float)mission.currentWaypointIndex / mission.TotalWaypoints);

        // 8. Relative position to home (local space) — 3 floats
        Vector3 localHome = droneRb.transform.InverseTransformPoint(
            homePoint.position);
        sensor.AddObservation(localHome / 50f);

        // 9. Current altitude — 1 float
        sensor.AddObservation(droneRb.position.y / 50f);

        // 10. Speed scalar — 1 float
        sensor.AddObservation(droneRb.linearVelocity.magnitude / 20f);
    }
    // Total: 3+1+3+3+6+3+1+3+1+1 = 25 observations
    public override void OnActionReceived(ActionBuffers actions)
    {

        // 4 continuous actions, each in [-1, 1]
        float throttle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float roll = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float pitch = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        float yaw = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);


        if (droneInput == null)
        {
            Debug.LogError($"{nameof(DroneAgent)}: missing reference to {nameof(droneInput)} (AgentDroneEmulator).");
            return;
        }

        // Feed ML actions into Yue's InputModule through the emulator.
        droneInput.SetUseAgentInput(true);
        droneInput.agentThrottle = throttle;
        droneInput.agentRoll = roll;
        droneInput.agentPitch = pitch;
        droneInput.agentYaw = yaw;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        // Keep the same action semantics used by training/inference:
        // [0] throttle, [1] roll, [2] pitch, [3] yaw
        // Jump axis is typically [0,1], so remap to [-1,1].
        float throttle = Mathf.Clamp(Input.GetAxis("Jump") * 2f - 1f, -1f, 1f);
        float roll = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        float pitch = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        float yaw = Mathf.Clamp(Input.GetAxis("Yaw"), -1f, 1f);

        continuousActions[0] = throttle;
        continuousActions[1] = roll;
        continuousActions[2] = pitch;
        continuousActions[3] = yaw;
    }

    // In DroneAgent.cs — called inside OnActionReceived or a separate method

private float previousDistanceToWaypoint;

private void CalculateReward()
{
    Transform currentTarget = mission.GetCurrentTargetTransform();
    float currentDistance = Vector3.Distance(
        droneRb.position, currentTarget.position);

    // === REWARD 1: Progress toward current waypoint ===
    // Positive when getting closer, negative when drifting away
    float progressReward = (previousDistanceToWaypoint - currentDistance) * 0.1f;
    AddReward(progressReward);

    // === REWARD 2: Waypoint reached bonus ===
    // (Call this from mission callback when waypoint index advances)
    // AddReward(+2.0f) per waypoint reached — see Step 7

    // === REWARD 3: Smoothness penalty (per-step) ===
    float angularVelMag = droneRb.angularVelocity.magnitude;
    float smoothnessPenalty = -0.001f * angularVelMag;
    AddReward(smoothnessPenalty);

    // === REWARD 4: Excessive tilt penalty ===
    float tiltAngle = Vector3.Angle(Vector3.up, droneRb.transform.up);
    if (tiltAngle > 45f)
    {
        AddReward(-0.005f * (tiltAngle - 45f) / 45f);
    }

    // === REWARD 5: Time penalty (encourage efficiency) ===
    AddReward(-0.001f); // small constant penalty per step

    // === REWARD 6: Collision penalty ===
    // Handled in OnCollisionEnter

    // === REWARD 7: Altitude penalty (don't crash into ground) ===
    if (droneRb.position.y < 1.0f)
    {
        AddReward(-1.0f);
        EndEpisode(); // crash = episode over
    }

    // === REWARD 8: Mission complete bonus ===
    if (mission.CurrentPhase == DroneWaypointMission.MissionPhase.Complete)
    {
        float finalScore = scorer.CalculateScore(); // 0-100
        AddReward(finalScore / 10f); // scale to ~0-10 range
        EndEpisode();
    }

    previousDistanceToWaypoint = currentDistance;
}

private void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Obstacle"))
    {
        AddReward(-2.0f);
        EndEpisode();
    }
}

public override void OnEpisodeBegin()
{
    // 1. Reset drone position and physics
    droneRb.transform.position = startPosition;
    droneRb.transform.rotation = startRotation;
    droneRb.linearVelocity = Vector3.zero;
    droneRb.angularVelocity = Vector3.zero;

    // 2. Reset the mission state machine
    mission.ResetMission();

    // 3. Reset the scorer
    scorer.ResetScorer();

    // 4. Reset tracking variables
    previousDistanceToWaypoint = Vector3.Distance(
        startPosition, mission.GetCurrentTargetTransform().position);

    // 5. (Optional) Randomize waypoint position slightly for generalization
    // targetWaypoint.position = new Vector3(
    //     20 + Random.Range(-5f, 5f),
    //     10 + Random.Range(-3f, 3f),
    //     30 + Random.Range(-5f, 5f));
    // mission.RebuildPath();
}
}