// DronePIDFlightController.cs
using System.Collections.Generic;
using UnityEngine;

public class DronePIDFlightController : MonoBehaviour
{
    [Header("引用")]
    public TeardropPathGenerator pathGenerator;
    public Rigidbody droneRigidbody;

    [Header("飞行阶段")]
    public float takeoffHeight = 10f;    // 起飞目标高度
    public float cruiseSpeed = 5f;       // 巡航速度

    [Header("位置PID（外环）")]
    public PIDController pidX = new PIDController { Kp = 2f, Ki = 0.1f, Kd = 1.5f, maxOutput = 10f };
    public PIDController pidY = new PIDController { Kp = 3f, Ki = 0.2f, Kd = 2.0f, maxOutput = 15f };
    public PIDController pidZ = new PIDController { Kp = 2f, Ki = 0.1f, Kd = 1.5f, maxOutput = 10f };

    [Header("速度PID（内环）- 可选的串级控制")]
    public PIDController pidVx = new PIDController { Kp = 1f, Ki = 0.05f, Kd = 0.3f, maxOutput = 8f };
    public PIDController pidVy = new PIDController { Kp = 1.5f, Ki = 0.1f, Kd = 0.5f, maxOutput = 12f };
    public PIDController pidVz = new PIDController { Kp = 1f, Ki = 0.05f, Kd = 0.3f, maxOutput = 8f };

    [Header("偏航PID")]
    public PIDController pidYaw = new PIDController { Kp = 3f, Ki = 0.0f, Kd = 1.0f, maxOutput = 5f };

    [Header("物理参数")]
    public float mass = 1.5f;
    public float maxTilt = 30f;          // 最大倾斜角度（度）

    // 内部状态
    private enum FlightPhase { Idle, TakeOff, FollowPath, Landing, Complete }
    private FlightPhase currentPhase = FlightPhase.Idle;

    private PathFollower pathFollower;
    private List<Vector3> flightPath;
    private Vector3 targetPosition;
    private Vector3 homePosition;

    // ==================== Unity 生命周期 ====================

    void Start()
    {
        if (droneRigidbody == null)
            droneRigidbody = GetComponent<Rigidbody>();

        pathFollower = gameObject.AddComponent<PathFollower>();
        homePosition = transform.position;

        // 确保Rigidbody设置正确
        droneRigidbody.useGravity = true;
        droneRigidbody.mass = mass;
        droneRigidbody.linearDamping = 0.5f;
        droneRigidbody.angularDamping = 2f;
    }

    void FixedUpdate()
    {
        switch (currentPhase)
        {
            case FlightPhase.TakeOff:
                HandleTakeOff();
                break;
            case FlightPhase.FollowPath:
                HandleFollowPath();
                break;
            case FlightPhase.Landing:
                HandleLanding();
                break;
        }
    }

    // ==================== 公共接口 ====================

    /// <summary>
    /// 按下按钮或触发事件时调用此方法，开始整个飞行任务
    /// </summary>
    public void StartMission()
    {
        Debug.Log("📍 任务开始：起飞");
        homePosition = transform.position;
        targetPosition = new Vector3(homePosition.x, homePosition.y + takeoffHeight, homePosition.z);
        currentPhase = FlightPhase.TakeOff;
        ResetAllPIDs();
    }

    // ==================== 飞行阶段处理 ====================

