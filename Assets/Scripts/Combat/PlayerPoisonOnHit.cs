using UnityEngine;

/// <summary>
/// 획득 기록에서 독 부여량을 계산하고 적중 시 저장값을 적용합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttackEvents))]
[RequireComponent(typeof(RewardSelectionHistory))]
public sealed class PlayerPoisonOnHit : MonoBehaviour
{
    private PlayerAttackEvents attackEvents;
    private RewardSelectionHistory history;

    // 획득 기록에서 미리 계산한 공격별 독 부여량
    private int normalStacks;
    private int enhancedStacks;
    private int frenzyStacks;

    private void Awake()
    {
        attackEvents = GetComponent<PlayerAttackEvents>();
        history = GetComponent<RewardSelectionHistory>();
    }

    private void OnEnable()
    {
        if (history != null)
        {
            history.RegisterChanged(RecalculateStacks);
        }

        RecalculateStacks();

        if (attackEvents != null)
        {
            attackEvents.RegisterHit(OnAttackHit);
        }
    }

    private void OnDisable()
    {
        if (history != null)
        {
            history.UnregisterChanged(RecalculateStacks);
        }

        if (attackEvents != null)
        {
            attackEvents.UnregisterHit(OnAttackHit);
        }
    }

    private void RecalculateStacks()
    {
        // 전체 기록에서 다시 계산하여 중복 적용을 막는다.
        normalStacks = 0;
        enhancedStacks = 0;
        frenzyStacks = 0;

        RewardCardCatalog catalog = RewardCardCatalog.Instance;

        if (history == null || catalog == null)
        {
            return;
        }

        foreach (RewardSelectionRecord record in history.Records)
        {
            if (record == null
                || !catalog.TryGetCard(
                    record.CardId, out RewardCardData card)
                || card.HitEffect != HitEffectType.ApplyPoison
                || card.ShardsPerIncrease < 1)
            {
                continue;
            }

            int increaseCount =
                record.InvestedShards / card.ShardsPerIncrease;

            float amount = card.BaseEffectAmount
                + increaseCount * card.AdditionalEffectAmount;

            int stacks = Mathf.Max(0, Mathf.FloorToInt(amount));

            // 카드 데이터의 적용 대상별로 독 부여량을 보관한다.
            if ((card.AttackTargets & AttackTargetType.Normal) != 0)
            {
                normalStacks += stacks;
            }

            if ((card.AttackTargets & AttackTargetType.Enhanced) != 0)
            {
                enhancedStacks += stacks;
            }

            if ((card.AttackTargets & AttackTargetType.Frenzy) != 0)
            {
                frenzyStacks += stacks;
            }
        }
    }

    private void OnAttackHit(
        BossHealth target, AttackTargetType attackType)
    {
        if (target == null || target.IsDefeated
            || Time.timeScale <= 0f)
        {
            return;
        }

        // 적중 시에는 미리 계산한 값만 선택한다.
        int stacks;

        switch (attackType)
        {
            case AttackTargetType.Normal:
                stacks = normalStacks;
                break;

            case AttackTargetType.Enhanced:
                stacks = enhancedStacks;
                break;

            case AttackTargetType.Frenzy:
                stacks = frenzyStacks;
                break;

            default:
                return;
        }

        if (stacks <= 0)
        {
            return;
        }

        if (target.TryGetComponent<BossPoisonStatus>(
            out BossPoisonStatus poison))
        {
            poison.AddStacks(stacks);
        }
    }
}
