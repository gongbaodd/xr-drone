using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class DroneWaypointMission : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Transform homePoint;
    public Transform targetWaypoint;

    [Header("Orbit / teardrop scale")]
    [Tooltip("Minimum scale for Bezier handles; also used if home and target coincide.")]
    public float orbitRadius = 10f;
    [Tooltip("Unused for path geometry; kept for existing scenes/prefabs.")]
    public int orbitPoints = 8;

    [Header("Bezier path (single cubic teardrop)")]
    [Tooltip("Waypoints sampled along one cubic Bezier from home back to home (P0 = P3).")]
    [Min(8)]
    [FormerlySerializedAs("outboundBezierSamples")]
    public int bezierSamples = 48;
    [Tooltip("Lateral offset of the handles vs scale (water-drop bulge).")]
    [Range(0.05f, 0.95f)]
    public float bezierBulge = 0.38f;

    [Header("Navigation")]
    public float waypointReachedThreshold = 3f;

    [Header("Ground")]
    public GameObject ground;
    public Color goToTargetCompletedGroundColor = Color.blue;
    public Color missionCompletedGroundColor = new Color(1f, 0.5f, 0f); // Orange

    private Renderer groundRenderer;
    private Color initialGroundColor = Color.white;
    private bool hasInitialGroundColor = false;

    // Internal state
    private List<Vector3> missionPath = new List<Vector3>();
    /// <summary>Waypoint count for the first third of the loop — phase transitions.</summary>
    private int outboundWaypointCount = 0;
    private int orbitWaypointCount = 0;
    public int currentWaypointIndex = 0;
    private bool hasFinishedGoToTarget = false;
    /// <summary>True after the drone moves beyond <see cref="waypointReachedThreshold"/> of home.</summary>
    private bool hasLeftHome = false;

    public enum MissionPhase { GoToTarget, Orbit, ReturnHome, Complete }
    public MissionPhase currentPhase = MissionPhase.GoToTarget;

    public MissionPhase CurrentPhase => currentPhase;
    public int TotalWaypoints => Mathf.Max(1, missionPath.Count);
    /// <summary>Mission is underway only after the drone has left the home position.</summary>
    public bool MissionStarted => hasLeftHome;

    void Start()
    {
        CacheGroundRenderer();
        BuildMissionPath();
        ResetGroundColor();
        Debug.Log("Waypoint path ready (" + missionPath.Count + " points). Mission starts when the drone leaves home.");
    }

    // Called by `DroneAgent` between episodes to rebuild internal mission state.
    public void ResetMission()
    {
        currentWaypointIndex = 0;
        currentPhase = MissionPhase.GoToTarget;
        hasFinishedGoToTarget = false;
        hasLeftHome = false;
        BuildMissionPath();
        ResetGroundColor();
    }

    void BuildMissionPath()
    {
        missionPath.Clear();

        if (homePoint == null || targetWaypoint == null)
            return;

        Vector3 home = homePoint.position;
        Vector3 target = targetWaypoint.position;

        // One cubic Bezier: P0 = P3 = home (closed teardrop); P1/P2 bulge toward target region.
        Vector3 delta = target - home;
        float d = delta.magnitude;
        Vector3 forward = d > 1e-6f ? delta / d : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 1e-8f)
            right = Vector3.Cross(Vector3.forward, forward);
        right.Normalize();

        float scale = Mathf.Max(d, orbitRadius, 1f);
        float lateral = scale * Mathf.Clamp(bezierBulge, 0.05f, 0.95f);

        Vector3 p0 = home;
        Vector3 p3 = home;
        Vector3 p1 = target + forward * (scale * 0.35f) + right * lateral;
        Vector3 p2 = target + forward * (scale * 0.65f) - right * lateral;

        int samples = Mathf.Max(8, bezierSamples);
        for (int s = 1; s <= samples; s++)
        {
            float t = s / (float)samples;
            missionPath.Add(EvaluateCubicBezier(p0, p1, p2, p3, t));
        }

        int n = missionPath.Count;
        int third = Mathf.Max(1, n / 3);
        outboundWaypointCount = third;
        orbitWaypointCount = third;
        if (outboundWaypointCount + orbitWaypointCount >= n)
            orbitWaypointCount = Mathf.Max(0, n - outboundWaypointCount - 1);
    }

    static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return u * u * u * p0
            + 3f * u * u * t * p1
            + 3f * u * t * t * p2
            + t * t * t * p3;
    }

    void Update()
    {
        if (currentPhase == MissionPhase.Complete) return;

        if (!hasLeftHome && !IsDroneAtHome())
            hasLeftHome = true;

        // Require full path before "home": first Bezier samples sit near P0, so otherwise
        // reaching waypoint 0 can still be within threshold of homePoint.
        if (hasLeftHome && IsDroneAtHome()
            && missionPath.Count > 0
            && currentWaypointIndex >= missionPath.Count)
        {
            currentPhase = MissionPhase.Complete;
            SetGroundColor(missionCompletedGroundColor);
            Debug.Log("=== MISSION COMPLETE (HOME REACHED) ===");
            return;
        }

        if (currentWaypointIndex >= missionPath.Count)
            return;

        // Check if drone reached the current waypoint
        float dist = Vector3.Distance(transform.position, GetCurrentTarget());
        if (dist < waypointReachedThreshold)
        {
            currentWaypointIndex++;
            Debug.Log("Reached waypoint " + currentWaypointIndex + " / " + missionPath.Count);

            // Phases split ~thirds along the single closed curve
            if (currentWaypointIndex == outboundWaypointCount)
            {
                hasFinishedGoToTarget = true;
                if (orbitWaypointCount >= 1 && outboundWaypointCount + orbitWaypointCount < missionPath.Count)
                    currentPhase = MissionPhase.Orbit;
                else
                    currentPhase = MissionPhase.ReturnHome;
                SetGroundColor(goToTargetCompletedGroundColor);
            }
            else if (currentWaypointIndex >= outboundWaypointCount + orbitWaypointCount)
                currentPhase = MissionPhase.ReturnHome;
        }
    }

    void CacheGroundRenderer()
    {
        if (ground == null) return;

        groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer == null)
            groundRenderer = ground.GetComponentInChildren<Renderer>();

        if (groundRenderer != null)
        {
            initialGroundColor = groundRenderer.material.color;
            hasInitialGroundColor = true;
        }
    }

    void ResetGroundColor()
    {
        if (groundRenderer == null)
            CacheGroundRenderer();

        if (groundRenderer == null) return;
        if (!hasInitialGroundColor) return;

        SetGroundColor(initialGroundColor);
    }

    void SetGroundColor(Color color)
    {
        if (groundRenderer == null)
            CacheGroundRenderer();

        if (groundRenderer == null) return;
        groundRenderer.material.color = color;
    }

    public Vector3 GetCurrentTarget()
    {
        if (currentWaypointIndex < missionPath.Count)
            return missionPath[currentWaypointIndex];
        return homePoint.position;
    }

    bool IsDroneAtHome()
    {
        return Vector3.Distance(transform.position, homePoint.position) < waypointReachedThreshold;
    }

    // Draw the waypoint path in the Scene view so you can see it
    void OnDrawGizmos()
    {
        if (!Application.isPlaying && homePoint != null && targetWaypoint != null)
            BuildMissionPath();

        if (missionPath == null || missionPath.Count == 0) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < missionPath.Count - 1; i++)
        {
            Gizmos.DrawLine(missionPath[i], missionPath[i + 1]);
            Gizmos.DrawSphere(missionPath[i], 0.5f);
        }
        Gizmos.DrawSphere(missionPath[missionPath.Count - 1], 0.5f);

        // Draw current target in red
        if (currentWaypointIndex < missionPath.Count)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(missionPath[currentWaypointIndex], 1f);
        }
    }
}