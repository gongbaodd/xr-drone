using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class XRJoystickHud : MonoBehaviour
{
    private const string HudQuadName = "XR Joystick HUD Quad";

    private UIDocument uiDocument;
    private LimitedDroneEmulator emulator;
    private VisualElement leftDot;
    private VisualElement rightDot;
    private Transform hudQuadTransform;
    private Transform mainCameraTransform;
    private bool isHudReady;
    private bool isUiReady;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        emulator = FindAnyObjectByType<LimitedDroneEmulator>();
        BuildUi();
        SetupWorldView();
    }

    private void Update()
    {
        if (emulator == null || !isUiReady)
            return;

        ApplyDotPosition(leftDot, emulator.CurrentLeftStick);
        ApplyDotPosition(rightDot, emulator.CurrentRightStick);
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
        isUiReady = leftDot != null && rightDot != null;
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
