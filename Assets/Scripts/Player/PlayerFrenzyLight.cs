using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>체력 헤일로와 별도로 폭주 조명을 표시한다.</summary>
public sealed class PlayerFrenzyLight : MonoBehaviour
{
    [SerializeField] private PlayerFrenzy frenzy;
    [SerializeField] private Light2D frenzyLight;
    // ##TEMP 승인한 폭주 조명 시작값. 판정 반경에는 영향을 주지 않는다.
    [SerializeField] private Color lightColor = new Color(0.3f, 0.9f, 1f, 1f);
    [SerializeField, Min(0f)] private float outerRadius = 5f;
    [SerializeField, Min(0f)] private float innerRadius = 2f;
    [SerializeField, Min(0f)] private float intensity = 2f;

    private void LateUpdate()
    {
        if (frenzyLight == null)
            return;
        frenzyLight.enabled = frenzy != null && frenzy.IsActive;
        frenzyLight.lightType = Light2D.LightType.Point;
        frenzyLight.pointLightOuterRadius = outerRadius;
        frenzyLight.pointLightInnerRadius = innerRadius;
        frenzyLight.intensity = intensity;
        frenzyLight.color = lightColor;
        frenzyLight.transform.position = transform.position;
    }

    private void OnDisable()
    {
        if (frenzyLight != null)
            frenzyLight.enabled = false;
    }

    private void OnValidate()
    {
        outerRadius = Mathf.Max(0f, outerRadius);
        innerRadius = Mathf.Clamp(innerRadius, 0f, outerRadius);
        intensity = Mathf.Max(0f, intensity);
    }
}
