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

    /// <summary>최종 이동 속도.</summary>
    public float MoveSpeed => baseMoveSpeed;
    /// <summary>최종 투사체 속도.</summary>
    public float BulletSpeed => baseBulletSpeed;
    /// <summary>최종 일반 투사체 반경.</summary>
    public float BulletRadius => baseBulletRadius;
    /// <summary>투사체 크기와 빛 반경의 기본 배율.</summary>
    public float BulletSizeMultiplier => 1f;
    /// <summary>일반 공격 피해량.</summary>
    public int Damage => baseDamage;
    /// <summary>최대 헤일로 반경. 현재 체력 비율은 표시 컴포넌트에서 적용한다.</summary>
    public float MaxHaloRadius => baseHaloRadius;
    /// <summary>최대 체력.</summary>
    public int MaxHealth => baseMaxHealth;
    /// <summary>PERFECT 탄 한 발당 게이지 충전량.</summary>
    public float GaugePerBullet => baseGaugePerBullet;
    /// <summary>다음 폭주를 시작할 때 사용할 지속 시간.</summary>
    public float FrenzyDuration => baseFrenzyDuration;
    /// <summary>PERFECT 성공 시 부여할 무적 시간.</summary>
    public float PerfectInvulnerability => basePerfectInvulnerability;

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
