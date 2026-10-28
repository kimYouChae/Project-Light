using System;
using UnityEngine;

/// <summary>발사 시점의 보스 위치를 향해 포물선으로 이동하는 풀링 탄.</summary>
public sealed class PlayerBullet : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D attackLight;
    private float baseLightIntensity;
    private bool lightIntensityCached;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 arcDirection;
    private float flightDuration;
    private float elapsed;
    private float arcHeight;
    private float bulletRadius;
    private int damage;
    private BossHealth targetBoss;
    private ComponentPool<PlayerBullet> pool;
    private Action<PlayerBullet> returned;
    private bool prepared;

    /// <summary>대여 탄의 위치, 궤적, 피해와 반환 상태를 초기화한다.</summary>
    public void Prepare(Vector3 start, BossHealth target, float speed, float height,
        float radius, int hitDamage, float lightMultiplier,
        Bounds fieldBounds, ComponentPool<PlayerBullet> sourcePool,
        Action<PlayerBullet> onReturned)
    {
        startPosition = start;
        endPosition = target.transform.position;
        Vector2 direction = endPosition - startPosition;
        Vector2 normalized = direction.normalized;
        arcDirection = new Vector3(-normalized.y, normalized.x, 0f);
        flightDuration = Mathf.Max(0.01f, direction.magnitude / Mathf.Max(0.01f, speed));
        elapsed = 0f;
        arcHeight = height;
        bulletRadius = radius;
        // 양쪽 궤적의 경계 이탈량을 비교한다. 같으면 기존 방향을 유지한다.
        float originalOverflow = GetOverflow(arcDirection, fieldBounds);
        float oppositeOverflow = GetOverflow(-arcDirection, fieldBounds);
        if (oppositeOverflow < originalOverflow)
            arcDirection = -arcDirection;
        damage = hitDamage;
        targetBoss = target;
        pool = sourcePool;
        returned = onReturned;
        transform.position = startPosition;
        if (visual != null)
            visual.localScale = Vector3.one * (radius * 2f);
        CacheLightIntensity();
        if (attackLight != null)
            attackLight.intensity = baseLightIntensity * lightMultiplier;
        prepared = true;
    }

    private void CacheLightIntensity()
    {
        if (lightIntensityCached || attackLight == null)
            return;
        baseLightIntensity = attackLight.intensity;
        lightIntensityCached = true;
    }

    private float GetOverflow(Vector3 bend, Bounds bounds)
    {
        float overflowX = GetAxisOverflow(
            startPosition.x, endPosition.x, bend.x * arcHeight,
            bounds.min.x + bulletRadius, bounds.max.x - bulletRadius);
        float overflowY = GetAxisOverflow(
            startPosition.y, endPosition.y, bend.y * arcHeight,
            bounds.min.y + bulletRadius, bounds.max.y - bulletRadius);
        return overflowX + overflowY;
    }

    private float GetAxisOverflow(
        float start, float end, float offset, float min, float max)
    {
        float lowest = Mathf.Min(start, end);
        float highest = Mathf.Max(start, end);
        // p(t) = start + (end-start)t + 4*offset*t*(1-t)의 극점.
        if (Mathf.Abs(offset) > Mathf.Epsilon)
        {
            float t = (end - start + 4f * offset) / (8f * offset);
            if (t > 0f && t < 1f)
            {
                float value = Mathf.Lerp(start, end, t)
                    + 4f * offset * t * (1f - t);
                lowest = Mathf.Min(lowest, value);
                highest = Mathf.Max(highest, value);
            }
        }
        return Mathf.Max(0f, min - lowest)
            + Mathf.Max(0f, highest - max);
    }

    private void Update()
    {
        if (!prepared)
            return;
        if (targetBoss == null || targetBoss.IsDefeated)
        {
            ReturnToPool();
            return;
        }
        Vector3 previousPosition = transform.position;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);
        transform.position = Vector3.Lerp(startPosition, endPosition, t)
            + arcDirection * (4f * arcHeight * t * (1f - t));
        if (HitsBoss(previousPosition, transform.position))
        {
            targetBoss.TryTakeDamage(damage);
            ReturnToPool();
        }
        else if (t >= 1f)
            ReturnToPool();
    }

    private bool HitsBoss(Vector2 from, Vector2 to)
    {
        Vector2 segment = to - from;
        Vector2 center = targetBoss.transform.position;
        float t = segment.sqrMagnitude > 0f
            ? Mathf.Clamp01(Vector2.Dot(center - from, segment) / segment.sqrMagnitude) : 0f;
        float radius = targetBoss.HitRadius + bulletRadius;
        return (center - (from + segment * t)).sqrMagnitude <= radius * radius;
    }

    /// <summary>대여 중인 탄을 한 번만 풀에 반환한다.</summary>
    public void ReturnToPool()
    {
        if (!prepared)
            return;
        prepared = false;
        Action<PlayerBullet> callback = returned;
        returned = null;
        targetBoss = null;
        callback?.Invoke(this);
        pool.Return(this);
    }
}
