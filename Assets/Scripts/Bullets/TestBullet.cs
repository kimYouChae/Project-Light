using System;
using UnityEngine;

/// <summary>몸체 피격·퍼펙트 상태와 수명·영역 밖 회수를 처리하는 적탄.</summary>
public sealed class TestBullet : MonoBehaviour
{
    [Header("퍼펙트 처리 표시")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private EnemyBulletMovement movement;
    [SerializeField] private bool reactsToCoreProximity;
    // ##TEMP 퍼펙트 처리된 탄의 구분 색상. 인스펙터에서 조정한다.
    [SerializeField] private Color perfectColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private Color originalColor;
    private bool originalColorCached;
    private Vector2 velocity;
    private float remainingLifetime;
    private float radius;
    private Bounds playfieldBounds;
    private Action<TestBullet> release;
    private bool prepared;
    private PlayerHealth hitTarget;
    private PlayerLight targetLight;
    private PlayerPerfectDodge targetPerfectDodge;
    private PlayerCoreBoundaryView targetCoreBoundaryView;
    private Vector2 previousTargetPosition;
    private double segmentStartedAt;
    private double? lastCoreCrossingAt;

    public float CoreClosingSpeed { get; private set; }
    public Vector2 LastCoreContactDirection { get; private set; }
    private BulletRegistry registry;
    private bool perfectProcessed;
    private int damageOverride;
    private bool hitOnce;
    private bool hitApplied;
    private bool ending;
    private Vector3 absorptionStart;

    /// <summary>발사 당시의 풀. null이면 Instantiate/Destroy 모드다.</summary>
    public ComponentPool<TestBullet> SourcePool { get; private set; }

    /// <summary>탄의 월드 판정 반경.</summary>
    public float Radius => radius;

    /// <summary>이번 발사에서 이미 퍼펙트 처리됐는지 반환한다.</summary>
    public bool PerfectProcessed => perfectProcessed;

    /// <summary>패턴 발사기가 대여 준비 시 설정하는 이동 컴포넌트.</summary>
    public EnemyBulletMovement Movement => movement;

    /// <summary>이 발사가 지정한 플레이어의 코어 근접 반응 대상인지 반환합니다.</summary>
    public bool CanReactToCoreProximity(PlayerHealth player)
    {
        return reactsToCoreProximity && prepared && !ending && isActiveAndEnabled
            && hitTarget == player && !perfectProcessed && !hitApplied;
    }

    public Vector2 PredictPosition(float seconds)
    {
        return movement != null
            ? movement.EvaluatePosition(seconds)
            : (Vector2)transform.position + velocity * seconds;
    }

    /// <summary>활성 탄을 이번 발사에서 한 번만 퍼펙트 처리한다.</summary>
    public bool TryMarkPerfect()
    {
        if (!prepared || ending || !isActiveAndEnabled || perfectProcessed || hitApplied)
            return false;

        perfectProcessed = true;
        if (spriteRenderer != null)
            spriteRenderer.color = perfectColor;
        return true;
    }

    /// <summary>해당 플레이어를 향한 탄의 마지막 코어 진입 시각을 반환합니다.</summary>
    public bool TryGetLastCoreCrossing(PlayerHealth player, out double crossedAt)
    {
        crossedAt = lastCoreCrossingAt.GetValueOrDefault();
        return hitTarget == player && lastCoreCrossingAt.HasValue
            && prepared && !ending && isActiveAndEnabled && !perfectProcessed && !hitApplied;
    }

    /// <summary>이동·수명·피해·PERFECT 검사를 멈춘다.</summary>
    public void FreezeForEnding()
    {
        if (!prepared || ending)
            return;
        ending = true;
        absorptionStart = transform.position;
        hitTarget = null;
        targetLight = null;
        targetPerfectDodge = null;
        targetCoreBoundaryView = null;
        registry?.Unregister(this);
    }

    /// <summary>멈춘 위치에서 목적지까지 가속하며 이동하고 도착하면 반환한다.</summary>
    public void AdvanceAbsorption(Vector3 destination, float progress)
    {
        if (!prepared || !ending)
            return;
        float t = Mathf.Clamp01(progress);
        transform.position = Vector3.Lerp(absorptionStart, destination, t * t);
        if (t >= 1f)
            Expire();
    }

    /// <summary>비활성 상태에서 월드 위치·방향·속도·수명·크기와 반환 방식을 설정한다.</summary>
    public void Prepare(Vector3 position, Vector2 direction, float speed, float lifetime,
        float bulletRadius, Bounds bounds, ComponentPool<TestBullet> sourcePool, Action<TestBullet> onRelease,
        PlayerHealth target = null, BulletRegistry bulletRegistry = null,
        int damageOverride = 0, bool hitOnce = false)
    {
        if (registry != null)
            registry.Unregister(this);
        registry = bulletRegistry;
        ending = false;
        this.damageOverride = Mathf.Max(0, damageOverride);
        this.hitOnce = hitOnce;
        hitApplied = false;
        // 비활성 프리팹은 Awake 전에도 준비되므로 여기서 최초 색상을 확보한다.
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (!originalColorCached)
            {
                originalColor = spriteRenderer.color;
                originalColorCached = true;
            }
            spriteRenderer.color = originalColor;
        }
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
        targetLight = target != null ? target.GetComponent<PlayerLight>() : null;
        targetPerfectDodge = target != null ? target.GetComponent<PlayerPerfectDodge>() : null;
        targetCoreBoundaryView = reactsToCoreProximity && target != null
            ? target.GetComponent<PlayerCoreBoundaryView>() : null;
        previousTargetPosition = target != null ? (Vector2)target.transform.position : Vector2.zero;
        segmentStartedAt = Time.timeAsDouble;
        lastCoreCrossingAt = null;
        CoreClosingSpeed = 0f;
        LastCoreContactDirection = Vector2.zero;
        if (movement != null)
            movement.PrepareStraight(position, direction, speed);
        prepared = true;
    }

