using UnityEngine;

/// <summary>보스 체력 비율과 전투 종료에 따른 HP바 표시를 관리한다.</summary>
public sealed class BossHealthView : MonoBehaviour
{
    [SerializeField] private StageBattleController stageBattle;
    [SerializeField] private StageBattleFlow battleFlow;
    [SerializeField] private GameObject barRoot;
    [SerializeField] private UnityEngine.UI.Image healthFill;

    // 전투 시작 알림으로 전달받는 표시 대상
    private BossHealth bossHealth;

    private void OnEnable()
    {
        if (stageBattle != null)
        {
            stageBattle.RegisterBattleStarted(OnBattleStarted);
            bossHealth = stageBattle.CurrentBoss;
        }
    }

    private void OnBattleStarted(BossHealth boss)
    {
        bossHealth = boss;
    }

    private void LateUpdate()
    {
        if (stageBattle == null || battleFlow == null
            || barRoot == null || healthFill == null)
            return;

        if (stageBattle.CurrentBoss == null)
        {
            bossHealth = null;
        }

        bool visible = bossHealth != null
            && stageBattle.CurrentBoss == bossHealth
            && !battleFlow.IsRetreatComplete;
        if (barRoot.activeSelf != visible)
            barRoot.SetActive(visible);

        if (bossHealth == null)
            return;

        float ratio = Mathf.Clamp01(
            (float)bossHealth.CurrentHealth / bossHealth.MaxHealth);
        if (!Mathf.Approximately(healthFill.fillAmount, ratio))
            healthFill.fillAmount = ratio;
    }

    private void OnDisable()
    {
        if (stageBattle != null)
        {
            stageBattle.UnregisterBattleStarted(OnBattleStarted);
        }

        bossHealth = null;
    }
}
