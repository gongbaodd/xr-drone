using UnityEngine;
using UnityEngine.UIElements;
using YueUltimateDronePhysics;

[RequireComponent(typeof(UIDocument))]
public class ControllerUI : MonoBehaviour
{
    [SerializeField] AgentDroneEmulator emulator;

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
    }

    void LateUpdate()
    {
        if (emulator == null || _leftDot == null || _rightDot == null)
            return;

        emulator.GetStickVisualization(out var left, out var right);
        ApplyLeftStick(_leftDot, left);
        ApplyRightStick(_rightDot, right);

        if (_leftAgentDot != null && _rightAgentDot != null)
        {
            emulator.GetAgentStickVisualization(out var leftAgent, out var rightAgent);
            ApplyLeftStick(_leftAgentDot, leftAgent);
            ApplyRightStick(_rightAgentDot, rightAgent);
        }
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
