using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Scene view gizmos for <see cref="HoverScorer"/>: target height and acceptable band
/// (targetHeight ± acceptableRadius). Not visible in Game view.
/// </summary>
[RequireComponent(typeof(HoverScorer))]
public class HeightIndicator : MonoBehaviour
{
    [Header("Ground")]
    public GameObject ground;
    
    [Header("Gizmos")]
    [FormerlySerializedAs("showIndicator")]
    public bool drawGizmos = true;
    [Tooltip("Edge length of the square drawn on XZ at each height.")]
    [FormerlySerializedAs("quadSize")]
    public float gizmoSize = 4f;
    public Color targetColor = new Color(0.25f, 0.85f, 1f, 1f);
    public Color topBandColor = new Color(1f, 0.45f, 0.2f, 1f);
    public Color bottomBandColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Tooltip("Drone height at or above (target − margin) counts as having reached target height.")]
    [SerializeField] float reachTargetHeightMargin = 0.02f;
    [Tooltip("If height drops below this (e.g. episode reset to ground), reach state resets.")]
    [SerializeField] float reachResetBelowHeight = 1f;

    Renderer _groundRenderer;
    Color _groundColorBeforeReach;
    bool _hasReachedTargetHeight;

    HoverScorer hoverScorer;

    float TargetH => hoverScorer.targetHeight;

    float Radius => Mathf.Max(hoverScorer.acceptableRadius, 1e-6f);

    float BandTop => TargetH + Radius;
    float BandBottom => TargetH - Radius;

    /// <summary>
    /// Gizmos run in edit mode before <see cref="Awake"/>; resolve scorer lazily.
    /// </summary>
    bool EnsureHoverScorer()
    {
        if (hoverScorer == null)
            hoverScorer = GetComponent<HoverScorer>();
        return hoverScorer != null;
    }

    void Awake()
    {
        hoverScorer = GetComponent<HoverScorer>();
        if (ground != null)
        {
            _groundRenderer = ground.GetComponent<Renderer>();
            if (_groundRenderer != null)
                _groundColorBeforeReach = _groundRenderer.material.color;
        }
    }

    void LateUpdate()
    {
        if (_groundRenderer == null)
            return;

        float y = transform.position.y;

        if (y < reachResetBelowHeight)
            _hasReachedTargetHeight = false;

        if (!_hasReachedTargetHeight)
        {
            if (y >= TargetH - reachTargetHeightMargin)
                _hasReachedTargetHeight = true;
            else
            {
                _groundRenderer.material.color = _groundColorBeforeReach;
                return;
            }
        }

        Color c;
        if (y > BandTop)
            c = topBandColor;
        else if (y < BandBottom)
            c = bottomBandColor;
        else
            c = targetColor;

        _groundRenderer.material.color = c;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;
        if (!EnsureHoverScorer())
            return;

        Vector3 p = transform.position;
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
