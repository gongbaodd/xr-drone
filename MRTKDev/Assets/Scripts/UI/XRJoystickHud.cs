using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class XRJoystickHud : MonoBehaviour
{
    private const string HudQuadName = "XR Joystick HUD Quad";
    private const float AxisHighlightThreshold = 0.05f;

    [Header("Stick data sources")]
    [SerializeField] private LimitedDroneEmulator emulator;
    [SerializeField] private PidMapAutoFlightController pidMapAutoFlightController;

    private UIDocument uiDocument;
    private VisualElement leftDot;
    private VisualElement rightDot;
    private VisualElement leftPidTopLeft;
    private VisualElement leftPidTopRight;
    private VisualElement leftPidBottomLeft;
    private VisualElement leftPidBottomRight;
    private VisualElement rightPidTopLeft;
    private VisualElement rightPidTopRight;
    private VisualElement rightPidBottomLeft;
    private VisualElement rightPidBottomRight;
    private Transform hudQuadTransform;
    private Transform mainCameraTransform;
    private bool isHudReady;
    private bool isUiReady;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        BuildUi();
        SetupWorldView();
    }

    private void Update()
    {
        if (!isUiReady)
            return;

        if (emulator != null)
        {
            ApplyDotPosition(leftDot, emulator.CurrentLeftStick);
            ApplyDotPosition(rightDot, emulator.CurrentRightStick);
        }

        if (pidMapAutoFlightController != null)
        {
            Vector2 pidLeftStick = new Vector2(
                Mathf.Clamp(pidMapAutoFlightController.OutRawLeftHorizontal, -1f, 1f),
                Mathf.Clamp(pidMapAutoFlightController.OutRawLeftVertical01 * 2f - 1f, -1f, 1f));
            Vector2 pidRightStick = new Vector2(
                Mathf.Clamp(pidMapAutoFlightController.OutRawRightHorizontal, -1f, 1f),
                Mathf.Clamp(pidMapAutoFlightController.OutRawRightVertical, -1f, 1f));

            ApplyQuadrantHighlights(leftPidTopLeft, leftPidTopRight, leftPidBottomLeft, leftPidBottomRight, pidLeftStick);
            ApplyQuadrantHighlights(rightPidTopLeft, rightPidTopRight, rightPidBottomLeft, rightPidBottomRight, pidRightStick);
        }
    }

    private void LateUpdate()
    {
        UpdateHudFacing();
    }

    private void BuildUi()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        if (uiDocument.visualTreeAsset != null)
            uiDocument.visualTreeAsset.CloneTree(root);

        leftDot = root.Q<VisualElement>("left-dot");
        rightDot = root.Q<VisualElement>("right-dot");
        leftPidTopLeft = root.Q<VisualElement>("left-pid-quadrant-tl");
        leftPidTopRight = root.Q<VisualElement>("left-pid-quadrant-tr");
        leftPidBottomLeft = root.Q<VisualElement>("left-pid-quadrant-bl");
        leftPidBottomRight = root.Q<VisualElement>("left-pid-quadrant-br");
        rightPidTopLeft = root.Q<VisualElement>("right-pid-quadrant-tl");
        rightPidTopRight = root.Q<VisualElement>("right-pid-quadrant-tr");
        rightPidBottomLeft = root.Q<VisualElement>("right-pid-quadrant-bl");
        rightPidBottomRight = root.Q<VisualElement>("right-pid-quadrant-br");
        isUiReady =
            leftDot != null &&
            rightDot != null &&
            leftPidTopLeft != null &&
            leftPidTopRight != null &&
            leftPidBottomLeft != null &&
            leftPidBottomRight != null &&
            rightPidTopLeft != null &&
            rightPidTopRight != null &&
            rightPidBottomLeft != null &&
            rightPidBottomRight != null;
    }

    private void SetupWorldView()
    {
        hudQuadTransform = transform.Find(HudQuadName);
        RenderTexture panelTextureAsset = hudQuadTransform?.GetComponent<Renderer>()?.sharedMaterial?.mainTexture as RenderTexture;

        isHudReady = uiDocument.panelSettings != null && hudQuadTransform != null && panelTextureAsset != null;
        if (!isHudReady)
            return;

        if (uiDocument.panelSettings.targetTexture != panelTextureAsset)
            uiDocument.panelSettings.targetTexture = panelTextureAsset;

        mainCameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    private static void ApplyDotPosition(VisualElement dot, Vector2 stick)
    {
        if (dot == null || dot.parent == null)
            return;

        float mapSize = dot.parent.resolvedStyle.width;
        float dotSize = dot.resolvedStyle.width;
        if (mapSize <= 0f || dotSize <= 0f)
            return;

        float travel = (mapSize - dotSize) * 0.5f;
        float x = Mathf.Clamp(stick.x, -1f, 1f) * travel;
        float y = Mathf.Clamp(stick.y, -1f, 1f) * travel;
        dot.style.left = travel + x;
        dot.style.top = travel - y;
    }

    private static void ApplyQuadrantHighlights(
        VisualElement topLeft,
        VisualElement topRight,
        VisualElement bottomLeft,
        VisualElement bottomRight,
        Vector2 stick)
    {
        if (topLeft == null || topRight == null || bottomLeft == null || bottomRight == null)
            return;

        float clampedX = Mathf.Clamp(stick.x, -1f, 1f);
        float clampedY = Mathf.Clamp(stick.y, -1f, 1f);
        bool onVerticalAxis = Mathf.Abs(clampedX) <= AxisHighlightThreshold;
        bool onHorizontalAxis = Mathf.Abs(clampedY) <= AxisHighlightThreshold;

        bool activateLeft = onVerticalAxis || clampedX < 0f;
        bool activateRight = onVerticalAxis || clampedX > 0f;
        bool activateTop = onHorizontalAxis || clampedY > 0f;
        bool activateBottom = onHorizontalAxis || clampedY < 0f;

        topLeft.style.opacity = activateTop && activateLeft ? 1f : 0f;
        topRight.style.opacity = activateTop && activateRight ? 1f : 0f;
        bottomLeft.style.opacity = activateBottom && activateLeft ? 1f : 0f;
        bottomRight.style.opacity = activateBottom && activateRight ? 1f : 0f;
    }

    private void UpdateHudFacing()
    {
        if (!isHudReady)
            return;

        if (mainCameraTransform == null && Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        if (mainCameraTransform == null)
            return;

        Vector3 toCamera = mainCameraTransform.position - hudQuadTransform.position;
        if (toCamera.sqrMagnitude <= Mathf.Epsilon)
            return;

        hudQuadTransform.rotation = Quaternion.LookRotation(-toCamera.normalized, mainCameraTransform.up);
    }
}
