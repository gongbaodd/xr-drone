using UnityEngine;
using UnityEngine.Serialization;

public class HoverScorer : MonoBehaviour
{
    [Tooltip("World-space height (m) the drone should hold.")]
    public float targetHeight = 10f;
    [Tooltip("Average absolute height error at or below this (m) earns 100. Above this, score falls linearly to 0 at twice this radius.")]
    [FormerlySerializedAs("maxError")]
    public float acceptableRadius = 2f;

    private float totalError;
    private int sampleCount;
    private bool scoring;

    public void StartScoring()
    {
        totalError = 0f;
        sampleCount = 0;
        scoring = true;
    }

    public float StopAndGetScore()
    {
        scoring = false;
        if (sampleCount == 0) return 0f;

        float avgError = totalError / sampleCount;
        float r = Mathf.Max(acceptableRadius, 1e-6f);
        float score = avgError <= r
            ? 100f
            : Mathf.Clamp01(1f - (avgError - r) / r) * 100f;

        Debug.Log(
            $"[HoverScorer] Score: {score:F0}/100 | Avg Error: {avgError:F2}m (target {targetHeight:F1}m ±{acceptableRadius:F1}m) | Samples: {sampleCount}");
        return score;
    }

    void FixedUpdate()
    {
        if (!scoring) return;

        float error = Mathf.Abs(transform.position.y - targetHeight);
        totalError += error;
        sampleCount++;
    }
}