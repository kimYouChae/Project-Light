using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>짤보스 전투 시작·적탄 흡수·퇴장과 완료 표시를 관리한다.</summary>
public sealed class MiniBossBattleFlow : MonoBehaviour
{
    private enum BattleState { Waiting, Fighting, Absorbing, Retreating, Finished, Complete, Entering }

    [Header("===Component===")]
    [SerializeField] private StageBattleController stageBattle;
    [SerializeField] private BulletRegistry bulletRegistry;

    [Header("===패턴 씬 참조===")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SpriteRenderer playfield;
    [SerializeField] private Transform bulletPoolParent;
    [SerializeField] private Transform tailPoolParent;
    [SerializeField] private Transform lanePoolParent;

    [Header("===Trs===")]
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

    // 관리자가 생성한 현재 보스의 실행 참조
    private BossHealth bossHealth;
    private IBossPattern bossPattern;
    private Transform bossTransform;

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
        if (stageBattle != null)
            stageBattle.RegisterBattleEnded(OnBattleEnded);
    }

    private void Start()
    {
        if (stageBattle == null || retreatPoint == null
            || bulletRegistry == null || completeText == null
            || playerHealth == null || playfield == null
            || bulletPoolParent == null
            || tailPoolParent == null
            || lanePoolParent == null)
        {
            Debug.LogError("보스 전투 흐름의 필수 참조를 확인해주세요.", this);
            enabled = false;
            return;
        }
        if (!stageBattle.TryCreateBoss(
            out BossHealth health,
            out IBossPattern pattern))
        {
            enabled = false;
            return;
        }

        bossHealth = health;
        bossTransform = health.transform;
        bossPattern = pattern;

        if (bossPattern == null || !bossPattern.IsConfigured)
        {
            Debug.LogError(
                "보스 패턴의 필수 컴포넌트 연결을 확인해주세요.", this);
            stageBattle.CancelBattle(bossHealth);
            enabled = false;
            return;
        }

        // 생성된 패턴의 Start에서 풀을 만들기 전에 초기화한다.
        bossPattern.Initialize(
            playerHealth,
            playfield,
            bulletRegistry,
            bulletPoolParent,
            tailPoolParent,
            lanePoolParent);

        completeText.SetActive(false);
        state = BattleState.Entering;
        bossPattern.BeginEntrance();

        // 별도 등장 이동이 없는 보스는 즉시 전투를 시작한다.
        if (bossPattern.IsEntranceComplete)
        {
            StartBattle();
        }
    }

    /// <summary>등장 완료 후 현재 보스를 등록하고 패턴을 시작합니다.</summary>
    public void StartBattle()
    {
        if (state != BattleState.Entering || stageBattle == null
            || bossPattern == null || !bossPattern.IsEntranceComplete)
            return;
        if (!stageBattle.TryStartBattle(bossHealth))
            return;
        state = BattleState.Fighting;
        bossPattern.StartPattern();
        // 시작 알림 처리 중 체력이 소진된 경우에도 종료 흐름에 진입한다.
        if (bossHealth.IsDefeated)
            BeginBattleEnd();
    }

    private void OnBattleEnded(BossHealth endedBoss)
    {
        if (endedBoss != bossHealth)
            return;
        BeginBattleEnd();
    }

    private void BeginBattleEnd()
    {
        if (state != BattleState.Fighting)
            return;
        state = BattleState.Absorbing;
        stateElapsed = 0f;
        bossPattern.StopPattern();
        // 꼬리는 위 종료 처리에서 먼저 판정 목록에서 제외한다.
        absorptionBullets.Clear();
        absorptionBullets.AddRange(bulletRegistry.ActiveBullets);
        foreach (TestBullet bullet in absorptionBullets)
            if (bullet != null)
                bullet.FreezeForEnding();
    }

    private void Update()
    {
        if (state == BattleState.Entering)
        {
            if (bossPattern != null && bossPattern.IsEntranceComplete)
            {
                StartBattle();
            }
        }
        else if (state == BattleState.Absorbing)
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
        if (progress < 1f || !bossPattern.IsRetreatComplete)
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
        // 종료 연출이 끝난 뒤 현재 대상 참조를 해제한다.
        stageBattle.TryCompleteBattle(bossHealth);
        rewardRequested?.Invoke();
    }

    private void OnDisable()
    {
        if (stageBattle != null)
        {
            stageBattle.UnregisterBattleEnded(OnBattleEnded);
            stageBattle.CancelBattle(bossHealth);
        }
        if (bossPattern != null)
            bossPattern.StopPattern();
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
