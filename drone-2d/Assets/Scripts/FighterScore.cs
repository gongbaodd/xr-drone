using UnityEngine;
using System.Collections.Generic;

public class FlightScorer : MonoBehaviour
{
    [Header("References - Assign in Inspector")]
    public DroneWaypointMission mission;
    public Rigidbody droneRigidbody;

    [Header("Scoring Weights (must add up to 1.0)")]
    [Range(0, 1)] public float weightPathAccuracy = 0.25f;
    [Range(0, 1)] public float weightSmoothness = 0.20f;
    [Range(0, 1)] public float weightTime = 0.20f;
    [Range(0, 1)] public float weightStability = 0.20f;
    [Range(0, 1)] public float weightEnergy = 0.15f;

    [Header("Tuning")]
    public float idealCompletionTime = 60f;
    public float maxAcceptableDeviation = 20f;

    // Tracking variables
    private float missionStartTime;
    private float totalPathDeviation;
    private int deviationSamples;
    private float totalAngularVelocity;
    private float totalAcceleration;
    private int physicsSamples;
    private Vector3 lastVelocity;
    private float maxTiltAngle;
    private List<float> speedSamples = new List<float>();
    private int collisionCount;
    private float totalCollisionForce;
    private bool scored = false;
    private float completionTime;

    void Start()
    {
        missionStartTime = Time.time;
        lastVelocity = Vector3.zero;

        if (droneRigidbody == null)
            droneRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (scored) return;
        if (mission == null) return;

        // --- Sample path deviation ---
        Vector3 target = mission.GetCurrentTarget();
        float deviation = Vector3.Distance(transform.position, target);
        totalPathDeviation += deviation;
        deviationSamples++;

        // --- Sample physics ---
        if (droneRigidbody != null)
        {
            totalAngularVelocity += droneRigidbody.angularVelocity.magnitude;

            Vector3 currentVelocity = droneRigidbody.linearVelocity;
            Vector3 acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
            totalAcceleration += acceleration.magnitude;
            lastVelocity = currentVelocity;

            float tilt = Vector3.Angle(transform.up, Vector3.up);
            if (tilt > maxTiltAngle) maxTiltAngle = tilt;

            speedSamples.Add(currentVelocity.magnitude);
            physicsSamples++;
        }

        // --- Check if mission is done ---
        if (mission.currentPhase == DroneWaypointMission.MissionPhase.Complete && !scored)
        {
            scored = true;
            completionTime = Time.time - missionStartTime;
            float finalScore = CalculateFinalScore();
            DisplayScore(finalScore);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        collisionCount++;
        totalCollisionForce += collision.impulse.magnitude;
    }

    float CalculateFinalScore()
    {
        // 1. Path accuracy (0-1)
        float avgDeviation = deviationSamples > 0 ? totalPathDeviation / deviationSamples : 999f;
        float pathScore = Mathf.Clamp01(1f - (avgDeviation / maxAcceptableDeviation));

        // 2. Smoothness (0-1)
        float avgAngVel = physicsSamples > 0 ? totalAngularVelocity / physicsSamples : 10f;
        float smoothScore = Mathf.Clamp01(1f - (avgAngVel / 10f));

        // 3. Time efficiency (0-1)
        float timeRatio = completionTime / idealCompletionTime;
        float timeScore = Mathf.Exp(-Mathf.Pow(timeRatio - 1f, 2f) / 0.5f);

        // 4. Stability (0-1)
        float tiltScore = Mathf.Clamp01(1f - (maxTiltAngle / 45f));
        float varianceScore = Mathf.Clamp01(1f - (CalculateSpeedVariance() / 50f));
        float stabilityScore = (tiltScore + varianceScore) / 2f;

        // 5. Energy efficiency (0-1)
        float avgAccel = physicsSamples > 0 ? totalAcceleration / physicsSamples : 30f;
        float energyScore = Mathf.Clamp01(1f - (avgAccel / 30f));

        // 6. Collision penalty
        float collisionPenalty = (collisionCount * 10f) + (totalCollisionForce * 0.1f);

        // Final weighted score
        float rawScore =
            pathScore * weightPathAccuracy +
            smoothScore * weightSmoothness +
            timeScore * weightTime +
            stabilityScore * weightStability +
            energyScore * weightEnergy;

        float finalScore = Mathf.Clamp((rawScore * 100f) - collisionPenalty, 0f, 100f);

        // Log breakdown
        Debug.Log("══════════════════════════════════════");
        Debug.Log("        FLIGHT SCORE BREAKDOWN        ");
        Debug.Log("══════════════════════════════════════");
        Debug.Log($"  Path Accuracy:   {pathScore * 100f:F1}%");
        Debug.Log($"  Smoothness:      {smoothScore * 100f:F1}%");
        Debug.Log($"  Time Efficiency: {timeScore * 100f:F1}%  ({completionTime:F1}s / {idealCompletionTime:F1}s ideal)");
        Debug.Log($"  Stability:       {stabilityScore * 100f:F1}%");
        Debug.Log($"  Energy:          {energyScore * 100f:F1}%");
        Debug.Log($"  Collisions:      {collisionCount} (penalty: -{collisionPenalty:F1})");
        Debug.Log("──────────────────────────────────────");
        Debug.Log($"  FINAL SCORE:     {finalScore:F1} / 100");
        Debug.Log("══════════════════════════════════════");

        return finalScore;
    }

    float CalculateSpeedVariance()
    {
        if (speedSamples.Count < 2) return 0f;
        float mean = 0f;
        foreach (float s in speedSamples) mean += s;
        mean /= speedSamples.Count;

        float variance = 0f;
        foreach (float s in speedSamples)
            variance += (s - mean) * (s - mean);
        return variance / speedSamples.Count;
    }

    void DisplayScore(float score)
    {
        // You'll see this in the Game view via OnGUI
        finalDisplayScore = score;
    }

    private float finalDisplayScore = -1f;

    void OnGUI()
    {
        if (finalDisplayScore >= 0f)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 32;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            GUI.Box(new Rect(Screen.width / 2 - 200, 50, 400, 80),
                $"Flight Score: {finalDisplayScore:F1} / 100", style);
        }

        // Always show current phase
        if (mission != null)
        {
            GUIStyle phaseStyle = new GUIStyle(GUI.skin.label);
            phaseStyle.fontSize = 20;
            phaseStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(20, 20, 400, 40),
                $"Phase: {mission.currentPhase}", phaseStyle);
        }
    }
}