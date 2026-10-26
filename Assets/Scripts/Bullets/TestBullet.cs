using System;
using UnityEngine;

/// <summary>풀링 비교용 직선 탄. 피격과 피해 처리는 아직 연결하지 않는다.</summary>
public sealed class TestBullet : MonoBehaviour
{
    private Vector2 velocity;
    private float remainingLifetime;
    private float radius;
    private Bounds playfieldBounds;
    private Action<TestBullet> release;
    private bool prepared;

    /// <summary>발사 당시의 풀. null이면 Instantiate/Destroy 모드다.</summary>
    public ComponentPool<TestBullet> SourcePool { get; private set; }

    /// <summary>비활성 상태에서 월드 위치·방향·속도·수명·크기와 반환 방식을 설정한다.</summary>
    public void Prepare(Vector3 position, Vector2 direction, float speed, float lifetime,
        float bulletRadius, Bounds bounds, ComponentPool<TestBullet> sourcePool, Action<TestBullet> onRelease)
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
        prepared = true;
    }

    private void Update()
    {
        if (!prepared)
            return;

        transform.position += (Vector3)(velocity * Time.deltaTime);
        remainingLifetime -= Time.deltaTime;
        Vector3 position = transform.position;
        bool outside = position.x < playfieldBounds.min.x - radius || position.x > playfieldBounds.max.x + radius
            || position.y < playfieldBounds.min.y - radius || position.y > playfieldBounds.max.y + radius;
        if (remainingLifetime <= 0f || outside)
            Expire();
    }

    /// <summary>발사할 때 등록한 방식으로 한 번만 회수한다.</summary>
    public void Expire()
    {
        if (!prepared)
            return;

        prepared = false;
        Action<TestBullet> callback = release;
        release = null;
        callback?.Invoke(this);
    }
}
