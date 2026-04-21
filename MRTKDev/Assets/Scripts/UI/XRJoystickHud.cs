using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class XRJoystickHud : MonoBehaviour
{
    private const string HudQuadName = "XR Joystick HUD Quad";

    private UIDocument uiDocument;
    private LimitedDroneEmulator emulator;
    private PidMapAutoFlightController pidMapAutoFlightController;
    private VisualElement leftDot;
    private VisualElement rightDot;
    private VisualElement leftPidDot;
    private VisualElement rightPidDot;
    private Transform hudQuadTransform;
    private Transform mainCameraTransform;
    private bool isHudReady;
    private bool isUiReady;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        emulator = FindAnyObjectByType<LimitedDroneEmulator>();
        pidMapAutoFlightController = FindAnyObjectByType<PidMapAutoFlightController>();
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

            ApplyDotPosition(leftPidDot, pidLeftStick);
            ApplyDotPosition(rightPidDot, pidRightStick);
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
        leftPidDot = root.Q<VisualElement>("left-pid-dot");
        rightPidDot = root.Q<VisualElement>("right-pid-dot");
        isUiReady = leftDot != null && rightDot != null && leftPidDot != null && rightPidDot != null;
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
