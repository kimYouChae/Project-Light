using System;
using UnityEngine;

/// <summary>기본 스탯과 선택 기록에서 합산한 증가분을 한곳에서 관리한다.</summary>
[DefaultExecutionOrder(-300)]
[DisallowMultipleComponent]
public sealed class PlayerStats : MonoBehaviour
{
    [SerializeField] private RewardSelectionHistory selectionHistory;

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

    [Header("카드 증가량 — 비율은 0.1 = 10%, 시간은 초")]
    // ##TEMP 기획서의 스탯 카드 시작값.
    [SerializeField] private StatRewardAmount swiftFeet = new StatRewardAmount(0.10f, 0.02f);
    [SerializeField] private StatRewardAmount largeLantern = new StatRewardAmount(0.15f, 0.03f);
    [SerializeField] private StatRewardAmount fastLight = new StatRewardAmount(0.15f, 0.03f);
    [SerializeField] private StatRewardAmount wideHalo = new StatRewardAmount(0.10f, 0.02f);
    [SerializeField] private StatRewardAmount pulse = new StatRewardAmount(0.15f, 0.03f);
    [SerializeField] private StatRewardAmount longBreath = new StatRewardAmount(0.5f, 0.1f);
    [SerializeField] private StatRewardAmount instant = new StatRewardAmount(0.05f, 0.01f);

    [Header("카드 누적 증가분 — 선택 기록에서 자동 계산")]
    [SerializeField] private float moveSpeedBonus;
    [SerializeField] private float bulletSpeedBonus;
    [SerializeField] private float bulletSizeBonus;
    [SerializeField] private float haloBonus;
    [SerializeField] private float gaugeBonus;
    [SerializeField] private float frenzyDurationBonus;
    [SerializeField] private float perfectInvulnerabilityBonus;

    /// <summary>최종 이동 속도.</summary>
    public float MoveSpeed => baseMoveSpeed * (1f + moveSpeedBonus);
    /// <summary>최종 투사체 속도.</summary>
    public float BulletSpeed => baseBulletSpeed * (1f + bulletSpeedBonus);
    /// <summary>최종 일반 투사체 반경.</summary>
    public float BulletRadius => baseBulletRadius * BulletSizeMultiplier;
    /// <summary>카드에 의한 투사체 크기와 빛 반경 배율.</summary>
    public float BulletSizeMultiplier => 1f + bulletSizeBonus;
    /// <summary>일반 공격 피해량.</summary>
    public int Damage => baseDamage;
    /// <summary>최대 헤일로 반경. 현재 체력 비율은 표시 컴포넌트에서 적용한다.</summary>
    public float MaxHaloRadius => baseHaloRadius * (1f + haloBonus);
    /// <summary>올림한 최대 체력.</summary>
    public int MaxHealth => Mathf.CeilToInt(baseMaxHealth * (1f + haloBonus));
    /// <summary>PERFECT 탄 한 발당 게이지 충전량.</summary>
    public float GaugePerBullet => baseGaugePerBullet * (1f + gaugeBonus);
    /// <summary>다음 폭주를 시작할 때 사용할 지속 시간.</summary>
    public float FrenzyDuration => baseFrenzyDuration + frenzyDurationBonus;
    /// <summary>PERFECT 성공 시 부여할 무적 시간.</summary>
    public float PerfectInvulnerability => basePerfectInvulnerability + perfectInvulnerabilityBonus;

    /// <summary>최종 스탯 변경 알림. 구독자는 비활성화 시 해제한다.</summary>
    public event Action Changed;

    private void Awake() => RebuildFromHistory();

    private void OnEnable()
    {
        if (selectionHistory != null)
            selectionHistory.Changed += RebuildFromHistory;
        RebuildFromHistory();
    }

    private void OnDisable()
    {
        if (selectionHistory != null)
            selectionHistory.Changed -= RebuildFromHistory;
    }

    private void RebuildFromHistory()
    {
        moveSpeedBonus = bulletSpeedBonus = bulletSizeBonus = 0f;
        haloBonus = gaugeBonus = 0f;
        frenzyDurationBonus = perfectInvulnerabilityBonus = 0f;
        if (selectionHistory != null)
            foreach (RewardSelectionRecord record in selectionHistory.Records)
            {
                int shards = record.InvestedShards;
                switch (record.CardId)
                {
                    case "swift_feet": moveSpeedBonus += swiftFeet.Calculate(shards); break;
                    case "large_lantern": bulletSizeBonus += largeLantern.Calculate(shards); break;
                    case "fast_light": bulletSpeedBonus += fastLight.Calculate(shards); break;
                    case "wide_halo": haloBonus += wideHalo.Calculate(shards); break;
                    case "pulse": gaugeBonus += pulse.Calculate(shards); break;
                    case "long_breath": frenzyDurationBonus += longBreath.Calculate(shards); break;
                    case "instant": perfectInvulnerabilityBonus += instant.Calculate(shards); break;
                }
            }
        if (Application.isPlaying)
            Changed?.Invoke();
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
        RebuildFromHistory();
    }
}
