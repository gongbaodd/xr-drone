using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Holds target hover height and acceptable band for <see cref="DroneHoverAgent"/> and <see cref="HeightIndicator"/>.
/// </summary>
public class HoverScorer : MonoBehaviour
{
    [Tooltip("World-space height (m) the drone should hold.")]
    public float targetHeight = 3f;
    [Tooltip("Band (m) around target height for agent rewards and gizmos.")]
    [FormerlySerializedAs("maxError")]
    public float acceptableRadius = 0.5f;
}
