using UnityEngine;

/// <summary>코드에서 전달한 중심과 반경에 맞춰 서치라이트를 표시합니다.</summary>
public sealed class HeadBossSearchlightVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

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
    }

    /// <summary>서치라이트 표시를 숨깁니다.</summary>
    public void Hide()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }
}
