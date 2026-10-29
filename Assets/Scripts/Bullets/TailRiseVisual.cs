using UnityEngine;

/// <summary>꼬리 표시물을 바닥에서 위로 펼쳐 솟아나는 모습을 표시한다.</summary>
public sealed class TailRiseVisual : MonoBehaviour
{
    [SerializeField] private Transform visual;
    // ##TEMP 승인한 꼬리 등장 연출 시간.
    [SerializeField, Min(0.01f)] private float riseDuration = 0.15f;
    private float elapsed;

    /// <summary>대여할 때 등장 연출을 처음부터 준비한다.</summary>
    public void Prepare()
    {
        elapsed = 0f;
        UpdateVisual();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (visual == null)
            return;
        float progress = Mathf.Clamp01(elapsed / riseDuration);
        visual.localScale = new Vector3(1f, progress, 1f);
        visual.localPosition = new Vector3(0f, (progress - 1f) * 0.5f, 0f);
    }

    private void OnValidate() => riseDuration = Mathf.Max(0.01f, riseDuration);
}
