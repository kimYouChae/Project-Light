using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이번 플레이에서 확정한 카드 획득 기록을 보관합니다.
/// </summary>
public sealed class RewardSelectionHistory : MonoBehaviour
{
    // 획득 순서대로 보관할 기록
    [SerializeField] private List<RewardSelectionRecord> records =
        new List<RewardSelectionRecord>();

    /// <summary>획득 기록을 읽기 전용으로 제공합니다.</summary>
    public IReadOnlyList<RewardSelectionRecord> Records => records;

    /// <summary>
    /// 확정된 획득 기록을 추가합니다.
    /// RewardSelectionController가 호출합니다.
    /// </summary>
    public void AddRecord(RewardCardData card, int investedShards)
    {
        records.Add(new RewardSelectionRecord(card, investedShards));
    }
}
