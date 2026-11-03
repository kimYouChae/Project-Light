using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>살아 있는 보스에게 일정 간격으로 풀링 기본 공격을 발사한다.</summary>
[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerAutoAttack : MonoBehaviour
{
    [Header("===Component===")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerBullet bulletPrefab;
    [SerializeField] private BossHealth targetBoss;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SpriteRenderer playfield;
    [SerializeField] private Transform firePoint;
    [Header("===Trs===")]
    [SerializeField] private Transform bulletParent;
    [SerializeField, Min(0.01f)] private float fireInterval = 2f;

    [Header("===강화 스탯===")]
    // ##TEMP 폭주 중 발사 간격 배율. 0.5면 간격이 절반이다.
    [SerializeField, Range(0.01f, 1f)] private float frenzyFireIntervalMultiplier = 0.5f;
    [SerializeField, Min(0)] private int initialPoolCount = 16; // ##TEMP 초기 풀 크기
    // ##TEMP 
    // 발사마다 동일 확률로 선택할 포물선 높이.
    [SerializeField] private float[] arcHeightOptions = { 1f, 2f, 1.5f }; 
    // ##TEMP 
    [SerializeField, Min(1)] private int enhancedDamageMultiplier = 2;  // 강화 데미지 배욜 
    [SerializeField, Min(1f)] private float enhancedSizeMultiplier = 2f; // 강화 사이즈 배율
    [SerializeField, Min(1f)] private float enhancedLightMultiplier = 2f;   //강화 빛 배율
    // ##TEMP 
    [SerializeField, Min(1)] private int enhancedShotCount = 3;     //강화 탄 발사 갯수
    [SerializeField, Min(0.01f)] private float enhancedShotInterval = 0.2f; // 강화탄 사이 간격

    private readonly Queue<float> enhancedShotTimes = new Queue<float>();
    private float shotClock;
    private bool preparingEnhanced;

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
        stats = GetComponent<PlayerStats>();
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
            || bulletParent == null || playerHealth == null || playfield == null || stats == null)
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
        // 필드 시간이 정지된 동안 이동·공격·판정·획득을 처리하지 않는다.
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (pool == null || playerHealth.IsDead || targetBoss == null || targetBoss.IsDefeated)
        {
            enhancedShotTimes.Clear();
            return;
        }
        shotClock += Time.deltaTime;
        while (enhancedShotTimes.Count > 0 && enhancedShotTimes.Peek() <= shotClock)
        {
            enhancedShotTimes.Dequeue();
            FireBullet(true);
        }
        float currentInterval = frenzy != null && frenzy.IsActive
            ? fireInterval * frenzyFireIntervalMultiplier
            : fireInterval;
        elapsed += Time.deltaTime;
        if (elapsed < currentInterval)
            return;
        elapsed %= currentInterval;
        BeginAttack();
    }

    private void BeginAttack()
    {
        bool enhanced = (frenzy != null && frenzy.IsActive)
            || attackIndex == ATTACK_CYCLE_LENGTH - 1;
        FireBullet(enhanced);
        if (enhanced)
        {
            for (int i = 1; i < enhancedShotCount; i++)
                enhancedShotTimes.Enqueue(shotClock + enhancedShotInterval * i);
        }
        if (frenzy == null || !frenzy.IsActive)
            attackIndex = (attackIndex + 1) % ATTACK_CYCLE_LENGTH;
    }

    private void FireBullet(bool enhanced)
    {
        preparingEnhanced = enhanced;
        pool.Rent(prepareBullet);
    }

    private void PrepareBullet(PlayerBullet bullet)
    {
        bool enhanced = preparingEnhanced;
        int shotDamage = enhanced ? stats.Damage * enhancedDamageMultiplier : stats.Damage;
        float shotRadius = enhanced ? stats.BulletRadius * enhancedSizeMultiplier : stats.BulletRadius;
        float lightMultiplier = enhanced ? enhancedLightMultiplier : 1f;

        float selectedHeight = arcHeightOptions[
            UnityEngine.Random.Range(0, arcHeightOptions.Length)];
        bullet.Prepare(firePoint.position, targetBoss, stats.BulletSpeed, selectedHeight,
            shotRadius, shotDamage, lightMultiplier,
            playfield.bounds, pool, returnedBullet, sightRadiusMultiplier: stats.BulletSizeMultiplier);
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
        enhancedShotTimes.Clear();
        shotClock = 0f;
        preparingEnhanced = false;
        elapsed = 0f;
    }

    private void OnDestroy() => pool?.Clear();

    private void OnValidate()
    {
        fireInterval = Mathf.Max(0.01f, fireInterval);
        frenzyFireIntervalMultiplier = Mathf.Clamp(frenzyFireIntervalMultiplier, 0.01f, 1f);
        initialPoolCount = Mathf.Max(0, initialPoolCount);
        if (arcHeightOptions == null || arcHeightOptions.Length == 0)
            arcHeightOptions = new[] { 1f, 2f, 1.5f }; // ##TEMP 초기 후보 복원
        for (int i = 0; i < arcHeightOptions.Length; i++)
            arcHeightOptions[i] = Mathf.Max(0f, arcHeightOptions[i]);
        enhancedDamageMultiplier = Mathf.Max(1, enhancedDamageMultiplier);
        enhancedSizeMultiplier = Mathf.Max(1f, enhancedSizeMultiplier);
        enhancedLightMultiplier = Mathf.Max(1f, enhancedLightMultiplier);
        enhancedShotCount = Mathf.Max(1, enhancedShotCount);
        enhancedShotInterval = Mathf.Max(0.01f, enhancedShotInterval);
    }
}
