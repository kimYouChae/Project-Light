using System.Collections.Generic;
using UnityEngine;

/// <summary>서치라이트 안의 이동을 감지하고 굳기 부채꼴을 발사합니다.</summary>
public sealed class HeadBossFreezePattern : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private TestBullet bulletPrefab;

    // ##TEMP 승인한 감지 시작값.
    [SerializeField, Min(0.01f)] private float duration = 2f;
    [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
    // ##TEMP 이동 거리 비교에 사용하는 시간 구간.
    [SerializeField, Min(0.01f)] private float movementSampleWindow = 0.1f;

    private const int FREEZE_BULLET_COUNT = 3;

    // ##TEMP 미끼 궤도와 가운데 탄의 프로토타입 시작값.
    [SerializeField, Min(0.01f)] private float decoyClearance = 0.1f;
    [SerializeField, Range(0.01f, 0.49f)] private float centerArrivalDelay = 0.15f;
    [SerializeField, Min(0.01f)] private float centerBulletSpeed = 4f;
    [SerializeField, Min(0.01f)] private float bulletRadius = 0.12f;

    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerLight playerLight;
    private PlayerPerfectDodge perfectDodge;
    private SpriteRenderer playfield;
    private BulletRegistry bulletRegistry;
    private Transform bulletParent;
    private ComponentPool<TestBullet> pool;

    private readonly HashSet<TestBullet> activeVolleyBullets = new HashSet<TestBullet>();

    private bool isVolleyRunning;

    private Vector2 detectionCenter;
    private float detectionRadiusSquared;
    private double detectionStartedAt;

    private readonly Queue<(double Time, Vector2 Position)> movementSamples =
        new Queue<(double Time, Vector2 Position)>();

    private (double Time, Vector2 Position) previousMovementSample;

    private bool isDetecting;
    // 감지 한 번에 굳기를 한 번만 발동한다.
    private bool hasTriggeredFreeze;
    private bool isComplete;

    /// <summary>감지 구간이 정상 종료됐는지 반환합니다.</summary>
    public bool IsComplete => isComplete;

    /// <summary>이동 감지 구간이 진행 중인지 나타냅니다.</summary>
    public bool IsDetecting => isDetecting;

    /// <summary>이번 감지에서 플레이어 이동을 잠갔는지 나타냅니다.</summary>
    public bool HasTriggeredFreeze => hasTriggeredFreeze;

    /// <summary>
    /// 이동 감지와 굳기 탄 발사에 필요한 씬 참조를 연결합니다.
    /// HeadBossPattern.Initialize가 호출합니다.
    /// </summary>
    public void Initialize(
        PlayerHealth player,
        SpriteRenderer field,
        BulletRegistry registry,
        Transform poolParent)
    {
        playerHealth = player;
        playerMovement = player != null ? player.GetComponent<PlayerMovement>() : null;
        playerLight = player != null ? player.GetComponent<PlayerLight>() : null;
        perfectDodge = player != null ? player.GetComponent<PlayerPerfectDodge>() : null;
        playfield = field;
        bulletRegistry = registry;
        bulletParent = poolParent;
    }

    /// <summary>포착된 서치라이트 원을 기준으로 이동 감지를 시작합니다.</summary>
    public void BeginDetection(Vector2 center, float radius)
    {
        if (!isActiveAndEnabled || isDetecting)
        {
            return;
        }

        if (playerHealth == null || playerMovement == null)
        {
            Debug.LogError(
                "서치라이트 이동 감지의 플레이어와 이동 컴포넌트를 확인해주세요.",
                this);
            return;
        }

        if (playerHealth.IsDead)
        {
            return;
        }

        detectionCenter = center;
        detectionRadiusSquared = radius * radius;
        detectionStartedAt = Time.timeAsDouble;

        movementSamples.Clear();
        previousMovementSample = (0d, (Vector2)playerHealth.transform.position);

        hasTriggeredFreeze = false;
        isComplete = false;
        isDetecting = true;
    }

    private void Update()
    {
        if (!isDetecting || Time.timeScale <= 0f)
        {
            return;
        }

        if (playerHealth == null || playerHealth.IsDead)
        {
            StopDetection();
            return;
        }

        if (isVolleyRunning)
        {
            if (activeVolleyBullets.Count == 0)
            {
                StopDetection();
                isComplete = true;
            }

            return;
        }

        double elapsed = Time.timeAsDouble - detectionStartedAt;
        if (elapsed >= duration)
        {
            StopDetection();
            isComplete = true;
            return;
        }

        if (hasTriggeredFreeze)
        {
            return;
        }

        Vector2 playerPosition = playerHealth.transform.position;
        bool hasMoved = HasMovedWithinWindow(elapsed, playerPosition);
        bool isInsideCircle =
            (playerPosition - detectionCenter).sqrMagnitude <= detectionRadiusSquared;

        if (!isInsideCircle || !hasMoved)
        {
            return;
        }

        hasTriggeredFreeze = true;
        movementSamples.Clear();
        playerMovement.SetFrozen(true);
        TryStartVolley();
    }

    private void TryStartVolley()
    {
        if (firePoint == null || bulletPrefab == null || playerLight == null || perfectDodge == null
            || playfield == null || bulletRegistry == null || bulletParent == null)
        {
            Debug.LogError("굳기 부채꼴의 발사점·탄·씬 참조를 확인해주세요.", this);
            return;
        }

        Vector3 origin = firePoint.position;
        Vector2 toPlayer = (Vector2)playerHealth.transform.position - (Vector2)origin;
        float distance = toPlayer.magnitude;
        float outerPerfectRadius =
            playerLight.CoreRadius + bulletRadius + perfectDodge.PerfectTolerance;
        float decoyDistance = outerPerfectRadius + decoyClearance;
        float haloRadius = playerLight.HaloRadius;

        float centerEntryTime = (distance - outerPerfectRadius) / centerBulletSpeed;
        float decoyClosestTime = centerEntryTime - centerArrivalDelay;

        // 미끼 궤도나 도착 순서를 확보할 수 없으면 발사를 생략한다.
        if (distance <= decoyDistance || decoyDistance >= haloRadius
            || decoyClosestTime <= 0f)
        {
            return;
        }

        Vector2 forward = toPlayer / distance;
        Vector2 sideways = new Vector2(-forward.y, forward.x);

        float sideRatio = decoyDistance / distance;
        float forwardRatio = Mathf.Sqrt(1f - sideRatio * sideRatio);
        float decoyClosestTravel = distance * forwardRatio;
        float decoySpeed = decoyClosestTravel / decoyClosestTime;

        // 미끼는 최근접 지점을 지나 헤일로 바깥까지 진행한다.
        float haloExitRadius = haloRadius + bulletRadius;
        float travelPastClosest = Mathf.Sqrt(
            haloExitRadius * haloExitRadius - decoyDistance * decoyDistance);
        float decoyLifetime = (decoyClosestTravel + travelPastClosest) / decoySpeed;

        if (pool == null)
        {
            pool = new ComponentPool<TestBullet>(
                new TestBulletFactory(bulletPrefab, bulletParent),
                FREEZE_BULLET_COUNT,
                bulletParent);
        }

        isVolleyRunning = true;
        FireFreezeBullet(origin, forward * forwardRatio - sideways * sideRatio,
            decoySpeed, decoyLifetime);
        FireFreezeBullet(origin, forward, centerBulletSpeed, distance / centerBulletSpeed);
        FireFreezeBullet(origin, forward * forwardRatio + sideways * sideRatio,
            decoySpeed, decoyLifetime);
    }

    private void FireFreezeBullet(
        Vector3 origin, Vector2 direction, float speed, float lifetime)
    {
        TestBullet bullet = pool.Rent(instance => instance.Prepare(
            origin, direction, speed, lifetime, bulletRadius,
            playfield.bounds, pool, ReturnVolleyBullet,
            playerHealth, bulletRegistry, hitOnce: true));

        activeVolleyBullets.Add(bullet);
    }

    private void ReturnVolleyBullet(TestBullet bullet)
    {
        activeVolleyBullets.Remove(bullet);
        pool.Return(bullet);
    }

    private bool HasMovedWithinWindow(double elapsed, Vector2 position)
    {
        if (elapsed > 0d)
        {
            movementSamples.Enqueue((elapsed, position));
        }

        double comparisonTime = elapsed - movementSampleWindow;
        if (comparisonTime < 0d)
        {
            return false;
        }

        // 비교 시각 앞뒤의 위치를 보간해 프레임 간격이 달라도 같은 시간 차이를 사용한다.
        while (movementSamples.TryPeek(out var sample) && sample.Time <= comparisonTime)
        {
            previousMovementSample = movementSamples.Dequeue();
        }

        var nextSample = movementSamples.Peek();
        float blend = (float)((comparisonTime - previousMovementSample.Time)
            / (nextSample.Time - previousMovementSample.Time));
        Vector2 comparisonPosition =
            Vector2.Lerp(previousMovementSample.Position, nextSample.Position, blend);

        return (position - comparisonPosition).sqrMagnitude
            > movementThreshold * movementThreshold;
    }

    /// <summary>
    /// 이동 감지를 중단하고 이 패턴이 건 이동 잠금을 풉니다.
    /// HeadBossSearchlightPattern.StopTracking과 비활성 처리에서 호출합니다.
    /// </summary>
    public void StopDetection()
    {
        if (hasTriggeredFreeze && playerMovement != null)
        {
            playerMovement.SetFrozen(false);
        }

        movementSamples.Clear();
        isVolleyRunning = false;
        isDetecting = false;
        hasTriggeredFreeze = false;
        isComplete = false;
    }

    private void OnDisable()
    {
        StopDetection();

        // 종료 연출에 넘기지 못한 탄은 컴포넌트 비활성화 때 회수한다.
        foreach (TestBullet bullet in new List<TestBullet>(activeVolleyBullets))
        {
            if (bullet != null)
            {
                bullet.Expire();
            }
        }

        activeVolleyBullets.Clear();
    }

    private void OnDestroy()
    {
        pool?.Clear();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        movementThreshold = Mathf.Max(0f, movementThreshold);
        movementSampleWindow = Mathf.Max(0.01f, movementSampleWindow);
        decoyClearance = Mathf.Max(0.01f, decoyClearance);
        centerArrivalDelay = Mathf.Clamp(centerArrivalDelay, 0.01f, 0.49f);
        centerBulletSpeed = Mathf.Max(0.01f, centerBulletSpeed);
        bulletRadius = Mathf.Max(0.01f, bulletRadius);
    }
}
