using System;
using UnityEngine;

/// <summary>
/// 획득한 카드 한 장과 실제 투입한 빛 조각 수를 보관합니다.
/// </summary>
[Serializable]
public sealed class RewardSelectionRecord
{
    // 획득한 카드
    [SerializeField] private RewardCardData card;

    // 실제 소비한 빛 조각 수
    [SerializeField] private int investedShards;

    /// <summary>획득한 카드를 반환합니다.</summary>
    public RewardCardData Card => card;

    /// <summary>실제 투입한 빛 조각 수를 반환합니다.</summary>
    public int InvestedShards => investedShards;

    /// <summary>
    /// 획득 기록을 생성합니다. RewardSelectionHistory가 호출합니다.
    /// </summary>
    public RewardSelectionRecord(
        RewardCardData card, int investedShards)
    {
        this.card = card;
        this.investedShards = investedShards;
    }
}
