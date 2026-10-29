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
    // ##TEMP 폭주 중 발사 간격 배율. 0.5면 간격이 절반이다.
    [SerializeField, Range(0.01f, 1f)] private float frenzyFireIntervalMultiplier = 0.5f;
    [SerializeField, Min(0)] private int initialPoolCount = 16; // ##TEMP 초기 풀 크기
    [SerializeField, Min(0.01f)] private float bulletSpeed = 6f; // ##TEMP 탄 속도
    [SerializeField, Min(0.01f)] private float bulletRadius = 0.12f; // ##TEMP 탄 반경
    // ##TEMP 발사마다 동일 확률로 선택할 포물선 높이.
    [SerializeField] private float[] arcHeightOptions = { 1f, 2f, 1.5f };
    [SerializeField, Min(1)] private int damage = 1; // ##TEMP 기본 공격 피해
    // ##TEMP 강화 공격 테스트용 배율. 인스펙터에서 조정한다.
    [SerializeField, Min(1)] private int enhancedDamageMultiplier = 2;
    [SerializeField, Min(1f)] private float enhancedSizeMultiplier = 2f;
    [SerializeField, Min(1f)] private float enhancedLightMultiplier = 2f;

    private const int ATTACK_CYCLE_LENGTH = 3;
    private int attackIndex;
    private readonly HashSet<PlayerBullet> activeBullets = new HashSet<PlayerBullet>();
    private ComponentPool<PlayerBullet> pool;
    private Action<PlayerBullet> prepareBullet;
    private Action<PlayerBullet> returnedBullet;
    private float elapsed;
    private PlayerFrenzy frenzy;

    private void Awake()
    {
        frenzy = GetComponent<PlayerFrenzy>();
    }

    private void OnEnable()
    {
        if (frenzy != null)
            frenzy.Ended += ResetAttackCycle;
    }

    private void ResetAttackCycle()
    {
        attackIndex = 0;
    }

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
        float currentInterval = frenzy != null && frenzy.IsActive
            ? fireInterval * frenzyFireIntervalMultiplier
            : fireInterval;
        elapsed += Time.deltaTime;
        if (elapsed < currentInterval)
            return;
        elapsed %= currentInterval;
        pool.Rent(prepareBullet);
        if (frenzy == null || !frenzy.IsActive)
            attackIndex = (attackIndex + 1) % ATTACK_CYCLE_LENGTH;
    }

    private void PrepareBullet(PlayerBullet bullet)
    {
        bool enhanced = (frenzy != null && frenzy.IsActive)
            || attackIndex == ATTACK_CYCLE_LENGTH - 1;
        int shotDamage = enhanced ? damage * enhancedDamageMultiplier : damage;
        float shotRadius = enhanced ? bulletRadius * enhancedSizeMultiplier : bulletRadius;
        float lightMultiplier = enhanced ? enhancedLightMultiplier : 1f;

        float selectedHeight = arcHeightOptions[
            UnityEngine.Random.Range(0, arcHeightOptions.Length)];
        bullet.Prepare(firePoint.position, targetBoss, bulletSpeed, selectedHeight,
            shotRadius, shotDamage, lightMultiplier,
            playfield.bounds, pool, returnedBullet);
        activeBullets.Add(bullet);
    }

    private void RemoveBullet(PlayerBullet bullet) => activeBullets.Remove(bullet);

    private void OnDisable()
    {
        if (frenzy != null)
            frenzy.Ended -= ResetAttackCycle;
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
        frenzyFireIntervalMultiplier = Mathf.Clamp(frenzyFireIntervalMultiplier, 0.01f, 1f);
        initialPoolCount = Mathf.Max(0, initialPoolCount);
        bulletSpeed = Mathf.Max(0.01f, bulletSpeed);
        bulletRadius = Mathf.Max(0.01f, bulletRadius);
        if (arcHeightOptions == null || arcHeightOptions.Length == 0)
            arcHeightOptions = new[] { 1f, 2f, 1.5f }; // ##TEMP 초기 후보 복원
        for (int i = 0; i < arcHeightOptions.Length; i++)
            arcHeightOptions[i] = Mathf.Max(0f, arcHeightOptions[i]);
        damage = Mathf.Max(1, damage);
        enhancedDamageMultiplier = Mathf.Max(1, enhancedDamageMultiplier);
        enhancedSizeMultiplier = Mathf.Max(1f, enhancedSizeMultiplier);
        enhancedLightMultiplier = Mathf.Max(1f, enhancedLightMultiplier);
    }
}
