using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>짤보스 전투 시작·적탄 흡수·퇴장과 보상 연결을 관리한다.</summary>
public sealed class MiniBossBattleFlow : MonoBehaviour
{
    private enum BattleState { Waiting, Fighting, Absorbing, Retreating, RewardPending }

    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private Transform bossTransform;
    [SerializeField] private Transform retreatPoint;
    [SerializeField] private BossBulletPattern bulletPattern;
    [SerializeField] private TailAttackPattern tailPattern;
    [SerializeField] private TailLanePattern lanePattern;
    [SerializeField] private BulletRegistry bulletRegistry;
    [SerializeField] private GameObject completeText;
    // ##TEMP 사용자 승인 전투 종료 연출 시간.
    [SerializeField, Min(0f)] private float freezeDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float absorptionDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float retreatDuration = 1f;
    [SerializeField] private BattleState state;

    private readonly List<TestBullet> absorptionBullets = new List<TestBullet>();
    private float stateElapsed;
    private Vector3 retreatStart;

    /// <summary>퇴장과 완료 표시 후 발생한다. 구독자는 비활성화 시 해제한다.</summary>
    public event Action RewardRequested;

    /// <summary>보상 연결 단계까지 도달했는지 반환한다.</summary>
    public bool IsRewardPending => state == BattleState.RewardPending;

    private void OnEnable()
    {
        if (bossHealth != null)
            bossHealth.Defeated += BeginBattleEnd;
    }

    private void Start()
    {
        if (bossHealth == null || bossTransform == null || retreatPoint == null
            || bulletPattern == null || tailPattern == null || lanePattern == null
            || bulletRegistry == null || completeText == null)
        {
            Debug.LogError("짤보스 전투 흐름의 필수 참조를 확인해주세요.", this);
            enabled = false;
            return;
        }
        StartBattle();
    }

    /// <summary>대기 상태에서 세 패턴을 가동해 전투를 시작한다.</summary>
    public void StartBattle()
    {
        if (state != BattleState.Waiting)
            return;
        completeText.SetActive(false);
        state = BattleState.Fighting;
        bulletPattern.StartEmission();
        tailPattern.StartEmission();
        lanePattern.StartEmission();
        if (bossHealth.IsDefeated)
            BeginBattleEnd();
    }

    private void BeginBattleEnd()
    {
        if (state != BattleState.Fighting)
            return;
        state = BattleState.Absorbing;
        stateElapsed = 0f;
        bulletPattern.StopForBattleEnd();
        tailPattern.StopForBattleEnd();
        lanePattern.StopForBattleEnd();
        // 꼬리는 위 종료 처리에서 먼저 판정 목록에서 제외한다.
        absorptionBullets.Clear();
        absorptionBullets.AddRange(bulletRegistry.ActiveBullets);
        foreach (TestBullet bullet in absorptionBullets)
            if (bullet != null)
                bullet.FreezeForEnding();
    }

    private void Update()
    {
        if (state == BattleState.Absorbing)
            UpdateAbsorption(Time.deltaTime);
        else if (state == BattleState.Retreating)
            UpdateRetreat(Time.deltaTime);
    }

    private void UpdateAbsorption(float deltaTime)
    {
        stateElapsed += Mathf.Max(0f, deltaTime);
        if (stateElapsed < freezeDuration)
            return;
        float progress = Mathf.Clamp01((stateElapsed - freezeDuration) / absorptionDuration);
        foreach (TestBullet bullet in absorptionBullets)
            if (bullet != null)
                bullet.AdvanceAbsorption(bossTransform.position, progress);
        if (progress < 1f || !tailPattern.IsRetreatComplete || !lanePattern.IsRetreatComplete)
            return;
        absorptionBullets.Clear();
        retreatStart = bossTransform.position;
        stateElapsed = 0f;
        state = BattleState.Retreating;
    }

    private void UpdateRetreat(float deltaTime)
    {
        stateElapsed += Mathf.Max(0f, deltaTime);
        float progress = Mathf.Clamp01(stateElapsed / retreatDuration);
        bossTransform.position = Vector3.Lerp(retreatStart, retreatPoint.position,
            Mathf.SmoothStep(0f, 1f, progress));
        if (progress < 1f)
            return;
        state = BattleState.RewardPending;
        completeText.SetActive(true);
        RewardRequested?.Invoke();
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.Defeated -= BeginBattleEnd;
        if (bulletPattern != null)
            bulletPattern.StopForBattleEnd();
        if (tailPattern != null)
            tailPattern.StopForBattleEnd();
        if (lanePattern != null)
            lanePattern.StopForBattleEnd();
        foreach (TestBullet bullet in absorptionBullets)
            if (bullet != null)
                bullet.Expire();
        absorptionBullets.Clear();
    }

    private void OnValidate()
    {
        freezeDuration = Mathf.Max(0f, freezeDuration);
        absorptionDuration = Mathf.Max(0.01f, absorptionDuration);
        retreatDuration = Mathf.Max(0.01f, retreatDuration);
    }
}
