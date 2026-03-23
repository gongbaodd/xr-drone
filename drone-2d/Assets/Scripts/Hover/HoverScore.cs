using UnityEngine;
using UnityEngine.Serialization;

public class HoverScorer : MonoBehaviour
{
    [Tooltip("World-space height (m) the drone should hold.")]
    public float targetHeight = 3f;
    [Tooltip("Average absolute height error at or below this (m) earns 100. Above this, score falls linearly to 0 at twice this radius.")]
    [FormerlySerializedAs("maxError")]
    public float acceptableRadius = 0.5f;

    private float totalError;
    private int sampleCount;
    private bool scoring;
    private bool waitingForArrival = true;

    /// <summary>
    /// Arms a run: clears samples and waits until the drone first enters the target band (within acceptableRadius of targetHeight) before scoring begins.
    /// </summary>
    public void StartScoring()
    {
        totalError = 0f;
        sampleCount = 0;
        scoring = false;
        waitingForArrival = true;
    }

    public float StopAndGetScore()
    {
        scoring = false;
        waitingForArrival = true;
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
        float error = Mathf.Abs(transform.position.y - targetHeight);
        float r = Mathf.Max(acceptableRadius, 1e-6f);

        if (!scoring && waitingForArrival)
        {
            if (error > r)
                return;
            scoring = true;
            waitingForArrival = false;
            totalError = 0f;
            sampleCount = 0;
        }

        if (!scoring)
            return;

        totalError += error;
        sampleCount++;
    }
}