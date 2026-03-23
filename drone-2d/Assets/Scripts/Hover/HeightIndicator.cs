using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Scene view gizmos for <see cref="HoverScorer"/>: target height and acceptable band
/// (targetHeight ± acceptableRadius). Not visible in Game view.
/// </summary>
public class HeightIndicator : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] HoverScorer hoverScorer;

    [Tooltip("Used only if HoverScorer is missing.")]
    public float targetHeight = 3f;
    [Tooltip("Used only if HoverScorer is missing.")]
    public float acceptableRadius = 0.5f;

    [Header("Gizmos")]
    [FormerlySerializedAs("showIndicator")]
    public bool drawGizmos = true;
    [Tooltip("Edge length of the square drawn on XZ at each height.")]
    [FormerlySerializedAs("quadSize")]
    public float gizmoSize = 4f;
    public Color targetColor = new Color(0.25f, 0.85f, 1f, 1f);
    public Color topBandColor = new Color(1f, 0.45f, 0.2f, 1f);
    public Color bottomBandColor = new Color(1f, 0.85f, 0.2f, 1f);

    float TargetH => hoverScorer != null ? hoverScorer.targetHeight : targetHeight;

    float Radius => Mathf.Max(hoverScorer != null ? hoverScorer.acceptableRadius : acceptableRadius, 1e-6f);

    float BandTop => TargetH + Radius;
    float BandBottom => TargetH - Radius;

    void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        if (hoverScorer == null)
            hoverScorer = GetComponent<HoverScorer>();

        Vector3 p = hoverScorer != null ? hoverScorer.transform.position : transform.position;
        float h = gizmoSize * 0.5f;

        DrawHorizontalSquare(new Vector3(p.x, TargetH, p.z), h, targetColor);
        DrawHorizontalSquare(new Vector3(p.x, BandTop, p.z), h, topBandColor);
        DrawHorizontalSquare(new Vector3(p.x, BandBottom, p.z), h, bottomBandColor);
    }

    static void DrawHorizontalSquare(Vector3 center, float halfExtent, Color color)
    {
        Gizmos.color = color;
        float x = center.x;
        float z = center.z;
        float y = center.y;

        var a = new Vector3(x - halfExtent, y, z - halfExtent);
        var b = new Vector3(x + halfExtent, y, z - halfExtent);
        var c = new Vector3(x + halfExtent, y, z + halfExtent);
        var d = new Vector3(x - halfExtent, y, z + halfExtent);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}
