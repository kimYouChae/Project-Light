using UnityEngine;

/// <summary>헤일로 경계에서 플레이어 방향을 가리키는 수풀 통과 탄 경고입니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class BushBulletWarning : MonoBehaviour
{
    [SerializeField] private LineRenderer warningLine;

    // ##TEMP 수풀 1-4 꺾쇠의 기본 비율과 고정 선 굵기입니다. 팔 크기는 탄 지름과 거리로 정합니다.
    [SerializeField, Min(0.001f)] private float warningLength = 0.2f;
    [SerializeField, Min(0.001f)] private float warningWidth = 0.24f;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.035f;

    // ##TEMP 헤일로 경계 바깥 3유닛부터 최소 크기 경고를 표시합니다.
    [SerializeField, Min(0.001f)] private float warningStartDistance = 3f;
    // ##TEMP 최소 폭은 해당 탄 지름의 35%입니다.
    [SerializeField, Range(0.01f, 1f)] private float minimumSizeRatio = 0.35f;
    // ##TEMP 1은 선형 성장입니다. 1보다 크면 경계 가까이에서 더 빠르게 커집니다.
    [SerializeField, Min(0.01f)] private float growthExponent = 1f;

    /// <summary>풀 대여 전에 표시 크기와 월드 위치·방향을 준비합니다.</summary>
    public void Prepare(
        Vector3 tipPosition, Vector2 outwardDirection,
        float bulletRadius, float distanceToHalo)
    {
        if (warningLine == null)
            warningLine = GetComponent<LineRenderer>();

        // 로컬 원점이 플레이어를 향하는 꼭짓점이고 +Y가 헤일로 바깥 방향입니다.
        warningLine.useWorldSpace = false;
        warningLine.loop = false;
        warningLine.positionCount = 3;
        warningLine.startColor = Color.red;
        warningLine.endColor = Color.red;
        warningLine.enabled = true;

        SetPose(tipPosition, outwardDirection, bulletRadius, distanceToHalo);
    }

    /// <summary>관리자가 현재 경계 위치·방향과 탄 반경·거리로 표시를 갱신합니다.</summary>
    public void SetPose(
        Vector3 tipPosition, Vector2 outwardDirection,
        float bulletRadius, float distanceToHalo)
    {
        transform.position = tipPosition;
        transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(outwardDirection.y, outwardDirection.x)
                * Mathf.Rad2Deg - 90f);
        transform.localScale = Vector3.one;
        SetSize(bulletRadius, distanceToHalo);
    }

    private void SetSize(float bulletRadius, float distanceToHalo)
    {
        float startDistance = Mathf.Max(0.001f, warningStartDistance);
        float gap = Mathf.Max(0f, distanceToHalo);
        warningLine.enabled = gap <= startDistance;

        if (!warningLine.enabled)
            return;

        // ##TEMP 현재 경계까지의 거리로 크기를 정합니다. 시간 누적이나 궤적 예측은 하지 않습니다.
        float progress = 1f - Mathf.Clamp01(gap / startDistance);
        float growth = Mathf.Pow(progress, Mathf.Max(0.01f, growthExponent));
        float sizeRatio = Mathf.Lerp(
            Mathf.Clamp(minimumSizeRatio, 0.01f, 1f), 1f, growth);
        float diameter = Mathf.Max(0f, bulletRadius) * 2f * sizeRatio;

        float baseLength = Mathf.Max(0.001f, warningLength);
        float baseWidth = Mathf.Max(0.001f, warningWidth);
        float halfWidth = baseWidth * 0.5f;
        float armLength = Mathf.Sqrt(
            halfWidth * halfWidth + baseLength * baseLength);

        // 선 굵기는 고정하고, 캡 없는 양 끝 선의 굵기가 차지하는 가로 폭까지 포함해 탄 지름에 맞춥니다.
        float lineWidthContribution = lineWidth * baseLength / armLength;
        // 목표 폭이 선 굵기 성분보다 작으면 배율을 0으로 제한해 팔 길이를 0으로 처리합니다.
        float scale = Mathf.Max(0f, (diameter - lineWidthContribution) / baseWidth);

        warningLine.startWidth = lineWidth;
        warningLine.endWidth = lineWidth;
        warningLine.SetPosition(
            0, new Vector3(-halfWidth * scale, baseLength * scale, 0f));
        warningLine.SetPosition(1, Vector3.zero);
        warningLine.SetPosition(
            2, new Vector3(halfWidth * scale, baseLength * scale, 0f));
    }
}
