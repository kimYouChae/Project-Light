using System;
using UnityEngine;

/// <summary>몸체 피격과 수명·영역 밖 회수를 처리하는 풀링 비교용 직선 탄.</summary>
public sealed class TestBullet : MonoBehaviour
{
    [Header("퍼펙트 처리 표시")]
    [SerializeField] private SpriteRenderer spriteRenderer;
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
    private Vector2 previousTargetPosition;
    private BulletRegistry registry;
    private bool perfectProcessed;

    /// <summary>발사 당시의 풀. null이면 Instantiate/Destroy 모드다.</summary>
    public ComponentPool<TestBullet> SourcePool { get; private set; }

    /// <summary>탄의 월드 판정 반경.</summary>
    public float Radius => radius;

    /// <summary>이번 발사에서 이미 퍼펙트 처리됐는지 반환한다.</summary>
    public bool PerfectProcessed => perfectProcessed;

    /// <summary>활성 탄을 이번 발사에서 한 번만 퍼펙트 처리한다.</summary>
    public bool TryMarkPerfect()
    {
        if (!prepared || !isActiveAndEnabled || perfectProcessed)
            return false;

        perfectProcessed = true;
        if (spriteRenderer != null)
            spriteRenderer.color = perfectColor;
        return true;
    }

    /// <summary>비활성 상태에서 월드 위치·방향·속도·수명·크기와 반환 방식을 설정한다.</summary>
    public void Prepare(Vector3 position, Vector2 direction, float speed, float lifetime,
        float bulletRadius, Bounds bounds, ComponentPool<TestBullet> sourcePool, Action<TestBullet> onRelease,
        PlayerHealth target = null, BulletRegistry bulletRegistry = null)
    {
        if (registry != null)
            registry.Unregister(this);
        registry = bulletRegistry;
        perfectProcessed = false;
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
        previousTargetPosition = target != null ? (Vector2)target.transform.position : Vector2.zero;
        prepared = true;
    }

    private void OnEnable()
    {
        if (prepared && registry != null)
            registry.Register(this);
    }

    private void OnDisable()
    {
        if (registry != null)
            registry.Unregister(this);
    }

    private void Update()
    {
        if (!prepared)
            return;

        Vector2 previousPosition = transform.position;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        remainingLifetime -= Time.deltaTime;
        Vector3 position = transform.position;
        // 몸체 접촉은 피해만 검사한다. 퍼펙트·일반 피격으로 탄을 회수하지 않는다.
        TryHitPlayer(previousPosition, position);
        bool outside = position.x < playfieldBounds.min.x - radius || position.x > playfieldBounds.max.x + radius
            || position.y < playfieldBounds.min.y - radius || position.y > playfieldBounds.max.y + radius;
        if (remainingLifetime <= 0f || outside)
            Expire();
    }

    private bool TryHitPlayer(Vector2 previousPosition, Vector2 currentPosition)
    {
        // 퍼펙트 처리된 탄은 이번 발사가 끝날 때까지 몸체 피해 판정에서 제외한다.
        if (perfectProcessed || hitTarget == null || hitTarget.IsDead)
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
        // Destroy는 프레임 끝에 실행되므로 회수 시점에 목록에서 먼저 제외한다.
        if (registry != null)
            registry.Unregister(this);
        Action<TestBullet> callback = release;
        release = null;
        hitTarget = null;
        callback?.Invoke(this);
    }
}
