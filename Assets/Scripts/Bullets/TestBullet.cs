using System;
using UnityEngine;

/// <summary>몸체 피격과 수명·영역 밖 회수를 처리하는 풀링 비교용 직선 탄.</summary>
public sealed class TestBullet : MonoBehaviour
{
    private Vector2 velocity;
    private float remainingLifetime;
    private float radius;
    private Bounds playfieldBounds;
    private Action<TestBullet> release;
    private bool prepared;
    private PlayerHealth hitTarget;
    private Vector2 previousTargetPosition;

    /// <summary>발사 당시의 풀. null이면 Instantiate/Destroy 모드다.</summary>
    public ComponentPool<TestBullet> SourcePool { get; private set; }

    /// <summary>비활성 상태에서 월드 위치·방향·속도·수명·크기와 반환 방식을 설정한다.</summary>
    public void Prepare(Vector3 position, Vector2 direction, float speed, float lifetime,
        float bulletRadius, Bounds bounds, ComponentPool<TestBullet> sourcePool, Action<TestBullet> onRelease,
        PlayerHealth target = null)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * (bulletRadius * 2f);
        velocity = direction.normalized * speed;
        remainingLifetime = lifetime;
        radius = bulletRadius;
        playfieldBounds = bounds;
        SourcePool = sourcePool;
        release = onRelease;
        hitTarget = target;
        previousTargetPosition = target != null ? (Vector2)target.transform.position : Vector2.zero;
        prepared = true;
    }

    private void Update()
    {
        if (!prepared)
            return;

        Vector2 previousPosition = transform.position;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        remainingLifetime -= Time.deltaTime;
        Vector3 position = transform.position;
        if (TryHitPlayer(previousPosition, position))
        {
            Expire();
            return;
        }
        bool outside = position.x < playfieldBounds.min.x - radius || position.x > playfieldBounds.max.x + radius
            || position.y < playfieldBounds.min.y - radius || position.y > playfieldBounds.max.y + radius;
        if (remainingLifetime <= 0f || outside)
            Expire();
    }

    private bool TryHitPlayer(Vector2 previousPosition, Vector2 currentPosition)
    {
        if (hitTarget == null || hitTarget.IsDead)
            return false;

        // 상대 이동 선분으로 검사해 한 프레임 사이에 몸체를 가로지르는 탄도 잡는다.
        Vector2 targetPosition = hitTarget.transform.position;
        Vector2 start = previousPosition - previousTargetPosition;
        Vector2 end = currentPosition - targetPosition;
        previousTargetPosition = targetPosition;
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float progress = lengthSquared > 0f ? Mathf.Clamp01(-Vector2.Dot(start, segment) / lengthSquared) : 0f;
        Vector2 closest = start + segment * progress;
        float combinedRadius = radius + hitTarget.BodyRadius;
        return closest.sqrMagnitude <= combinedRadius * combinedRadius && hitTarget.TryTakeHit();
    }

    /// <summary>발사할 때 등록한 방식으로 한 번만 회수한다.</summary>
    public void Expire()
    {
        if (!prepared)
            return;

        prepared = false;
        Action<TestBullet> callback = release;
        release = null;
        hitTarget = null;
        callback?.Invoke(this);
    }
}
