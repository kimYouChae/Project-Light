using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>코드에서 전달한 중심과 반경에 맞춰 서치라이트를 표시합니다.</summary>
public sealed class HeadBossSearchlightVisual : MonoBehaviour
{
    private static readonly int ShadowVertices = Shader.PropertyToID("_ShadowVertices");
    private static readonly int ShadowVertexCount = Shader.PropertyToID("_ShadowVertexCount");

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Light2D beamLight;
    [SerializeField] private SpriteRenderer leftEyeSprite;
    [SerializeField] private SpriteRenderer rightEyeSprite;
    // ##TEMP 수축 완료 시 눈 조명에 더할 밝기.
    [SerializeField, Min(0f)] private float focusIntensity = 1.5f;

    private bool showingFocus;
    private float baseBeamIntensity;
    private Color baseLeftEyeColor;
    private Color baseRightEyeColor;

    private MaterialPropertyBlock shadowProperties;
    private GraphicsBuffer shadowBuffer;

    /// <summary>이 원에 사용할 월드 좌표 그림자 삼각형을 전달합니다.</summary>
    public void SetShadowTriangles(List<Vector4> vertices)
    {
        if (spriteRenderer == null || (shadowBuffer == null && vertices.Count == 0 && !isActiveAndEnabled))
        {
            return;
        }

        if (shadowProperties == null)
        {
            shadowProperties = new MaterialPropertyBlock();
        }

        int count = vertices.Count;
        if (shadowBuffer == null || shadowBuffer.count < count)
        {
            shadowBuffer?.Dispose();
            int capacity = Mathf.NextPowerOfTwo(Mathf.Max(1, count));
            shadowBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, capacity, sizeof(float) * 4);
        }

        if (count > 0)
        {
            shadowBuffer.SetData(vertices);
        }

        spriteRenderer.GetPropertyBlock(shadowProperties);
        shadowProperties.SetBuffer(ShadowVertices, shadowBuffer);
        shadowProperties.SetInteger(ShadowVertexCount, count);
        spriteRenderer.SetPropertyBlock(shadowProperties);
    }

    private void OnDestroy()
    {
        shadowBuffer?.Dispose();
    }

    /// <summary>지정한 월드 중심과 반경으로 서치라이트를 표시합니다.</summary>
    public void Show(Vector2 worldCenter, float radius)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || radius <= 0f)
        {
            Hide();
            return;
        }

        // 기존 표시 깊이를 유지하면서 원 중심을 옮긴다.
        Vector3 position = transform.position;
        position.x = worldCenter.x;
        position.y = worldCenter.y;
        transform.position = position;

        // 부모 배율이 1인 HeadBoss 프리팹에서 스프라이트 지름을 맞춘다.
        Vector3 spriteSize = spriteRenderer.sprite.bounds.size;
        float diameter = radius * 2f;
        transform.localScale = new Vector3(
            diameter / spriteSize.x,
            diameter / spriteSize.y,
            1f);

        spriteRenderer.enabled = true;

        if (beamLight != null)
        {
            Vector2 direction = worldCenter - (Vector2)beamLight.transform.position;
            float distance = direction.magnitude;
            if (distance > 0f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                beamLight.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }

            float outerAngle = distance > radius
                ? 2f * Mathf.Asin(radius / distance) * Mathf.Rad2Deg
                : 360f;

            beamLight.pointLightOuterAngle = outerAngle;
            beamLight.pointLightInnerAngle = outerAngle * 0.8f;
            beamLight.pointLightOuterRadius = distance + radius;
            beamLight.pointLightInnerRadius = distance;
            beamLight.enabled = true;
        }
    }

    /// <summary>수축 진행률에 따라 포착한 눈을 밝힙니다.</summary>
    public void SetFocusProgress(float progress)
    {
        if (!showingFocus)
        {
            showingFocus = true;
            if (beamLight != null)
                baseBeamIntensity = beamLight.intensity;
            if (leftEyeSprite != null)
                baseLeftEyeColor = leftEyeSprite.color;
            if (rightEyeSprite != null)
                baseRightEyeColor = rightEyeSprite.color;
        }

        if (beamLight != null)
            beamLight.intensity = baseBeamIntensity + progress * focusIntensity;
        if (leftEyeSprite != null)
            leftEyeSprite.color = Color.Lerp(baseLeftEyeColor, Color.white, progress);
        if (rightEyeSprite != null)
            rightEyeSprite.color = Color.Lerp(baseRightEyeColor, Color.white, progress);
    }

    /// <summary>서치라이트 표시를 숨깁니다.</summary>
    public void Hide()
    {
        if (showingFocus)
        {
            if (beamLight != null)
                beamLight.intensity = baseBeamIntensity;
            if (leftEyeSprite != null)
                leftEyeSprite.color = baseLeftEyeColor;
            if (rightEyeSprite != null)
                rightEyeSprite.color = baseRightEyeColor;
            showingFocus = false;
        }

        if (beamLight != null)
        {
            beamLight.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }
}
