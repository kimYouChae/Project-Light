using UnityEngine;

/// <summary>
/// 카드 한 장의 기본 정보, 속성, 적용 대상을 보관합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "RewardCard",
    menuName = "Project Light/Rewards/Reward Card")]
public sealed class RewardCardData : ScriptableObject
{
    // 카드를 구분하는 ID
    [SerializeField] private string id;

    // 화면에 표시할 카드 이름
    [SerializeField] private string displayName;

    // 화면에 표시할 카드 설명
    [SerializeField, TextArea] private string description;

    // 카드의 속성
    [SerializeField] private CardTagType tag;

    // 효과를 적용할 공격 구분. 여러 항목을 함께 선택할 수 있다.
    [SerializeField] private AttackTargetType attackTargets;

    // 카드 효과의 기본 수치
    [SerializeField] private float baseEffectAmount;

    // 빛 조각 투입으로 한 번 증가하는 효과 수치
    [SerializeField] private float additionalEffectAmount;

    // 추가 효과 한 번에 필요한 빛 조각 개수
    [SerializeField, Min(1)] private int shardsPerIncrease = 1;

    // 계산된 효과 수치를 화면에 표시할 서식
    [SerializeField] private string effectDisplayFormat;

    /// <summary>
    /// 카드의 ID를 반환합니다.
    /// </summary>
    public string Id => id;

    /// <summary>
    /// 카드의 표시 이름을 반환합니다.
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// 카드의 설명을 반환합니다.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// 카드의 속성을 반환합니다.
    /// </summary>
    public CardTagType Tag => tag;

    /// <summary>
    /// 카드 효과의 적용 대상들을 반환합니다.
    /// </summary>
    public AttackTargetType AttackTargets => attackTargets;

    /// <summary>
    /// 카드 효과의 기본 수치를 반환합니다.
    /// </summary>
    public float BaseEffectAmount => baseEffectAmount;

    /// <summary>
    /// 빛 조각 투입에 따른 추가 효과 수치를 반환합니다.
    /// </summary>
    public float AdditionalEffectAmount => additionalEffectAmount;

    /// <summary>
    /// 추가 효과 한 번에 필요한 빛 조각 개수를 반환합니다.
    /// </summary>
    public int ShardsPerIncrease => shardsPerIncrease;

    /// <summary>
    /// 효과 수치의 표시 서식을 반환합니다.
    /// </summary>
    public string EffectDisplayFormat => effectDisplayFormat;
}
