using System;
using UnityEngine;

/// <summary>한 번의 카드 선택과 빛 조각 소비 기록.</summary>
[Serializable]
public sealed class RewardSelectionRecord
{
    [SerializeField] private string cardId;
    [SerializeField] private int investedShards;

    /// <summary>선택한 카드 식별자.</summary>
    public string CardId => cardId;
    /// <summary>확정 시 소비한 빛 조각 수.</summary>
    public int InvestedShards => investedShards;

    /// <summary>선택 확정 결과를 보관한다.</summary>
    public RewardSelectionRecord(string cardId, int investedShards)
    {
        this.cardId = cardId;
        this.investedShards = investedShards;
    }
}
