using UnityEngine;
using System.Collections.Generic;

public class DroneWaypointMission : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Transform homePoint;
    public Transform targetWaypoint;

    [Header("Orbit Settings")]
    public float orbitRadius = 10f;
    public int orbitPoints = 8;

    [Header("Bezier path (teardrop: home ↔ orbit)")]
    [Tooltip("Samples along the cubic Bezier from home to orbit entry.")]
    [Min(2)]
    public int outboundBezierSamples = 16;
    [Tooltip("Samples along the cubic Bezier from orbit exit back to home.")]
    [Min(2)]
    public int returnBezierSamples = 16;
    [Tooltip("Lateral offset of the Bezier handles as a fraction of chord length (water-drop bulge).")]
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
    /// <summary>Waypoints on the outbound Bezier leg (home → orbit entry) — used for phase transitions.</summary>
    private int outboundWaypointCount = 0;
    public int currentWaypointIndex = 0;
    private bool hasFinishedGoToTarget = false;

    public enum MissionPhase { GoToTarget, Orbit, ReturnHome, Complete }
    public MissionPhase currentPhase = MissionPhase.GoToTarget;

    public MissionPhase CurrentPhase => currentPhase;
    public int TotalWaypoints => Mathf.Max(1, missionPath.Count);

    void Start()
    {
        CacheGroundRenderer();
        BuildMissionPath();
        ResetGroundColor();
        Debug.Log("Mission started! Waypoints: " + missionPath.Count);
    }

    // Called by `DroneAgent` between episodes to rebuild internal mission state.
    public void ResetMission()
    {
        currentWaypointIndex = 0;
        currentPhase = MissionPhase.GoToTarget;
        hasFinishedGoToTarget = false;
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

        int nOrbit = Mathf.Max(1, orbitPoints);

        // Orbit arc (semi-circle in XZ), same convention as before
        var orbitRing = new Vector3[nOrbit];
        for (int i = 0; i < nOrbit; i++)
        {
            float angle = (Mathf.PI / nOrbit) * i;
            orbitRing[i] = target + new Vector3(
                Mathf.Cos(angle) * orbitRadius,
                0f,
                Mathf.Sin(angle) * orbitRadius
            );
        }

        Vector3 orbitStart = orbitRing[0];
        Vector3 orbitEnd = orbitRing[nOrbit - 1];

        // Step 1: Teardrop Bezier — home → orbit entry (narrow tip → wide bulge)
        GetBezierControls(home, orbitStart, out Vector3 o0, out Vector3 o1, out Vector3 o2, out Vector3 o3, bezierBulge, mirrorLateral: false);
        outboundWaypointCount = Mathf.Max(2, outboundBezierSamples);
        for (int s = 1; s <= outboundWaypointCount; s++)
        {
            float t = s / (float)outboundWaypointCount;
            missionPath.Add(EvaluateCubicBezier(o0, o1, o2, o3, t));
        }

        // Step 2: Fly along the orbit arc (skip duplicate at orbit start)
        for (int i = 1; i < nOrbit; i++)
            missionPath.Add(orbitRing[i]);

        // Step 3: Teardrop Bezier — orbit exit → home
        GetBezierControls(orbitEnd, home, out Vector3 r0, out Vector3 r1, out Vector3 r2, out Vector3 r3, bezierBulge, mirrorLateral: true);
        int retSamples = Mathf.Max(2, returnBezierSamples);
        for (int s = 1; s <= retSamples; s++)
        {
            float t = s / (float)retSamples;
            missionPath.Add(EvaluateCubicBezier(r0, r1, r2, r3, t));
        }
    }

    void GetBezierControls(Vector3 p0, Vector3 p3, out Vector3 o0, out Vector3 o1, out Vector3 o2, out Vector3 o3, float bulge, bool mirrorLateral)
    {
        o0 = p0;
        o3 = p3;
        Vector3 delta = p3 - p0;
        float chord = delta.magnitude;
        if (chord < 1e-6f)
        {
            o1 = o2 = p0;
            return;
        }

        Vector3 forward = delta / chord;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 1e-8f)
            right = Vector3.Cross(Vector3.forward, forward);
        right.Normalize();

        float lateral = chord * Mathf.Clamp(bulge, 0.05f, 0.95f);
        if (mirrorLateral) lateral = -lateral;
        o1 = p0 + forward * (chord * 0.33f) + right * lateral;
        o2 = p3 - forward * (chord * 0.33f) + right * lateral;
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

        // Mission completes only if the drone has already reached the target once
        // and is now back at home.
        if (hasFinishedGoToTarget && IsDroneAtHome())
        {
            currentPhase = MissionPhase.Complete;
            SetGroundColor(missionCompletedGroundColor);
            Debug.Log("=== MISSION COMPLETE (HOME REACHED) ===");
            return;
        }

        // Check if drone reached the current waypoint
        float dist = Vector3.Distance(transform.position, GetCurrentTarget());
        if (dist < waypointReachedThreshold)
        {
            currentWaypointIndex++;
            Debug.Log("Reached waypoint " + currentWaypointIndex + " / " + missionPath.Count);

            if (currentWaypointIndex >= missionPath.Count)
            {
                currentPhase = MissionPhase.Complete;
                SetGroundColor(missionCompletedGroundColor);
                Debug.Log("=== MISSION COMPLETE ===");
                return;
            }

            // Update phase (outbound leg length is outboundWaypointCount)
            if (currentWaypointIndex == outboundWaypointCount)
            {
                hasFinishedGoToTarget = true;
                if (orbitPoints >= 2)
                    currentPhase = MissionPhase.Orbit;
                else
                    currentPhase = MissionPhase.ReturnHome;
                SetGroundColor(goToTargetCompletedGroundColor);
            }
            else if (currentWaypointIndex >= outboundWaypointCount + Mathf.Max(1, orbitPoints) - 1)
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