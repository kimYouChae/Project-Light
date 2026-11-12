using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>스테이지의 보스 등장·전투 종료·보상 전환 순서를 관리합니다.</summary>
public sealed class StageBattleFlow : MonoBehaviour
{
    private enum BattleState
    {
        Waiting, Fighting, Absorbing, Retreating, Finished, Complete, Entering, WaitingForReward,
        Stopped
    }

    [Header("===Component===")]
    [SerializeField] private StageBattleController stageBattle;
    [SerializeField] private BulletRegistry bulletRegistry;
    [SerializeField] private GameObject[] bossPrefabs;
    [SerializeField] private LightShardDropper lightShardDropper;
    [SerializeField, Range(0f, 1f)] private float battleEndHealRatio = 1f;

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
    private int bossIndex;
    private TMPro.TMP_Text completionLabel;
    private bool ownsClearPause;
    private float timeScaleBeforeClear;
    private PlayerFrenzy playerFrenzy;
    private PlayerPerfectDodge playerPerfectDodge;
    private Vector3 retreatStart;

    // 관리자가 생성한 현재 보스의 실행 참조
    private BossHealth bossHealth;
    private IBossPattern bossPattern;
    private Transform bossTransform;

    // COMPLETE 표시까지 끝났을 때 전달할 보상 요청
    private Action rewardRequested;
    private Action progressStopped;

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
    public bool IsRetreatComplete => state == BattleState.Complete
        || state == BattleState.WaitingForReward || state == BattleState.Finished;

    /// <summary>최종 보스 처치와 종료 처리가 끝났는지 반환합니다.</summary>
    public bool IsStageCleared => state == BattleState.Finished;

    /// <summary>플레이어 사망으로 스테이지 진행이 중단됐는지 반환합니다.</summary>
    public bool IsProgressStopped => state == BattleState.Stopped;

    /// <summary>스테이지 진행 중단 알림을 등록합니다.</summary>
    public void RegisterProgressStopped(Action callback)
    {
        progressStopped += callback;
    }

    /// <summary>스테이지 진행 중단 알림을 해제합니다.</summary>
    public void UnregisterProgressStopped(Action callback)
    {
        progressStopped -= callback;
    }

    private bool IsFinalBattle => bossIndex == bossPrefabs.Length - 1;

    private void OnEnable()
    {
        if (stageBattle != null)
            stageBattle.RegisterBattleEnded(OnBattleEnded);
    }

    private void Start()
    {
        completionLabel = completeText != null
            ? completeText.GetComponent<TMPro.TMP_Text>() : null;

        if (stageBattle == null || retreatPoint == null
            || bulletRegistry == null || completionLabel == null || lightShardDropper == null
            || playerHealth == null || playfield == null
            || bulletPoolParent == null
            || tailPoolParent == null
            || lanePoolParent == null
            || bossPrefabs == null || bossPrefabs.Length == 0)
        {
            PlayLog.Write("System", "보스 전투 흐름의 필수 참조를 확인해주세요.", LogType.Error, this);
            enabled = false;
            return;
        }
        playerFrenzy = playerHealth.GetComponent<PlayerFrenzy>();
        playerPerfectDodge = playerHealth.GetComponent<PlayerPerfectDodge>();
        if (playerFrenzy == null || playerPerfectDodge == null)
        {
            PlayLog.Write("System", "플레이어의 폭주·퍼펙트 컴포넌트를 확인해주세요.",
                LogType.Error, this);
            enabled = false;
            return;
        }

        bossIndex = 0;
        BeginCurrentBoss();
    }

