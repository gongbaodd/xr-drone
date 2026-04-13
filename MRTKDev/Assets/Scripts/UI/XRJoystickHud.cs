using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace YueUltimateDronePhysics
{
    [RequireComponent(typeof(UIDocument))]
    public class XRJoystickHud : MonoBehaviour
    {
        private static readonly Vector2Int PanelTextureSize = new(512, 256);
        private static readonly Vector3 HudLocalPosition = new(0f, -0.12f, 0.7f);
        private static readonly Vector2 HudWorldSize = new(0.34f, 0.18f);

        [Header("Data source")]
        [SerializeField] private LimitedDroneEmulator emulator;

        [SerializeField, HideInInspector] private VisualTreeAsset layoutAsset;

        private UIDocument uiDocument;
        private VisualElement leftDot;
        private VisualElement rightDot;
        private PanelSettings runtimePanelSettings;
        private RenderTexture panelTexture;
        private GameObject hudQuad;
        private Material hudMaterial;
        private Transform mainCameraTransform;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            emulator ??= FindAnyObjectByType<LimitedDroneEmulator>();
            EnsureUiAssets();
            SetupWorldView();
            BuildUi();
        }

        private void Update()
        {
            if (emulator == null || leftDot == null || rightDot == null)
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

            if (layoutAsset != null)
                layoutAsset.CloneTree(root);

            leftDot = root.Q<VisualElement>("left-dot");
            rightDot = root.Q<VisualElement>("right-dot");
        }

        private void EnsureUiAssets()
        {
            layoutAsset ??= uiDocument.visualTreeAsset;
        }

        private void SetupWorldView()
        {
            if (uiDocument.panelSettings == null)
                return;

            runtimePanelSettings = Instantiate(uiDocument.panelSettings);
            runtimePanelSettings.name = $"{uiDocument.panelSettings.name}_Runtime";

            panelTexture = new RenderTexture(PanelTextureSize.x, PanelTextureSize.y, 0, RenderTextureFormat.ARGB32)
            {
                name = "XRJoystickHudRT"
            };
            panelTexture.Create();
            runtimePanelSettings.targetTexture = panelTexture;
            uiDocument.panelSettings = runtimePanelSettings;

            hudQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hudQuad.name = "XR Joystick HUD Quad";
            hudQuad.transform.SetParent(transform, false);
            hudQuad.transform.localPosition = HudLocalPosition;
            hudQuad.transform.localRotation = Quaternion.identity;
            hudQuad.transform.localScale = new Vector3(HudWorldSize.x, HudWorldSize.y, 1f);

            Collider quadCollider = hudQuad.GetComponent<Collider>();
            if (quadCollider != null)
                Destroy(quadCollider);

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            if (shader != null)
            {
                hudMaterial = new Material(shader);
                hudMaterial.mainTexture = panelTexture;
                Renderer renderer = hudQuad.GetComponent<Renderer>();
                renderer.sharedMaterial = hudMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            if (Camera.main != null)
                mainCameraTransform = Camera.main.transform;
        }

        private void OnDestroy()
        {
            if (hudQuad != null)
                Destroy(hudQuad);

            if (hudMaterial != null)
                Destroy(hudMaterial);

            if (panelTexture != null)
            {
                panelTexture.Release();
                Destroy(panelTexture);
            }

            if (runtimePanelSettings != null)
                Destroy(runtimePanelSettings);
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
            if (hudQuad == null)
                return;

            if (mainCameraTransform == null && Camera.main != null)
                mainCameraTransform = Camera.main.transform;

            if (mainCameraTransform == null)
                return;

            Vector3 toCamera = mainCameraTransform.position - hudQuad.transform.position;
            if (toCamera.sqrMagnitude <= Mathf.Epsilon)
                return;

            hudQuad.transform.rotation = Quaternion.LookRotation(toCamera.normalized, mainCameraTransform.up);
        }
    }
}
