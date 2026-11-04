using UnityEngine;

/// <summary>
/// 보스들이 공유할 독의 피해·시간·중첩 규칙을 보관합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "PoisonSettings",
    menuName = "Project Light/Combat/Poison Settings")]
public sealed class PoisonSettings : ScriptableObject
{
    // ##TEMP 승인한 독 프로토타입 시작값. 인스펙터에서 조정한다.
    [SerializeField, Min(1)] private int damagePerStack = 1;
    [SerializeField, Min(0.01f)] private float damageInterval = 1f;
    [SerializeField, Min(0.01f)] private float duration = 3f;
    [SerializeField, Min(1)] private int maxStacks = 10;

    /// <summary>한 주기마다 중첩당 적용할 피해를 반환합니다.</summary>
    public int DamagePerStack => damagePerStack;

    /// <summary>독 피해 주기를 초 단위로 반환합니다.</summary>
    public float DamageInterval => damageInterval;

    /// <summary>마지막 독 부여부터의 지속 시간을 반환합니다.</summary>
    public float Duration => duration;

    /// <summary>독 최대 중첩 수를 반환합니다.</summary>
    public int MaxStacks => maxStacks;

    private void OnValidate()
    {
        damagePerStack = Mathf.Max(1, damagePerStack);
        damageInterval = Mathf.Max(0.01f, damageInterval);
        duration = Mathf.Max(0.01f, duration);
        maxStacks = Mathf.Max(1, maxStacks);
    }
}
