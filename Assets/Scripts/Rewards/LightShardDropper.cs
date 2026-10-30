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
    // ##TEMP 사용자 승인 고정 드랍 확률과 풀 사전 생성 수량.
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.1f;
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
        if (shardPool == null || UnityEngine.Random.value >= dropChance)
            return;
        Bounds bounds = playfield.bounds;
        float marginX = bounds.size.x * spawnMarginRatio;
        float marginY = bounds.size.y * spawnMarginRatio;
        spawnPosition = new Vector3(
            UnityEngine.Random.Range(bounds.min.x + marginX, bounds.max.x - marginX),
            UnityEngine.Random.Range(bounds.min.y + marginY,
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
        dropChance = Mathf.Clamp01(dropChance);
        spawnMarginRatio = Mathf.Clamp(spawnMarginRatio, 0f, 0.49f);
        spawnTopMarginRatio = Mathf.Clamp(spawnTopMarginRatio, 0f, 0.49f);
        initialPoolCount = Mathf.Max(1, initialPoolCount);
    }
}
