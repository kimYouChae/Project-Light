using UnityEngine;

/// <summary>레인 꼬리의 등장·유지·회수와 몸체 접촉 피해를 처리한다.</summary>
public sealed class TailLaneHazard : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    private Bounds field;
    private Bounds area;
    private PlayerHealth playerHealth;
    private Vector3 insidePosition;
    private Vector3 outsidePosition;
    private float holdDuration;
    private float travelDuration;
    private int damage;

    /// <summary>등장 방향과 영역·피해를 설정하고 이전 대여 상태를 초기화한다.</summary>
    public void Prepare(Bounds fieldBounds, Bounds dangerBounds, bool fromTop,
        float holdTime, float travelTime, int hitDamage, PlayerHealth target)
    {
        field = fieldBounds;
        area = dangerBounds;
        insidePosition = area.center;
        outsidePosition = insidePosition + Vector3.up * (fromTop ? field.size.y : -field.size.y);
        holdDuration = Mathf.Max(0f, holdTime);
        travelDuration = Mathf.Max(0.01f, travelTime);
        damage = hitDamage;
        playerHealth = target;
        transform.localScale = Vector3.one;
        UpdatePose(0f);
    }

    /// <summary>등장 시작 후 경과 시간으로 연출과 현재 접촉 피해를 갱신한다.</summary>
    public void Advance(float elapsed)
    {
        Bounds hitBounds = UpdatePose(elapsed);
        if (hitBounds.size.x <= 0f || hitBounds.size.y <= 0f || playerHealth == null)
            return;
        Vector2 position = playerHealth.transform.position;
        Vector2 closest = new Vector2(
            Mathf.Clamp(position.x, hitBounds.min.x, hitBounds.max.x),
            Mathf.Clamp(position.y, hitBounds.min.y, hitBounds.max.y));
        float radius = playerHealth.BodyRadius;
        if ((position - closest).sqrMagnitude <= radius * radius)
            playerHealth.TryTakeHit(damage);
    }

    private Bounds UpdatePose(float elapsed)
    {
        Vector3 position;
        if (elapsed < travelDuration)
            position = Vector3.Lerp(outsidePosition, insidePosition, elapsed / travelDuration);
        else if (elapsed < travelDuration + holdDuration)
            position = insidePosition;
        else
            position = Vector3.Lerp(insidePosition, outsidePosition,
                (elapsed - travelDuration - holdDuration) / travelDuration);
        transform.position = position;
        float bottom = Mathf.Max(field.min.y, position.y - area.extents.y);
        float top = Mathf.Min(field.max.y, position.y + area.extents.y);
        float height = Mathf.Max(0f, top - bottom);
        // 화면 밖으로 나온 부분은 표시와 피해 영역 모두에서 제외한다.
        Bounds visible = new Bounds(new Vector3(area.center.x, (bottom + top) * 0.5f, 0f),
            new Vector3(area.size.x, height, 0f));
        visual.enabled = height > 0f;
        visual.transform.position = visible.center;
        visual.transform.localScale = new Vector3(area.size.x, height, 1f);
        return visible;
    }
}
