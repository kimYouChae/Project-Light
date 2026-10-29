using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>물결 조준탄과 직선 조준탄의 발사 구간을 반복한다.</summary>
public sealed class BossBulletPattern : MonoBehaviour
{
    [SerializeField] private TestBullet bulletPrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SpriteRenderer playfield;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private BulletRegistry bulletRegistry;

    // ##TEMP 사용자 승인 패턴 시간·발사 간격.
    [SerializeField, Min(0.01f)] private float waveDuration = 5f;
    [SerializeField, Min(0.01f)] private float straightDuration = 10f;
    [SerializeField, Min(0.01f)] private float straightFireInterval = 2f;
    // ##TEMP 물결탄 한 줄의 구성과 줄 시작 간격.
    [SerializeField, Min(1)] private int waveBurstCount = 6;
    [SerializeField, Min(0.01f)] private float waveBulletInterval = 0.15f;
    [SerializeField, Min(0.01f)] private float waveBurstInterval = 1.5f;
    // ##TEMP 이동 테스트용 시작값. 인스펙터에서 조정한다.
    [SerializeField, Min(0f)] private float bulletSpeed = 2f;
    [SerializeField, Min(0f)] private float waveAmplitude = 0.8f;
    [SerializeField, Min(0.01f)] private float wavePeriod = 2f;
    // ##TEMP 기존 적탄과 동일한 크기·사전 생성 수량.
    [SerializeField, Min(0.01f)] private float bulletRadius = 0.12f;
    [SerializeField, Min(1)] private int initialPoolCount = 64;

    private readonly HashSet<TestBullet> activeBullets = new HashSet<TestBullet>();
    private ComponentPool<TestBullet> pool;
    private Action<TestBullet> prepareBullet;
    private Action<TestBullet> releaseBullet;
    private double patternTime;
    private double nextShotTime;
    private double nextPhaseTime;
    private bool wavePhase = true;
    private readonly List<WaveBurst> waveBursts = new List<WaveBurst>();
    private WaveBurst spawnBurst;

    private sealed class WaveBurst
    {
        public Vector3 position;
        public Vector2 direction;
        public float speed;
        public float amplitude;
        public float period;
        public float radius;
        public float bulletInterval;
        public int remainingCount;
        public double nextShotTime;
    }

    private void Start()
    {
        if (bulletPrefab == null || bulletPrefab.Movement == null || playerTransform == null
            || playerHealth == null || playfield == null || bulletParent == null || bulletRegistry == null)
        {
            Debug.LogError("패턴 발사기의 이동 프리팹·플레이어·필드·탄 부모·탄 관리자 참조를 확인해주세요.", this);
            enabled = false;
            return;
        }
        try
        {
            prepareBullet = PrepareBullet;
            releaseBullet = ReleaseBullet;
            pool = new ComponentPool<TestBullet>(
                new TestBulletFactory(bulletPrefab, bulletParent), initialPoolCount, bulletParent);
            ResetPattern();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (pool != null && playerTransform != null)
            AdvancePattern(Time.deltaTime);
    }

    private void AdvancePattern(float deltaTime)
    {
        double endTime = patternTime + Mathf.Max(0f, deltaTime);
        while (true)
        {
            int burstIndex = -1;
            double burstTime = double.PositiveInfinity;
            for (int i = 0; i < waveBursts.Count; i++)
            {
                if (waveBursts[i].nextShotTime < burstTime)
                {
                    burstIndex = i;
                    burstTime = waveBursts[i].nextShotTime;
                }
            }
            double eventTime = Math.Min(burstTime, Math.Min(nextShotTime, nextPhaseTime));
            if (eventTime > endTime)
                break;

            // 구간 경계에서는 새 줄 시작만 전환하고 기존 줄은 끝까지 발사한다.
            if (nextPhaseTime <= eventTime)
            {
                wavePhase = !wavePhase;
                nextShotTime = nextPhaseTime;
                nextPhaseTime += wavePhase ? waveDuration : straightDuration;
            }
            else if (nextShotTime <= eventTime)
            {
                if (wavePhase)
                    StartWaveBurst(nextShotTime);
                else
                    pool.Rent(prepareBullet);
                nextShotTime += wavePhase ? waveBurstInterval : straightFireInterval;
            }
            else
            {
                WaveBurst burst = waveBursts[burstIndex];
                spawnBurst = burst;
                try
                {
                    pool.Rent(prepareBullet);
                }
                finally
                {
                    spawnBurst = null;
                }
                burst.remainingCount--;
                if (burst.remainingCount <= 0)
                    waveBursts.RemoveAt(burstIndex);
                else
                    burst.nextShotTime += burst.bulletInterval;
            }
        }
        patternTime = endTime;
    }

    private void StartWaveBurst(double startTime)
    {
        Vector2 direction = playerTransform.position - transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = Vector2.down;
        waveBursts.Add(new WaveBurst
        {
            position = transform.position,
            direction = direction.normalized,
            speed = bulletSpeed,
            amplitude = waveAmplitude,
            period = wavePeriod,
            radius = bulletRadius,
            bulletInterval = waveBulletInterval,
            remainingCount = waveBurstCount,
            nextShotTime = startTime
        });
    }

    private void PrepareBullet(TestBullet bullet)
    {
        Vector3 position = spawnBurst != null ? spawnBurst.position : transform.position;
        Vector2 aimDirection = spawnBurst != null
            ? spawnBurst.direction : (Vector2)(playerTransform.position - position);
        if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
            aimDirection = Vector2.down;
        float speed = spawnBurst != null ? spawnBurst.speed : bulletSpeed;
        float radius = spawnBurst != null ? spawnBurst.radius : bulletRadius;
        bullet.Prepare(position, aimDirection, speed,
            float.PositiveInfinity, radius, playfield.bounds, pool,
            releaseBullet, playerHealth, bulletRegistry);
        if (spawnBurst != null)
            bullet.Movement.PrepareWave(position, aimDirection,
                speed, spawnBurst.amplitude, spawnBurst.period);
        else
            bullet.Movement.PrepareStraight(position, aimDirection, speed);
        activeBullets.Add(bullet);
    }

    private void ReleaseBullet(TestBullet bullet)
    {
        activeBullets.Remove(bullet);
        pool.Return(bullet);
    }

    private void ResetPattern()
    {
        patternTime = 0d;
        nextShotTime = 0d;
        nextPhaseTime = waveDuration;
        wavePhase = true;
        waveBursts.Clear();
        spawnBurst = null;
    }

    private void OnDisable()
    {
        foreach (TestBullet bullet in new List<TestBullet>(activeBullets))
            if (bullet != null)
                bullet.Expire();
        activeBullets.Clear();
        ResetPattern();
    }

    private void OnDestroy() => pool?.Clear();

    private void OnValidate()
    {
        waveDuration = Mathf.Max(0.01f, waveDuration);
        straightDuration = Mathf.Max(0.01f, straightDuration);
        waveBurstCount = Mathf.Max(1, waveBurstCount);
        waveBulletInterval = Mathf.Max(0.01f, waveBulletInterval);
        waveBurstInterval = Mathf.Max(0.01f, waveBurstInterval);
        straightFireInterval = Mathf.Max(0.01f, straightFireInterval);
        bulletSpeed = Mathf.Max(0f, bulletSpeed);
        waveAmplitude = Mathf.Max(0f, waveAmplitude);
        wavePeriod = Mathf.Max(0.01f, wavePeriod);
        bulletRadius = Mathf.Max(0.01f, bulletRadius);
        initialPoolCount = Mathf.Max(1, initialPoolCount);
    }
}
