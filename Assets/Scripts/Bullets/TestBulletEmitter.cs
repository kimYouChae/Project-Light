using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>동일한 탄과 발사 조건으로 풀링과 생성·파괴 방식을 비교하는 테스트 발사기.</summary>
public sealed class TestBulletEmitter : MonoBehaviour
{
    [Header("비교 모드")]
    [Tooltip("켜짐: 풀 대여/반환. 꺼짐: Instantiate/Destroy. 이미 발사한 탄은 발사 당시 방식을 유지합니다.")]
    [SerializeField] private bool usePooling = true;
    [SerializeField] private bool autoFire = true;

    [Header("참조")]
    [SerializeField] private TestBullet bulletPrefab;
    [SerializeField] private Transform target;
    [SerializeField] private SpriteRenderer playfield;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private BulletRegistry bulletRegistry;

    [Header("테스트 탄 설정")]
    // ##TEMP 풀 비교용 초기값. 인스펙터에서 조정한다.
    [SerializeField, Min(1)] private int initialPoolCount = 64;
    [SerializeField, Min(0.01f)] private float fireInterval = 2f;
    [SerializeField, Min(0f)] private float bulletSpeed = 3f;
    [SerializeField, Min(0.01f)] private float bulletLifetime = 8f;
    [SerializeField, Min(0.01f)] private float bulletRadius = 0.12f;

    private readonly HashSet<TestBullet> activeBullets = new HashSet<TestBullet>();
    private ComponentPool<TestBullet> pool;
    private TestBulletFactory factory;
    private Action<TestBullet> prepareBullet;
    private Action<TestBullet> releaseBullet;
    private ComponentPool<TestBullet> spawnPool;
    private float elapsed;
    private PlayerHealth targetHealth;

    private void Start()
    {
        if (bulletPrefab == null || target == null || playfield == null || bulletParent == null || bulletRegistry == null)
        {
            Debug.LogError("테스트 발사기의 프리팹·타겟·필드·탄 부모·탄 관리자 참조를 확인해주세요.", this);
            enabled = false;
            return;
        }
        try
        {
            factory = new TestBulletFactory(bulletPrefab, bulletParent);
            prepareBullet = PrepareBullet;
            releaseBullet = ReleaseBullet;
            targetHealth = target.GetComponent<PlayerHealth>();
            if (usePooling)
                EnsurePool();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            enabled = false;
        }
    }

    private void EnsurePool()
    {
        if (pool == null)
            pool = new ComponentPool<TestBullet>(factory, initialPoolCount, bulletParent);
    }

    private void Update()
    {
        if (!autoFire || target == null || factory == null)
            return;

        elapsed += Time.deltaTime;
        if (elapsed < fireInterval)
            return;
        elapsed %= fireInterval;
        SpawnBullet();
    }

    private void SpawnBullet()
    {
        if (usePooling)
        {
            EnsurePool();
            spawnPool = pool;
            pool.Rent(prepareBullet);
        }
        else
        {
            spawnPool = null;
            TestBullet bullet = factory.CreateInstance();
            PrepareBullet(bullet);
            bullet.gameObject.SetActive(true);
        }
    }

    private void PrepareBullet(TestBullet bullet)
    {
        Vector2 direction = (Vector2)(target.position - transform.position);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = Vector2.down;
        bullet.Prepare(transform.position, direction, bulletSpeed, bulletLifetime,
            bulletRadius, playfield.bounds, spawnPool, releaseBullet, targetHealth, bulletRegistry);
        activeBullets.Add(bullet);
    }

    private void ReleaseBullet(TestBullet bullet)
    {
        activeBullets.Remove(bullet);
        // 현재 토글이 아닌 발사 당시 소속으로 회수한다.
        if (bullet.SourcePool != null)
            bullet.SourcePool.Return(bullet);
        else
            Destroy(bullet.gameObject);
    }

    private void OnDisable()
    {
        // 회수 중 집합이 바뀌므로 정지 시에만 복사한다.
        var snapshot = new TestBullet[activeBullets.Count];
        activeBullets.CopyTo(snapshot);
        foreach (TestBullet bullet in snapshot)
        {
            if (bullet != null)
                bullet.Expire();
        }
        activeBullets.Clear();
        elapsed = 0f;
    }

    private void OnDestroy()
    {
        pool?.Clear();
    }

    private void OnValidate()
    {
        initialPoolCount = Mathf.Max(1, initialPoolCount);
        fireInterval = Mathf.Max(0.01f, fireInterval);
        bulletSpeed = Mathf.Max(0f, bulletSpeed);
        bulletLifetime = Mathf.Max(0.01f, bulletLifetime);
        bulletRadius = Mathf.Max(0.01f, bulletRadius);
    }
}