    private void HandleTakeOff()
    {
        // 目标：垂直爬升到指定高度
        ApplyPositionPID(targetPosition);

        // 检查是否到达起飞高度
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            Debug.Log("✅ 起飞完成，开始跟踪路径");
            
            // 生成路径
            flightPath = pathGenerator.GeneratePath();
            
            // 将路径的起始点调整到当前位置的高度
            AdjustPathAltitude(flightPath, transform.position.y);
            
            pathFollower.SetPath(flightPath);
            currentPhase = FlightPhase.FollowPath;
            ResetAllPIDs();
        }
    }

    private void HandleFollowPath()
    {
        if (pathFollower.IsComplete)
        {
            Debug.Log("✅ 路径跟踪完成，开始降落");
            targetPosition = homePosition;
            currentPhase = FlightPhase.Landing;
            ResetAllPIDs();
            return;
        }

        // 获取前方目标点
        Vector3 target = pathFollower.GetTargetPoint(transform.position);
        
        // 串级PID控制
        ApplyCascadePID(target);

        // 让无人机朝飞行方向转向
        Vector3 velocity = droneRigidbody.linearVelocity;
        if (velocity.magnitude > 0.5f)
        {
            Vector3 flatVelocity = new Vector3(velocity.x, 0, velocity.z);
            if (flatVelocity.magnitude > 0.1f)
            {
                float targetYaw = Mathf.Atan2(flatVelocity.x, flatVelocity.z) * Mathf.Rad2Deg;
                ApplyYawControl(targetYaw);
            }
        }
    }

    private void HandleLanding()
    {
        // 缓慢降落到起始位置
        ApplyPositionPID(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
        {
            Debug.Log("🏠 降落完成！任务结束");
            currentPhase = FlightPhase.Complete;
            droneRigidbody.linearVelocity = Vector3.zero;
        }
    }

    // ==================== PID 控制核心 ====================

    /// <summary>
    /// 简单位置PID（用于起飞和降落）
    /// </summary>
    private void ApplyPositionPID(Vector3 target)
    {
        float dt = Time.fixedDeltaTime;
        Vector3 pos = transform.position;

        // 计算位置误差
        float errorX = target.x - pos.x;
        float errorY = target.y - pos.y;
        float errorZ = target.z - pos.z;

        // PID计算
        float forceX = pidX.Update(errorX, dt);
        float forceY = pidY.Update(errorY, dt);
        float forceZ = pidZ.Update(errorZ, dt);

        // 补偿重力
        forceY += 9.81f * mass;

        // 施加力
        Vector3 force = new Vector3(forceX, forceY, forceZ);
        droneRigidbody.AddForce(force, ForceMode.Force);
    }

    /// <summary>
    /// 串级PID（用于路径跟踪，更精确）
    /// 外环：位置误差 → 期望速度
    /// 内环：速度误差 → 控制力
    /// </summary>
    private void ApplyCascadePID(Vector3 target)
    {
        float dt = Time.fixedDeltaTime;
        Vector3 pos = transform.position;
        Vector3 vel = droneRigidbody.linearVelocity;

        // ===== 外环：位置 → 期望速度 =====
        float errorX = target.x - pos.x;
        float errorY = target.y - pos.y;
        float errorZ = target.z - pos.z;

        float desiredVx = pidX.Update(errorX, dt);
        float desiredVy = pidY.Update(errorY, dt);
        float desiredVz = pidZ.Update(errorZ, dt);

        // 限制期望速度
        Vector3 desiredVel = new Vector3(desiredVx, desiredVy, desiredVz);
        if (desiredVel.magnitude > cruiseSpeed)
        {
            desiredVel = desiredVel.normalized * cruiseSpeed;
        }

        // ===== 内环：速度误差 → 力 =====
        float velErrorX = desiredVel.x - vel.x;
        float velErrorY = desiredVel.y - vel.y;
        float velErrorZ = desiredVel.z - vel.z;

        float forceX = pidVx.Update(velErrorX, dt);
        float forceY = pidVy.Update(velErrorY, dt);
        float forceZ = pidVz.Update(velErrorZ, dt);

        // 补偿重力
        forceY += 9.81f * mass;

        // 施加力
        Vector3 force = new Vector3(forceX, forceY, forceZ);
        droneRigidbody.AddForce(force, ForceMode.Force);
    }

    /// <summary>
    /// 偏航控制
    /// </summary>
    private void ApplyYawControl(float targetYawDeg)
    {
        float currentYaw = transform.eulerAngles.y;
        float yawError = Mathf.DeltaAngle(currentYaw, targetYawDeg);
        float torque = pidYaw.Update(yawError, Time.fixedDeltaTime);
        droneRigidbody.AddTorque(Vector3.up * torque, ForceMode.Force);
    }

    // ==================== 辅助方法 ====================

    private void AdjustPathAltitude(List<Vector3> path, float altitude)
    {
        for (int i = 0; i < path.Count; i++)
        {
            path[i] = new Vector3(path[i].x, altitude, path[i].z);
        }
    }

    private void ResetAllPIDs()
    {
        pidX.Reset(); pidY.Reset(); pidZ.Reset();
        pidVx.Reset(); pidVy.Reset(); pidVz.Reset();
        pidYaw.Reset();
    }

    // ==================== 调试可视化 ====================

    private void OnDrawGizmos()
    {
        if (pathFollower != null && pathFollower.IsFollowing)
        {
            Vector3 target = pathFollower.GetTargetPoint(transform.position);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(target, 0.3f);
            Gizmos.DrawLine(transform.position, target);
        }
    }
}