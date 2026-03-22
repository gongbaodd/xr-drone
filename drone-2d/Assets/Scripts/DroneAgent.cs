using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.UI;
using YueUltimateDronePhysics;

public class DroneAgent : Agent
{
    [Header("References")]
    public Rigidbody droneRb;
    public DroneWaypointMission mission;
    public FlightScorer scorer;
    public Transform homePoint;
    public AgentDroneEmulator droneInput;

    [Header("UI")]
    [Tooltip("If enabled and Reward Text is unassigned, a screen-space Canvas is created at runtime.")]
    [SerializeField] private bool showRewardOnCanvas = true;
    [SerializeField] private Text rewardText;

    [Header("Rewards")]
    [SerializeField] private float waypointReward = 2f;
    [SerializeField] private float progressPerMeter = 0.5f;
    [SerializeField] private float livingCostPerSecond = 0.02f;
    [SerializeField] private float minYEndEpisode = 0.1f;
    [SerializeField] private float maxYEndEpisode = 25f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float prevDistToTarget;
    private int prevWaypointIndex;
    private bool gaveMissionCompleteReward;

    public override void Initialize()
    {
        startPosition = droneRb.transform.position;
        startRotation = droneRb.transform.rotation;
    }

    private void Awake()
    {
        if (showRewardOnCanvas && rewardText == null)
            rewardText = CreateOrFindRewardText();
    }

    private void LateUpdate()
    {
        // After DroneWaypointMission.Update — waypoint index is up to date.
        ApplyRewards();

        if (showRewardOnCanvas && rewardText != null)
            rewardText.text = $"Reward: {GetCumulativeReward():F2}";
    }

    private void ApplyRewards()
    {
        Vector3 target = mission.GetCurrentTarget();
        float dist = Vector3.Distance(droneRb.position, target);
        float dt = Time.deltaTime;

        if (mission.currentWaypointIndex > prevWaypointIndex)
        {
            AddReward(waypointReward * (mission.currentWaypointIndex - prevWaypointIndex));
            prevWaypointIndex = mission.currentWaypointIndex;
        }

        AddReward((prevDistToTarget - dist) * progressPerMeter);
        AddReward(-livingCostPerSecond * dt);

        if (droneRb.position.y < minYEndEpisode)
        {
            AddReward(-1f);
            EndEpisode();
        }
        else if (droneRb.position.y > maxYEndEpisode)
        {
            AddReward(-2f);
            EndEpisode();
        }
        else if (mission.CurrentPhase == DroneWaypointMission.MissionPhase.Complete
                 && !gaveMissionCompleteReward)
        {
            gaveMissionCompleteReward = true;
            AddReward(scorer.CalculateScore() * 0.1f);
            EndEpisode();
        }

        prevDistToTarget = dist;
    }

    private static Text CreateOrFindRewardText()
    {
        const string canvasName = "DroneRewardCanvas";
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

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 currentTarget = mission.GetCurrentTarget();

        Vector3 localTargetDir = droneRb.transform.InverseTransformPoint(
            currentTarget);
        sensor.AddObservation(localTargetDir / 50f);

        sensor.AddObservation(Vector3.Distance(
            droneRb.position, currentTarget) / 100f);

        Vector3 localVelocity = droneRb.transform.InverseTransformDirection(
            droneRb.linearVelocity);
        sensor.AddObservation(localVelocity / 20f);

        Vector3 localAngVel = droneRb.transform.InverseTransformDirection(
            droneRb.angularVelocity);
        sensor.AddObservation(localAngVel / 10f);

        sensor.AddObservation(droneRb.transform.forward);
        sensor.AddObservation(droneRb.transform.up);

        sensor.AddOneHotObservation((int)mission.CurrentPhase, 3);

        sensor.AddObservation(
            (float)mission.currentWaypointIndex / mission.TotalWaypoints);

        Vector3 localHome = droneRb.transform.InverseTransformPoint(
            homePoint.position);
        sensor.AddObservation(localHome / 50f);

        sensor.AddObservation(droneRb.position.y / 50f);

        sensor.AddObservation(droneRb.linearVelocity.magnitude / 20f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float roll = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float pitch = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        float yaw = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);

        if (droneInput == null)
        {
            Debug.LogError($"{nameof(DroneAgent)}: missing reference to {nameof(droneInput)} (AgentDroneEmulator).");
            return;
        }

        droneInput.SetUseAgentInput(true);
        droneInput.agentThrottle = throttle;
        droneInput.agentRoll = roll;
        droneInput.agentPitch = pitch;
        droneInput.agentYaw = yaw;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        float throttle = Mathf.Clamp(Input.GetAxis("Jump") * 2f - 1f, -1f, 1f);
        float roll = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        float pitch = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        float yaw = Mathf.Clamp(Input.GetAxis("Yaw"), -1f, 1f);

        continuousActions[0] = throttle;
        continuousActions[1] = roll;
        continuousActions[2] = pitch;
        continuousActions[3] = yaw;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-2f);
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        droneRb.transform.position = startPosition;
        droneRb.transform.rotation = startRotation;
        droneRb.linearVelocity = Vector3.zero;
        droneRb.angularVelocity = Vector3.zero;

        mission.ResetMission();
        scorer.ResetScorer();

        prevWaypointIndex = mission.currentWaypointIndex;
        prevDistToTarget = Vector3.Distance(startPosition, mission.GetCurrentTarget());
        gaveMissionCompleteReward = false;
    }
}
