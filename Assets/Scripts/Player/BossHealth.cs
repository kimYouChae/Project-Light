using UnityEngine;

/// <summary>임시 보스의 체력과 원형 피격 반경을 관리한다.</summary>
public sealed class BossHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100; // ##TEMP 임시 보스 체력
    [SerializeField, Min(0.01f)] private float hitRadius = 0.6f; // ##TEMP 임시 피격 반경
    [SerializeField] private int currentHealth;

    /// <summary>남은 체력.</summary>
    public int CurrentHealth => currentHealth;
    /// <summary>보스의 최대 체력.</summary>
    public int MaxHealth => maxHealth;
    /// <summary>월드 단위 피격 반경.</summary>
    public float HitRadius => hitRadius;
    /// <summary>체력이 소진되었는지 여부.</summary>
    public bool IsDefeated => currentHealth <= 0;

    /// <summary>체력이 소진됐을 때 한 번 발생한다.</summary>
    public event System.Action Defeated;

    /// <summary>플레이어 탄이 직접 적중했을 때 발생한다.</summary>
    public event System.Action<Vector3> PlayerAttackHit;

    private void Awake() => currentHealth = maxHealth;

    /// <summary>직접 공격 피해를 적용하고 적중 위치를 알린다.</summary>
    public bool TryTakePlayerAttackDamage(int damage)
    {
        Vector3 hitPosition = transform.position;
        if (!TryTakeDamage(damage))
            return false;
        PlayerAttackHit?.Invoke(hitPosition);
        return true;
    }

    /// <summary>살아 있는 보스에게 양수 피해를 적용한다.</summary>
    public bool TryTakeDamage(int damage)
    {
        if (IsDefeated || damage <= 0)
            return false;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (IsDefeated)
            Defeated?.Invoke();
        return true;
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        hitRadius = Mathf.Max(0.01f, hitRadius);
    }
}
