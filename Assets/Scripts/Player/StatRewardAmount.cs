using System;
using UnityEngine;

/// <summary>스탯 카드 기본 증가량과 빛 조각당 증가량.</summary>
[Serializable]
public sealed class StatRewardAmount
{
    [SerializeField, Min(0f)] private float baseAmount;
    [SerializeField, Min(0f)] private float perShard;

    /// <summary>카드의 기본 증가량과 조각당 증가량을 지정한다.</summary>
    public StatRewardAmount(float baseAmount, float perShard)
    {
        this.baseAmount = baseAmount;
        this.perShard = perShard;
    }

    /// <summary>투입한 빛 조각을 포함한 증가량을 계산한다.</summary>
    public float Calculate(int shards)
    {
        return baseAmount + perShard * Mathf.Max(0, shards);
    }
}
