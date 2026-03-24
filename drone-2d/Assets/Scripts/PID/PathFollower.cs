// PathFollower.cs
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    [Header("跟踪参数")]
    public float lookAheadDistance = 2.0f; // 前瞻距离：无人机追逐前方多远的点
    public float pathCompleteThreshold = 1.0f; // 到终点多近算完成

    private List<Vector3> path;
    private int currentIndex = 0;
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

        // 找到路径上离无人机最近的点
        float minDist = float.MaxValue;
        int closestIndex = currentIndex;

        // 只向前搜索，避免走回头路
        int searchEnd = Mathf.Min(currentIndex + 30, path.Count);
        for (int i = currentIndex; i < searchEnd; i++)
        {
            float dist = Vector3.Distance(currentPosition, path[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }

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
            if (distToEnd < pathCompleteThreshold)
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