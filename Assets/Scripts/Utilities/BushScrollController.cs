using UnityEngine;

/// <summary>씬에 배치된 수풀 덩어리를 아래로 흘려보내고 순환합니다.</summary>
[DefaultExecutionOrder(-100)]
public sealed class BushScrollController : MonoBehaviour
{
    [SerializeField] private StageBattleFlow stageBattleFlow;
    [SerializeField] private Transform[] chunks;

    // ##TEMP 수풀 스크롤 시작값. 월드 유닛 기준.
    [SerializeField, Min(0f)] private float scrollSpeed = 1f;
    [SerializeField, Min(0.01f)] private float chunkSpacing = 9f;
    [SerializeField] private float recycleCenterY = -9f;

    private float loopHeight;
    private BushArea[] bushAreas;

    private void Awake()
    {
        if (chunks == null)
            return;

        // 매 프레임 컴포넌트를 찾지 않도록 영역 참조를 저장합니다.
        bushAreas = new BushArea[chunks.Length];
        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i] != null)
                bushAreas[i] = chunks[i].GetComponent<BushArea>();
        }

        float lowestY = float.PositiveInfinity;
        float highestY = float.NegativeInfinity;
        foreach (Transform chunk in chunks)
        {
            if (chunk == null)
                continue;

            lowestY = Mathf.Min(lowestY, chunk.position.y);
            highestY = Mathf.Max(highestY, chunk.position.y);
        }

        if (float.IsFinite(lowestY))
            loopHeight = highestY - lowestY + chunkSpacing;
    }

    private void Update()
    {
        if (loopHeight <= 0f || Time.deltaTime <= 0f || scrollSpeed <= 0f)
            return;

        if (stageBattleFlow != null
            && (stageBattleFlow.IsProgressStopped || stageBattleFlow.IsStageCleared))
        {
            return;
        }

        ApplyChunkPositions(scrollSpeed * Time.deltaTime);
    }

    private void ApplyChunkPositions(float distance)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            Transform chunk = chunks[i];
            if (chunk == null)
                continue;

            if (bushAreas[i] != null && bushAreas[i].isActiveAndEnabled)
                bushAreas[i].RecordScroll(distance, recycleCenterY, loopHeight);

            Vector3 position = chunk.position;
            position.y -= distance;
            if (position.y < recycleCenterY)
            {
                position.y = recycleCenterY
                    + Mathf.Repeat(position.y - recycleCenterY, loopHeight);
            }

            chunk.position = position;
        }
    }
}
