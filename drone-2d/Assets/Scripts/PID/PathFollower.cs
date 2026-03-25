// PathFollower.cs
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    [Header("跟踪参数")]
    public float lookAheadDistance = 2.0f; // 前瞻距离：无人机追逐前方多远的点
    public float pathCompleteThreshold = 1.0f; // 到终点多近算完成
    [Tooltip("How far back along the polyline to search for the closest point (recovers lateral error).")]
    public int closestPointSearchBack = 45;
    [Tooltip("Treat distances within this as a tie when choosing among equally close polyline samples.")]
    public float closestTieEpsilon = 0.001f;
    [Tooltip("After this fraction of path indices has been visited, distance ties (e.g. start/end at home) resolve to the higher index. Before that, resolve to the lower index so takeoff above home does not jump to the path end.")]
    [Range(0.1f, 0.9f)]
    public float tiePreferHigherAfterPathFraction = 0.45f;
    [Tooltip("Do not mark complete until at least this fraction of the path has been visited (guards edge cases).")]
    [Range(0f, 0.9f)]
    public float minCompletionPathFraction = 0.2f;

    private List<Vector3> path;
    private int currentIndex = 0;
    /// <summary>Monotonic max index reached along the path; used to disambiguate start/end at the same world position.</summary>
    private int maxPathIndexVisited = 0;
    private bool isFollowing = false;
    private bool isComplete = false;

    public bool IsComplete => isComplete;
    public bool IsFollowing => isFollowing;

    /// <summary>
    /// 设置要跟踪的路径
    /// </summary>
    public void SetPath(List<Vector3> newPath)
    {
        path = newPath;
        currentIndex = 0;
        maxPathIndexVisited = 0;
        isFollowing = true;
        isComplete = false;
    }

    /// <summary>
    /// 获取当前应该追逐的目标点
    /// </summary>
    public Vector3 GetTargetPoint(Vector3 currentPosition)
    {
        if (path == null || path.Count == 0)
            return currentPosition;

        // Closest point on polyline: allow limited backward search for lateral recovery.
        // Teardrop paths revisit home: path[0] and path[^1] share the same XZ. Right after takeoff the drone is
        // still above home, so distance ties with the final sample; we must not pick the higher index until we have
        // progressed along the path (tracked by maxPathIndexVisited).
        int searchStart = Mathf.Max(0, currentIndex - closestPointSearchBack);
        float minDist = float.MaxValue;
        for (int i = searchStart; i < path.Count; i++)
            minDist = Mathf.Min(minDist, Vector3.Distance(currentPosition, path[i]));

        int tieThresholdIndex = Mathf.RoundToInt((path.Count - 1) * tiePreferHigherAfterPathFraction);
        bool preferHigherOnTie = maxPathIndexVisited >= tieThresholdIndex;

        int closestIndex = searchStart;
        bool haveCandidate = false;
        for (int i = searchStart; i < path.Count; i++)
        {
            float dist = Vector3.Distance(currentPosition, path[i]);
            if (dist > minDist + closestTieEpsilon)
                continue;

            if (!haveCandidate)
            {
                closestIndex = i;
                haveCandidate = true;
                continue;
            }

            if (preferHigherOnTie)
            {
                if (i > closestIndex)
                    closestIndex = i;
            }
            else
            {
                if (i < closestIndex)
                    closestIndex = i;
            }
        }

        maxPathIndexVisited = Mathf.Max(maxPathIndexVisited, closestIndex);

        // 从最近点向前找前瞻点
        float accumulated = 0f;
        int targetIndex = closestIndex;
        for (int i = closestIndex; i < path.Count - 1; i++)
        {
            accumulated += Vector3.Distance(path[i], path[i + 1]);
            if (accumulated >= lookAheadDistance)
            {
                targetIndex = i + 1;
                break;
            }
            targetIndex = i + 1;
        }

        currentIndex = closestIndex;

        // 检查是否完成
        if (targetIndex >= path.Count - 1)
        {
            float distToEnd = Vector3.Distance(currentPosition, path[path.Count - 1]);
            int minIndexForCompletion = Mathf.RoundToInt((path.Count - 1) * minCompletionPathFraction);
            if (distToEnd < pathCompleteThreshold && maxPathIndexVisited >= minIndexForCompletion)
            {
                isComplete = true;
                isFollowing = false;
            }
            return path[path.Count - 1];
        }

        return path[targetIndex];
    }

    /// <summary>
    /// 获取路径进度（0~1）
    /// </summary>
    public float GetProgress()
    {
        if (path == null || path.Count == 0) return 0f;
        return (float)currentIndex / (path.Count - 1);
    }
}