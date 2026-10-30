using UnityEngine;

/// <summary>꼬리 표시물을 바닥에서 위로 펼쳐 솟아나는 모습을 표시한다.</summary>
public sealed class TailRiseVisual : MonoBehaviour
{
    [SerializeField] private Transform visual;
    // ##TEMP 승인한 꼬리 등장 연출 시간.
    [SerializeField, Min(0.01f)] private float riseDuration = 0.15f;
    private float elapsed;
    private bool retreating;
    private float retreatElapsed;
    private float retreatHeight;

    /// <summary>땅으로 접는 연출이 끝났는지 반환한다.</summary>
    public bool IsRetreatComplete => retreating && retreatElapsed >= riseDuration;

    /// <summary>대여할 때 등장 연출을 처음부터 준비한다.</summary>
    public void Prepare()
    {
        elapsed = 0f;
        retreating = false;
        retreatElapsed = 0f;
        UpdateVisual();
    }

    private void Update()
    {
        if (retreating)
            return;
        elapsed += Time.deltaTime;
        UpdateVisual();
    }

    /// <summary>현재 펼쳐진 높이에서 땅으로 접는 연출을 시작한다.</summary>
    public void BeginRetreat()
    {
        if (retreating)
            return;
        retreating = true;
        retreatElapsed = 0f;
        retreatHeight = visual != null ? visual.localScale.y : 0f;
    }

    /// <summary>소유 패턴이 회수 시간을 갱신한다.</summary>
    public void AdvanceRetreat(float deltaTime)
    {
        if (!retreating)
            return;
        retreatElapsed += Mathf.Max(0f, deltaTime);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (visual == null)
            return;
        float progress = Mathf.Clamp01(elapsed / riseDuration);
        if (retreating)
            progress = retreatHeight * (1f - Mathf.Clamp01(retreatElapsed / riseDuration));
        visual.localScale = new Vector3(1f, progress, 1f);
        visual.localPosition = new Vector3(0f, (progress - 1f) * 0.5f, 0f);
    }

    private void OnValidate() => riseDuration = Mathf.Max(0.01f, riseDuration);
}
