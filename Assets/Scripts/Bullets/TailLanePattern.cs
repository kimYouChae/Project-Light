using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>랜덤 레인·방향의 꼬리 내려찍기를 경고하고 풀에서 실행한다.</summary>
public sealed class TailLanePattern : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playfield;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform objectParent;
    [SerializeField] private TailLaneWarning warningPrefab;
    [SerializeField] private TailLaneHazard hazardPrefab;
    // ##TEMP 사용자 승인 프로토타입 수치.
    [SerializeField, Min(0.01f)] private float patternInterval = 30f;
    [SerializeField, Min(0.01f)] private float warningDuration = 5f;
    [SerializeField, Min(0.01f)] private float holdDuration = 3f;
    [SerializeField, Min(1)] private int damage = 2;
    [SerializeField, Range(0f, 0.49f)] private float safeEdgeRatio = 0.1f;
    [SerializeField, Min(0.01f)] private float travelDuration = 0.2f;
    // ##TEMP 경고와 꼬리 풀 사전 생성 수량.
    [SerializeField, Min(1)] private int initialCount = 2;

    private ComponentPool<TailLaneWarning> warningPool;
    private ComponentPool<TailLaneHazard> hazardPool;
    private readonly List<Attack> attacks = new List<Attack>();
    private double patternTime;
    private double nextWarningTime;
    private bool emitting;

    /// <summary>레인 꼬리 회수가 모두 끝났는지 반환한다.</summary>
    public bool IsRetreatComplete => attacks.Count == 0;

    /// <summary>전투 시작부터 새 레인 경고와 공격을 생성한다.</summary>
    public void StartEmission() => emitting = true;

    /// <summary>경고를 취소하고 이미 나온 꼬리만 피해 없이 회수한다.</summary>
    public void StopForBattleEnd()
    {
        emitting = false;
        for (int i = attacks.Count - 1; i >= 0; i--)
        {
            Attack attack = attacks[i];
            if (attack.warning != null)
                warningPool?.Return(attack.warning);
            attack.warning = null;
            if (attack.hazard == null)
                attacks.RemoveAt(i);
            else
                attack.hazard.BeginRetreat();
        }
    }

    private sealed class Attack
    {
        public TailLaneWarning warning;
        public TailLaneHazard hazard;
        public Bounds field;
        public Bounds area;
        public bool fromTop;
        public double startTime;
        public float warningTime;
        public float holdTime;
        public float travelTime;
        public int damage;
    }

    private void Start()
    {
        if (playfield == null || playerHealth == null || objectParent == null
            || warningPrefab == null || hazardPrefab == null)
        {
            Debug.LogError("레인 꼬리 패턴 참조를 확인해주세요.", this);
            enabled = false;
            return;
        }
        try
        {
            warningPool = new ComponentPool<TailLaneWarning>(
                new TailLaneWarningFactory(warningPrefab, objectParent), initialCount, objectParent);
            hazardPool = new ComponentPool<TailLaneHazard>(
                new TailLaneHazardFactory(hazardPrefab, objectParent), initialCount, objectParent);
            ResetPattern();
        }
        catch (Exception exception)
        {
            warningPool?.Clear();
            hazardPool?.Clear();
            Debug.LogException(exception, this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (warningPool != null && hazardPool != null)
            AdvancePattern(Time.deltaTime);
    }

    private void AdvancePattern(float deltaTime)
    {
        if (!emitting)
        {
            for (int i = attacks.Count - 1; i >= 0; i--)
            {
                Attack attack = attacks[i];
                if (attack.hazard != null)
                {
                    attack.hazard.AdvanceRetreat(deltaTime);
                    if (!attack.hazard.IsRetreatComplete)
                        continue;
                }
                ReturnAttack(attack);
                attacks.RemoveAt(i);
            }
            return;
        }
        patternTime += Mathf.Max(0f, deltaTime);
        while (nextWarningTime <= patternTime)
        {
            BeginWarning(nextWarningTime);
            nextWarningTime += patternInterval;
        }
        for (int i = attacks.Count - 1; i >= 0; i--)
        {
            Attack attack = attacks[i];
            float age = (float)(patternTime - attack.startTime) - attack.warningTime;
            if (age < 0f)
                continue;
            if (age >= attack.travelTime * 2f + attack.holdTime)
            {
                ReturnAttack(attack);
                attacks.RemoveAt(i);
                continue;
            }
            if (attack.hazard == null)
            {
                attack.warning.HideAttackWarning();
                attack.hazard = hazardPool.Rent(hazard => hazard.Prepare(attack.field, attack.area,
                    attack.fromTop, attack.holdTime, attack.travelTime, attack.damage, playerHealth));
            }
            attack.hazard.Advance(age);
        }
    }

    private void BeginWarning(double startTime)
    {
        Bounds field = playfield.bounds;
        int laneIndex = UnityEngine.Random.Range(0, 3);
        bool fromTop = UnityEngine.Random.Range(0, 2) == 0;
        float laneWidth = field.size.x / 3f;
        float laneLeft = field.min.x + laneWidth * laneIndex;
        float dangerLeft = laneLeft + laneWidth * safeEdgeRatio;
        float dangerRight = laneLeft + laneWidth * (1f - safeEdgeRatio);
        Bounds area = new Bounds(new Vector3((dangerLeft + dangerRight) * 0.5f, field.center.y, 0f),
            new Vector3(dangerRight - dangerLeft, field.size.y, 0f));
        TailLaneWarning warning = warningPool.Rent(item => item.Prepare(field, area, fromTop, safeEdgeRatio));
        attacks.Add(new Attack
        {
            warning = warning, field = field, area = area, fromTop = fromTop, startTime = startTime,
            warningTime = warningDuration, holdTime = holdDuration, travelTime = travelDuration, damage = damage
        });
    }

    private void ReturnAttack(Attack attack)
    {
        if (attack.warning != null)
            warningPool?.Return(attack.warning);
        if (attack.hazard != null)
            hazardPool?.Return(attack.hazard);
    }

    private void ResetPattern()
    {
        patternTime = 0d;
        nextWarningTime = patternInterval;
    }

    private void OnDisable()
    {
        emitting = false;
        foreach (Attack attack in attacks)
            ReturnAttack(attack);
        attacks.Clear();
        ResetPattern();
    }

    private void OnDestroy()
    {
        warningPool?.Clear();
        hazardPool?.Clear();
    }

    private void OnValidate()
    {
        patternInterval = Mathf.Max(0.01f, patternInterval);
        warningDuration = Mathf.Max(0.01f, warningDuration);
        holdDuration = Mathf.Max(0.01f, holdDuration);
        travelDuration = Mathf.Max(0.01f, travelDuration);
        damage = Mathf.Max(1, damage);
        safeEdgeRatio = Mathf.Clamp(safeEdgeRatio, 0f, 0.49f);
        initialCount = Mathf.Max(1, initialCount);
    }
}
