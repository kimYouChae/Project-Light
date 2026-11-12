using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>머리 보스의 입에서 부채꼴 직선탄을 발사합니다.</summary>
public sealed class HeadBossFanPattern : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private TestBullet bulletPrefab;

    // ##TEMP 넓은 부채꼴 시작값. 인스펙터에서 조절한다.
    [SerializeField, Min(1)] private int bulletCount = 15;
    [SerializeField, Range(0f, 360f)] private float spreadAngle = 140f;

    // ##TEMP 비워둘 연속 방향 수와 틈 중심의 좌우 각도.
    [SerializeField, Min(1)] private int gapSlotCount = 3;
    [SerializeField, Min(0f)] private float gapCenterAngle = 20f;

    // ##TEMP 기존 승인한 탄속·크기·구간·풀 시작값.
    [SerializeField, Min(0f)] private float bulletSpeed = 2f;
    [SerializeField, Min(0.01f)] private float bulletRadius = 0.12f;
    [SerializeField, Min(0.01f)] private float duration = 5f;
    [SerializeField, Min(0.01f)] private float fireInterval = 1f;
    [SerializeField, Min(1)] private int initialPoolCount = 64;

    // 초기화 시 전달받는 씬 참조
    private PlayerHealth playerHealth;
    private SpriteRenderer playfield;
    private FieldManager fieldManager;
    private BulletRegistry bulletRegistry;
    private Transform bulletParent;

    private readonly HashSet<TestBullet> activeBullets =
        new HashSet<TestBullet>();

    private ComponentPool<TestBullet> pool;
    private Action<TestBullet> prepareBullet;
    private Action<TestBullet> releaseBullet;

    private bool emitting;
    private double elapsed;
    private double nextShotTime;
    private double lastShotAt = double.NegativeInfinity;

    // 다음 발사의 틈 방향. 첫 발사는 왼쪽이다.
    private bool gapOnLeft;

    // 한 번의 대여에 사용할 발사 정보
    private Vector3 shotPosition;
    private Vector2 shotDirection;

    /// <summary>현재 부채꼴 발사 구간이 진행 중인지 반환합니다.</summary>
    public bool IsEmitting => emitting;

    /// <summary>발사가 끝나고 발사된 부채꼴 탄이 모두 반환됐는지 나타냅니다.</summary>
    public bool IsFinished => !IsEmitting && activeBullets.Count == 0;

    /// <summary>마지막 부채꼴 발사 후 지정 시간이 지났는지 반환합니다.</summary>
    public bool HasFinishedFiringAfter(float delay)
    {
        return !double.IsNegativeInfinity(lastShotAt)
            && nextShotTime >= duration
            && Time.timeAsDouble - lastShotAt >= delay;
    }

    /// <summary>
    /// 실행에 필요한 씬 참조를 전달합니다.
    /// 이후 머리 보스 전투 초기화에서 호출합니다.
    /// </summary>
    public void Initialize(
        PlayerHealth player,
        SpriteRenderer field,
        BulletRegistry registry,
        Transform poolParent,
        FieldManager fieldManager = null)
    {
        playerHealth = player;
        playfield = field;
        this.fieldManager = fieldManager;
        bulletRegistry = registry;
        bulletParent = poolParent;
    }

    /// <summary>
    /// 부채꼴 구간을 시작하고 첫 탄막을 즉시 발사합니다.
    /// 이후 머리 보스 패턴 제어부가 호출합니다.
    /// </summary>
    public void StartEmission()
    {
        if (!isActiveAndEnabled || emitting)
        {
            return;
        }

        lastShotAt = double.NegativeInfinity;

        if (firePoint == null || bulletPrefab == null
            || playerHealth == null || playfield == null
            || bulletRegistry == null || bulletParent == null)
        {
            PlayLog.Write("HeadBoss",
                "머리 부채꼴 패턴의 발사점·탄·씬 참조를 확인해주세요.",
                LogType.Error, this);
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
                PlayLog.Write("HeadBoss", exception.ToString(), LogType.Exception, this);
                return;
            }
        }

        elapsed = 0d;
        nextShotTime = fireInterval;
        emitting = true;

        gapOnLeft = true;
        FireFan();
    }

    /// <summary>
    /// 새 발사를 중단합니다. 이미 발사한 탄은 유지합니다.
    /// 이후 머리 보스의 패턴 전환·전투 종료에서 호출합니다.
    /// </summary>
    public void StopForBattleEnd()
    {
        emitting = false;
    }

    private void Update()
    {
        if (!emitting || Time.timeScale <= 0f)
        {
            return;
        }

        if (playerHealth == null || playerHealth.IsDead)
        {
            StopForBattleEnd();
            return;
        }

        elapsed += Time.deltaTime;

        // 구간 종료 시각의 발사는 제외한다.
        while (nextShotTime < duration && nextShotTime <= elapsed)
        {
            FireFan();
            nextShotTime += fireInterval;
        }

        if (elapsed >= duration)
        {
            StopForBattleEnd();
        }
    }

    private void FireFan()
    {
        shotPosition = firePoint.position;

        // 아래쪽 기준 양수 각도는 화면 오른쪽이다.
        float widthRatio = fieldManager != null ? fieldManager.CurrentWidthRatio : 1f;
        float currentSpreadAngle = spreadAngle * widthRatio;
        float desiredGapAngle = (gapOnLeft ? -gapCenterAngle : gapCenterAngle) * widthRatio;

        float angleStep = bulletCount > 1
            ? currentSpreadAngle / (bulletCount - 1)
            : 0f;

        // 원하는 각도와 가장 가까운 발사 방향을 틈 중심으로 고른다.
        int gapCenterSlot = angleStep > 0f
            ? Mathf.RoundToInt(
                (desiredGapAngle + currentSpreadAngle * 0.5f) / angleStep)
            : bulletCount / 2;

        int emptyCount = Mathf.Clamp(gapSlotCount, 1, bulletCount);
        int gapStart = Mathf.Clamp(
            gapCenterSlot - emptyCount / 2,
            0,
            bulletCount - emptyCount);

        int gapEnd = gapStart + emptyCount;

        for (int i = 0; i < bulletCount; i++)
        {
            // 틈에 해당하는 방향은 탄을 대여하지 않는다.
            if (i >= gapStart && i < gapEnd)
            {
                continue;
            }

            float angle = bulletCount == 1
                ? 0f
                : -currentSpreadAngle * 0.5f + angleStep * i;

            float radians = angle * Mathf.Deg2Rad;

            // 플레이어 위치와 무관하게 아래쪽 중심으로 펼친다.
            shotDirection = new Vector2(
                Mathf.Sin(radians),
                -Mathf.Cos(radians));

            pool.Rent(prepareBullet);
        }

        gapOnLeft = !gapOnLeft;
        lastShotAt = Time.timeAsDouble;
        PlayLog.Write("HeadBoss", $"Fan Shoot ({bulletCount - emptyCount} bullets)");
    }

    private void PrepareBullet(TestBullet bullet)
    {
        bullet.Prepare(
            shotPosition,
            shotDirection,
            bulletSpeed,
            float.PositiveInfinity,
            bulletRadius,
            fieldManager != null ? fieldManager.CurrentBounds : playfield.bounds,
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

    private void OnDisable()
    {
        StopForBattleEnd();

        // 반환 콜백이 원본 목록을 변경하므로 복사본을 순회한다.
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
        bulletCount = Mathf.Max(1, bulletCount);
        spreadAngle = Mathf.Clamp(spreadAngle, 0f, 360f);
        gapSlotCount = Mathf.Clamp(gapSlotCount, 1, bulletCount);
        gapCenterAngle = Mathf.Clamp(
            gapCenterAngle, 0f, spreadAngle * 0.5f);
        bulletSpeed = Mathf.Max(0f, bulletSpeed);
        bulletRadius = Mathf.Max(0.01f, bulletRadius);
        duration = Mathf.Max(0.01f, duration);
        fireInterval = Mathf.Max(0.01f, fireInterval);
        initialPoolCount = Mathf.Max(1, initialPoolCount);
    }
}
