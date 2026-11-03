using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement), typeof(PlayerLight))]
[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerHealth : MonoBehaviour
{
    [Header("===피격데미지 / 피격 후 무적 시간===")]
    // ##TEMP 사용자 지정 프로토타입 수치. 인스펙터에서 조정한다.
    [SerializeField] private PlayerStats stats;
    [SerializeField, Min(1)] private int damagePerHit = 1;      // 피격 데미지
    [SerializeField, Min(0f)] private float hitInvulnerabilityDuration = 1f;    // 피격 후 무적

    [Header("===현재상태===")]
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isDead;

    private PlayerMovement playerMovement;
    private PlayerLight playerLight;
    private float invulnerableUntil;

    /// <summary>남은 헤일로 체력 칸 수. 0에서도 추가 피격 전까지 살아 있다.</summary>
    public int CurrentHealth => currentHealth;

    /// <summary>현재 최대 체력.</summary>
    public int MaxHealth => stats.MaxHealth;

    /// <summary>추가 피격으로 사망했는지 반환한다.</summary>
    public bool IsDead => isDead;

    /// <summary>몸체 피격 판정에 사용하는 월드 반경.</summary>
    public float BodyRadius => playerMovement.BodyRadius;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerLight = GetComponent<PlayerLight>();
        stats = GetComponent<PlayerStats>();
        currentHealth = stats.MaxHealth;
        isDead = false;
        invulnerableUntil = float.NegativeInfinity;
        playerLight.SetHealthRatio(1f);
    }

    private void OnEnable()
    {
        if (stats != null)
            RefreshHealthRatio();
    }

    private void RefreshHealthRatio()
    {
        currentHealth = Mathf.Min(currentHealth, stats.MaxHealth);
        playerLight.SetHealthRatio((float)currentHealth / stats.MaxHealth);
    }

    /// <summary>실제 피격을 적용하면 true, 무적·사망·비활성 상태면 false를 반환한다.</summary>
    public bool TryTakeHit()
    {
        return TryTakeHit(damagePerHit);
    }

    /// <summary>지정한 피해를 적용하며 기존 무적·사망 규칙을 유지한다.</summary>
    public bool TryTakeHit(int damage)
    {
        if (damage <= 0 || !isActiveAndEnabled || isDead || Time.time < invulnerableUntil)
            return false;

        if (currentHealth == 0)
        {
            isDead = true;
            playerMovement.enabled = false;
            return true;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        invulnerableUntil = Time.time + hitInvulnerabilityDuration;
        RefreshHealthRatio();
        return true;
    }

    /// <summary>남은 무적을 단축하지 않고 지정 시간만큼 무적을 부여한다.</summary>
    public void GrantInvulnerability(float duration)
    {
        if (!isDead)
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + Mathf.Max(0f, duration));
    }

    /// <summary>살아 있는 플레이어의 체력을 최대치까지 회복한다.</summary>
    public bool TryHeal(int amount)
    {
        if (!isActiveAndEnabled || isDead || amount <= 0)
            return false;
        int healedHealth = Mathf.Min(stats.MaxHealth, currentHealth + amount);
        if (healedHealth == currentHealth)
            return false;
        currentHealth = healedHealth;
        RefreshHealthRatio();
        return true;
    }

    private void OnValidate()
    {
        damagePerHit = Mathf.Max(1, damagePerHit);
        hitInvulnerabilityDuration = Mathf.Max(0f, hitInvulnerabilityDuration);
    }
}
