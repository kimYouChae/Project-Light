using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>살아 있는 보스에게 일정 간격으로 풀링 기본 공격을 발사한다.</summary>
public sealed class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField] private PlayerBullet bulletPrefab;
    [SerializeField] private BossHealth targetBoss;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SpriteRenderer playfield;
    [SerializeField, Min(0.01f)] private float fireInterval = 2f;
    [SerializeField, Min(0)] private int initialPoolCount = 16; // ##TEMP 초기 풀 크기
    [SerializeField, Min(0.01f)] private float bulletSpeed = 6f; // ##TEMP 탄 속도
    [SerializeField, Min(0.01f)] private float bulletRadius = 0.12f; // ##TEMP 탄 반경
    [SerializeField, Min(0f)] private float arcHeight = 1f; // ##TEMP 포물선 높이
    [SerializeField, Min(1)] private int damage = 1; // ##TEMP 기본 공격 피해
    private readonly HashSet<PlayerBullet> activeBullets = new HashSet<PlayerBullet>();
    private ComponentPool<PlayerBullet> pool;
    private Action<PlayerBullet> prepareBullet;
    private Action<PlayerBullet> returnedBullet;
    private float elapsed;

    private void Start()
    {
        if (bulletPrefab == null || targetBoss == null || firePoint == null
            || bulletParent == null || playerHealth == null || playfield == null)
        {
            Debug.LogError("기본 공격의 프리팹, 보스, 발사점, 부모, 체력을 지정해주세요.", this);
            enabled = false;
            return;
        }
        returnedBullet = RemoveBullet;
        prepareBullet = PrepareBullet;
        pool = new ComponentPool<PlayerBullet>(
            new PlayerBulletFactory(bulletPrefab, bulletParent), initialPoolCount, bulletParent);
    }

    private void Update()
    {
        if (pool == null || playerHealth.IsDead || targetBoss == null || targetBoss.IsDefeated)
            return;
        elapsed += Time.deltaTime;
        if (elapsed < fireInterval)
            return;
        elapsed %= fireInterval;
        pool.Rent(prepareBullet);
    }

    private void PrepareBullet(PlayerBullet bullet)
    {
        bullet.Prepare(firePoint.position, targetBoss, bulletSpeed, arcHeight,
            bulletRadius, damage, playfield.bounds, pool, returnedBullet);
        activeBullets.Add(bullet);
    }

    private void RemoveBullet(PlayerBullet bullet) => activeBullets.Remove(bullet);

    private void OnDisable()
    {
        foreach (PlayerBullet bullet in new List<PlayerBullet>(activeBullets))
            if (bullet != null)
                bullet.ReturnToPool();
        activeBullets.Clear();
        elapsed = 0f;
    }

    private void OnDestroy() => pool?.Clear();

    private void OnValidate()
    {
        fireInterval = Mathf.Max(0.01f, fireInterval);
        initialPoolCount = Mathf.Max(0, initialPoolCount);
        bulletSpeed = Mathf.Max(0.01f, bulletSpeed);
        bulletRadius = Mathf.Max(0.01f, bulletRadius);
        arcHeight = Mathf.Max(0f, arcHeight);
        damage = Mathf.Max(1, damage);
    }
}
