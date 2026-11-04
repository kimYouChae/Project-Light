using UnityEngine;

/// <summary>플레이어 기본 스탯을 한곳에서 관리한다.</summary>
[DefaultExecutionOrder(-300)]
[DisallowMultipleComponent]
public sealed class PlayerStats : MonoBehaviour
{
    [Header("기본 스탯")]
    // ##TEMP 기존 프로토타입 기본값. 씬의 실제 설정값을 이관해 사용한다.
    [SerializeField, Min(0f)] private float baseMoveSpeed = 5f;
    [SerializeField, Min(0.01f)] private float baseBulletSpeed = 6f;
    [SerializeField, Min(0.01f)] private float baseBulletRadius = 0.06f;
    [SerializeField, Min(1)] private int baseDamage = 1;
    [SerializeField, Min(0f)] private float baseHaloRadius = 3.9f;
    [SerializeField, Min(1)] private int baseMaxHealth = 10;
    [SerializeField, Min(0f)] private float baseGaugePerBullet = 10f;
    [SerializeField, Min(0.01f)] private float baseFrenzyDuration = 6f;
    [SerializeField, Min(0f)] private float basePerfectInvulnerability = 0.25f;

    [Header("이동속도 보상")]
    // 이번 플레이의 획득 기록
    [SerializeField] private RewardSelectionHistory history;

    // 획득 기록에서 계산한 이동속도 증가율 합계
    private float moveSpeedBonus;

    // 획득 기록에서 계산한 투사체 크기 증가율 합계
    private float bulletSizeBonus;

    // 획득 기록에서 계산한 투사체 속도 증가율 합계
    private float bulletSpeedBonus;

    // 획득 기록에서 계산한 헤일로 증가율 합계
    private float haloBonus;

    // 획득 기록에서 계산한 게이지 충전량 증가율 합계
    private float gaugeBonus;

    // 획득 기록에서 계산한 폭주 추가 시간 합계
    private float frenzyDurationBonus;

    // 획득 기록에서 계산한 퍼펙트 회피 추가 무적 시간 합계
    private float perfectInvulnerabilityBonus;

    // 헤일로 스탯이 다시 계산됐을 때 전달할 알림
    private System.Action haloChanged;

    /// <summary>헤일로 스탯 변경 알림을 등록합니다.</summary>
    public void RegisterHaloChanged(System.Action callback)
    {
        haloChanged += callback;
    }

    /// <summary>헤일로 스탯 변경 알림을 해제합니다.</summary>
    public void UnregisterHaloChanged(System.Action callback)
    {
        haloChanged -= callback;
    }

    /// <summary>카드 보너스를 반영한 최종 이동속도.</summary>
    public float MoveSpeed => baseMoveSpeed * (1f + moveSpeedBonus);
    /// <summary>카드 보너스를 반영한 최종 투사체 속도.</summary>
    public float BulletSpeed => baseBulletSpeed * (1f + bulletSpeedBonus);
    /// <summary>카드 보너스를 반영한 최종 일반 투사체 반경.</summary>
    public float BulletRadius => baseBulletRadius * BulletSizeMultiplier;
    /// <summary>투사체 크기와 빛 반경에 적용할 카드 배율.</summary>
    public float BulletSizeMultiplier => 1f + bulletSizeBonus;
    /// <summary>일반 공격 피해량.</summary>
    public int Damage => baseDamage;
    /// <summary>카드 보너스를 반영한 최대 헤일로 반경.</summary>
    public float MaxHaloRadius => baseHaloRadius * (1f + haloBonus);
    /// <summary>카드 보너스를 반영하고 소수점을 올림한 최대 체력.</summary>
    public int MaxHealth =>
        Mathf.CeilToInt(baseMaxHealth * (1f + haloBonus));
    /// <summary>카드 보너스를 반영한 PERFECT 탄 한 발당 게이지 충전량.</summary>
    public float GaugePerBullet => baseGaugePerBullet * (1f + gaugeBonus);
    /// <summary>카드 보너스를 반영한 다음 폭주의 지속 시간.</summary>
    public float FrenzyDuration => baseFrenzyDuration + frenzyDurationBonus;
    /// <summary>카드 보너스를 반영한 퍼펙트 회피 무적 시간.</summary>
    public float PerfectInvulnerability =>
        basePerfectInvulnerability + perfectInvulnerabilityBonus;

    private void OnEnable()
    {
        if (history != null)
        {
            history.RegisterChanged(RecalculateStats);
        }

        RecalculateStats();
    }

    private void OnDisable()
    {
        if (history != null)
        {
            history.UnregisterChanged(RecalculateStats);
        }
    }

    private void RecalculateStats()
    {
        // 전체 기록에서 다시 계산하기 전에 보너스를 초기화한다.
        moveSpeedBonus = 0f;
        bulletSizeBonus = 0f;
        bulletSpeedBonus = 0f;
        haloBonus = 0f;
        gaugeBonus = 0f;
        frenzyDurationBonus = 0f;
        perfectInvulnerabilityBonus = 0f;

        RewardCardCatalog catalog = RewardCardCatalog.Instance;

        if (history != null && catalog != null)
        {
            foreach (RewardSelectionRecord record in history.Records)
            {
                if (record == null
                    || !catalog.TryGetCard(
                        record.CardId, out RewardCardData card)
                    || card.ShardsPerIncrease < 1)
                {
                    continue;
                }

                int increaseCount =
                    record.InvestedShards / card.ShardsPerIncrease;

                float amount = card.BaseEffectAmount
                    + increaseCount * card.AdditionalEffectAmount;

                // 카드 이름이나 ID가 아닌 효과 종류로 처리한다.
                switch (card.StatEffect)
                {
                    case StatEffectType.MoveSpeed:
                        moveSpeedBonus += amount;
                        break;

                    case StatEffectType.BulletSize:
                        bulletSizeBonus += amount;
                        break;

                    case StatEffectType.BulletSpeed:
                        bulletSpeedBonus += amount;
                        break;

                    case StatEffectType.Halo:
                        haloBonus += amount;
                        break;

                    case StatEffectType.Gauge:
                        gaugeBonus += amount;
                        break;

                    case StatEffectType.FrenzyDuration:
                        frenzyDurationBonus += amount;
                        break;

                    case StatEffectType.PerfectInvulnerability:
                        perfectInvulnerabilityBonus += amount;
                        break;
                }
            }
        }

        // 모든 계산이 끝난 뒤 현재 체력 비율을 갱신한다.
        haloChanged?.Invoke();
    }

    private void OnValidate()
    {
        baseMoveSpeed = Mathf.Max(0f, baseMoveSpeed);
        baseBulletSpeed = Mathf.Max(0.01f, baseBulletSpeed);
        baseBulletRadius = Mathf.Max(0.01f, baseBulletRadius);
        baseDamage = Mathf.Max(1, baseDamage);
        baseHaloRadius = Mathf.Max(0f, baseHaloRadius);
        baseMaxHealth = Mathf.Max(1, baseMaxHealth);
        baseGaugePerBullet = Mathf.Max(0f, baseGaugePerBullet);
        baseFrenzyDuration = Mathf.Max(0.01f, baseFrenzyDuration);
        basePerfectInvulnerability = Mathf.Max(0f, basePerfectInvulnerability);
    }
}
