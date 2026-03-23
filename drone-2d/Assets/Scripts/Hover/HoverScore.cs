using UnityEngine;

public class HoverScorer : MonoBehaviour
{
    public float targetHeight = 10f;
    public float maxError = 10f;

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
        float score = (1f - Mathf.Clamp01(avgError / maxError)) * 100f;

        Debug.Log($"[HoverScorer] Score: {score:F0}/100 | Avg Error: {avgError:F2}m | Samples: {sampleCount}");
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