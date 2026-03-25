using UnityEngine;
using UnityEngine.UIElements;
using YueUltimateDronePhysics;

[RequireComponent(typeof(UIDocument))]
public class ControllerUI : MonoBehaviour
{
    [SerializeField] AgentDroneEmulator emulator;
    [Tooltip("Optional (e.g. PID scene): HUD sticks from DronePIDFlightController / PIDController outputs when no AgentDroneEmulator or when the PID mission is driving inputs.")]
    [SerializeField] DronePIDFlightController pidFlightController;

    UIDocument _doc;
    VisualElement _leftDot;
    VisualElement _rightDot;
    VisualElement _leftAgentDot;
    VisualElement _rightAgentDot;

    const float StickPx = 120f;
    const float DotPx = 14f;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        var root = _doc.rootVisualElement;
        _leftDot = root.Q<VisualElement>("left-dot");
        _rightDot = root.Q<VisualElement>("right-dot");
        _leftAgentDot = root.Q<VisualElement>("left-agent-dot");
        _rightAgentDot = root.Q<VisualElement>("right-agent-dot");
    }

    void Start()
    {
        if (emulator == null)
            emulator = FindFirstObjectByType<AgentDroneEmulator>();
        if (pidFlightController == null)
            pidFlightController = FindFirstObjectByType<DronePIDFlightController>();
    }

    void LateUpdate()
    {
        if (_leftDot == null || _rightDot == null)
            return;

        if (!TryGetStickVisualization(out var left, out var right))
            return;

        ApplyLeftStick(_leftDot, left);
        ApplyRightStick(_rightDot, right);

        if (_leftAgentDot != null && _rightAgentDot != null && emulator != null)
        {
            emulator.GetAgentStickVisualization(out var leftAgent, out var rightAgent);
            ApplyLeftStick(_leftAgentDot, leftAgent);
            ApplyRightStick(_rightAgentDot, rightAgent);
        }
    }

    /// <summary>
    /// Prefer PID mission sticks when <see cref="DronePIDFlightController"/> is present and either there is no
    /// emulator or the mission is active; otherwise use <see cref="AgentDroneEmulator"/> sticks.
    /// </summary>
    bool TryGetStickVisualization(out Vector2 left, out Vector2 right)
    {
        left = right = default;

        if (pidFlightController != null && (emulator == null || pidFlightController.IsPidDrivingInputs))
        {
            GetPidStickVisualization(pidFlightController, out left, out right);
            return true;
        }

        if (emulator != null)
        {
            emulator.GetStickVisualization(out left, out right);
            return true;
        }

        return false;
    }

    /// <summary>Same Mode2 HUD convention as <see cref="AgentDroneEmulator.GetStickVisualization"/>.</summary>
    static void GetPidStickVisualization(DronePIDFlightController pid, out Vector2 leftStick, out Vector2 rightStick)
    {
        leftStick = new Vector2(
            Mathf.Clamp(pid.OutRawLeftHorizontal, -1f, 1f),
            Mathf.Clamp01(pid.OutRawLeftVertical));
        rightStick = new Vector2(
            Mathf.Clamp(pid.OutRawRightHorizontal, -1f, 1f),
            Mathf.Clamp(pid.OutRawRightVertical, -1f, 1f));
    }

    /// <summary>Left: x ∈ [-1,1], y ∈ [0,1] with 0 = bottom of the square.</summary>
    static void ApplyLeftStick(VisualElement dot, Vector2 stick)
    {
        float half = (StickPx - DotPx) * 0.5f;
        float px = stick.x * half;
        float t = Mathf.Clamp01(stick.y);
        float py = half * (1f - 2f * t);
        dot.style.translate = new Translate(
            new Length(px, LengthUnit.Pixel),
            new Length(py, LengthUnit.Pixel));
    }

    static void ApplyRightStick(VisualElement dot, Vector2 stick)
    {
        float half = (StickPx - DotPx) * 0.5f;
        float px = stick.x * half;
        float py = -stick.y * half;
        dot.style.translate = new Translate(
            new Length(px, LengthUnit.Pixel),
            new Length(py, LengthUnit.Pixel));
    }
}
