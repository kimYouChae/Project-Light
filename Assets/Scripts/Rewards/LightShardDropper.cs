using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>플레이어 탄의 직접 적중 때 확률로 빛 조각을 대여한다.</summary>
public sealed class LightShardDropper : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private LightShardWallet wallet;
    [SerializeField] private LightShard shardPrefab;
    [SerializeField] private Transform shardParent;
    [SerializeField] private SpriteRenderer playfield;
    // ##TEMP 맵 각 가장자리에서 제외할 비율.
    [SerializeField, Range(0f, 0.49f)] private float spawnMarginRatio = 0.1f;
    // ##TEMP 위쪽 생성 제외 비율.
    [SerializeField, Range(0f, 0.49f)] private float spawnTopMarginRatio = 0.2f;
    // 최대 체력 대비 이 비율 미만이면 낮은 체력 드롭 확률을 적용한다.
    // ##TEMP 낮은 체력 기준 20%. 인스펙터에서 조정한다.
    [SerializeField, Range(0f, 1f)]
    private float lowHealthThreshold = 0.2f;

    // 체력이 기준 이상일 때 드롭 확률
    [SerializeField, Range(0f, 1f)]
    private float normalDropChance = 0.1f;

    // 체력이 기준 미만일 때 드롭 확률
    [SerializeField, Range(0f, 1f)]
    private float lowHealthDropChance = 0.3f;

    // ##TEMP 플레이어 주변 생성 거리. 인스펙터에서 조정한다.
    [SerializeField, Min(0f)] private float minSpawnDistance = 1f;
    [SerializeField, Min(0f)] private float maxSpawnDistance = 2f;

    // ##TEMP 풀 사전 생성 수량.
    [SerializeField, Min(1)] private int initialPoolCount = 32;

    private ComponentPool<LightShard> shardPool;
    private readonly HashSet<LightShard> activeShards = new HashSet<LightShard>();
    private Action<LightShard> prepareShard;
    private Action<LightShard> releaseShard;
    private Vector3 spawnPosition;

    private void OnEnable()
    {
        if (bossHealth != null)
            bossHealth.PlayerAttackHit += OnPlayerAttackHit;
    }

    private void Start()
    {
        if (bossHealth == null || playerHealth == null || wallet == null
            || shardPrefab == null || shardParent == null || playfield == null)
        {
            Debug.LogError("빛 조각 드랍의 보스·플레이어·보유량·프리팹·부모·필드 참조를 확인해주세요.", this);
            enabled = false;
            return;
        }
        try
        {
            prepareShard = PrepareShard;
            releaseShard = ReturnShard;
            shardPool = new ComponentPool<LightShard>(
                new LightShardFactory(shardPrefab, shardParent), initialPoolCount, shardParent);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            enabled = false;
        }
    }

    private void OnPlayerAttackHit(Vector3 position)
    {
        if (shardPool == null)
        {
            return;
        }

        float healthRatio = playerHealth.MaxHealth > 0
            ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 0f;

        float chance = healthRatio < lowHealthThreshold
            ? lowHealthDropChance
            : normalDropChance;

        if (UnityEngine.Random.value >= chance)
        {
            return;
        }

        // 플레이어를 중심으로 지정한 거리 안에서 생성 위치를 정한다.
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float distance = UnityEngine.Random.Range(
            minSpawnDistance, maxSpawnDistance);

        Vector3 playerPosition = playerHealth.transform.position;
        Vector2 offset = new Vector2(
            Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

        Bounds bounds = playfield.bounds;
        float marginX = bounds.size.x * spawnMarginRatio;
        float marginY = bounds.size.y * spawnMarginRatio;
        // 필드 가장자리에서는 기존 생성 가능 영역 안으로 제한한다.
        spawnPosition = new Vector3(
            Mathf.Clamp(
                playerPosition.x + offset.x,
                bounds.min.x + marginX,
                bounds.max.x - marginX),
            Mathf.Clamp(
                playerPosition.y + offset.y,
                bounds.min.y + marginY,
                bounds.max.y - bounds.size.y * spawnTopMarginRatio),
            bounds.center.z);
        shardPool.Rent(prepareShard);
    }

    private void PrepareShard(LightShard shard)
    {
        shard.Prepare(spawnPosition, playerHealth, wallet, releaseShard);
        activeShards.Add(shard);
    }

    private void ReturnShard(LightShard shard)
    {
        activeShards.Remove(shard);
        shardPool.Return(shard);
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.PlayerAttackHit -= OnPlayerAttackHit;
        foreach (LightShard shard in new List<LightShard>(activeShards))
            if (shard != null)
                shard.ReturnToPool();
        activeShards.Clear();
    }

    private void OnDestroy() => shardPool?.Clear();

    private void OnValidate()
    {
        lowHealthThreshold = Mathf.Clamp01(lowHealthThreshold);
        normalDropChance = Mathf.Clamp01(normalDropChance);
        lowHealthDropChance = Mathf.Clamp01(lowHealthDropChance);
        minSpawnDistance = Mathf.Max(0f, minSpawnDistance);
        maxSpawnDistance = Mathf.Max(minSpawnDistance, maxSpawnDistance);
        spawnMarginRatio = Mathf.Clamp(spawnMarginRatio, 0f, 0.49f);
        spawnTopMarginRatio = Mathf.Clamp(spawnTopMarginRatio, 0f, 0.49f);
        initialPoolCount = Mathf.Max(1, initialPoolCount);
    }
}
