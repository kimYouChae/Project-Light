using System;
using UnityEngine;

/// <summary>발사 시점의 보스 위치를 향해 포물선으로 이동하는 풀링 탄.</summary>
public sealed class PlayerBullet : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D attackLight;
    // ##TEMP 돌진 시작 위치에 도착했을 때의 빛 번짐 시간.
    [SerializeField, Min(0.01f)] private float dashFlashDuration = 0.12f;
    private float baseLightIntensity;
    private bool lightIntensityCached;
    private float baseLightOuterRadius;
    private float baseLightInnerRadius;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 arcDirection;
    private float flightDuration;
    private float elapsed;
    private float arcHeight;
    private float bulletRadius;
    private int damage;
    private BossHealth targetBoss;
    private PlayerHealth sourcePlayer;
    private ComponentPool<PlayerBullet> pool;
    private Action<PlayerBullet> returned;
    private bool prepared;
    private bool redirected;
    private float dashFlashRemaining;
    private float dashFlashIntensity;
    private float dashFlashRadius;

    // 이 탄의 직접 적중 알림을 전달할 컴포넌트
    private PlayerAttackEvents attackEvents;

    // 발사 당시의 공격 구분
    private AttackTargetType attackType;

    /// <summary>대여 탄의 위치, 궤적, 피해와 반환 상태를 초기화한다.</summary>
    public void Prepare(
        Vector3 start,
        BossHealth target,
        float speed,
        float height,
        float radius,
        int hitDamage,
        float lightMultiplier,
        Bounds fieldBounds,
        ComponentPool<PlayerBullet> sourcePool,
        Action<PlayerBullet> onReturned,
        PlayerAttackEvents sourceEvents,
        AttackTargetType sourceAttackType,
        float sightRadiusMultiplier = 1f,
        PlayerHealth sourcePlayer = null)
    {
        startPosition = start;
        redirected = false;
        dashFlashRemaining = 0f;
        endPosition = target.transform.position;
        Vector2 direction = endPosition - startPosition;
        Vector2 normalized = direction.normalized;
        arcDirection = new Vector3(-normalized.y, normalized.x, 0f);
        flightDuration = Mathf.Max(0.01f, direction.magnitude / Mathf.Max(0.01f, speed));
        elapsed = 0f;
        arcHeight = height;
        bulletRadius = radius;
        // 양쪽 궤적의 경계 이탈량을 비교한다.
        float originalOverflow = GetOverflow(arcDirection, fieldBounds);
        float oppositeOverflow = GetOverflow(-arcDirection, fieldBounds);
        bool originalInside = originalOverflow <= 0f;
        bool oppositeInside = oppositeOverflow <= 0f;
        if (originalInside && oppositeInside)
        {
            // 양쪽 모두 맵 안에 들어오면 발사마다 좌·우를 랜덤 선택한다.
            if (UnityEngine.Random.value < 0.5f)
                arcDirection = -arcDirection;
        }
        else if (oppositeOverflow < originalOverflow)
        {
            // 가장자리에서는 이탈량이 적은 방향을 선택한다.
            arcDirection = -arcDirection;
        }
        damage = hitDamage;
        targetBoss = target;
        this.sourcePlayer = sourcePlayer;
        pool = sourcePool;
        returned = onReturned;
        transform.position = startPosition;
        if (visual != null)
            visual.localScale = Vector3.one * (radius * 2f);
        CacheLightIntensity();
        if (attackLight != null)
        {
            attackLight.intensity = baseLightIntensity * lightMultiplier;
            // 카드 확대분을 원래 빛 반경에 적용해 재대여 때 누적되지 않게 한다.
            attackLight.pointLightOuterRadius = baseLightOuterRadius * sightRadiusMultiplier;
            attackLight.pointLightInnerRadius = baseLightInnerRadius * sightRadiusMultiplier;
        }
        // 풀 대여마다 이전 발사 정보를 덮어쓴다.
        attackEvents = sourceEvents;
        attackType = sourceAttackType;
        prepared = true;
    }

    private void CacheLightIntensity()
    {
        if (lightIntensityCached || attackLight == null)
            return;
        baseLightIntensity = attackLight.intensity;
        baseLightOuterRadius = attackLight.pointLightOuterRadius;
        baseLightInnerRadius = attackLight.pointLightInnerRadius;
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
        // 필드 시간이 정지된 동안 이동·공격·판정·획득을 처리하지 않는다.
        if (Time.timeScale <= 0f || !prepared
            || (sourcePlayer != null && sourcePlayer.IsFieldShrinkCutsceneActive))
        {
            return;
        }

        if (dashFlashRemaining > 0f)
        {
            AdvanceDashFlash();
            return;
        }

        if (!redirected && (targetBoss == null || targetBoss.IsDefeated))
        {
            ReturnToPool();
            return;
        }
        Vector3 previousPosition = transform.position;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);
        transform.position = Vector3.Lerp(startPosition, endPosition, t)
            + arcDirection * (4f * arcHeight * t * (1f - t));
        if (!redirected && HitsBoss(previousPosition, transform.position))
        {
            // 직접 피해가 적용된 경우에만 적중 효과 알림을 보낸다.
            if (targetBoss.TryTakePlayerAttackDamage(damage)
                && attackEvents != null)
            {
                attackEvents.RaiseHit(targetBoss, attackType);
            }

            ReturnToPool();
        }
        else if (t >= 1f)
        {
            if (redirected && attackLight != null)
            {
                dashFlashRemaining = dashFlashDuration;
                dashFlashIntensity = attackLight.intensity;
                dashFlashRadius = attackLight.pointLightOuterRadius;
            }
            else
            {
                ReturnToPool();
            }
        }
    }

    /// <summary>해당 보스를 향하던 탄을 돌진 시작 위치로 보냅니다.</summary>
    public void RedirectToDashOrigin(BossHealth boss, Vector3 headPosition)
    {
        if (!prepared || redirected || targetBoss != boss)
            return;

        redirected = true;
        startPosition = transform.position;
        endPosition = headPosition;
        Vector2 direction = endPosition - startPosition;
        Vector2 normal = direction.normalized;
        Vector3 bend = new Vector3(-normal.y, normal.x, 0f);
        arcDirection = Vector3.Dot(bend, arcDirection) < 0f ? -bend : bend;
        arcHeight *= Mathf.Clamp01(1f - elapsed / flightDuration);
        flightDuration = Mathf.Max(0.01f, flightDuration - elapsed);
        elapsed = 0f;
    }

    private void AdvanceDashFlash()
    {
        dashFlashRemaining = Mathf.Max(0f, dashFlashRemaining - Time.deltaTime);
        float progress = 1f - dashFlashRemaining / dashFlashDuration;
        attackLight.intensity = dashFlashIntensity * (1f - progress);
        attackLight.pointLightOuterRadius = dashFlashRadius * (1f + progress);
        if (dashFlashRemaining <= 0f)
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
        // 이전 발사의 이벤트 참조와 공격 구분을 제거한다.
        attackEvents = null;
        attackType = AttackTargetType.None;
        Action<PlayerBullet> callback = returned;
        returned = null;
        targetBoss = null;
        sourcePlayer = null;
        callback?.Invoke(this);
        pool.Return(this);
    }
}
