// TeardropPathGenerator.cs
using System.Collections.Generic;
using UnityEngine;

public class TeardropPathGenerator : MonoBehaviour
{
    [Header("路径参数")]
    public Transform homePoint;        // 起点/终点
    public Transform waypointTarget;   // 要绕过的waypoint
    
    [Header("形状控制")]
    [Range(0.1f, 1.0f)]
    public float widthRatio = 0.4f;    // 水滴宽度相对于长度的比例
    [Range(0.2f, 0.8f)]
    public float bulgePosition = 0.4f; // 最宽处在路径上的位置（0=起点, 1=waypoint）
    
    [Header("精度")]
    public int pointsPerSegment = 50;  // 每段曲线的采样点数

    private List<Vector3> pathPoints = new List<Vector3>();

    /// <summary>
    /// 生成完整的水滴形路径
    /// </summary>
    public List<Vector3> GeneratePath()
    {
        pathPoints.Clear();

        Vector3 home = homePoint.position;
        Vector3 waypoint = waypointTarget.position;

        // 计算方向和距离
        Vector3 forward = (waypoint - home).normalized;
        float distance = Vector3.Distance(home, waypoint);
        float width = distance * widthRatio;

        // 计算垂直于飞行方向的横向向量（在水平面上）
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        
        // 如果forward几乎是垂直的，用另一个向量
        if (right.magnitude < 0.01f)
        {
            right = Vector3.Cross(Vector3.forward, forward).normalized;
        }

        // 最宽处的位置
        Vector3 bulgeCenter = Vector3.Lerp(home, waypoint, bulgePosition);

        // ===== 去程路径（右侧弧线）：Home → Waypoint =====
        Vector3 p0_out = home;
        Vector3 p1_out = bulgeCenter + right * width;   // 控制点：向右偏移
        Vector3 p2_out = waypoint + right * width * 0.3f; // 控制点：waypoint附近轻微偏右
        Vector3 p3_out = waypoint;

        // ===== 回程路径（左侧弧线）：Waypoint → Home =====
        Vector3 p0_ret = waypoint;
        Vector3 p1_ret = waypoint - right * width * 0.3f; // 控制点：waypoint附近轻微偏左
        Vector3 p2_ret = bulgeCenter - right * width;      // 控制点：向左偏移
        Vector3 p3_ret = home;

        // 采样去程
        for (int i = 0; i <= pointsPerSegment; i++)
        {
            float t = (float)i / pointsPerSegment;
            pathPoints.Add(CubicBezier(p0_out, p1_out, p2_out, p3_out, t));
        }

        // 采样回程（跳过第一个点，避免重复waypoint）
        for (int i = 1; i <= pointsPerSegment; i++)
        {
            float t = (float)i / pointsPerSegment;
            pathPoints.Add(CubicBezier(p0_ret, p1_ret, p2_ret, p3_ret, t));
        }

        return pathPoints;
    }

    /// <summary>
    /// 三阶贝塞尔曲线插值
    /// </summary>
    private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0
             + 3f * u * u * t * p1
             + 3f * u * t * t * p2
             + t * t * t * p3;
    }

    /// <summary>
    /// 在Scene视图中可视化路径
    /// </summary>
    private void OnDrawGizmos()
    {
        if (homePoint == null || waypointTarget == null) return;

        List<Vector3> preview = GeneratePath();
        
        Gizmos.color = Color.cyan;
        for (int i = 0; i < preview.Count - 1; i++)
        {
            Gizmos.DrawLine(preview[i], preview[i + 1]);
        }

        // 标记关键点
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(homePoint.position, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(waypointTarget.position, 0.5f);
    }
}