    private void OnEnable()
    {
        if (prepared && !ending && registry != null)
            registry.Register(this);
    }

    private void OnDisable()
    {
        if (registry != null)
            registry.Unregister(this);
    }

    private void Update()
    {
        // 필드 시간이 정지된 동안 이동·공격·판정·획득을 처리하지 않는다.
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (!prepared || ending)
            return;

        Vector2 previousPosition = transform.position;
        if (movement != null)
            transform.position = movement.Move(Time.deltaTime);
        else
        {
            Vector2 next = PredictPosition(Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }
        remainingLifetime -= Time.deltaTime;
        Vector3 position = transform.position;
        // 몸체 접촉은 피해만 검사한다. 퍼펙트·일반 피격으로 탄을 회수하지 않는다.
        double segmentEndedAt = Time.timeAsDouble;
        TryHitPlayer(previousPosition, position, segmentEndedAt);
        segmentStartedAt = segmentEndedAt;
        bool outside = position.x < playfieldBounds.min.x - radius || position.x > playfieldBounds.max.x + radius
            || position.y < playfieldBounds.min.y - radius || position.y > playfieldBounds.max.y + radius;
        if (remainingLifetime <= 0f || outside)
            Expire();
    }

    private bool TryHitPlayer(
        Vector2 previousPosition, Vector2 currentPosition, double segmentEndedAt)
    {
        // 퍼펙트 처리된 탄은 이번 발사가 끝날 때까지 몸체 피해 판정에서 제외한다.
        if (ending || perfectProcessed || (hitOnce && hitApplied) || hitTarget == null || hitTarget.IsDead)
            return false;

        Vector2 targetPosition = hitTarget.transform.position;
        if (hitTarget.IsHeldByBite)
        {
            CoreClosingSpeed = 0f;
            // 물림 중 위치를 갱신해 해제 후 이전 위치부터 다시 검사하지 않는다.
            previousTargetPosition = targetPosition;
            return false;
        }

        // 상대 이동 선분으로 검사해 한 프레임 사이에 몸체를 가로지르는 탄도 잡는다.
        Vector2 start = previousPosition - previousTargetPosition;
        Vector2 end = currentPosition - targetPosition;
        if (reactsToCoreProximity)
        {
            double interval = segmentEndedAt - segmentStartedAt;
            CoreClosingSpeed = interval > 0d
                ? (float)((start.magnitude - end.magnitude) / interval) : 0f;
        }

        previousTargetPosition = targetPosition;
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float progress = lengthSquared > 0f ? Mathf.Clamp01(-Vector2.Dot(start, segment) / lengthSquared) : 0f;
        Vector2 closest = start + segment * progress;
        float combinedRadius = radius + hitTarget.BodyRadius;
        bool hitBody = closest.sqrMagnitude <= combinedRadius * combinedRadius;
        bool bodyBeforeCore = hitBody && targetLight != null && combinedRadius >= targetLight.CoreRadius;
        if (!bodyBeforeCore)
            RecordCoreCrossing(start, segment, lengthSquared, segmentEndedAt);

        if (!hitBody || perfectProcessed)
            return false;

        bool damaged = damageOverride > 0
            ? hitTarget.TryTakeHit(damageOverride) : hitTarget.TryTakeHit();
        if (damaged)
            hitApplied = true;
        else if (bodyBeforeCore)
            RecordCoreCrossing(start, segment, lengthSquared, segmentEndedAt);

        return damaged;
    }

    private void RecordCoreCrossing(
        Vector2 start, Vector2 movement, float movementSquared, double segmentEndedAt)
    {
        if (hitApplied || registry == null || targetLight == null || targetPerfectDodge == null
            || movementSquared <= 0f)
        {
            return;
        }

        float coreRadius = targetLight.CoreRadius;
        float distanceSquared = start.sqrMagnitude - coreRadius * coreRadius;
        float approach = Vector2.Dot(start, movement);
        float discriminant = approach * approach - movementSquared * distanceSquared;
        if (distanceSquared < 0f || approach >= 0f || discriminant <= 0f)
            return;

        float fraction = distanceSquared / (-approach + Mathf.Sqrt(discriminant));
        if (fraction > 1f)
            return;

        double crossedAt = segmentStartedAt + (segmentEndedAt - segmentStartedAt) * fraction;
        lastCoreCrossingAt = crossedAt;
        if (reactsToCoreProximity)
        {
            LastCoreContactDirection = (start + movement * fraction).normalized;
            if (targetCoreBoundaryView != null)
                targetCoreBoundaryView.NotifyBulletContact(LastCoreContactDirection);
        }

        targetPerfectDodge.OnBulletCoreCrossed(this, crossedAt);
    }

    /// <summary>발사할 때 등록한 방식으로 한 번만 회수한다.</summary>
    public void Expire()
    {
        if (!prepared)
            return;

        prepared = false;
        targetLight = null;
        targetPerfectDodge = null;
        targetCoreBoundaryView = null;
        lastCoreCrossingAt = null;
        // Destroy는 프레임 끝에 실행되므로 회수 시점에 목록에서 먼저 제외한다.
        if (registry != null)
            registry.Unregister(this);
        Action<TestBullet> callback = release;
        release = null;
        hitTarget = null;
        callback?.Invoke(this);
    }
}
