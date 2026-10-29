using UnityEngine;

/// <summary>레인 경고 영역과 등장 방향, 안전 구간 경계를 표시한다.</summary>
public sealed class TailLaneWarning : MonoBehaviour
{
    [SerializeField] private SpriteRenderer danger;
    [SerializeField] private LineRenderer arrow;
    [SerializeField] private LineRenderer[] safeBoundaries;

    /// <summary>월드 좌표의 경고 영역과 세 레인의 안전 경계를 준비한다.</summary>
    public void Prepare(Bounds field, Bounds area, bool fromTop, float safeRatio)
    {
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;
        danger.transform.position = area.center;
        danger.transform.localScale = new Vector3(area.size.x, area.size.y, 1f);
        danger.enabled = true;
        arrow.enabled = true;
        float sign = fromTop ? -1f : 1f;
        // ##TEMP 임시 화살표 모양. 맵 높이 비율로 배치한다.
        Vector3 tip = new Vector3(area.center.x, field.center.y - sign * field.size.y * 0.25f, 0f);
        float length = field.size.y * 0.12f;
        arrow.positionCount = 5;
        arrow.SetPositions(new[]
        {
            tip + new Vector3(-length * 0.3f, -sign * length * 0.3f, 0f),
            tip,
            tip + new Vector3(length * 0.3f, -sign * length * 0.3f, 0f),
            tip,
            tip + new Vector3(0f, -sign * length, 0f)
        });
        float laneWidth = field.size.x / 3f;
        for (int i = 0; i < safeBoundaries.Length; i++)
        {
            float edge = i % 2 == 0 ? safeRatio : 1f - safeRatio;
            float x = field.min.x + laneWidth * (i / 2 + edge);
            safeBoundaries[i].positionCount = 2;
            safeBoundaries[i].SetPosition(0, new Vector3(x, field.min.y, 0f));
            safeBoundaries[i].SetPosition(1, new Vector3(x, field.max.y, 0f));
        }
    }

    /// <summary>등장 후 안전 경계를 남기고 경고 영역과 화살표를 숨긴다.</summary>
    public void HideAttackWarning()
    {
        danger.enabled = false;
        arrow.enabled = false;
    }
}
