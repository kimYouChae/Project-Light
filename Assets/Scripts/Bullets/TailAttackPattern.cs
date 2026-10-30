using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>발밑 경고 후 꼬리와 가속 흙 탄을 생성하는 패턴.</summary>
public sealed class TailAttackPattern : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SpriteRenderer playfield;
    [SerializeField] private BulletRegistry bulletRegistry;
    [SerializeField] private Transform objectParent;
    [SerializeField] private SpriteRenderer warningPrefab;
    [SerializeField] private TestBullet tailPrefab;
    [SerializeField] private TestBullet dirtPrefab;

    // ##TEMP 승인한 꼬리 패턴 테스트값.
    [SerializeField, Min(0.01f)] private float patternInterval = 10f;
    [SerializeField, Min(0.01f)] private float warningDuration = 1f;
    [SerializeField, Min(0.01f)] private float tailDuration = 1f;
    [SerializeField, Min(0.01f)] private float tailRadius = 0.6f;
    [SerializeField, Min(1)] private int tailDamage = 2;
    // ##TEMP 승인한 흙 원형탄 테스트값.
    [SerializeField, Min(1)] private int dirtSlotCount = 12;
    [SerializeField] private int[] gapSlots = { 0, 6 };
    [SerializeField, Min(0f)] private float dirtInitialSpeed = 0.5f;
    [SerializeField, Min(0f)] private float dirtAcceleration = 1f;
    // ##TEMP 기존 적탄 크기와 풀 사전 생성 수량.
    [SerializeField, Min(0.01f)] private float dirtRadius = 0.12f;
    [SerializeField, Min(1)] private int initialWarningCount = 2;
    [SerializeField, Min(1)] private int initialTailCount = 2;
    [SerializeField, Min(1)] private int initialDirtCount = 64;

    private readonly List<PendingEruption> pendingEruptions = new List<PendingEruption>();
    private readonly HashSet<TestBullet> activeTails = new HashSet<TestBullet>();
    private readonly HashSet<TestBullet> activeDirt = new HashSet<TestBullet>();
    private ComponentPool<SpriteRenderer> warningPool;
    private ComponentPool<TestBullet> tailPool;
    private ComponentPool<TestBullet> dirtPool;
    private Action<SpriteRenderer> prepareWarning;
    private Action<TestBullet> prepareTail;
    private Action<TestBullet> prepareDirt;
    private Action<TestBullet> releaseTail;
    private Action<TestBullet> releaseDirt;
    private Vector3 spawnPosition;
    private Vector2 spawnDirection;
    private double patternTime;
    private double nextWarningTime;
    private bool emitting;

    /// <summary>발밑 꼬리 회수가 모두 끝났는지 반환한다.</summary>
    public bool IsRetreatComplete => activeTails.Count == 0;

    /// <summary>전투 시작부터 새 경고와 공격을 생성한다.</summary>
    public void StartEmission() => emitting = true;

    /// <summary>경고 예약을 취소하고 이미 나온 꼬리를 피해 없이 회수한다.</summary>
    public void StopForBattleEnd()
    {
        emitting = false;
        foreach (PendingEruption pending in pendingEruptions)
            if (pending.warning != null)
                warningPool?.Return(pending.warning);
        pendingEruptions.Clear();
        foreach (TestBullet tail in activeTails)
        {
            if (tail == null)
                continue;
            tail.FreezeForEnding();
            tail.GetComponent<TailRiseVisual>().BeginRetreat();
        }
    }

    private sealed class PendingEruption
    {
        public SpriteRenderer warning;
        public Vector3 position;
        public double eruptionTime;
    }

    private void Start()
    {
        if (playerTransform == null || playerHealth == null || playfield == null
            || bulletRegistry == null || objectParent == null || warningPrefab == null
            || tailPrefab == null || tailPrefab.GetComponent<TailRiseVisual>() == null
            || dirtPrefab == null || dirtPrefab.Movement == null)
        {
            Debug.LogError("꼬리 패턴의 플레이어·필드·풀 프리팹 참조를 확인해주세요.", this);
            enabled = false;
            return;
        }
        try
        {
            prepareWarning = PrepareWarning;
            prepareTail = PrepareTail;
            prepareDirt = PrepareDirt;
            releaseTail = ReleaseTail;
            releaseDirt = ReleaseDirt;
            warningPool = new ComponentPool<SpriteRenderer>(
                new TailWarningFactory(warningPrefab, objectParent), initialWarningCount, objectParent);
            tailPool = new ComponentPool<TestBullet>(
                new TestBulletFactory(tailPrefab, objectParent), initialTailCount, objectParent);
            dirtPool = new ComponentPool<TestBullet>(
                new TestBulletFactory(dirtPrefab, objectParent), initialDirtCount, objectParent);
            ResetPattern();
        }
        catch (Exception exception)
        {
            warningPool?.Clear();
            tailPool?.Clear();
            dirtPool?.Clear();
            Debug.LogException(exception, this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (warningPool != null && tailPool != null && dirtPool != null && playerTransform != null)
            AdvancePattern(Time.deltaTime);
    }

    private void AdvancePattern(float deltaTime)
    {
        if (!emitting)
        {
            foreach (TestBullet tail in new List<TestBullet>(activeTails))
            {
                if (tail == null)
                {
                    activeTails.Remove(tail);
                    continue;
                }
                TailRiseVisual visual = tail.GetComponent<TailRiseVisual>();
                visual.AdvanceRetreat(deltaTime);
                if (visual.IsRetreatComplete)
                    tail.Expire();
            }
            return;
        }
        double endTime = patternTime + Mathf.Max(0f, deltaTime);
        while (true)
        {
            int pendingIndex = -1;
            double eruptionTime = double.PositiveInfinity;
            for (int i = 0; i < pendingEruptions.Count; i++)
            {
                if (pendingEruptions[i].eruptionTime < eruptionTime)
                {
                    pendingIndex = i;
                    eruptionTime = pendingEruptions[i].eruptionTime;
                }
            }
            if (Math.Min(nextWarningTime, eruptionTime) > endTime)
                break;
            if (eruptionTime <= nextWarningTime)
            {
                PendingEruption pending = pendingEruptions[pendingIndex];
                pendingEruptions.RemoveAt(pendingIndex);
                spawnPosition = pending.position;
                warningPool.Return(pending.warning);
                tailPool.Rent(prepareTail);
                SpawnDirt();
            }
            else
            {
                spawnPosition = playerTransform.position;
                SpriteRenderer warning = warningPool.Rent(prepareWarning);
                pendingEruptions.Add(new PendingEruption
                {
                    warning = warning,
                    position = spawnPosition,
                    eruptionTime = nextWarningTime + warningDuration
                });
                nextWarningTime += patternInterval;
            }
        }
        patternTime = endTime;
    }

    private void PrepareWarning(SpriteRenderer warning)
    {
        warning.transform.position = spawnPosition;
        warning.transform.localScale = Vector3.one * (tailRadius * 2f);
    }

    private void PrepareTail(TestBullet tail)
    {
        tail.Prepare(spawnPosition, Vector2.down, 0f, tailDuration, tailRadius,
            playfield.bounds, tailPool, releaseTail, playerHealth, bulletRegistry,
            damageOverride: tailDamage, hitOnce: true);
        tail.GetComponent<TailRiseVisual>().Prepare();
        activeTails.Add(tail);
    }

    private void SpawnDirt()
    {
        for (int slot = 0; slot < dirtSlotCount; slot++)
        {
            if (IsGap(slot))
                continue;
            float angle = slot * Mathf.PI * 2f / dirtSlotCount;
            spawnDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            dirtPool.Rent(prepareDirt);
        }
    }

    private bool IsGap(int slot)
    {
        if (gapSlots != null)
            foreach (int gap in gapSlots)
                if (gap == slot)
                    return true;
        return false;
    }

    private void PrepareDirt(TestBullet dirt)
    {
        dirt.Prepare(spawnPosition, spawnDirection, dirtInitialSpeed, float.PositiveInfinity,
            dirtRadius, playfield.bounds, dirtPool, releaseDirt, playerHealth, bulletRegistry);
        dirt.Movement.PrepareAccelerating(spawnPosition, spawnDirection,
            dirtInitialSpeed, dirtAcceleration);
        activeDirt.Add(dirt);
    }

    private void ReleaseTail(TestBullet tail)
    {
        activeTails.Remove(tail);
        tailPool.Return(tail);
    }

    private void ReleaseDirt(TestBullet dirt)
    {
        activeDirt.Remove(dirt);
        dirtPool.Return(dirt);
    }

    private void ResetPattern()
    {
        patternTime = 0d;
        nextWarningTime = patternInterval;
    }

    private void OnDisable()
    {
        emitting = false;
        foreach (PendingEruption pending in pendingEruptions)
        {
            if (pending.warning != null)
                warningPool?.Return(pending.warning);
        }
        pendingEruptions.Clear();
        foreach (TestBullet tail in new List<TestBullet>(activeTails))
            if (tail != null)
                tail.Expire();
        foreach (TestBullet dirt in new List<TestBullet>(activeDirt))
            if (dirt != null)
                dirt.Expire();
        activeTails.Clear();
        activeDirt.Clear();
        ResetPattern();
    }

    private void OnDestroy()
    {
        warningPool?.Clear();
        tailPool?.Clear();
        dirtPool?.Clear();
    }

    private void OnValidate()
    {
        patternInterval = Mathf.Max(0.01f, patternInterval);
        warningDuration = Mathf.Max(0.01f, warningDuration);
        tailDuration = Mathf.Max(0.01f, tailDuration);
        tailRadius = Mathf.Max(0.01f, tailRadius);
        tailDamage = Mathf.Max(1, tailDamage);
        dirtSlotCount = Mathf.Max(1, dirtSlotCount);
        dirtInitialSpeed = Mathf.Max(0f, dirtInitialSpeed);
        dirtAcceleration = Mathf.Max(0f, dirtAcceleration);
        dirtRadius = Mathf.Max(0.01f, dirtRadius);
        initialWarningCount = Mathf.Max(1, initialWarningCount);
        initialTailCount = Mathf.Max(1, initialTailCount);
        initialDirtCount = Mathf.Max(1, initialDirtCount);
    }
}
