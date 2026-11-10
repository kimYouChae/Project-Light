using System;
using UnityEngine;

/// <summary>
/// 현재 전투 대상과 전투 시작·종료 상태를 관리합니다.
/// </summary>
public sealed class StageBattleController : MonoBehaviour
{
    [Header("===보스 생성===")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform bossParent;

    // 이 관리자가 생성한 보스
    private GameObject createdBoss;

    private BossHealth currentBoss;
    private bool isFighting;

    private Action<BossHealth> battleStarted;
    private Action<BossHealth> battleEnded;

    /// <summary>현재 보스. 종료 연출이 끝날 때까지 유지합니다.</summary>
    public BossHealth CurrentBoss => currentBoss;

    /// <summary>현재 보스와 전투 중인지 반환합니다.</summary>
    public bool IsFighting => isFighting;

    /// <summary>전투 시작 알림을 등록합니다.</summary>
    public void RegisterBattleStarted(Action<BossHealth> callback)
    {
        battleStarted += callback;
    }

    /// <summary>전투 시작 알림을 해제합니다.</summary>
    public void UnregisterBattleStarted(Action<BossHealth> callback)
    {
        battleStarted -= callback;
    }

    /// <summary>전투 종료 알림을 등록합니다.</summary>
    public void RegisterBattleEnded(Action<BossHealth> callback)
    {
        battleEnded += callback;
    }

    /// <summary>전투 종료 알림을 해제합니다.</summary>
    public void UnregisterBattleEnded(Action<BossHealth> callback)
    {
        battleEnded -= callback;
    }

    /// <summary>
    /// 지정된 프리팹으로 보스를 생성하고 체력·패턴을 반환합니다.
    /// MiniBossBattleFlow.Start가 호출합니다.
    /// </summary>
    public bool TryCreateBoss(
        out BossHealth health,
        out IBossPattern pattern)
    {
        health = null;
        pattern = null;

        if (!isActiveAndEnabled || currentBoss != null || createdBoss != null)
        {
            return false;
        }

        if (bossPrefab == null || spawnPoint == null || bossParent == null)
        {
            PlayLog.Write("Stage", "보스 프리팹·생성 위치·부모를 지정해주세요.", LogType.Error, this);
            return false;
        }

        if (bossPrefab.GetComponent<BossHealth>() == null
            || bossPrefab.GetComponent<IBossPattern>() == null)
        {
            PlayLog.Write("Stage",
                "보스 프리팹 루트에 BossHealth와 IBossPattern 구현이 필요합니다.",
                LogType.Error, this);
            return false;
        }

        createdBoss = Instantiate(
            bossPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            bossParent);

        health = createdBoss.GetComponent<BossHealth>();
        pattern = createdBoss.GetComponent<IBossPattern>();
        return true;
    }

    /// <summary>
    /// 현재 대상이 없을 때 전투를 시작합니다.
    /// 보스의 전투 흐름 컴포넌트가 호출합니다.
    /// </summary>
    public bool TryStartBattle(BossHealth boss)
    {
        if (!isActiveAndEnabled || currentBoss != null
            || boss == null || boss.IsDefeated)
        {
            return false;
        }

        currentBoss = boss;
        isFighting = true;
        currentBoss.Defeated += OnBossDefeated;

        PlayLog.Write("Stage", $"Battle Start ({boss.name})");
        battleStarted?.Invoke(currentBoss);
        return true;
    }

    private void OnBossDefeated()
    {
        if (!isFighting || currentBoss == null)
        {
            return;
        }

        // 종료 연출 중에는 대상을 유지하고 종료 알림은 한 번만 보낸다.
        isFighting = false;
        currentBoss.Defeated -= OnBossDefeated;
        PlayLog.Write("Stage", $"Boss Defeated ({currentBoss.name})");
        battleEnded?.Invoke(currentBoss);
    }

    /// <summary>
    /// 종료 연출이 끝난 대상의 참조를 해제하고 생성한 보스를 제거합니다.
    /// MiniBossBattleFlow.UpdateComplete가 호출합니다.
    /// </summary>
    public bool TryCompleteBattle(BossHealth boss)
    {
        if (currentBoss == null || currentBoss != boss || isFighting)
        {
            return false;
        }

        PlayLog.Write("Stage", $"Battle End ({boss.name})");
        ClearCurrentBoss();
        DestroyCreatedBoss();
        return true;
    }

    /// <summary>
    /// 해당 보스의 관리를 중단하고 생성한 객체를 제거합니다.
    /// Flow의 초기화 실패 또는 비활성 처리에서 호출합니다.
    /// </summary>
    public void CancelBattle(BossHealth boss)
    {
        if (boss == null)
        {
            return;
        }

        bool isCurrentBoss = currentBoss == boss;
        bool isCreatedBoss = createdBoss != null
            && createdBoss == boss.gameObject;

        if (!isCurrentBoss && !isCreatedBoss)
        {
            return;
        }

        if (isCurrentBoss)
        {
            ClearCurrentBoss();
        }

        if (isCreatedBoss)
        {
            DestroyCreatedBoss();
        }
    }

    private void DestroyCreatedBoss()
    {
        if (createdBoss == null)
        {
            return;
        }

        Destroy(createdBoss);
        createdBoss = null;
    }

    private void ClearCurrentBoss()
    {
        if (currentBoss != null)
        {
            currentBoss.Defeated -= OnBossDefeated;
        }

        currentBoss = null;
        isFighting = false;
    }

    private void OnDisable()
    {
        ClearCurrentBoss();
        DestroyCreatedBoss();
    }
}
