using System;
using UnityEngine;

/// <summary>
/// 획득한 카드 ID와 실제 투입한 빛 조각 수를 보관합니다.
/// </summary>
[Serializable]
public sealed class RewardSelectionRecord
{
    // 획득한 카드의 ID
    [SerializeField] private string cardId;

    // 실제 소비한 빛 조각 수
    [SerializeField] private int investedShards;

    /// <summary>획득한 카드 ID를 반환합니다.</summary>
    public string CardId => cardId;

    /// <summary>실제 투입한 빛 조각 수를 반환합니다.</summary>
    public int InvestedShards => investedShards;

    /// <summary>
    /// 획득 기록을 생성합니다. History가 호출합니다.
    /// </summary>
    public RewardSelectionRecord(
        string cardId, int investedShards)
    {
        this.cardId = cardId;
        this.investedShards = investedShards;
    }
}
