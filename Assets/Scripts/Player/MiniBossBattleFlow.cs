using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>짤보스 전투 시작·적탄 흡수·퇴장과 완료 표시를 관리한다.</summary>
public sealed class MiniBossBattleFlow : MonoBehaviour
{
    private enum BattleState { Waiting, Fighting, Absorbing, Retreating, Finished, Complete }

    [Header("===Component===")]
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private BossBulletPattern bulletPattern;
    [SerializeField] private TailAttackPattern tailPattern;
    [SerializeField] private TailLanePattern lanePattern;
    [SerializeField] private BulletRegistry bulletRegistry;

    [Header("===Trs===")]
    [SerializeField] private Transform bossTransform;
    [SerializeField] private Transform retreatPoint;
    [SerializeField] private GameObject completeText;

    [Header("===연출 종료 수치===")]
    [SerializeField, Min(0f)] private float freezeDuration = 0.15f; // 보스가 죽은 후 탄이 자리에 멈춰있는 시간
    [SerializeField, Min(0.01f)] private float absorptionDuration = 0.6f; // 멈춘 직후 보스에게 빨려들어가는 시간
    [SerializeField, Min(0.01f)] private float retreatDuration = 1f; // 회수 끝난 후 보스가 퇴장하는 시간 
    [SerializeField, Min(0f)] private float completeDisplayDuration = 3f;   // "Complete" 텍스트 보여주는 시간
    [SerializeField] private BattleState state;

    private readonly List<TestBullet> absorptionBullets = new List<TestBullet>();
    private float stateElapsed;
    private Vector3 retreatStart;

    // COMPLETE 표시까지 끝났을 때 전달할 보상 요청
    private Action rewardRequested;

    /// <summary>보상 요청 알림을 등록합니다.</summary>
    public void RegisterRewardRequested(Action callback)
    {
        rewardRequested += callback;
    }

    /// <summary>보상 요청 알림을 해제합니다.</summary>
    public void UnregisterRewardRequested(Action callback)
    {
        rewardRequested -= callback;
    }

    /// <summary>보스 퇴장이 끝나 HP바를 숨겨야 하는지 반환한다.</summary>
    public bool IsRetreatComplete => state == BattleState.Complete || state == BattleState.Finished;

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
        else if (state == BattleState.Complete)
            UpdateComplete(Time.deltaTime);
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
        state = BattleState.Complete;
        stateElapsed = 0f;
        completeText.SetActive(true);
    }

    private void UpdateComplete(float deltaTime)
    {
        stateElapsed += Mathf.Max(0f, deltaTime);
        if (stateElapsed < completeDisplayDuration)
            return;
        completeText.SetActive(false);
        state = BattleState.Finished;
        rewardRequested?.Invoke();
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
        completeDisplayDuration = Mathf.Max(0f, completeDisplayDuration);
    }
}
