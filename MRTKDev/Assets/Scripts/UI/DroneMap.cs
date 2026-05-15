using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class DroneMap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform quad;
    [SerializeField] private Transform finishPosition;
    [SerializeField] private RawImage view;

    [Header("Style")]
    [SerializeField] private Color cursorColor = new Color(0.16f, 0.95f, 1f, 0.95f);

    private UIDocument uiDocument;
    private VisualElement circle;
    private VisualElement cursor;
    private VisualElement finishDot;
    private bool isUiReady;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        BuildUi();
        MirrorRenderTextureToPanel();
    }

    private void LateUpdate()
    {
        if (!isUiReady || quad == null || finishPosition == null)
            return;

        UpdateFinishDot();
    }

    private void BuildUi()
    {
        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        root.Clear();

        if (uiDocument.visualTreeAsset != null)
            uiDocument.visualTreeAsset.CloneTree(root);

        circle = root.Q<VisualElement>("map-circle");
        cursor = root.Q<VisualElement>("map-cursor");
        finishDot = root.Q<VisualElement>("map-finish-dot");

        isUiReady = circle != null && cursor != null && finishDot != null;
        if (!isUiReady)
            return;

        cursor.generateVisualContent += DrawCursorTriangle;
        cursor.MarkDirtyRepaint();
    }

    private void DrawCursorTriangle(MeshGenerationContext ctx)
    {
        Rect r = cursor.contentRect;
        if (r.width <= 0f || r.height <= 0f)
            return;

        Painter2D painter = ctx.painter2D;
        painter.fillColor = cursorColor;
        painter.BeginPath();
        painter.MoveTo(new Vector2(r.center.x, r.yMin));
        painter.LineTo(new Vector2(r.xMax, r.yMax));
        painter.LineTo(new Vector2(r.xMin, r.yMax));
        painter.ClosePath();
        painter.Fill();
    }

    private void MirrorRenderTextureToPanel()
    {
        if (view == null || uiDocument.panelSettings == null)
            return;

        if (view.texture is RenderTexture rt && uiDocument.panelSettings.targetTexture != rt)
            uiDocument.panelSettings.targetTexture = rt;
    }

    private void UpdateFinishDot()
    {
        Vector3 forwardXZ = quad.forward;
        forwardXZ.y = 0f;
        if (forwardXZ.sqrMagnitude < Mathf.Epsilon)
            return;
        forwardXZ.Normalize();

        Vector3 toFinishXZ = finishPosition.position - quad.position;
        toFinishXZ.y = 0f;
        if (toFinishXZ.sqrMagnitude < Mathf.Epsilon)
            return;
        toFinishXZ.Normalize();

        float circleSize = circle.resolvedStyle.width;
        float dotSize = finishDot.resolvedStyle.width;
        if (circleSize <= 0f || dotSize <= 0f)
            return;

        float circleRadius = circleSize * 0.5f;
        float effectiveRadius = circleRadius - dotSize * 0.5f;
        if (effectiveRadius <= 0f)
            return;

        float thetaRad = Vector3.SignedAngle(forwardXZ, toFinishXZ, Vector3.up) * Mathf.Deg2Rad;
        float dx = Mathf.Sin(thetaRad) * effectiveRadius;
        float dy = -Mathf.Cos(thetaRad) * effectiveRadius;

        finishDot.style.left = circleRadius + dx - dotSize * 0.5f;
        finishDot.style.top = circleRadius + dy - dotSize * 0.5f;
    }
}
