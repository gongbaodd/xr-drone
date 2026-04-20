// DroneDebugUI.cs
using UnityEngine;

public class DroneDebugUI : MonoBehaviour
{
    public DronePIDFlightController droneController;
    public PathFollower pathFollower;

    private bool showPIDTuning = false;

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.Box("无人机控制面板");

        if (GUILayout.Button("🚁 开始任务", GUILayout.Height(50)))
        {
            droneController.StartMission();
        }

        // 显示飞行状态
        GUILayout.Label($"高度: {droneController.transform.position.y:F1}m");
        
        if (pathFollower != null)
        {
            GUILayout.Label($"路径进度: {pathFollower.GetProgress() * 100:F1}%");
        }

        // PID调参面板
        showPIDTuning = GUILayout.Toggle(showPIDTuning, "显示PID调参");
        if (showPIDTuning)
        {
            GUILayout.Label("--- 位置PID (X) ---");
            droneController.pidX.Kp = GUISlider("Kp", droneController.pidX.Kp, 0, 10);
            droneController.pidX.Ki = GUISlider("Ki", droneController.pidX.Ki, 0, 2);
            droneController.pidX.Kd = GUISlider("Kd", droneController.pidX.Kd, 0, 5);

            GUILayout.Label("--- 高度PID (Y) ---");
            droneController.pidY.Kp = GUISlider("Kp", droneController.pidY.Kp, 0, 10);
            droneController.pidY.Ki = GUISlider("Ki", droneController.pidY.Ki, 0, 2);
            droneController.pidY.Kd = GUISlider("Kd", droneController.pidY.Kd, 0, 5);
        }

        GUILayout.EndArea();
    }

    private float GUISlider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:F2}", GUILayout.Width(100));
        value = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
        return value;
    }
}