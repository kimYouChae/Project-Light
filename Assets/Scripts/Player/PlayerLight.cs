using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PlayerLight : MonoBehaviour
{
    [Header("월드 좌표 반경")]
    // ##TEMP 코어와 헤일로의 초기 반경. 게임 판정의 기준값이다.
    [SerializeField, Min(0.01f)] private float coreRadius = 0.8f;
    [SerializeField, Min(0f)] private float haloRadius = 3f;

    [Header("표시 참조")]
    [SerializeField] private LineRenderer coreLine;
    [SerializeField] private Light2D haloLight;

    [Header("표시 설정")]
    // ##TEMP 선의 굵기, 분할 수, 색상과 빛의 강도·감쇠 설정.
    [SerializeField, Min(0.001f)] private float lineWidth = 0.035f;
    [SerializeField, Range(16, 256)] private int segmentCount = 96;
    [SerializeField] private Color coreColor = new Color(1f, 0.87f, 0.45f, 1f);
    [SerializeField] private Color haloColor = new Color(1f, 0.83f, 0.46f, 1f);
    [SerializeField, Min(0f)] private float haloIntensity = 1.5f;
    [SerializeField, Range(0f, 1f)] private float innerRadiusRatio = 0.4f;

    private float healthRatio = 1f;

    /// <summary>퍼펙트 회피 판정에 사용하는 고정 코어의 월드 반경.</summary>
    public float CoreRadius => coreRadius;

    /// <summary>시야와 체력에 사용하는 헤일로의 월드 반경.</summary>
    public float HaloRadius => haloRadius * healthRatio;

    /// <summary>코어는 유지하고 최대 헤일로 반경에 남은 체력 비율을 적용한다.</summary>
    public void SetHealthRatio(float ratio)
    {
        healthRatio = Mathf.Clamp01(ratio);
        UpdateHalo();
    }

    /// <summary>원형 탄이 추가 허용 폭을 포함한 코어 경계에 걸치는지 검사한다.</summary>
    public bool IsOnCoreBoundary(
        Vector2 bulletPosition, float bulletRadius, float tolerance = 0f)
    {
        float distance = Vector2.Distance(transform.position, bulletPosition);
        float allowedWidth = Mathf.Max(0f, bulletRadius) + Mathf.Max(0f, tolerance);
        return Mathf.Abs(distance - coreRadius) <= allowedWidth;
    }

    /// <summary>지정한 월드 좌표가 헤일로 안쪽인지 반환한다.</summary>
    public bool IsInsideHalo(Vector2 worldPosition)
    {
        float currentRadius = HaloRadius;
        return currentRadius > 0f && (worldPosition - (Vector2)transform.position).sqrMagnitude <= currentRadius * currentRadius;
    }

    private void LateUpdate()
    {
        // 표시는 판정용 반경으로 만들고 Light2D 값은 판정에 사용하지 않는다.
        if (coreLine != null)
        {
            coreLine.useWorldSpace = true;
            coreLine.loop = true;
            coreLine.positionCount = segmentCount;
            coreLine.widthMultiplier = lineWidth;
            coreLine.startColor = coreColor;
            coreLine.endColor = coreColor;
            Vector3 center = transform.position;
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / segmentCount;
                coreLine.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * coreRadius);
            }
        }

        UpdateHalo();
    }

    private void UpdateHalo()
    {
        if (haloLight != null)
        {
            float currentRadius = HaloRadius;
            haloLight.lightType = Light2D.LightType.Point;
            haloLight.transform.position = transform.position;
            haloLight.pointLightOuterRadius = currentRadius;
            haloLight.pointLightInnerRadius = currentRadius * innerRadiusRatio;
            haloLight.color = haloColor;
            haloLight.intensity = haloIntensity;
            haloLight.enabled = currentRadius > 0f;
        }
    }

    private void OnValidate()
    {
        coreRadius = Mathf.Max(0.01f, coreRadius);
        haloRadius = Mathf.Max(0f, haloRadius);
        lineWidth = Mathf.Max(0.001f, lineWidth);
        segmentCount = Mathf.Clamp(segmentCount, 16, 256);
        haloIntensity = Mathf.Max(0f, haloIntensity);
        innerRadiusRatio = Mathf.Clamp01(innerRadiusRatio);
    }
}

