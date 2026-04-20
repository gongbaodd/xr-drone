// PIDController.cs
using UnityEngine;

[System.Serializable]
public class PIDController
{
    [Header("PID 参数")]
    public float Kp = 1.0f;   // 比例增益
    public float Ki = 0.0f;   // 积分增益
    public float Kd = 0.1f;   // 微分增益

    [Header("限制")]
    public float maxOutput = 10f;      // 输出上限
    public float maxIntegral = 5f;     // 积分上限（防止积分饱和）

    private float integral = 0f;
    private float previousError = 0f;
    private bool isFirstUpdate = true;

    /// <summary>
    /// 每帧调用，输入当前误差，返回控制输出
    /// </summary>
    public float Update(float error, float deltaTime)
    {
        if (deltaTime <= 0f) return 0f;

        // ---- P：比例项 ----
        float pTerm = Kp * error;

        // ---- I：积分项 ----
        integral += error * deltaTime;
        integral = Mathf.Clamp(integral, -maxIntegral, maxIntegral); // 防止积分饱和
        float iTerm = Ki * integral;

        // ---- D：微分项 ----
        float dTerm = 0f;
        if (!isFirstUpdate)
        {
            float derivative = (error - previousError) / deltaTime;
            dTerm = Kd * derivative;
        }
        isFirstUpdate = false;
        previousError = error;

        // ---- 合并输出 ----
        float output = pTerm + iTerm + dTerm;
        output = Mathf.Clamp(output, -maxOutput, maxOutput);

        return output;
    }

    /// <summary>
    /// 重置控制器状态
    /// </summary>
    public void Reset()
    {
        integral = 0f;
        previousError = 0f;
        isFirstUpdate = true;
    }

    /// <summary>Clears integral only (keeps derivative state). Use near setpoint to avoid I windup sticking throttle.</summary>
    public void ClearIntegral()
    {
        integral = 0f;
    }
}