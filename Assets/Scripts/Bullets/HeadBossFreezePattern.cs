using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>정지 안전 통로와 이동 감지 조준탄을 관리합니다.</summary>
public sealed class HeadBossFreezePattern : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private TestBullet bulletPrefab;

    // ##TEMP 플레이어 중심에서 좌우 통로 탄 중심까지의 거리.
    [SerializeField, Min(0f)] private float laneDistance = 1.2f;

    // ##TEMP 승인한 감지·발사 시작값.
    [SerializeField, Min(0.01f)] private float duration = 2f;
    [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
    // ##TEMP 통로 탄 발사 간격.
    [SerializeField, Min(0.01f)] private float laneFireInterval = 0.5f;

    // ##TEMP 이동 감지 조준탄 발사 간격.
    [SerializeField, Min(0.01f)] private float aimedFireInterval = 0.2f;
    [SerializeField, Min(0f)] private float bulletSpeed = 2f;
    [SerializeField, Min(0.01f)] private float bulletRadius = 0.12f;
    [SerializeField, Min(1)] private int initialPoolCount = 64;

    private PlayerHealth playerHealth;
    private SpriteRenderer playfield;
    private BulletRegistry bulletRegistry;
    private Transform bulletParent;

    private readonly HashSet<TestBullet> activeBullets =
        new HashSet<TestBullet>();

    private ComponentPool<TestBullet> pool;
    private Action<TestBullet> prepareBullet;
    private Action<TestBullet> releaseBullet;

    private Vector3 safePosition;
    private float laneOffset;
    private float laneStartY;

    private double elapsed;
    private double nextLaneTime;
    private double nextAimTime;

    private bool isDetecting;
    private bool wasDetected;
    private bool isComplete;

    // 대여 직전에 설정하는 발사 정보
    private Vector3 shotPosition;
    private Vector2 shotDirection;

    /// <summary>감지 구간이 정상 종료됐는지 반환합니다.</summary>
    public bool IsComplete => isComplete;

    /// <summary>
    /// 씬 참조를 연결합니다.
    /// HeadBossPattern.Initialize가 호출합니다.
    /// </summary>
    public void Initialize(
        PlayerHealth player,
        SpriteRenderer field,
        BulletRegistry registry,
        Transform poolParent)
    {
        playerHealth = player;
        playfield = field;
        bulletRegistry = registry;
        bulletParent = poolParent;
    }

    /// <summary>
    /// 현재 위치를 안전 위치로 저장하고 감지를 시작합니다.
    /// 이후 예고 완료 시 패턴 제어부가 호출합니다.
    /// </summary>
    public void BeginDetection()
    {
        if (!isActiveAndEnabled || isDetecting)
        {
            return;
        }

        if (firePoint == null || bulletPrefab == null
            || playerHealth == null
            || playfield == null || bulletRegistry == null
            || bulletParent == null)
        {
            Debug.LogError(
                "얼어붙기 공격의 발사점·탄·씬 참조를 확인해주세요.",
                this);
            return;
        }

        if (playerHealth.IsDead)
        {
            return;
        }

        if (pool == null)
        {
            try
            {
                prepareBullet = PrepareBullet;
                releaseBullet = ReleaseBullet;

                pool = new ComponentPool<TestBullet>(
                    new TestBulletFactory(bulletPrefab, bulletParent),
                    initialPoolCount,
                    bulletParent);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return;
            }
        }

        safePosition = playerHealth.transform.position;

        // 안전 위치에서 직접 설정한 좌우 거리를 사용한다.
        laneOffset = laneDistance;
        laneStartY = playfield.bounds.max.y - bulletRadius;

        elapsed = 0d;
        nextLaneTime = laneFireInterval;
        nextAimTime = 0d;
        wasDetected = false;
        isComplete = false;
        isDetecting = true;

        FireLaneBullet();
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

        elapsed += Time.deltaTime;

        // 통로 탄은 감지 구간 동안 일정 간격으로 내려보낸다.
        while (nextLaneTime < duration && nextLaneTime <= elapsed)
        {
            FireLaneBullet();
            nextLaneTime += laneFireInterval;
        }

        if (elapsed >= duration)
        {
            isDetecting = false;
            isComplete = true;
            return;
        }

        Vector2 movement =
            playerHealth.transform.position - safePosition;

        bool detected =
            movement.sqrMagnitude > movementThreshold * movementThreshold;

        if (!detected)
        {
            // 안전 위치로 돌아오면 조준탄 발사를 중단한다.
            wasDetected = false;
            return;
        }

        if (!wasDetected)
        {
            // 처음 감지되거나 다시 벗어난 순간에는 즉시 한 발 발사한다.
            nextAimTime = elapsed;
        }

        wasDetected = true;

        while (nextAimTime < duration && nextAimTime <= elapsed)
        {
            FireAimedBullet();
            nextAimTime += aimedFireInterval;
        }
    }

    private void FireLaneBullet()
    {
        // 발사할 때마다 좌우 중 한쪽을 같은 확률로 선택한다.
        float side = UnityEngine.Random.Range(0, 2) == 0 ? -1f : 1f;

        shotDirection = Vector2.down;
        shotPosition = new Vector3(
            safePosition.x + laneOffset * side,
            laneStartY,
            playfield.bounds.center.z);

        pool.Rent(prepareBullet);
    }

    private void FireAimedBullet()
    {
        shotPosition = firePoint.position;
        shotDirection =
            playerHealth.transform.position - shotPosition;

        if (shotDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            shotDirection = Vector2.down;
        }

        pool.Rent(prepareBullet);
    }

    private void PrepareBullet(TestBullet bullet)
    {
        bullet.Prepare(
            shotPosition,
            shotDirection,
            bulletSpeed,
            float.PositiveInfinity,
            bulletRadius,
            playfield.bounds,
            pool,
            releaseBullet,
            playerHealth,
            bulletRegistry);

        activeBullets.Add(bullet);
    }

    private void ReleaseBullet(TestBullet bullet)
    {
        activeBullets.Remove(bullet);
        pool.Return(bullet);
    }

    /// <summary>
    /// 감지와 새 발사를 중단합니다. 이미 발사한 탄은 유지합니다.
    /// HeadBossPattern.StopPattern과 비활성 처리에서 호출합니다.
    /// </summary>
    public void StopDetection()
    {
        isDetecting = false;
        wasDetected = false;
        isComplete = false;
    }

    private void OnDisable()
    {
        StopDetection();

        foreach (TestBullet bullet in new List<TestBullet>(activeBullets))
        {
            if (bullet != null)
            {
                bullet.Expire();
            }
        }

        activeBullets.Clear();
    }

    private void OnDestroy()
    {
        pool?.Clear();
    }

    private void OnValidate()
    {
        laneDistance = Mathf.Max(0f, laneDistance);
        duration = Mathf.Max(0.01f, duration);
        movementThreshold = Mathf.Max(0f, movementThreshold);
        laneFireInterval = Mathf.Max(0.01f, laneFireInterval);
        aimedFireInterval = Mathf.Max(0.01f, aimedFireInterval);
        bulletSpeed = Mathf.Max(0f, bulletSpeed);
        bulletRadius = Mathf.Max(0.01f, bulletRadius);
        initialPoolCount = Mathf.Max(1, initialPoolCount);
    }
}