    private void BeginCurrentBoss()
    {
        if (!stageBattle.TryCreateBoss(
                bossPrefabs[bossIndex],
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
            PlayLog.Write("System",
                "보스 패턴의 필수 컴포넌트 연결을 확인해주세요.", LogType.Error, this);
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
        PlayLog.Write("Stage", $"Boss Entrance Start ({bossHealth.name})");
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
        PlayLog.Write("Stage", $"Bullet Absorption Start ({absorptionBullets.Count} bullets)");
        foreach (TestBullet bullet in absorptionBullets)
            if (bullet != null)
                bullet.FreezeForEnding();
    }

    private void Update()
    {
        if (IsStageCleared || IsProgressStopped)
        {
            return;
        }

        if (playerHealth.IsDead)
        {
            StopProgressOnPlayerDeath();
            return;
        }

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

    private void StopProgressOnPlayerDeath()
    {
        state = BattleState.Stopped;
        completeText.SetActive(false);
        PlayLog.Write("Stage", "Progress Stopped (player defeated)");

        progressStopped?.Invoke();
        bossPattern?.StopPattern();

        lightShardDropper.ReleaseBoss(bossHealth);
        lightShardDropper.ReturnActiveShards();

        // 흡수 중인 탄은 이미 활성 목록에서 빠져 있으므로 두 목록을 회수한다.
        foreach (TestBullet bullet in absorptionBullets)
        {
            if (bullet != null)
            {
                bullet.Expire();
            }
        }

        absorptionBullets.Clear();

        foreach (TestBullet bullet in new List<TestBullet>(bulletRegistry.ActiveBullets))
        {
            if (bullet != null)
            {
                bullet.Expire();
            }
        }

        stageBattle.CancelBattle(bossHealth);
        bossHealth = null;
        bossPattern = null;
        bossTransform = null;
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
        PlayLog.Write("Stage", $"Boss Retreat Start ({bossHealth.name})");
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
        completeText.SetActive(!IsFinalBattle);
        PlayLog.Write("Stage", $"Boss Retreat Complete ({bossHealth.name})");
    }

    private void UpdateComplete(float deltaTime)
    {
        stateElapsed += Mathf.Max(0f, deltaTime);
        if (!IsFinalBattle && stateElapsed < completeDisplayDuration)
            return;
        completeText.SetActive(false);
        if (playerHealth.IsDead || lightShardDropper.ActiveShardCount > 0)
        {
            return;
        }

        // 종료 연출이 끝난 뒤 현재 대상 참조를 해제한다.
        if (!stageBattle.TryCompleteBattle(bossHealth))
        {
            enabled = false;
            return;
        }

        lightShardDropper.ReleaseBoss(bossHealth);
        int healAmount = Mathf.CeilToInt(playerHealth.MaxHealth * battleEndHealRatio);
        playerHealth.TryHeal(healAmount);
        playerFrenzy.End();
        playerPerfectDodge.ResetGauge();
        PlayLog.Write("Stage",
            $"Battle End Recovery (health={playerHealth.CurrentHealth}/{playerHealth.MaxHealth}, "
            + $"healRatio={battleEndHealRatio:F2}, gauge={playerPerfectDodge.CurrentGauge:F0})");

        bossHealth = null;
        bossPattern = null;
        bossTransform = null;

        if (IsFinalBattle)
        {
            ShowStageClear();
            return;
        }

        state = BattleState.WaitingForReward;
        rewardRequested?.Invoke();
    }

    private void ShowStageClear()
    {
        state = BattleState.Finished;
        completionLabel.text = "STAGE CLEAR";
        completeText.SetActive(true);

        timeScaleBeforeClear = Time.timeScale;
        ownsClearPause = true;
        Time.timeScale = 0f;
        PlayLog.Write("Stage", "Stage Clear");
    }

    /// <summary>짤보스 보상 확정 뒤 다음 보스를 등장시킵니다.</summary>
    public void AdvanceAfterReward()
    {
        if (!isActiveAndEnabled || state != BattleState.WaitingForReward
            || playerHealth == null || playerHealth.IsDead
            || bossIndex + 1 >= bossPrefabs.Length)
        {
            return;
        }

        bossIndex++;
        BeginCurrentBoss();
    }

    private void OnDisable()
    {
        if (ownsClearPause)
        {
            ownsClearPause = false;
            if (Time.timeScale == 0f)
            {
                Time.timeScale = timeScaleBeforeClear;
            }
        }

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
