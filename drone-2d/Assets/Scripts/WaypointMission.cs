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

    [Header("Navigation")]
    public float waypointReachedThreshold = 3f;

    // Internal state
    private List<Vector3> missionPath = new List<Vector3>();
    private int currentWaypointIndex = 0;

    public enum MissionPhase { GoToTarget, Orbit, ReturnHome, Complete }
    public MissionPhase currentPhase = MissionPhase.GoToTarget;

    void Start()
    {
        BuildMissionPath();
        Debug.Log("Mission started! Waypoints: " + missionPath.Count);
    }

    void BuildMissionPath()
    {
        missionPath.Clear();

        // Step 1: Fly to the waypoint
        missionPath.Add(targetWaypoint.position);

        // Step 2: Fly in a circle around the waypoint
        for (int i = 0; i < orbitPoints; i++)
        {
            float angle = (1f * Mathf.PI / orbitPoints) * i;
            Vector3 orbitPos = targetWaypoint.position + new Vector3(
                Mathf.Cos(angle) * orbitRadius,
                0f,
                Mathf.Sin(angle) * orbitRadius
            );
            missionPath.Add(orbitPos);
        }

        // Step 3: Fly back home
        missionPath.Add(homePoint.position);
    }

    void Update()
    {
        if (currentPhase == MissionPhase.Complete) return;

        // Check if drone reached the current waypoint
        float dist = Vector3.Distance(transform.position, GetCurrentTarget());
        if (dist < waypointReachedThreshold)
        {
            currentWaypointIndex++;
            Debug.Log("Reached waypoint " + currentWaypointIndex + " / " + missionPath.Count);

            if (currentWaypointIndex >= missionPath.Count)
            {
                currentPhase = MissionPhase.Complete;
                Debug.Log("=== MISSION COMPLETE ===");
                return;
            }

            // Update phase
            if (currentWaypointIndex == 1)
                currentPhase = MissionPhase.Orbit;
            else if (currentWaypointIndex >= 1 + orbitPoints)
                currentPhase = MissionPhase.ReturnHome;
        }
    }

    public Vector3 GetCurrentTarget()
    {
        if (currentWaypointIndex < missionPath.Count)
            return missionPath[currentWaypointIndex];
        return homePoint.position;
    }

    // Draw the waypoint path in the Scene view so you can see it
    void OnDrawGizmos()
    {
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