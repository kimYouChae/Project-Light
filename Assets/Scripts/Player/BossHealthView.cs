using UnityEngine;

/// <summary>보스 체력 비율과 전투 종료에 따른 HP바 표시를 관리한다.</summary>
public sealed class BossHealthView : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private MiniBossBattleFlow battleFlow;
    [SerializeField] private GameObject barRoot;
    [SerializeField] private UnityEngine.UI.Image healthFill;

    private void LateUpdate()
    {
        if (bossHealth == null || battleFlow == null
            || barRoot == null || healthFill == null)
            return;

        bool visible = !battleFlow.IsRewardPending;
        if (barRoot.activeSelf != visible)
            barRoot.SetActive(visible);

        float ratio = Mathf.Clamp01(
            (float)bossHealth.CurrentHealth / bossHealth.MaxHealth);
        if (!Mathf.Approximately(healthFill.fillAmount, ratio))
            healthFill.fillAmount = ratio;
    }
}
